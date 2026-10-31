use std::fs::{self, File};
use std::io::Read;
use std::path::Path;
use std::sync::{
    atomic::{AtomicBool, AtomicU64, Ordering},
    Arc, Mutex, OnceLock,
};
use std::time::Duration;
use std::time::{SystemTime, UNIX_EPOCH};

use bytes::Bytes;
use librespot_audio::{AudioDecrypt, AudioFile};
use librespot_connect::{ConnectConfig, Spirc};
use librespot_core::{
    authentication::Credentials, cache::Cache, cdn_url::CdnUrl, config::SessionConfig,
    session::Session, spotify_id::FileId, SpotifyId, SpotifyUri,
};
use librespot_discovery::{DeviceType, Discovery};
use librespot_metadata::audio::{AudioFileFormat, AudioFiles, AudioItem};
use librespot_metadata::{Album, Artist, Metadata, Track};
use librespot_playback::{
    audio_backend::{Sink, SinkResult},
    config::{AudioFormat, Bitrate, PlayerConfig},
    convert::Converter,
    decoder::AudioPacket,
    mixer::{softmixer::SoftMixer, Mixer, MixerConfig, NoOpVolume, VolumeGetter},
    player::{Player, PlayerEvent},
};
use log::{LevelFilter, Log, Metadata as LogMetadata, Record};
use napi::bindgen_prelude::{AsyncTask, Error, Result};
use napi::threadsafe_function::{
    ErrorStrategy, ThreadSafeCallContext, ThreadsafeFunction, ThreadsafeFunctionCallMode,
};
use napi::{Env, JsFunction, Task};
use napi_derive::napi;
use serde_json;
use std::thread::sleep;
use std::time::Instant;
use tokio::sync::mpsc;
use tokio::time::timeout;
use tokio_stream::StreamExt;

static RUNTIME: OnceLock<tokio::runtime::Runtime> = OnceLock::new();
static LOGGER_INIT: OnceLock<()> = OnceLock::new();
static SESSION_COUNTER: AtomicU64 = AtomicU64::new(1);
static COLLECTION_PARSE_LOG_NEXT_AT_MS: AtomicU64 = AtomicU64::new(0);

fn next_session_id(prefix: &str) -> String {
    let next = SESSION_COUNTER.fetch_add(1, Ordering::Relaxed);
    format!("{prefix}-{next}")
}

fn unix_epoch_ms() -> u64 {
    SystemTime::now()
        .duration_since(UNIX_EPOCH)
        .unwrap_or_else(|_| Duration::from_secs(0))
        .as_millis() as u64
}

fn normalize_collection_parse_warning(event: &mut LogEvent) -> bool {
    let scope = event.scope.as_deref().unwrap_or("").to_lowercase();
    if !scope.contains("librespot_core::dealer") {
        return false;
    }
    let message = event.message.to_lowercase();
    let looks_like_collection_parse_noise = message
        .contains("failure during data parsing for hm://collection/collection/")
        && message.contains("base64 decoding failed");
    if !looks_like_collection_parse_noise {
        return false;
    }

    let now = unix_epoch_ms();
    let next_at = COLLECTION_PARSE_LOG_NEXT_AT_MS.load(Ordering::Relaxed);
    if now < next_at {
        return true;
    }
    COLLECTION_PARSE_LOG_NEXT_AT_MS.store(now + 60_000, Ordering::Relaxed);
    event.level = "debug".to_string();
    event.message =
        "suppressed recurring dealer parse warning for hm://collection payload".to_string();
    false
}

fn runtime() -> &'static tokio::runtime::Runtime {
    RUNTIME.get_or_init(|| {
        tokio::runtime::Runtime::new().expect("failed to create tokio runtime for librespot addon")
    })
}

fn stream_data_rate(format: AudioFileFormat) -> Option<usize> {
    let kbps = match format {
        AudioFileFormat::OGG_VORBIS_96 => 12.,
        AudioFileFormat::OGG_VORBIS_160 => 20.,
        AudioFileFormat::OGG_VORBIS_320 => 40.,
        AudioFileFormat::MP3_256 => 32.,
        AudioFileFormat::MP3_320 => 40.,
        AudioFileFormat::MP3_160 => 20.,
        AudioFileFormat::MP3_96 => 12.,
        AudioFileFormat::MP3_160_ENC => 20.,
        AudioFileFormat::AAC_24 => 3.,
        AudioFileFormat::AAC_48 => 6.,
        AudioFileFormat::AAC_160 => 20.,
        AudioFileFormat::AAC_320 => 40.,
        AudioFileFormat::MP4_128 => 16.,
        AudioFileFormat::OTHER5 => 40.,
        AudioFileFormat::FLAC_FLAC => 112., // assume ~900 kbit/s
        AudioFileFormat::XHE_AAC_12 => 1.5,
        AudioFileFormat::XHE_AAC_16 => 2.,
        AudioFileFormat::XHE_AAC_24 => 3.,
        AudioFileFormat::FLAC_FLAC_24BIT => 3.,
    };
    let data_rate: f32 = kbps * 1024.;
    Some(data_rate.ceil() as usize)
}

/// Options used to create a librespot session.
#[napi(object)]
pub struct CreateSessionOpts {
    pub access_token: Option<String>,
    pub client_id: Option<String>,
    pub device_name: Option<String>,
    /// Directory for the librespot audio file cache.  When set, decoded audio
    /// is written here so subsequent plays avoid a CDN round-trip.
    pub cache_dir: Option<String>,
    /// Maximum size of the audio cache in megabytes.  Only used when
    /// `cache_dir` is set.  None means no size limit is enforced.
    pub cache_size_limit_mb: Option<u32>,
}

/// Options for streaming a track.
#[napi(object)]
pub struct StreamTrackOpts {
    pub uri: String,
    pub start_position_ms: Option<u32>,
    pub bitrate: Option<u32>,
    pub output: Option<String>,
    pub emit_events: Option<bool>,
}

#[napi(object)]
pub struct DownloadTrackOpts {
    pub uri: String,
    pub bitrate: Option<u32>,
}

/// Result of resolving a track's CDN location + decryption key, without
/// downloading or decrypting any audio. Lets the caller fetch the signed
/// (expiring) CDN URL directly with HTTP Range and AES-128-CTR decrypt itself.
#[napi(object)]
pub struct ResolveAudioFileResult {
    /// Signed, expiring CDN URL for the encrypted audio file (GET with Range).
    pub cdn_url: String,
    /// 16-byte AES-128 audio key, hex-encoded (lowercase, 32 chars).
    pub key_hex: String,
    /// Chosen Spotify audio format, e.g. "OGG_VORBIS_320" or "MP3_320".
    pub format: String,
}

/// Follow Spotify track relinking. A track that is unavailable in the user's
/// market carries empty `files` plus a list of `alternatives` (the playable
/// substitute Spotify's own client transparently swaps in). Without this, such a
/// track resolves to "no audio files available" even though it plays fine in the
/// official app. Mirrors librespot's player `find_available_alternative`: keep the
/// original if it has files, otherwise resolve each alternative and take the first
/// one available to this user. The returned item's `track_id` is the id to use for
/// the audio-key request (the alternative's id when relinked).
async fn resolve_playable_audio_item(
    session: &Session,
    audio_item: AudioItem,
) -> Result<AudioItem> {
    if audio_item.availability.is_err() {
        return Err(Error::from_reason("track is unavailable"));
    }
    if !audio_item.files.is_empty() {
        return Ok(audio_item);
    }
    if let Some(alternatives) = audio_item.alternatives {
        for alt_uri in alternatives.0 {
            if let Ok(alt) = AudioItem::get_file(session, alt_uri).await {
                if alt.availability.is_ok() && !alt.files.is_empty() {
                    log::debug!("track relinked; using available alternative");
                    return Ok(alt);
                }
            }
        }
    }
    Err(Error::from_reason("no audio files available"))
}

/// The `SpotifyId` for the audio-key request, taken from a resolved `AudioItem`
/// (the alternative's id when the track was relinked).
fn audio_item_track_id(item: &AudioItem) -> Result<SpotifyId> {
    (&item.track_id)
        .try_into()
        .map_err(|e| Error::from_reason(format!("invalid spotify id: {e:?}")))
}

/// Shared resolve body: load the audio item, pick a format, resolve the CDN URL
/// and request the audio key. Runs on `runtime()`; used by both the sync
/// `resolve_audio_file` and the async (off-main-thread) `resolve_audio_file_async`.
async fn do_resolve_audio_file(
    session: Session,
    spotify_uri: SpotifyUri,
    bitrate_pref: Option<u32>,
) -> Result<ResolveAudioFileResult> {
    let audio_item = AudioItem::get_file(&session, spotify_uri)
        .await
        .map_err(|e| Error::from_reason(format!("failed to load audio item: {e:?}")))?;
    // Follow relinking before picking a format; `track_id` is the playable item's
    // id (the alternative's when relinked) so the audio key matches.
    let audio_item = resolve_playable_audio_item(&session, audio_item).await?;
    let track_id = audio_item_track_id(&audio_item)?;

    let select_format =
        |files: &AudioFiles, bitrate: Option<u32>| -> Option<(AudioFileFormat, FileId)> {
            let prefer = match bitrate {
                Some(96) => {
                    vec![AudioFileFormat::OGG_VORBIS_96, AudioFileFormat::MP3_96]
                }
                Some(160) => {
                    vec![AudioFileFormat::OGG_VORBIS_160, AudioFileFormat::MP3_160]
                }
                _ => vec![
                    AudioFileFormat::OGG_VORBIS_320,
                    AudioFileFormat::MP3_320,
                    AudioFileFormat::MP3_256,
                ],
            };
            for f in prefer {
                if let Some(id) = files.get(&f) {
                    return Some((f, *id));
                }
            }
            files.iter().next().map(|(f, id)| (*f, *id))
        };

    let (format, file_id) = select_format(&audio_item.files, bitrate_pref)
        .ok_or_else(|| Error::from_reason("no audio files available"))?;

    let cdn = CdnUrl::new(file_id)
        .resolve_audio(&session)
        .await
        .map_err(|e| Error::from_reason(format!("failed to resolve cdn url: {e:?}")))?;
    let cdn_url = cdn
        .try_get_urls()
        .map_err(|e| Error::from_reason(format!("no cdn url available: {e:?}")))?
        .into_iter()
        .next()
        .ok_or_else(|| Error::from_reason("no cdn url available"))?
        .to_owned();

    let key = session
        .audio_key()
        .request(track_id, file_id)
        .await
        .map_err(|e| Error::from_reason(format!("audio key unavailable: {e:?}")))?;
    let key_hex = key.0.iter().map(|b| format!("{:02x}", b)).collect::<String>();

    Ok(ResolveAudioFileResult {
        cdn_url,
        key_hex,
        format: format!("{:?}", format),
    })
}

/// Parse + validate a Spotify URI into the ids needed to resolve audio. Cheap
/// and synchronous; safe to run on the main thread before handing off to a
/// worker.
fn parse_resolve_target(uri: &str) -> Result<(SpotifyUri, SpotifyId)> {
    if uri.is_empty() {
        return Err(Error::from_reason("uri is required"));
    }
    let spotify_uri = SpotifyUri::from_uri(uri)
        .map_err(|e| Error::from_reason(format!("invalid spotify uri: {:?}", e)))?;
    let track_id: SpotifyId = (&spotify_uri)
        .try_into()
        .map_err(|e| Error::from_reason(format!("invalid spotify id: {e:?}")))?;
    Ok((spotify_uri, track_id))
}

/// libuv-threadpool task for `resolve_audio_file_async`: runs the blocking
/// `runtime().block_on(do_resolve_audio_file(..))` on a worker thread so the
/// Node event loop (and thus every other zone's audio pacing) is never stalled
/// by the synchronous CDN/key lookup.
pub struct ResolveAudioFileTask {
    session: Session,
    spotify_uri: SpotifyUri,
    bitrate_pref: Option<u32>,
}

impl Task for ResolveAudioFileTask {
    type Output = ResolveAudioFileResult;
    type JsValue = ResolveAudioFileResult;

    fn compute(&mut self) -> Result<Self::Output> {
        runtime().block_on(do_resolve_audio_file(
            self.session.clone(),
            self.spotify_uri.clone(),
            self.bitrate_pref,
        ))
    }

    fn resolve(&mut self, _env: Env, output: Self::Output) -> Result<Self::JsValue> {
        Ok(output)
    }
}

/// Session tokens for first-party API access (pathfinder etc.): the login5
/// bearer + the client-token, both refreshed/cached internally by librespot.
/// The app performs the actual content HTTP itself with these (content browsing
/// lives in TS — librespot only mints the tokens and streams audio).
#[napi(object)]
pub struct SessionTokens {
    pub access_token: String,
    pub token_type: String,
    pub client_token: String,
    /// Access-token lifetime in milliseconds (for client-side caching).
    pub expires_in_ms: u32,
}

/// libuv-threadpool task for `get_tokens`: mints the login5 bearer + client-token.
pub struct GetTokensTask {
    session: Session,
}

impl Task for GetTokensTask {
    type Output = SessionTokens;
    type JsValue = SessionTokens;

    fn compute(&mut self) -> Result<Self::Output> {
        let session = self.session.clone();
        runtime().block_on(async move {
            let token = session
                .login5()
                .auth_token()
                .await
                .map_err(|e| Error::from_reason(format!("auth token failed: {e:?}")))?;
            let client_token = session
                .spclient()
                .client_token()
                .await
                .map_err(|e| Error::from_reason(format!("client token failed: {e:?}")))?;
            Ok(SessionTokens {
                access_token: token.access_token,
                token_type: token.token_type,
                client_token,
                expires_in_ms: token.expires_in.as_millis().min(u128::from(u32::MAX)) as u32,
            })
        })
    }

    fn resolve(&mut self, _env: Env, output: Self::Output) -> Result<Self::JsValue> {
        Ok(output)
    }
}

/// Result of a credentials login flow.
#[napi(object)]
pub struct CredentialsResult {
    pub username: String,
    pub credentials_json: String,
}

/// Event payload emitted by the connect host.
#[napi(object)]
pub struct ConnectEvent {
    pub r#type: String,
    pub device_id: Option<String>,
    pub session_id: Option<String>,
    pub track_id: Option<String>,
    pub uri: Option<String>,
    pub title: Option<String>,
    pub artist: Option<String>,
    pub album: Option<String>,
    pub duration_ms: Option<u32>,
    pub position_ms: Option<u32>,
    pub volume: Option<u16>,
    pub error_code: Option<String>,
    pub error_message: Option<String>,
    pub metric_name: Option<String>,
    pub metric_value_ms: Option<u32>,
    pub metric_message: Option<String>,
    /// Credentials JSON blob delivered via mDNS discovery when a new Spotify user connects.
    pub credentials_json: Option<String>,
}

/// Log payload emitted by the native module.
#[napi(object)]
#[derive(Clone)]
pub struct LogEvent {
    pub level: String,
    pub message: String,
    pub scope: Option<String>,
    pub device_id: Option<String>,
    pub session_id: Option<String>,
}

/// Handle returned when hosting a connect device (placeholder).
#[napi]
pub struct ConnectHandle {
    spirc: Spirc,
    stop_tx: Option<mpsc::Sender<()>>,
    #[allow(dead_code)]
    tsfn: ThreadsafeFunction<Bytes, ErrorStrategy::Fatal>,
    sample_rate: u32,
    channels: u16,
    stop_flag: Arc<AtomicBool>,
    task: tokio::task::JoinHandle<()>,
}

#[napi]
impl ConnectHandle {
    #[napi]
    pub fn stop(&mut self) {
        self.stop_flag.store(true, Ordering::Release);
        if let Some(tx) = self.stop_tx.take() {
            let _ = tx.try_send(());
        }
        let _ = self.spirc.shutdown();
        self.task.abort();
    }

    #[napi]
    pub fn shutdown(&mut self) {
        self.stop();
    }

    #[napi]
    pub fn close(&mut self) {
        self.stop();
    }

    #[napi]
    pub fn play(&self) {
        let _ = self.spirc.play();
    }

    #[napi]
    pub fn pause(&self) {
        let _ = self.spirc.pause();
    }

    #[napi]
    pub fn next(&self) {
        let _ = self.spirc.next();
    }

    #[napi]
    pub fn prev(&self) {
        let _ = self.spirc.prev();
    }

    #[napi(getter)]
    pub fn sample_rate(&self) -> u32 {
        self.sample_rate
    }

    #[napi(getter)]
    pub fn channels(&self) -> u16 {
        self.channels
    }
}

/// Handle to a librespot session.
#[napi]
pub struct LibrespotSession {
    session: Session,
    player_config: PlayerConfig,
    device_id: String,
}

#[napi]
pub struct StreamHandle {
    stop_tx: Option<mpsc::Sender<()>>,
    #[allow(dead_code)]
    tsfn: ThreadsafeFunction<Bytes, ErrorStrategy::Fatal>,
    sample_rate: u32,
    channels: u16,
    #[allow(dead_code)]
    event_tsfn: Option<ThreadsafeFunction<ConnectEvent, ErrorStrategy::Fatal>>,
}

#[napi]
pub struct DownloadHandle {
    stop_flag: Arc<AtomicBool>,
    #[allow(dead_code)]
    task: tokio::task::JoinHandle<()>,
    #[allow(dead_code)]
    tsfn: ThreadsafeFunction<Bytes, ErrorStrategy::Fatal>,
}

#[napi]
impl DownloadHandle {
    #[napi]
    pub fn stop(&mut self) {
        self.stop_flag.store(true, Ordering::Release);
        self.task.abort();
    }
}

#[napi]
impl StreamHandle {
    #[napi]
    pub fn stop(&mut self) {
        if let Some(tx) = self.stop_tx.take() {
            let _ = tx.try_send(());
        }
    }

    #[napi(getter)]
    pub fn sample_rate(&self) -> u32 {
        self.sample_rate
    }

    #[napi(getter)]
    pub fn channels(&self) -> u16 {
        self.channels
    }
}

struct ChannelSink {
    tx: mpsc::Sender<Bytes>,
    format: AudioFormat,
    sample_rate: u32,
    channels: u16,
    start: Option<Instant>,
    expected_elapsed: Duration,
    first_chunk_logged: bool,
    log_tsfn: Option<ThreadsafeFunction<LogEvent, ErrorStrategy::Fatal>>,
    event_tsfn: Option<ThreadsafeFunction<ConnectEvent, ErrorStrategy::Fatal>>,
    scope: Option<String>,
    device_id: Option<String>,
    session_id: Option<String>,
    track_id: Option<String>,
    uri: Option<String>,
    stream_start: Instant,
    metric_sent: Arc<AtomicBool>,
    first_chunk_flag: Arc<AtomicBool>,
    last_pcm_at: Arc<AtomicU64>,
}

static LOG_OBSERVERS: OnceLock<Arc<Mutex<Vec<mpsc::Sender<LogEvent>>>>> = OnceLock::new();
static LOG_TSFNS: OnceLock<Arc<Mutex<Vec<ThreadsafeFunction<LogEvent, ErrorStrategy::Fatal>>>>> =
    OnceLock::new();

fn log_observers() -> &'static Arc<Mutex<Vec<mpsc::Sender<LogEvent>>>> {
    LOG_OBSERVERS.get_or_init(|| Arc::new(Mutex::new(Vec::new())))
}

fn log_tsfns() -> &'static Arc<Mutex<Vec<ThreadsafeFunction<LogEvent, ErrorStrategy::Fatal>>>> {
    LOG_TSFNS.get_or_init(|| Arc::new(Mutex::new(Vec::new())))
}

struct NativeLogger;

impl Log for NativeLogger {
    fn enabled(&self, _metadata: &LogMetadata<'_>) -> bool {
        true
    }

    fn log(&self, record: &Record<'_>) {
        let mut event = LogEvent {
            level: record.level().to_string().to_lowercase(),
            message: record.args().to_string(),
            scope: Some(record.target().to_string()),
            device_id: None,
            session_id: None,
        };
        if normalize_collection_parse_warning(&mut event) {
            return;
        }
        let mut observers = log_observers()
            .lock()
            .unwrap_or_else(|err| err.into_inner());
        observers.retain(|sender| match sender.try_send(event.clone()) {
            Ok(_) => true,
            Err(mpsc::error::TrySendError::Full(_)) => true,
            Err(mpsc::error::TrySendError::Closed(_)) => false,
        });
        drop(observers);
        let tsfns = log_tsfns().lock().unwrap_or_else(|err| err.into_inner());
        for tsfn in tsfns.iter() {
            let _ = tsfn.call(event.clone(), ThreadsafeFunctionCallMode::NonBlocking);
        }
    }

    fn flush(&self) {}
}

fn init_native_logger(log_tsfn: Option<ThreadsafeFunction<LogEvent, ErrorStrategy::Fatal>>) {
    if let Some(tsfn) = log_tsfn {
        log_tsfns()
            .lock()
            .unwrap_or_else(|err| err.into_inner())
            .push(tsfn);
    }
    if LOGGER_INIT.get().is_some() {
        return;
    }
    if LOGGER_INIT.set(()).is_err() {
        return;
    }
    let _ = log::set_boxed_logger(Box::new(NativeLogger));
    log::set_max_level(LevelFilter::Debug);
}

#[napi]
pub fn set_log_level(level: String) -> Result<()> {
    init_native_logger(None);
    let normalized = level.trim().to_lowercase();
    let filter = match normalized.as_str() {
        "off" => LevelFilter::Off,
        "error" => LevelFilter::Error,
        "warn" | "warning" => LevelFilter::Warn,
        "info" => LevelFilter::Info,
        "debug" => LevelFilter::Debug,
        "trace" => LevelFilter::Trace,
        _ => return Err(Error::from_reason(format!("invalid log level: {}", level))),
    };
    log::set_max_level(filter);
    Ok(())
}

fn subscribe_log_events() -> mpsc::Receiver<LogEvent> {
    let (tx, rx) = mpsc::channel::<LogEvent>(256);
    log_observers()
        .lock()
        .unwrap_or_else(|err| err.into_inner())
        .push(tx);
    rx
}

fn is_audio_key_error(event: &LogEvent) -> bool {
    let message = event.message.to_lowercase();
    message.contains("audio key")
        || message.contains("audiokeyerror")
        || message.contains("decryption key")
        || message.contains("unable to load key")
        || message.contains("unable to load decryption key")
}

fn is_decoder_error(event: &LogEvent) -> bool {
    let message = event.message.to_lowercase();
    message.contains("decoder error")
        || message.contains("symphonia decoder error")
        || message.contains("unable to read audio file")
        || message.contains("end of stream")
}

impl ChannelSink {
    fn new(
        tx: mpsc::Sender<Bytes>,
        format: AudioFormat,
        sample_rate: u32,
        channels: u16,
        log_tsfn: Option<ThreadsafeFunction<LogEvent, ErrorStrategy::Fatal>>,
        event_tsfn: Option<ThreadsafeFunction<ConnectEvent, ErrorStrategy::Fatal>>,
        scope: Option<String>,
        device_id: Option<String>,
        session_id: Option<String>,
        track_id: Option<String>,
        uri: Option<String>,
        stream_start: Instant,
        metric_sent: Arc<AtomicBool>,
        first_chunk_flag: Arc<AtomicBool>,
        last_pcm_at: Arc<AtomicU64>,
    ) -> Self {
        Self {
            tx,
            format,
            sample_rate,
            channels,
            start: None,
            expected_elapsed: Duration::from_millis(0),
            first_chunk_logged: false,
            log_tsfn,
            event_tsfn,
            scope,
            device_id,
            session_id,
            track_id,
            uri,
            stream_start,
            metric_sent,
            first_chunk_flag,
            last_pcm_at,
        }
    }
}

fn emit_log_ctx(
    tsfn: &Option<ThreadsafeFunction<LogEvent, ErrorStrategy::Fatal>>,
    level: &str,
    message: impl Into<String>,
    scope: Option<&str>,
    device_id: Option<&str>,
    session_id: Option<&str>,
) {
    if let Some(tsfn) = tsfn {
        let _ = tsfn.call(
            LogEvent {
                level: level.to_string(),
                message: message.into(),
                scope: scope.map(|val| val.to_string()),
                device_id: device_id.map(|val| val.to_string()),
                session_id: session_id.map(|val| val.to_string()),
            },
            ThreadsafeFunctionCallMode::NonBlocking,
        );
    }
}
impl Sink for ChannelSink {
    fn start(&mut self) -> SinkResult<()> {
        emit_log_ctx(
            &self.log_tsfn,
            "debug",
            "sink start",
            self.scope.as_deref(),
            self.device_id.as_deref(),
            self.session_id.as_deref(),
        );
        Ok(())
    }

    fn stop(&mut self) -> SinkResult<()> {
        // Reset pacing so the next start() (e.g. after Connect pause) is paced from now,
        // not from the original stream_start. Without this the post-resume catch-up logic
        // forwards minutes of decoded PCM at full speed.
        self.start = None;
        self.expected_elapsed = Duration::from_millis(0);
        Ok(())
    }

    fn write(&mut self, packet: AudioPacket, converter: &mut Converter) -> SinkResult<()> {
        let bytes: Bytes = match packet {
            AudioPacket::Samples(samples) => match self.format {
                AudioFormat::S16 => {
                    let s16: &[i16] = &converter.f64_to_s16(&samples);
                    Bytes::copy_from_slice(bytemuck::cast_slice(s16))
                }
                AudioFormat::F32 => {
                    let s32: &[f32] = &converter.f64_to_f32(&samples);
                    Bytes::copy_from_slice(bytemuck::cast_slice(s32))
                }
                AudioFormat::S24 => {
                    let s24: &[i32] = &converter.f64_to_s24(&samples);
                    Bytes::copy_from_slice(bytemuck::cast_slice(s24))
                }
                _ => {
                    let s16: &[i16] = &converter.f64_to_s16(&samples);
                    Bytes::copy_from_slice(bytemuck::cast_slice(s16))
                }
            },
            AudioPacket::Raw(data) => Bytes::from(data),
        };
        if !self.first_chunk_logged && !bytes.is_empty() {
            self.first_chunk_logged = true;
            self.first_chunk_flag.store(true, Ordering::Release);
            emit_log_ctx(
                &self.log_tsfn,
                "info",
                format!(
                    "first pcm chunk: bytes={} sample_rate={} channels={}",
                    bytes.len(),
                    self.sample_rate,
                    self.channels
                ),
                self.scope.as_deref(),
                self.device_id.as_deref(),
                self.session_id.as_deref(),
            );
            eprintln!(
                "[node-librespot] first pcm chunk: bytes={} sample_rate={} channels={}",
                bytes.len(),
                self.sample_rate,
                self.channels
            );
            if let Some(tsfn_ev) = &self.event_tsfn {
                if !self.metric_sent.swap(true, Ordering::AcqRel) {
                    let elapsed_ms = self.stream_start.elapsed().as_millis() as u64;
                    let elapsed_ms_u32 = elapsed_ms.min(u32::MAX as u64) as u32;
                    let payload = ConnectEvent {
                        r#type: "metric".into(),
                        device_id: self.device_id.clone(),
                        session_id: self.session_id.clone(),
                        track_id: self.track_id.clone(),
                        uri: self.uri.clone(),
                        title: None,
                        artist: None,
                        album: None,
                        duration_ms: None,
                        position_ms: None,
                        volume: None,
                        error_code: None,
                        error_message: None,
                        metric_name: Some("first_pcm_ms".into()),
                        metric_value_ms: Some(elapsed_ms_u32),
                        metric_message: None,
                        credentials_json: None,
                    };
                    let _ = tsfn_ev.call(payload, ThreadsafeFunctionCallMode::NonBlocking);
                }
            }
        }
        if !bytes.is_empty() {
            let now_ms = SystemTime::now()
                .duration_since(UNIX_EPOCH)
                .unwrap_or_else(|_| Duration::from_secs(0))
                .as_millis() as u64;
            self.last_pcm_at.store(now_ms, Ordering::Release);
        }
        // Pacing: throttle to approximate realtime based on sample count.
        // Send first chunk immediately to minimize startup latency; apply pacing after forwarding.
        let bytes_per_sample = match self.format {
            AudioFormat::S24 => 3,
            AudioFormat::S32 | AudioFormat::F32 => 4,
            _ => 2,
        };
        let samples = bytes.len() / (bytes_per_sample * self.channels as usize);
        let duration = Duration::from_secs_f64(samples as f64 / self.sample_rate as f64);
        if self.tx.try_send(bytes).is_err() {
            // Drop chunk if JS side is backpressured to avoid blocking the player thread.
        }
        let now = Instant::now();
        // If wall-clock has drifted far ahead of our expected timeline (a pause that didn't
        // route through stop()/start(), a decoder stall, etc.), rebase pacing onto `now`.
        // Otherwise `sleep_dur` saturates to 0 for the duration of the gap and the sink
        // dumps the catch-up backlog at full speed downstream.
        if let Some(start) = self.start {
            if now > start + self.expected_elapsed + Duration::from_millis(200) {
                self.start = None;
                self.expected_elapsed = Duration::from_millis(0);
            }
        }
        let start = *self.start.get_or_insert(now);
        self.expected_elapsed += duration;
        let target = start + self.expected_elapsed;
        let sleep_dur = target.saturating_duration_since(now);
        if !sleep_dur.is_zero() {
            sleep(sleep_dur);
        }
        Ok(())
    }
}

#[napi]
impl LibrespotSession {
    /// Stream a Spotify track or episode as PCM s16le and deliver chunks via a JS callback.
    /// The callback receives Buffer chunks. Returns a handle with a `stop()` method.
    #[napi]
    pub fn stream_track(
        &self,
        opts: StreamTrackOpts,
        on_chunk: JsFunction,
        on_event: Option<JsFunction>,
        on_log: Option<JsFunction>,
    ) -> Result<StreamHandle> {
        let uri = opts.uri;
        if uri.is_empty() {
            return Err(Error::from_reason("uri is required"));
        }
        let spotify_uri = SpotifyUri::from_uri(&uri)
            .map_err(|e| Error::from_reason(format!("invalid spotify uri: {:?}", e)))?;

        let (tx, mut rx) = mpsc::channel::<Bytes>(16);
        let (stop_tx, mut stop_rx) = mpsc::channel::<()>(1);

        // Threadsafe function to deliver chunks to JS.
        let tsfn: ThreadsafeFunction<Bytes, ErrorStrategy::Fatal> = on_chunk
            .create_threadsafe_function(0, |ctx: ThreadSafeCallContext<Bytes>| {
                let env = ctx.env;
                let buffer = ctx.value;
                let js_buffer = env.create_buffer_with_data(buffer.to_vec())?;
                Ok(vec![js_buffer.into_unknown()])
            })?;

        // Optional threadsafe function to deliver playback events.
        let event_tsfn: Option<ThreadsafeFunction<ConnectEvent, ErrorStrategy::Fatal>> = on_event
            .map(|f| {
                f.create_threadsafe_function(0, |ctx: ThreadSafeCallContext<ConnectEvent>| {
                    let env = ctx.env;
                    let val = ctx.value;
                    let mut obj = env.create_object()?;
                    obj.set_named_property("type", env.create_string(&val.r#type)?)?;
                    if let Some(device_id) = val.device_id {
                        obj.set_named_property("deviceId", env.create_string(&device_id)?)?;
                    }
                    if let Some(session_id) = val.session_id {
                        obj.set_named_property("sessionId", env.create_string(&session_id)?)?;
                    }
                    if let Some(tid) = val.track_id {
                        obj.set_named_property("trackId", env.create_string(&tid)?)?;
                    }
                    if let Some(uri) = val.uri {
                        obj.set_named_property("uri", env.create_string(&uri)?)?;
                    }
                    if let Some(pos) = val.position_ms {
                        obj.set_named_property("positionMs", env.create_uint32(pos))?;
                    }
                    if let Some(dur) = val.duration_ms {
                        obj.set_named_property("durationMs", env.create_uint32(dur))?;
                    }
                    if let Some(vol) = val.volume {
                        obj.set_named_property("volume", env.create_uint32(vol as u32))?;
                    }
                    if let Some(code) = val.error_code {
                        obj.set_named_property("errorCode", env.create_string(&code)?)?;
                    }
                    if let Some(message) = val.error_message {
                        obj.set_named_property("errorMessage", env.create_string(&message)?)?;
                    }
                    if let Some(metric_name) = val.metric_name {
                        obj.set_named_property("metricName", env.create_string(&metric_name)?)?;
                    }
                    if let Some(metric_value_ms) = val.metric_value_ms {
                        obj.set_named_property(
                            "metricValueMs",
                            env.create_uint32(metric_value_ms as u32),
                        )?;
                    }
                    if let Some(metric_message) = val.metric_message {
                        obj.set_named_property(
                            "metricMessage",
                            env.create_string(&metric_message)?,
                        )?;
                    }
                    if let Some(creds_json) = val.credentials_json {
                        obj.set_named_property("credentialsJson", env.create_string(&creds_json)?)?;
                    }
                    Ok(vec![obj.into_unknown()])
                })
            })
            .transpose()?;

        let log_tsfn: Option<ThreadsafeFunction<LogEvent, ErrorStrategy::Fatal>> = on_log
            .map(|f| {
                f.create_threadsafe_function(0, |ctx: ThreadSafeCallContext<LogEvent>| {
                    let env = ctx.env;
                    let val = ctx.value;
                    let mut obj = env.create_object()?;
                    obj.set_named_property("level", env.create_string(&val.level)?)?;
                    obj.set_named_property("message", env.create_string(&val.message)?)?;
                    if let Some(scope) = val.scope {
                        obj.set_named_property("scope", env.create_string(&scope)?)?;
                    }
                    if let Some(device_id) = val.device_id {
                        obj.set_named_property("deviceId", env.create_string(&device_id)?)?;
                    }
                    if let Some(session_id) = val.session_id {
                        obj.set_named_property("sessionId", env.create_string(&session_id)?)?;
                    }
                    Ok(vec![obj.into_unknown()])
                })
            })
            .transpose()?;

        init_native_logger(log_tsfn.clone());
        let device_id = self.device_id.clone();
        let session_id = next_session_id("stream");
        emit_log_ctx(
            &log_tsfn,
            "info",
            format!("stream_track start uri={}", uri),
            Some("stream_track"),
            Some(&device_id),
            Some(&session_id),
        );

        // Spawn forwarder task to drain mpsc into TSFN.
        runtime().spawn({
            let tsfn = tsfn.clone();
            async move {
                while let Some(chunk) = rx.recv().await {
                    let _ = tsfn.call(chunk, ThreadsafeFunctionCallMode::NonBlocking);
                }
            }
        });

        let mut player_config = self.player_config.clone();
        let session = self.session.clone();

        // Build a playback backend that writes to our channel.
        let log_tsfn_for_sink = log_tsfn.clone();
        let event_tsfn_for_sink = event_tsfn.clone();
        let log_tsfn_for_spawn = log_tsfn.clone();
        let first_chunk_flag = Arc::new(AtomicBool::new(false));
        let first_chunk_flag_for_sink = first_chunk_flag.clone();
        let last_pcm_at = Arc::new(AtomicU64::new(0));
        let last_pcm_at_for_sink = last_pcm_at.clone();
        let stream_start = Instant::now();
        let metric_sent = Arc::new(AtomicBool::new(false));
        let metric_sent_for_sink = metric_sent.clone();
        let error_sent = Arc::new(AtomicBool::new(false));
        let decoder_metric_sent = Arc::new(AtomicBool::new(false));
        let stop_flag = Arc::new(AtomicBool::new(false));
        let track_id_for_events = spotify_uri.to_id();
        let track_id_for_sink = track_id_for_events.clone();
        let uri_for_sink = uri.clone();
        let backend_factory = {
            let tx_clone = tx.clone();
            let device_id = device_id.clone();
            let session_id = session_id.clone();
            move || {
                emit_log_ctx(
                    &log_tsfn_for_sink,
                    "debug",
                    "sink created",
                    Some("stream_track"),
                    Some(&device_id),
                    Some(&session_id),
                );
                let sink = ChannelSink::new(
                    tx_clone.clone(),
                    AudioFormat::S16,
                    44100,
                    2,
                    log_tsfn_for_sink.clone(),
                    event_tsfn_for_sink.clone(),
                    Some("stream_track".to_string()),
                    Some(device_id.clone()),
                    Some(session_id.clone()),
                    Some(track_id_for_sink.clone()),
                    Some(uri_for_sink.clone()),
                    stream_start,
                    metric_sent_for_sink.clone(),
                    first_chunk_flag_for_sink.clone(),
                    last_pcm_at_for_sink.clone(),
                );
                Box::new(sink) as Box<dyn Sink>
            }
        };

        let start_position_ms = opts.start_position_ms.unwrap_or(0);
        player_config.bitrate = match opts.bitrate {
            Some(96) => Bitrate::Bitrate96,
            Some(160) => Bitrate::Bitrate160,
            _ => Bitrate::Bitrate320,
        };

        // Volume getter (no-op).
        let volume_getter: Box<dyn VolumeGetter + Send> = Box::new(NoOpVolume);

        // Spawn playback task.
        let event_tsfn_clone = event_tsfn.clone();
        let log_tsfn_clone = log_tsfn.clone();
        let error_sent_for_spawn = error_sent.clone();
        let error_sent_for_probe = error_sent.clone();
        let error_sent_for_logs = error_sent.clone();
        let decoder_metric_sent_for_logs = decoder_metric_sent.clone();
        let stop_flag_for_logs = stop_flag.clone();
        let uri_for_events = uri.clone();
        let device_id_for_events = device_id.clone();
        let session_id_for_events = session_id.clone();
        let stop_flag_for_events = stop_flag.clone();
        let stop_flag_for_health = stop_flag.clone();
        let last_pcm_for_health = last_pcm_at.clone();
        let mut log_rx = subscribe_log_events();
        let event_tsfn_for_logs = event_tsfn.clone();
        let uri_for_logs = uri.clone();
        let track_for_logs = track_id_for_events.clone();
        let device_id_for_logs = device_id.clone();
        let session_id_for_logs = session_id.clone();
        runtime().spawn(async move {
            while let Some(event) = log_rx.recv().await {
                if stop_flag_for_logs.load(Ordering::Acquire) {
                    break;
                }
                if is_decoder_error(&event) {
                    if let Some(tsfn_ev) = &event_tsfn_for_logs {
                        if !decoder_metric_sent_for_logs.swap(true, Ordering::AcqRel) {
                            let payload = ConnectEvent {
                                r#type: "metric".into(),
                                device_id: Some(device_id_for_logs.clone()),
                                session_id: Some(session_id_for_logs.clone()),
                                track_id: Some(track_for_logs.clone()),
                                uri: Some(uri_for_logs.clone()),
                                title: None,
                                artist: None,
                                album: None,
                                duration_ms: None,
                                position_ms: None,
                                volume: None,
                                error_code: None,
                                error_message: None,
                                metric_name: Some("decode_error".into()),
                                metric_value_ms: None,
                                metric_message: Some(event.message.clone()),
                                credentials_json: None,
                            };
                            let _ = tsfn_ev.call(payload, ThreadsafeFunctionCallMode::NonBlocking);
                        }
                    }
                }
                if error_sent_for_logs.load(Ordering::Acquire) {
                    continue;
                }
                if !is_audio_key_error(&event) {
                    continue;
                }
                if error_sent_for_logs.swap(true, Ordering::AcqRel) {
                    continue;
                }
                if let Some(tsfn_ev) = &event_tsfn_for_logs {
                    let payload = ConnectEvent {
                        r#type: "error".into(),
                        device_id: Some(device_id_for_logs.clone()),
                        session_id: Some(session_id_for_logs.clone()),
                        track_id: Some(track_for_logs.clone()),
                        uri: Some(uri_for_logs.clone()),
                        title: None,
                        artist: None,
                        album: None,
                        duration_ms: None,
                        position_ms: None,
                        volume: None,
                        error_code: Some("audio_key_error".into()),
                        error_message: Some(event.message.clone()),
                        metric_name: None,
                        metric_value_ms: None,
                        metric_message: None,
                        credentials_json: None,
                    };
                    let _ = tsfn_ev.call(payload, ThreadsafeFunctionCallMode::NonBlocking);
                }
            }
        });

        runtime().spawn(async move {
      let player = Player::new(player_config, session, volume_getter, backend_factory);
      let mut event_rx = player.get_player_event_channel();
      player.load(spotify_uri, true, start_position_ms);
      player.play();
      emit_log_ctx(
        &log_tsfn_clone,
        "info",
        "player.load + player.play invoked",
        Some("stream_track"),
        Some(&device_id_for_events),
        Some(&session_id_for_events),
      );

      let first_chunk_probe = first_chunk_flag.clone();
      let log_for_probe = log_tsfn_for_spawn.clone();
      let event_for_probe = event_tsfn_clone.clone();
      let uri_for_probe = uri_for_events.clone();
      let track_for_probe = track_id_for_events.clone();
      let device_id_for_probe = device_id_for_events.clone();
      let session_id_for_probe = session_id_for_events.clone();
      runtime().spawn(async move {
        tokio::time::sleep(Duration::from_millis(1500)).await;
        if !first_chunk_probe.load(Ordering::Acquire)
          && !error_sent_for_probe.swap(true, Ordering::AcqRel)
        {
          emit_log_ctx(
            &log_for_probe,
            "warn",
            "no pcm after 1500ms",
            Some("stream_track"),
            Some(&device_id_for_probe),
            Some(&session_id_for_probe),
          );
          if let Some(tsfn_ev) = &event_for_probe {
            let payload = ConnectEvent {
              r#type: "error".into(),
              device_id: Some(device_id_for_probe.clone()),
              session_id: Some(session_id_for_probe.clone()),
              track_id: Some(track_for_probe.clone()),
              uri: Some(uri_for_probe.clone()),
              title: None,
              artist: None,
              album: None,
              duration_ms: None,
              position_ms: None,
              volume: None,
              error_code: Some("no_pcm".into()),
              error_message: Some("no pcm after 1500ms".into()),
              metric_name: None,
              metric_value_ms: None,
              metric_message: None,
              credentials_json: None,
            };
            let _ = tsfn_ev.call(payload, ThreadsafeFunctionCallMode::NonBlocking);
          }
        }
      });
      if let Some(tsfn_ev) = event_tsfn_clone.clone() {
        let uri_for_health = uri_for_events.clone();
        let track_for_health = track_id_for_events.clone();
        let device_id_for_health = device_id_for_events.clone();
        let session_id_for_health = session_id_for_events.clone();
        runtime().spawn(async move {
          let mut stall_reported = false;
          loop {
            tokio::time::sleep(Duration::from_secs(5)).await;
            if stop_flag_for_health.load(Ordering::Acquire) {
              break;
            }
            let last_ms = last_pcm_for_health.load(Ordering::Acquire);
            let now_ms = SystemTime::now()
              .duration_since(UNIX_EPOCH)
              .unwrap_or_else(|_| Duration::from_secs(0))
              .as_millis() as u64;
            let stall_ms = now_ms.saturating_sub(last_ms);
            let (code, message) = if last_ms == 0 {
              ("pcm_missing", "no pcm received yet")
            } else if stall_ms > 2000 {
              ("pcm_stalled", "pcm stalled")
            } else {
              ("pcm_ok", "pcm flowing")
            };
            if code == "pcm_ok" {
              stall_reported = false;
            }
            if code == "pcm_stalled" && !stall_reported {
              stall_reported = true;
            let stall_ms_u32 = stall_ms.min(u32::MAX as u64) as u32;
            let metric_payload = ConnectEvent {
              r#type: "metric".into(),
              device_id: Some(device_id_for_health.clone()),
              session_id: Some(session_id_for_health.clone()),
              track_id: Some(track_for_health.clone()),
              uri: Some(uri_for_health.clone()),
              title: None,
              artist: None,
              album: None,
              duration_ms: None,
              position_ms: None,
              volume: None,
              error_code: None,
              error_message: None,
              metric_name: Some("buffer_stall_ms".into()),
              metric_value_ms: Some(stall_ms_u32),
              metric_message: Some("pcm stalled".into()),
              credentials_json: None,
            };
              let _ = tsfn_ev.call(metric_payload, ThreadsafeFunctionCallMode::NonBlocking);
            }
            let payload = ConnectEvent {
              r#type: "health".into(),
              device_id: Some(device_id_for_health.clone()),
              session_id: Some(session_id_for_health.clone()),
              track_id: Some(track_for_health.clone()),
              uri: Some(uri_for_health.clone()),
              title: None,
              artist: None,
              album: None,
              duration_ms: None,
              position_ms: None,
              volume: None,
              error_code: Some(code.into()),
              error_message: Some(message.into()),
              metric_name: None,
              metric_value_ms: None,
              metric_message: None,
              credentials_json: None,
            };
            let _ = tsfn_ev.call(payload, ThreadsafeFunctionCallMode::NonBlocking);
          }
        });
      }

      let mut last_position_ms: Option<u32> = None;
      let mut last_duration_ms: Option<u32> = None;
      let mut saw_playing = false;
      let mut first_event_logged = false;
      let mut event_count: u32 = 0;
      let mut sent_unavailable = false;
      loop {
        tokio::select! {
          _ = stop_rx.recv() => {
            player.stop();
            emit_log_ctx(
              &log_tsfn_clone,
              "info",
              "stop received; player.stop()",
              Some("stream_track"),
              Some(&device_id_for_events),
              Some(&session_id_for_events),
            );
            stop_flag_for_events.store(true, Ordering::Release);
            break;
          }
          Some(ev) = event_rx.recv(), if opts.emit_events.unwrap_or(true) => {
            let event_name = match &ev {
              PlayerEvent::Playing { .. } => "playing",
              PlayerEvent::Paused { .. } => "paused",
              PlayerEvent::Loading { .. } => "loading",
              PlayerEvent::Stopped { .. } => "stopped",
              PlayerEvent::EndOfTrack { .. } => "end_of_track",
              PlayerEvent::Unavailable { .. } => "unavailable",
              PlayerEvent::Preloading { .. } => "preloading",
              PlayerEvent::TimeToPreloadNextTrack { .. } => "time_to_preload",
              PlayerEvent::VolumeChanged { .. } => "volume",
              PlayerEvent::PositionCorrection { .. } => "position_correction",
              PlayerEvent::PlayRequestIdChanged { .. } => "play_request_id",
              _ => "other",
            };
            if !first_event_logged {
              first_event_logged = true;
              emit_log_ctx(
                &log_tsfn_clone,
                "debug",
                format!("first player event: {}", event_name),
                Some("stream_track"),
                Some(&device_id_for_events),
                Some(&session_id_for_events),
              );
            }
            event_count += 1;
            if event_count <= 5 {
              emit_log_ctx(
                &log_tsfn_clone,
                "debug",
                format!("player event {}", event_name),
                Some("stream_track"),
                Some(&device_id_for_events),
                Some(&session_id_for_events),
              );
            }
            if event_name == "unavailable" && !sent_unavailable {
              sent_unavailable = true;
              emit_log_ctx(
                &log_tsfn_clone,
                "warn",
                "player reported track unavailable",
                Some("stream_track"),
                Some(&device_id_for_events),
                Some(&session_id_for_events),
              );
              if !error_sent_for_spawn.swap(true, Ordering::AcqRel) {
                if let Some(tsfn_ev) = &event_tsfn_clone {
                  let payload = ConnectEvent {
                    r#type: "error".into(),
                    device_id: Some(device_id_for_events.clone()),
                    session_id: Some(session_id_for_events.clone()),
                    track_id: Some(track_id_for_events.clone()),
                    uri: Some(uri_for_events.clone()),
                    title: None,
                    artist: None,
                    album: None,
                    duration_ms: None,
                    position_ms: None,
                    volume: None,
                    error_code: Some("unavailable".into()),
                    error_message: Some("track unavailable".into()),
                    metric_name: None,
                    metric_value_ms: None,
                    metric_message: None,
                    credentials_json: None,
                  };
                  let _ = tsfn_ev.call(payload, ThreadsafeFunctionCallMode::NonBlocking);
                }
              }
            }
            if let Some(tsfn_ev) = &event_tsfn_clone {
              let payload = match ev {
                PlayerEvent::Playing { track_id, position_ms, .. } => ConnectEvent {
                  r#type: "playing".into(),
                  device_id: Some(device_id_for_events.clone()),
                  session_id: Some(session_id_for_events.clone()),
                  track_id: Some(track_id.to_id()),
                  uri: Some(track_id.to_uri()),
                  title: None,
                  artist: None,
                  album: None,
                  position_ms: Some(position_ms),
                  duration_ms: None,
                  volume: None,
                  error_code: None,
                  error_message: None,
                  metric_name: None,
                  metric_value_ms: None,
                  metric_message: None,
                  credentials_json: None,
                },
                PlayerEvent::Paused { track_id, position_ms, .. } => ConnectEvent {
                  r#type: "paused".into(),
                  device_id: Some(device_id_for_events.clone()),
                  session_id: Some(session_id_for_events.clone()),
                  track_id: Some(track_id.to_id()),
                  uri: Some(track_id.to_uri()),
                  title: None,
                  artist: None,
                  album: None,
                  position_ms: Some(position_ms),
                  duration_ms: None,
                  volume: None,
                  error_code: None,
                  error_message: None,
                  metric_name: None,
                  metric_value_ms: None,
                  metric_message: None,
                  credentials_json: None,
                },
                PlayerEvent::Loading { track_id, position_ms, .. } => ConnectEvent {
                  r#type: "loading".into(),
                  device_id: Some(device_id_for_events.clone()),
                  session_id: Some(session_id_for_events.clone()),
                  track_id: Some(track_id.to_id()),
                  uri: Some(track_id.to_uri()),
                  title: None,
                  artist: None,
                  album: None,
                  position_ms: Some(position_ms),
                  duration_ms: None,
                  volume: None,
                  error_code: None,
                  error_message: None,
                  metric_name: None,
                  metric_value_ms: None,
                  metric_message: None,
                  credentials_json: None,
                },
                PlayerEvent::Stopped { track_id, .. } => ConnectEvent {
                  r#type: "stopped".into(),
                  device_id: Some(device_id_for_events.clone()),
                  session_id: Some(session_id_for_events.clone()),
                  track_id: Some(track_id.to_id()),
                  uri: Some(track_id.to_uri()),
                  title: None,
                  artist: None,
                  album: None,
                  position_ms: None,
                  duration_ms: None,
                  volume: None,
                  error_code: None,
                  error_message: None,
                  metric_name: None,
                  metric_value_ms: None,
                  metric_message: None,
                  credentials_json: None,
                },
                PlayerEvent::EndOfTrack { track_id, .. } => {
                  if !saw_playing || last_duration_ms.is_none() {
                    if !error_sent_for_spawn.swap(true, Ordering::AcqRel) {
                      let fallback_id = Some(track_id.to_id());
                      let payload = ConnectEvent {
                        r#type: "error".into(),
                        device_id: Some(device_id_for_events.clone()),
                        session_id: Some(session_id_for_events.clone()),
                        track_id: fallback_id,
                        uri: Some(track_id.to_uri()),
                        title: None,
                        artist: None,
                        album: None,
                        duration_ms: None,
                        position_ms: None,
                        volume: None,
                        error_code: Some("end_of_track".into()),
                        error_message: Some("end_of_track before pcm".into()),
                        metric_name: None,
                        metric_value_ms: None,
                        metric_message: None,
                        credentials_json: None,
                      };
                      let _ = tsfn_ev.call(payload, ThreadsafeFunctionCallMode::NonBlocking);
                    }
                    continue;
                  }
                  ConnectEvent {
                    r#type: "end_of_track".into(),
                    device_id: Some(device_id_for_events.clone()),
                    session_id: Some(session_id_for_events.clone()),
                    track_id: Some(track_id.to_id()),
                    uri: Some(track_id.to_uri()),
                    title: None,
                    artist: None,
                    album: None,
                    position_ms: last_position_ms,
                    duration_ms: last_duration_ms,
                    volume: None,
                    error_code: None,
                    error_message: None,
                    metric_name: None,
                    metric_value_ms: None,
                    metric_message: None,
                    credentials_json: None,
                  }
                }
                PlayerEvent::Unavailable { track_id, .. } => ConnectEvent {
                  r#type: "unavailable".into(),
                  device_id: Some(device_id_for_events.clone()),
                  session_id: Some(session_id_for_events.clone()),
                  track_id: Some(track_id.to_id()),
                  uri: Some(track_id.to_uri()),
                  title: None,
                  artist: None,
                  album: None,
                  position_ms: None,
                  duration_ms: None,
                  volume: None,
                  error_code: None,
                  error_message: None,
                  metric_name: None,
                  metric_value_ms: None,
                  metric_message: None,
                  credentials_json: None,
                },
                PlayerEvent::VolumeChanged { volume } => ConnectEvent {
                  r#type: "volume".into(),
                  device_id: Some(device_id_for_events.clone()),
                  session_id: Some(session_id_for_events.clone()),
                  track_id: None,
                  uri: None,
                  title: None,
                  artist: None,
                  album: None,
                  position_ms: None,
                  duration_ms: None,
                  volume: Some(volume),
                  error_code: None,
                  error_message: None,
                  metric_name: None,
                  metric_value_ms: None,
                  metric_message: None,
                  credentials_json: None,
                },
                PlayerEvent::PositionCorrection { track_id, position_ms, .. } => ConnectEvent {
                  r#type: "position_correction".into(),
                  device_id: Some(device_id_for_events.clone()),
                  session_id: Some(session_id_for_events.clone()),
                  track_id: Some(track_id.to_id()),
                  uri: Some(track_id.to_uri()),
                  title: None,
                  artist: None,
                  album: None,
                  position_ms: Some(position_ms),
                  duration_ms: None,
                  volume: None,
                  error_code: None,
                  error_message: None,
                  metric_name: None,
                  metric_value_ms: None,
                  metric_message: None,
                  credentials_json: None,
                },
                _ => continue,
              };
              match payload.r#type.as_str() {
                "playing" | "paused" => {
                  saw_playing = true;
                  if let Some(pos) = payload.position_ms {
                    last_position_ms = Some(pos);
                  }
                  if let Some(dur) = payload.duration_ms {
                    last_duration_ms = Some(dur);
                  }
                }
                _ => {}
              }
              let _ = tsfn_ev.call(payload, ThreadsafeFunctionCallMode::NonBlocking);
            }
          }
        }
      }
      stop_flag_for_events.store(true, Ordering::Release);
    });

        Ok(StreamHandle {
            stop_tx: Some(stop_tx),
            tsfn,
            sample_rate: 44100,
            channels: 2,
            event_tsfn,
        })
    }

    /// Resolve a track's signed CDN URL + AES audio key WITHOUT downloading or
    /// decrypting any audio. The caller fetches the CDN URL directly (HTTP Range
    /// supported) and decrypts with AES-128-CTR (fixed Spotify IV, counter =
    /// byte_offset / 16). For OGG, strip up to the first `OggS` page before
    /// decoding (Spotify prepends a ~167-byte header).
    #[napi]
    pub fn resolve_audio_file(&self, opts: DownloadTrackOpts) -> Result<ResolveAudioFileResult> {
        let (spotify_uri, _) = parse_resolve_target(&opts.uri)?;
        runtime().block_on(do_resolve_audio_file(
            self.session.clone(),
            spotify_uri,
            opts.bitrate,
        ))
    }

    /// Mint the first-party session tokens (login5 bearer + client-token) for
    /// the app to call pathfinder / spclient itself. Both are refreshed/cached
    /// internally by librespot, so callers may call this whenever they need a
    /// fresh pair. Runs on the libuv threadpool.
    #[napi(ts_return_type = "Promise<SessionTokens>")]
    pub fn get_tokens(&self) -> Result<AsyncTask<GetTokensTask>> {
        Ok(AsyncTask::new(GetTokensTask {
            session: self.session.clone(),
        }))
    }

    /// Async variant of `resolve_audio_file`: returns a Promise and runs the
    /// blocking CDN/key lookup on the libuv threadpool, so resolving a track
    /// never stalls the Node event loop (and other zones' audio pacing). Prefer
    /// this on the playback hot path; the sync version remains for simple/probe
    /// callers.
    #[napi(ts_return_type = "Promise<ResolveAudioFileResult>")]
    pub fn resolve_audio_file_async(
        &self,
        opts: DownloadTrackOpts,
    ) -> Result<AsyncTask<ResolveAudioFileTask>> {
        let (spotify_uri, _) = parse_resolve_target(&opts.uri)?;
        Ok(AsyncTask::new(ResolveAudioFileTask {
            session: self.session.clone(),
            spotify_uri,
            bitrate_pref: opts.bitrate,
        }))
    }

    /// Download (stream) raw decrypted audio bytes for a track/episode.
    #[napi]
    pub fn download_track(
        &self,
        opts: DownloadTrackOpts,
        on_chunk: JsFunction,
        on_log: Option<JsFunction>,
    ) -> Result<DownloadHandle> {
        let uri = opts.uri.clone();
        if uri.is_empty() {
            return Err(Error::from_reason("uri is required"));
        }
        let spotify_uri = SpotifyUri::from_uri(&uri)
            .map_err(|e| Error::from_reason(format!("invalid spotify uri: {:?}", e)))?;

        let (tx, mut rx) = mpsc::channel::<Bytes>(16);
        let stop_flag = Arc::new(AtomicBool::new(false));

        let tsfn: ThreadsafeFunction<Bytes, ErrorStrategy::Fatal> = on_chunk
            .create_threadsafe_function(0, |ctx: ThreadSafeCallContext<Bytes>| {
                let env = ctx.env;
                let buffer = ctx.value;
                let js_buffer = env.create_buffer_with_data(buffer.to_vec())?;
                Ok(vec![js_buffer.into_unknown()])
            })?;

        let log_tsfn: Option<ThreadsafeFunction<LogEvent, ErrorStrategy::Fatal>> = on_log
            .map(|f| {
                f.create_threadsafe_function(0, |ctx: ThreadSafeCallContext<LogEvent>| {
                    let env = ctx.env;
                    let val = ctx.value;
                    let mut obj = env.create_object()?;
                    obj.set_named_property("level", env.create_string(&val.level)?)?;
                    obj.set_named_property("message", env.create_string(&val.message)?)?;
                    if let Some(scope) = val.scope {
                        obj.set_named_property("scope", env.create_string(&scope)?)?;
                    }
                    if let Some(device_id) = val.device_id {
                        obj.set_named_property("deviceId", env.create_string(&device_id)?)?;
                    }
                    if let Some(session_id) = val.session_id {
                        obj.set_named_property("sessionId", env.create_string(&session_id)?)?;
                    }
                    Ok(vec![obj.into_unknown()])
                })
            })
            .transpose()?;

        init_native_logger(log_tsfn.clone());
        let device_id = self.device_id.clone();
        let session_id = next_session_id("download");
        emit_log_ctx(
            &log_tsfn,
            "info",
            format!("download_track start uri={}", uri),
            Some("download_track"),
            Some(&device_id),
            Some(&session_id),
        );

        runtime().spawn({
            let tsfn = tsfn.clone();
            let stop_flag = stop_flag.clone();
            async move {
                while let Some(chunk) = rx.recv().await {
                    if stop_flag.load(Ordering::Acquire) {
                        break;
                    }
                    let _ = tsfn.call(chunk, ThreadsafeFunctionCallMode::NonBlocking);
                }
            }
        });

        let session = self.session.clone();
        let bitrate_pref = opts.bitrate;

        let (encrypted_file, key) = runtime().block_on(async {
            let audio_item = AudioItem::get_file(&session, spotify_uri.clone())
                .await
                .map_err(|e| Error::from_reason(format!("failed to load audio item: {e:?}")))?;
            // Follow relinking; `track_id` becomes the playable item's id (the
            // alternative's when relinked) so the audio key matches.
            let audio_item = resolve_playable_audio_item(&session, audio_item).await?;
            let track_id = audio_item_track_id(&audio_item)?;

            let select_format =
                |files: &AudioFiles, bitrate: Option<u32>| -> Option<(AudioFileFormat, FileId)> {
                    let prefer = match bitrate {
                        Some(96) => {
                            vec![AudioFileFormat::OGG_VORBIS_96, AudioFileFormat::MP3_96]
                        }
                        Some(160) => {
                            vec![AudioFileFormat::OGG_VORBIS_160, AudioFileFormat::MP3_160]
                        }
                        _ => vec![
                            AudioFileFormat::OGG_VORBIS_320,
                            AudioFileFormat::MP3_320,
                            AudioFileFormat::MP3_256,
                        ],
                    };
                    for f in prefer {
                        if let Some(id) = files.get(&f) {
                            return Some((f, *id));
                        }
                    }
                    files.iter().next().map(|(f, id)| (*f, *id))
                };

            let (format, file_id) = select_format(&audio_item.files, bitrate_pref)
                .ok_or_else(|| Error::from_reason("no audio files available"))?;

            let bytes_per_second = stream_data_rate(format)
                .ok_or_else(|| Error::from_reason("unable to compute data rate"))?;

            let encrypted_file = AudioFile::open(&session, file_id, bytes_per_second)
                .await
                .map_err(|e| Error::from_reason(format!("failed to open audio file: {e:?}")))?;

            let key = match session.audio_key().request(track_id, file_id).await {
                Ok(key) => Some(key),
                Err(e) => {
                    emit_log_ctx(
                        &log_tsfn,
                        "warn",
                        format!("audio key unavailable, continuing without decryption: {e:?}"),
                        Some("download_track"),
                        None,
                        None,
                    );
                    None
                }
            };

            Ok::<_, Error>((encrypted_file, key))
        })?;

        let stop_flag_clone = stop_flag.clone();
        let log_tsfn_clone = log_tsfn.clone();

        let task = runtime().spawn(async move {
            let mut decrypted = AudioDecrypt::new(key, encrypted_file);
            let mut buf = vec![0u8; 32 * 1024];
            loop {
                if stop_flag_clone.load(Ordering::Acquire) {
                    break;
                }
                match decrypted.read(&mut buf) {
                    Ok(0) => break,
                    Ok(n) => {
                        if tx.send(Bytes::copy_from_slice(&buf[..n])).await.is_err() {
                            break;
                        }
                    }
                    Err(e) => {
                        emit_log_ctx(
                            &log_tsfn_clone,
                            "error",
                            format!("read error: {e:?}"),
                            Some("download_track"),
                            None,
                            None,
                        );
                        break;
                    }
                }
            }
        });

        Ok(DownloadHandle {
            stop_flag,
            task,
            tsfn,
        })
    }

    #[napi]
    pub async fn close(&self) -> Result<()> {
        Ok(())
    }
}

/// Build a librespot `Cache` for audio file storage.
/// Returns `None` if `cache_dir` is absent, the directory cannot be created,
/// or `Cache::new` fails — in which case streaming still works without cache.
fn build_audio_cache(cache_dir: Option<&str>, cache_size_limit_mb: Option<u32>) -> Option<Cache> {
    let dir = cache_dir?;
    if dir.trim().is_empty() {
        return None;
    }
    let path = std::path::Path::new(dir);
    if let Err(e) = fs::create_dir_all(path) {
        eprintln!("[lox-librespot] could not create cache dir {dir}: {e}");
        return None;
    }
    let size_limit = cache_size_limit_mb.map(|mb| mb as u64 * 1024 * 1024);
    Cache::new(
        Some(path),
        None::<&std::path::Path>,
        None::<&std::path::Path>,
        size_limit,
    )
    .ok()
}

/// Create a session using a Web API access token (client id optional via opts or env).
#[napi]
pub async fn create_session(opts: CreateSessionOpts) -> Result<LibrespotSession> {
    let access_token = opts
        .access_token
        .clone()
        .unwrap_or_default()
        .trim()
        .to_string();
    if access_token.is_empty() {
        return Err(Error::from_reason(
            "access token is required; obtain a user token via PKCE/Web API",
        ));
    }
    let credentials = Credentials::with_access_token(access_token);

    let mut session_config = SessionConfig::default();
    let mut device_id = opts.device_name.unwrap_or_else(|| "librespot".to_string());
    if device_id.trim().is_empty() {
        device_id = "librespot".to_string();
    }
    session_config.device_id = device_id.clone();
    if let Some(client_id) = opts.client_id {
        if !client_id.trim().is_empty() {
            session_config.client_id = client_id;
        }
    }

    let cache = build_audio_cache(opts.cache_dir.as_deref(), opts.cache_size_limit_mb);
    let session = Session::new(session_config, cache);
    session
        .connect(credentials, false)
        .await
        .map_err(|e| Error::from_reason(format!("session connect failed: {e}")))?;

    let player_config = PlayerConfig::default();

    Ok(LibrespotSession {
        session,
        player_config,
        device_id,
    })
}

/// Create a session using a reusable librespot credentials blob (JSON) or a path to credentials.json.
///
/// This avoids relying on a Web API access token for streaming.
#[napi]
pub async fn create_session_with_credentials(
    credentials_path: String,
    device_name: Option<String>,
    cache_dir: Option<String>,
    cache_size_limit_mb: Option<u32>,
) -> Result<LibrespotSession> {
    if credentials_path.trim().is_empty() {
        return Err(Error::from_reason("credentials payload is required"));
    }

    let credentials: Credentials = if Path::new(&credentials_path).exists() {
        let mut file =
            File::open(&credentials_path).map_err(|e| Error::from_reason(format!("{e}")))?;
        let mut buf = String::new();
        file.read_to_string(&mut buf)
            .map_err(|e| Error::from_reason(format!("{e}")))?;
        serde_json::from_str(&buf)
            .map_err(|e| Error::from_reason(format!("invalid credentials json: {e}")))?
    } else {
        serde_json::from_str(&credentials_path)
            .map_err(|e| Error::from_reason(format!("invalid credentials json: {e}")))?
    };

    let mut session_config = SessionConfig::default();
    let mut device_id = device_name.unwrap_or_else(|| "librespot".to_string());
    if device_id.trim().is_empty() {
        device_id = "librespot".to_string();
    }
    session_config.device_id = device_id.clone();
    if let Ok(client_id_override) = std::env::var("LOX_LIBRESPOT_CLIENT_ID") {
        if !client_id_override.trim().is_empty() {
            session_config.client_id = client_id_override;
        }
    }

    let cache = build_audio_cache(cache_dir.as_deref(), cache_size_limit_mb);
    let session = Session::new(session_config, cache);
    session
        .connect(credentials, false)
        .await
        .map_err(|e| Error::from_reason(format!("session connect failed: {e}")))?;

    let player_config = PlayerConfig::default();

    Ok(LibrespotSession {
        session,
        player_config,
        device_id,
    })
}

/// Internal helper to start a Spotify Connect device using provided credentials.
/// Accepts credentials (typically created from an OAuth access token) and is shared by the
/// token-based public entrypoint.
fn start_connect_device_inner(
    credentials_path: String,
    name: String,
    device_id: String,
    on_chunk: JsFunction,
    on_event: Option<JsFunction>,
    on_log: Option<JsFunction>,
) -> Result<ConnectHandle> {
    let tsfn: ThreadsafeFunction<Bytes, ErrorStrategy::Fatal> = on_chunk
        .create_threadsafe_function(0, |ctx: ThreadSafeCallContext<Bytes>| {
            let env = ctx.env;
            let buffer = ctx.value;
            let js_buffer = env.create_buffer_with_data(buffer.to_vec())?;
            Ok(vec![js_buffer.into_unknown()])
        })?;
    let event_tsfn: Option<ThreadsafeFunction<ConnectEvent, ErrorStrategy::Fatal>> = on_event
        .map(|f| {
            f.create_threadsafe_function(0, |ctx: ThreadSafeCallContext<ConnectEvent>| {
                let env = ctx.env;
                let val = ctx.value;
                // Serialize ConnectEvent into a JS object.
                let mut obj = env.create_object()?;
                obj.set_named_property("type", env.create_string(&val.r#type)?)?;
                if let Some(device_id) = val.device_id {
                    obj.set_named_property("deviceId", env.create_string(&device_id)?)?;
                }
                if let Some(session_id) = val.session_id {
                    obj.set_named_property("sessionId", env.create_string(&session_id)?)?;
                }
                if let Some(tid) = val.track_id {
                    obj.set_named_property("trackId", env.create_string(&tid)?)?;
                }
                if let Some(uri) = val.uri {
                    obj.set_named_property("uri", env.create_string(&uri)?)?;
                }
                if let Some(title) = val.title {
                    obj.set_named_property("title", env.create_string(&title)?)?;
                }
                if let Some(artist) = val.artist {
                    obj.set_named_property("artist", env.create_string(&artist)?)?;
                }
                if let Some(album) = val.album {
                    obj.set_named_property("album", env.create_string(&album)?)?;
                }
                if let Some(pos) = val.position_ms {
                    obj.set_named_property("positionMs", env.create_uint32(pos))?;
                }
                if let Some(dur) = val.duration_ms {
                    obj.set_named_property("durationMs", env.create_uint32(dur))?;
                }
                if let Some(vol) = val.volume {
                    obj.set_named_property("volume", env.create_uint32(vol as u32))?;
                }
                if let Some(code) = val.error_code {
                    obj.set_named_property("errorCode", env.create_string(&code)?)?;
                }
                if let Some(message) = val.error_message {
                    obj.set_named_property("errorMessage", env.create_string(&message)?)?;
                }
                if let Some(metric_name) = val.metric_name {
                    obj.set_named_property("metricName", env.create_string(&metric_name)?)?;
                }
                if let Some(metric_value_ms) = val.metric_value_ms {
                    obj.set_named_property(
                        "metricValueMs",
                        env.create_uint32(metric_value_ms as u32),
                    )?;
                }
                if let Some(metric_message) = val.metric_message {
                    obj.set_named_property("metricMessage", env.create_string(&metric_message)?)?;
                }
                if let Some(creds_json) = val.credentials_json {
                    obj.set_named_property("credentialsJson", env.create_string(&creds_json)?)?;
                }
                Ok(vec![obj.into_unknown()])
            })
        })
        .transpose()?;

    let log_tsfn: Option<ThreadsafeFunction<LogEvent, ErrorStrategy::Fatal>> = on_log
        .map(|f| {
            f.create_threadsafe_function(0, |ctx: ThreadSafeCallContext<LogEvent>| {
                let env = ctx.env;
                let val = ctx.value;
                let mut obj = env.create_object()?;
                obj.set_named_property("level", env.create_string(&val.level)?)?;
                obj.set_named_property("message", env.create_string(&val.message)?)?;
                if let Some(scope) = val.scope {
                    obj.set_named_property("scope", env.create_string(&scope)?)?;
                }
                if let Some(device_id) = val.device_id {
                    obj.set_named_property("deviceId", env.create_string(&device_id)?)?;
                }
                if let Some(session_id) = val.session_id {
                    obj.set_named_property("sessionId", env.create_string(&session_id)?)?;
                }
                Ok(vec![obj.into_unknown()])
            })
        })
        .transpose()?;

    init_native_logger(log_tsfn.clone());
    let session_id = next_session_id("connect");
    let stop_flag = Arc::new(AtomicBool::new(false));
    let stop_flag_for_block = stop_flag.clone();
    runtime().block_on(async move {
        emit_log_ctx(
            &log_tsfn,
            "info",
            "connect host start",
            Some("connect_host"),
            Some(&device_id),
            Some(&session_id),
        );
        let credentials: Credentials = if Path::new(&credentials_path).exists() {
            let mut file =
                File::open(&credentials_path).map_err(|e| Error::from_reason(format!("{e}")))?;
            let mut buf = String::new();
            file.read_to_string(&mut buf)
                .map_err(|e| Error::from_reason(format!("{e}")))?;
            serde_json::from_str(&buf)
                .map_err(|e| Error::from_reason(format!("invalid credentials json: {e}")))?
        } else {
            serde_json::from_str(&credentials_path)
                .map_err(|e| Error::from_reason(format!("invalid credentials json: {e}")))?
        };

        let mut session_config = SessionConfig::default();
        session_config.device_id = device_id.clone();
        if let Ok(client_id_override) = std::env::var("LOX_LIBRESPOT_CLIENT_ID") {
            if !client_id_override.trim().is_empty() {
                session_config.client_id = client_id_override;
            }
        }
        let client_id_for_discovery = session_config.client_id.clone();
        // Spirc::new neemt zelf de connect stap; we maken hier alleen een verse session.
        let session = Session::new(session_config.clone(), None);

        let connect_config = ConnectConfig {
            name: name.clone(),
            device_type: DeviceType::Speaker,
            is_group: false,
            // Start at full volume: the Player does not attenuate locally (NoOpVolume below),
            // so the PCM stays full-scale and the zone applies volume once on output. Volume
            // changes from the Spotify picker are still forwarded to the zone via VolumeChanged.
            // Spotify volume scale is 0..65535; use max to avoid a muted start.
            initial_volume: u16::MAX,
            disable_volume: false,
            volume_steps: 64,
            emit_set_queue_events: false,
        };

        let player_config = PlayerConfig::default();
        // The SoftMixer is still handed to the Spirc below so it tracks connect volume
        // state and keeps emitting VolumeChanged events (forwarded to the zone). But the
        // Player must NOT attenuate the PCM locally: volume is owned by the zone, which
        // applies it once on output. Feeding the SoftMixer's soft volume into the Player
        // here would double-attenuate (librespot volume × zone volume) whenever the
        // Spotify slider is moved. NoOpVolume keeps the PCM at full scale so the zone is
        // the sole attenuator while the Spotify picker still drives the zone volume.
        let mixer = SoftMixer::open(MixerConfig::default())
            .map_err(|e| Error::from_reason(format!("mixer init failed: {e}")))?;
        let volume_getter: Box<dyn VolumeGetter + Send> = Box::new(NoOpVolume);

        let (tx, mut rx) = mpsc::channel::<Bytes>(256);

        runtime().spawn({
            let tsfn = tsfn.clone();
            async move {
                while let Some(chunk) = rx.recv().await {
                    let _ = tsfn.call(chunk, ThreadsafeFunctionCallMode::NonBlocking);
                }
            }
        });

        let log_tsfn_for_sink = log_tsfn.clone();
        let event_tsfn_for_sink = event_tsfn.clone();
        let first_chunk_flag = Arc::new(AtomicBool::new(false));
        let first_chunk_flag_for_sink = first_chunk_flag.clone();
        let last_pcm_at = Arc::new(AtomicU64::new(0));
        let last_pcm_at_for_sink = last_pcm_at.clone();
        let stream_start = Instant::now();
        let metric_sent = Arc::new(AtomicBool::new(false));
        let metric_sent_for_sink = metric_sent.clone();
        let sink_builder = {
            let tx_clone = tx.clone();
            let device_id = device_id.clone();
            let session_id = session_id.clone();
            move || {
                let sink = ChannelSink::new(
                    tx_clone.clone(),
                    AudioFormat::S16,
                    44100,
                    2,
                    log_tsfn_for_sink.clone(),
                    event_tsfn_for_sink.clone(),
                    Some("connect_host".to_string()),
                    Some(device_id.clone()),
                    Some(session_id.clone()),
                    None,
                    None,
                    stream_start,
                    metric_sent_for_sink.clone(),
                    first_chunk_flag_for_sink.clone(),
                    last_pcm_at_for_sink.clone(),
                );
                Box::new(sink) as Box<dyn Sink>
            }
        };

        let player = Player::new(player_config, session.clone(), volume_getter, sink_builder);
        // Clone log_tsfn before it is moved into the player-event spawn below.
        let log_tsfn_for_disc = log_tsfn.clone();
        // [issue #252] Bumped by the player-event observer on signals that
        // prove audio is flowing (`playing` / `position_correction`). Read by
        // the log observer further down to suppress transient `audio_key_error`
        // events that fire while spirc is already recovering from an
        // account-side dealer-disconnect burst. A permanent audio-key fault
        // never produces a healthy event, so the error still surfaces after
        // the grace window.
        let healthy_seq = Arc::new(AtomicU64::new(0));
        // Forward player events to JS if requested.
        if let Some(tsfn_ev) = event_tsfn.clone() {
            let mut ev_rx = player.get_player_event_channel();
            let session_for_events = session.clone();
            let device_id_for_events = device_id.clone();
            let session_id_for_events = session_id.clone();
            let healthy_seq_for_events = healthy_seq.clone();
            let last_pcm_for_health = last_pcm_at.clone();
            let stop_flag_for_health = stop_flag_for_block.clone();
            let device_id_for_health = device_id.clone();
            let session_id_for_health = session_id.clone();
            let tsfn_health = tsfn_ev.clone();
            runtime().spawn(async move {
                let mut stall_reported = false;
                loop {
                    tokio::time::sleep(Duration::from_secs(5)).await;
                    if stop_flag_for_health.load(Ordering::Acquire) {
                        break;
                    }
                    let last_ms = last_pcm_for_health.load(Ordering::Acquire);
                    let now_ms = SystemTime::now()
                        .duration_since(UNIX_EPOCH)
                        .unwrap_or_else(|_| Duration::from_secs(0))
                        .as_millis() as u64;
                    let stall_ms = now_ms.saturating_sub(last_ms);
                    let (code, message) = if last_ms == 0 {
                        ("pcm_missing", "no pcm received yet")
                    } else if stall_ms > 2000 {
                        ("pcm_stalled", "pcm stalled")
                    } else {
                        ("pcm_ok", "pcm flowing")
                    };
                    if code == "pcm_ok" {
                        stall_reported = false;
                    }
                    if code == "pcm_stalled" && !stall_reported {
                        stall_reported = true;
                        let stall_ms_u32 = stall_ms.min(u32::MAX as u64) as u32;
                        let metric_payload = ConnectEvent {
                            r#type: "metric".into(),
                            device_id: Some(device_id_for_health.clone()),
                            session_id: Some(session_id_for_health.clone()),
                            track_id: None,
                            uri: None,
                            title: None,
                            artist: None,
                            album: None,
                            duration_ms: None,
                            position_ms: None,
                            volume: None,
                            error_code: None,
                            error_message: None,
                            metric_name: Some("buffer_stall_ms".into()),
                            metric_value_ms: Some(stall_ms_u32),
                            metric_message: Some("pcm stalled".into()),
                            credentials_json: None,
                        };
                        let _ = tsfn_health
                            .call(metric_payload, ThreadsafeFunctionCallMode::NonBlocking);
                    }
                    let payload = ConnectEvent {
                        r#type: "health".into(),
                        device_id: Some(device_id_for_health.clone()),
                        session_id: Some(session_id_for_health.clone()),
                        track_id: None,
                        uri: None,
                        title: None,
                        artist: None,
                        album: None,
                        duration_ms: None,
                        position_ms: None,
                        volume: None,
                        error_code: Some(code.into()),
                        error_message: Some(message.into()),
                        metric_name: None,
                        metric_value_ms: None,
                        metric_message: None,
                        credentials_json: None,
                    };
                    let _ = tsfn_health.call(payload, ThreadsafeFunctionCallMode::NonBlocking);
                }
            });
            runtime().spawn(async move {
                let mut last_position_ms: Option<u32> = None;
                let mut last_duration_ms: Option<u32> = None;
                let mut saw_playing = false;
                let mut first_event_logged = false;
                let mut event_count: u32 = 0;
                while let Some(ev) = ev_rx.recv().await {
                    let event_name = match &ev {
                        PlayerEvent::Playing { .. } => "playing",
                        PlayerEvent::Paused { .. } => "paused",
                        PlayerEvent::Loading { .. } => "loading",
                        PlayerEvent::Stopped { .. } => "stopped",
                        PlayerEvent::EndOfTrack { .. } => "end_of_track",
                        PlayerEvent::Unavailable { .. } => "unavailable",
                        PlayerEvent::Preloading { .. } => "preloading",
                        PlayerEvent::TimeToPreloadNextTrack { .. } => "time_to_preload",
                        PlayerEvent::VolumeChanged { .. } => "volume",
                        PlayerEvent::PositionCorrection { .. } => "position_correction",
                        PlayerEvent::PlayRequestIdChanged { .. } => "play_request_id",
                        _ => "other",
                    };
                    if !first_event_logged {
                        first_event_logged = true;
                        emit_log_ctx(
                            &log_tsfn,
                            "debug",
                            format!("first player event: {}", event_name),
                            Some("connect_host"),
                            Some(&device_id_for_events),
                            Some(&session_id_for_events),
                        );
                    }
                    event_count += 1;
                    if event_count <= 5 {
                        emit_log_ctx(
                            &log_tsfn,
                            "debug",
                            format!("player event {}", event_name),
                            Some("connect_host"),
                            Some(&device_id_for_events),
                            Some(&session_id_for_events),
                        );
                    }
                    if event_name == "unavailable" {
                        emit_log_ctx(
                            &log_tsfn,
                            "warn",
                            "player reported track unavailable",
                            Some("connect_host"),
                            Some(&device_id_for_events),
                            Some(&session_id_for_events),
                        );
                    }

                    let mut payload = match ev {
                        PlayerEvent::Playing {
                            track_id,
                            position_ms,
                            ..
                        } => ConnectEvent {
                            r#type: "playing".into(),
                            device_id: Some(device_id_for_events.clone()),
                            session_id: Some(session_id_for_events.clone()),
                            track_id: Some(track_id.to_id()),
                            uri: Some(track_id.to_uri()),
                            title: None,
                            artist: None,
                            album: None,
                            position_ms: Some(position_ms),
                            duration_ms: None,
                            volume: None,
                            error_code: None,
                            error_message: None,
                            metric_name: None,
                            metric_value_ms: None,
                            metric_message: None,
                            credentials_json: None,
                        },
                        PlayerEvent::Paused {
                            track_id,
                            position_ms,
                            ..
                        } => ConnectEvent {
                            r#type: "paused".into(),
                            device_id: Some(device_id_for_events.clone()),
                            session_id: Some(session_id_for_events.clone()),
                            track_id: Some(track_id.to_id()),
                            uri: Some(track_id.to_uri()),
                            title: None,
                            artist: None,
                            album: None,
                            position_ms: Some(position_ms),
                            duration_ms: None,
                            volume: None,
                            error_code: None,
                            error_message: None,
                            metric_name: None,
                            metric_value_ms: None,
                            metric_message: None,
                            credentials_json: None,
                        },
                        PlayerEvent::Loading {
                            track_id,
                            position_ms,
                            ..
                        } => ConnectEvent {
                            r#type: "loading".into(),
                            device_id: Some(device_id_for_events.clone()),
                            session_id: Some(session_id_for_events.clone()),
                            track_id: Some(track_id.to_id()),
                            uri: Some(track_id.to_uri()),
                            title: None,
                            artist: None,
                            album: None,
                            position_ms: Some(position_ms),
                            duration_ms: None,
                            volume: None,
                            error_code: None,
                            error_message: None,
                            metric_name: None,
                            metric_value_ms: None,
                            metric_message: None,
                            credentials_json: None,
                        },
                        PlayerEvent::Stopped { track_id, .. } => ConnectEvent {
                            r#type: "stopped".into(),
                            device_id: Some(device_id_for_events.clone()),
                            session_id: Some(session_id_for_events.clone()),
                            track_id: Some(track_id.to_id()),
                            uri: Some(track_id.to_uri()),
                            title: None,
                            artist: None,
                            album: None,
                            position_ms: None,
                            duration_ms: None,
                            volume: None,
                            error_code: None,
                            error_message: None,
                            metric_name: None,
                            metric_value_ms: None,
                            metric_message: None,
                            credentials_json: None,
                        },
                        PlayerEvent::EndOfTrack { track_id, .. } => {
                            if !saw_playing || last_duration_ms.is_none() {
                                continue;
                            }
                            ConnectEvent {
                                r#type: "end_of_track".into(),
                                device_id: Some(device_id_for_events.clone()),
                                session_id: Some(session_id_for_events.clone()),
                                track_id: Some(track_id.to_id()),
                                uri: Some(track_id.to_uri()),
                                title: None,
                                artist: None,
                                album: None,
                                position_ms: last_position_ms,
                                duration_ms: last_duration_ms,
                                volume: None,
                                error_code: None,
                                error_message: None,
                                metric_name: None,
                                metric_value_ms: None,
                                metric_message: None,
                                credentials_json: None,
                            }
                        }
                        PlayerEvent::Unavailable { track_id, .. } => ConnectEvent {
                            r#type: "unavailable".into(),
                            device_id: Some(device_id_for_events.clone()),
                            session_id: Some(session_id_for_events.clone()),
                            track_id: Some(track_id.to_id()),
                            uri: Some(track_id.to_uri()),
                            title: None,
                            artist: None,
                            album: None,
                            position_ms: None,
                            duration_ms: None,
                            volume: None,
                            error_code: None,
                            error_message: None,
                            metric_name: None,
                            metric_value_ms: None,
                            metric_message: None,
                            credentials_json: None,
                        },
                        PlayerEvent::VolumeChanged { volume } => ConnectEvent {
                            r#type: "volume".into(),
                            device_id: Some(device_id_for_events.clone()),
                            session_id: Some(session_id_for_events.clone()),
                            track_id: None,
                            uri: None,
                            title: None,
                            artist: None,
                            album: None,
                            position_ms: None,
                            duration_ms: None,
                            volume: Some(volume),
                            error_code: None,
                            error_message: None,
                            metric_name: None,
                            metric_value_ms: None,
                            metric_message: None,
                            credentials_json: None,
                        },
                        PlayerEvent::PositionCorrection {
                            track_id,
                            position_ms,
                            ..
                        } => ConnectEvent {
                            r#type: "position_correction".into(),
                            device_id: Some(device_id_for_events.clone()),
                            session_id: Some(session_id_for_events.clone()),
                            track_id: Some(track_id.to_id()),
                            uri: Some(track_id.to_uri()),
                            title: None,
                            artist: None,
                            album: None,
                            position_ms: Some(position_ms),
                            duration_ms: None,
                            volume: None,
                            error_code: None,
                            error_message: None,
                            metric_name: None,
                            metric_value_ms: None,
                            metric_message: None,
                            credentials_json: None,
                        },
                        _ => continue,
                    };

                    // Best-effort metadata enrichment.
                    if payload.title.is_none()
                        || payload.duration_ms.is_none()
                        || payload.uri.is_none()
                    {
                        let uri = payload.uri.clone().or_else(|| {
                            payload
                                .track_id
                                .clone()
                                .map(|tid| format!("spotify:track:{tid}"))
                        });
                        if let Some(uri) = uri {
                            if let Ok(spotify_uri) = SpotifyUri::from_uri(&uri) {
                                if let Ok(item) =
                                    AudioItem::get_file(&session_for_events, spotify_uri).await
                                {
                                    payload.title = Some(item.name);
                                    payload.duration_ms = Some(item.duration_ms);
                                    payload.uri = Some(item.uri);
                                }
                            }
                        }
                    }

                    // Enrich artist/album names if missing.
                    if payload.artist.is_none() || payload.album.is_none() {
                        let uri = payload.uri.clone().or_else(|| {
                            payload
                                .track_id
                                .clone()
                                .map(|tid| format!("spotify:track:{tid}"))
                        });
                        if let Some(uri) = uri {
                            if let Ok(spotify_uri) = SpotifyUri::from_uri(&uri) {
                                if let Ok(track_meta) =
                                    Track::get(&session_for_events, &spotify_uri).await
                                {
                                    if payload.album.is_none() {
                                        if let Ok(album_meta) =
                                            Album::get(&session_for_events, &track_meta.album.id)
                                                .await
                                        {
                                            payload.album = Some(album_meta.name);
                                        }
                                    }
                                    if payload.artist.is_none() {
                                        if let Some(first_artist) = track_meta.artists.first() {
                                            if let Ok(artist_meta) =
                                                Artist::get(&session_for_events, &first_artist.id)
                                                    .await
                                            {
                                                payload.artist = Some(artist_meta.name);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }

                    match payload.r#type.as_str() {
                        "playing" | "paused" | "position_correction" => {
                            saw_playing = true;
                            if let Some(pos) = payload.position_ms {
                                last_position_ms = Some(pos);
                            }
                            if let Some(dur) = payload.duration_ms {
                                last_duration_ms = Some(dur);
                            }
                        }
                        _ => {}
                    }

                    // [issue #252] Signal "audio is actually flowing" to the
                    // log observer. Excludes `paused` because a paused stream
                    // can't prove the dealer/session is healthy.
                    if matches!(payload.r#type.as_str(), "playing" | "position_correction") {
                        healthy_seq_for_events.fetch_add(1, Ordering::AcqRel);
                    }

                    let _ = tsfn_ev.call(payload, ThreadsafeFunctionCallMode::NonBlocking);
                }
            });
        }

        // Log observer: detect audio_key_error and decoder errors from librespot log output.
        // This mirrors the same pattern used in stream_track to ensure the JS layer receives
        // error events and can trigger recovery (e.g. re-authentication, skip to next track).
        if let Some(tsfn_ev) = event_tsfn.clone() {
            let mut log_rx = subscribe_log_events();
            let error_sent = Arc::new(AtomicBool::new(false));
            let decoder_metric_sent = Arc::new(AtomicBool::new(false));
            let stop_flag_for_logs = stop_flag_for_block.clone();
            let device_id_for_logs = device_id.clone();
            let session_id_for_logs = session_id.clone();
            let healthy_seq_for_logs = healthy_seq.clone();
            // [issue #252] Grace window before surfacing audio_key_error to JS.
            // Account-side Spotify dealer-disconnect bursts cause every
            // offload=false zone to emit a single audio-key-timeout log line,
            // while spirc reconnects internally within seconds. Without this
            // delay every burst trips the per-zone cooldown in
            // SpotifyConnectInstance and produces ~5 minutes of unplayable
            // state per zone. 5s is well above typical spirc recovery time
            // and well below the cooldown window.
            const AUDIO_KEY_GRACE_MS: u64 = 5000;
            runtime().spawn(async move {
                while let Some(event) = log_rx.recv().await {
                    if stop_flag_for_logs.load(Ordering::Acquire) {
                        break;
                    }
                    if is_decoder_error(&event) {
                        if !decoder_metric_sent.swap(true, Ordering::AcqRel) {
                            let payload = ConnectEvent {
                                r#type: "metric".into(),
                                device_id: Some(device_id_for_logs.clone()),
                                session_id: Some(session_id_for_logs.clone()),
                                track_id: None,
                                uri: None,
                                title: None,
                                artist: None,
                                album: None,
                                duration_ms: None,
                                position_ms: None,
                                volume: None,
                                error_code: None,
                                error_message: None,
                                metric_name: Some("decode_error".into()),
                                metric_value_ms: None,
                                metric_message: Some(event.message.clone()),
                                credentials_json: None,
                            };
                            let _ = tsfn_ev.call(payload, ThreadsafeFunctionCallMode::NonBlocking);
                        }
                    }
                    if error_sent.load(Ordering::Acquire) {
                        continue;
                    }
                    if !is_audio_key_error(&event) {
                        continue;
                    }
                    if error_sent.swap(true, Ordering::AcqRel) {
                        continue;
                    }
                    let payload = ConnectEvent {
                        r#type: "error".into(),
                        device_id: Some(device_id_for_logs.clone()),
                        session_id: Some(session_id_for_logs.clone()),
                        track_id: None,
                        uri: None,
                        title: None,
                        artist: None,
                        album: None,
                        duration_ms: None,
                        position_ms: None,
                        volume: None,
                        error_code: Some("audio_key_error".into()),
                        error_message: Some(event.message.clone()),
                        metric_name: None,
                        metric_value_ms: None,
                        metric_message: None,
                        credentials_json: None,
                    };
                    // [issue #252] Defer the emit by AUDIO_KEY_GRACE_MS and
                    // suppress it if a healthy event arrived in the meantime
                    // (spirc finished its internal reconnect). Re-arm
                    // `error_sent` on suppression so a *later* fault in the
                    // same session can still surface.
                    let baseline = healthy_seq_for_logs.load(Ordering::Acquire);
                    let healthy_seq_for_check = healthy_seq_for_logs.clone();
                    let stop_flag_for_grace = stop_flag_for_logs.clone();
                    let error_sent_for_reset = error_sent.clone();
                    let tsfn_ev_for_grace = tsfn_ev.clone();
                    runtime().spawn(async move {
                        tokio::time::sleep(Duration::from_millis(AUDIO_KEY_GRACE_MS)).await;
                        if stop_flag_for_grace.load(Ordering::Acquire) {
                            return;
                        }
                        if healthy_seq_for_check.load(Ordering::Acquire) != baseline {
                            error_sent_for_reset.store(false, Ordering::Release);
                            return;
                        }
                        let _ = tsfn_ev_for_grace
                            .call(payload, ThreadsafeFunctionCallMode::NonBlocking);
                    });
                }
            });
        }

        let (spirc, spirc_task) = Spirc::new(
            connect_config,
            session,
            credentials,
            player,
            Arc::new(mixer),
        )
        .await
        .map_err(|e| Error::from_reason(format!("spirc start failed: {e}")))?;

        // Launch mDNS Discovery so the device is visible to ALL Spotify users on the LAN,
        // not just the owner of the credentials used to start the connect host. This is the
        // same mechanism hardware devices (Sonos, Onkyo, etc.) use. When a different user
        // connects via mDNS, their credentials are emitted as a "credentials_changed" event
        // so the JS layer can restart the connect host under the new account.
        match Discovery::builder(device_id.clone(), client_id_for_discovery)
            .name(name.clone())
            .device_type(DeviceType::Speaker)
            .launch()
        {
            Ok(mut discovery) => {
                emit_log_ctx(
                    &log_tsfn_for_disc,
                    "info",
                    "mDNS discovery started; device is now visible to all Spotify users on the LAN",
                    Some("connect_host"),
                    Some(&device_id),
                    Some(&session_id),
                );
                let event_tsfn_disc = event_tsfn.clone();
                let log_tsfn_disc = log_tsfn_for_disc.clone();
                let stop_flag_disc = stop_flag_for_block.clone();
                let device_id_disc = device_id.clone();
                let session_id_disc = session_id.clone();
                runtime().spawn(async move {
                    loop {
                        if stop_flag_disc.load(Ordering::Acquire) {
                            break;
                        }
                        match tokio::time::timeout(
                            Duration::from_secs(2),
                            discovery.next(),
                        )
                        .await
                        {
                            Ok(None) => break,
                            Ok(Some(creds)) => {
                                match serde_json::to_string_pretty(&creds) {
                                    Ok(json) => {
                                        emit_log_ctx(
                                            &log_tsfn_disc,
                                            "info",
                                            "mDNS: new Spotify user connected; emitting credentials_changed",
                                            Some("connect_host"),
                                            Some(&device_id_disc),
                                            Some(&session_id_disc),
                                        );
                                        if let Some(ref tsfn) = event_tsfn_disc {
                                            let payload = ConnectEvent {
                                                r#type: "credentials_changed".into(),
                                                credentials_json: Some(json),
                                                device_id: Some(device_id_disc.clone()),
                                                session_id: Some(session_id_disc.clone()),
                                                track_id: None,
                                                uri: None,
                                                title: None,
                                                artist: None,
                                                album: None,
                                                duration_ms: None,
                                                position_ms: None,
                                                volume: None,
                                                error_code: None,
                                                error_message: None,
                                                metric_name: None,
                                                metric_value_ms: None,
                                                metric_message: None,
                                            };
                                            let _ = tsfn.call(payload, ThreadsafeFunctionCallMode::NonBlocking);
                                        }
                                        break;
                                    }
                                    Err(e) => {
                                        emit_log_ctx(
                                            &log_tsfn_disc,
                                            "warn",
                                            &format!("mDNS: failed to serialize new credentials: {e}"),
                                            Some("connect_host"),
                                            Some(&device_id_disc),
                                            Some(&session_id_disc),
                                        );
                                    }
                                }
                            }
                            Err(_timeout) => {
                                // No new connection in this window; loop and check stop flag.
                            }
                        }
                    }
                });
            }
            Err(e) => {
                emit_log_ctx(
                    &log_tsfn_for_disc,
                    "warn",
                    &format!("mDNS discovery failed to start (device only visible to owner account): {e}"),
                    Some("connect_host"),
                    Some(&device_id),
                    Some(&session_id),
                );
            }
        }

        let (stop_tx, mut stop_rx) = mpsc::channel::<()>(1);
        let task_handle = runtime().spawn(async move {
            tokio::select! {
              _ = spirc_task => {},
              _ = stop_rx.recv() => {},
            }
        });

        Ok(ConnectHandle {
            spirc,
            stop_tx: Some(stop_tx),
            tsfn,
            sample_rate: 44100,
            channels: 2,
            stop_flag,
            task: task_handle,
        })
    })
}

/// Legacy entrypoint removed from the JS API; kept for binary compatibility but always errors.
#[napi]
pub fn start_connect_device(
    _credentials_path: String,
    _name: String,
    _device_id: String,
    _on_chunk: JsFunction,
    _on_event: Option<JsFunction>,
    _on_log: Option<JsFunction>,
) -> Result<ConnectHandle> {
    Err(Error::from_reason(
        "startConnectDevice is deprecated; use startConnectDeviceWithToken(accessToken, clientId, ...)",
    ))
}

/// Start a Spotify Connect device using an existing credentials JSON blob (or a path to credentials.json).
///
/// This avoids exchanging a Web API token for credentials, and allows reusing credentials minted via
/// Zeroconf or other flows.
#[napi]
pub fn start_connect_device_with_credentials(
    credentials_path: String,
    name: String,
    device_id: String,
    on_chunk: JsFunction,
    on_event: Option<JsFunction>,
    on_log: Option<JsFunction>,
) -> Result<ConnectHandle> {
    if credentials_path.trim().is_empty() {
        return Err(Error::from_reason("credentials payload is required"));
    }
    start_connect_device_inner(
        credentials_path,
        name,
        device_id,
        on_chunk,
        on_event,
        on_log,
    )
}

/// Start a Spotify Connect device using a Web API access token + client id (bypasses builtin login).
#[napi]
pub fn start_connect_device_with_token(
    access_token: String,
    client_id: Option<String>,
    name: String,
    device_id: String,
    on_chunk: JsFunction,
    on_event: Option<JsFunction>,
    on_log: Option<JsFunction>,
) -> Result<ConnectHandle> {
    if access_token.trim().is_empty() {
        return Err(Error::from_reason("access token is required"));
    }
    if let Some(client_id) = client_id {
        if !client_id.trim().is_empty() {
            std::env::set_var("LOX_LIBRESPOT_CLIENT_ID", client_id);
        }
    }

    // Exchange the access token for reusable librespot credentials (same as login_with_access_token).
    let credentials_json = {
        let token = access_token.clone();
        let device_for_session = device_id.clone();
        runtime()
            .block_on(async move {
                let mut session_config = SessionConfig::default();
                session_config.device_id = device_for_session.clone();
                if let Ok(client_id_override) = std::env::var("LOX_LIBRESPOT_CLIENT_ID") {
                    if !client_id_override.trim().is_empty() {
                        session_config.client_id = client_id_override;
                    }
                }

                let credentials = Credentials::with_access_token(token);
                let epoch_ms = SystemTime::now()
                    .duration_since(UNIX_EPOCH)
                    .map_err(|e| Error::from_reason(format!("{e}")))?
                    .as_millis();
                let temp_dir = std::env::temp_dir().join(format!(
                    "lox-librespot-oauth-connect-{}-{}",
                    epoch_ms,
                    std::process::id()
                ));
                fs::create_dir_all(&temp_dir).map_err(|e| Error::from_reason(format!("{e}")))?;
                let cache = Cache::new(
                    Some(&temp_dir),
                    None::<&std::path::PathBuf>,
                    None::<&std::path::PathBuf>,
                    None,
                )
                .map_err(|e| Error::from_reason(format!("{e}")))?;

                let session = Session::new(session_config, Some(cache.clone()));
                session
                    .connect(credentials.clone(), true)
                    .await
                    .map_err(|e| Error::from_reason(format!("session connect failed: {e}")))?;

                let reusable_credentials = cache.credentials().ok_or_else(|| {
                    Error::from_reason("no reusable credentials after oauth login")
                })?;

                drop(session);
                let _ = fs::remove_dir_all(&temp_dir);

                serde_json::to_string(&reusable_credentials)
                    .map_err(|e| Error::from_reason(format!("{e}")))
            })
            .map_err(|e| Error::from_reason(format!("{e}")))?
    };

    start_connect_device_inner(
        credentials_json,
        name,
        device_id,
        on_chunk,
        on_event,
        on_log,
    )
}

/// Perform access-token login and return the credentials JSON blob.
#[napi]
pub async fn login_with_access_token(
    access_token: String,
    device_name: Option<String>,
) -> Result<CredentialsResult> {
    if access_token.trim().is_empty() {
        return Err(Error::from_reason("access token is required"));
    }

    let mut session_config = SessionConfig::default();
    if let Some(name) = device_name {
        session_config.device_id = name;
    }
    let credentials = Credentials::with_access_token(access_token);

    let epoch_ms = SystemTime::now()
        .duration_since(UNIX_EPOCH)
        .map_err(|e| Error::from_reason(format!("{e}")))?
        .as_millis();
    let temp_dir = std::env::temp_dir().join(format!(
        "lox-librespot-oauth-{}-{}",
        epoch_ms,
        std::process::id()
    ));
    fs::create_dir_all(&temp_dir).map_err(|e| Error::from_reason(format!("{e}")))?;
    let cache = Cache::new(
        Some(&temp_dir),
        None::<&std::path::PathBuf>,
        None::<&std::path::PathBuf>,
        None,
    )
    .map_err(|e| Error::from_reason(format!("{e}")))?;

    let session = Session::new(session_config, Some(cache.clone()));
    session
        .connect(credentials.clone(), true)
        .await
        .map_err(|e| Error::from_reason(format!("session connect failed: {e}")))?;

    let reusable_credentials = cache
        .credentials()
        .ok_or_else(|| Error::from_reason("no reusable credentials after oauth login"))?;

    // Explicitly drop session to stop background tasks after acquiring credentials.
    drop(session);
    let _ = fs::remove_dir_all(&temp_dir);

    let credentials_json = serde_json::to_string_pretty(&reusable_credentials)
        .map_err(|e| Error::from_reason(format!("{e}")))?;

    Ok(CredentialsResult {
        username: reusable_credentials.username.clone().unwrap_or_default(),
        credentials_json,
    })
}

/// Start a Zeroconf login and return the credentials blob once a user connects in the Spotify app.
#[napi]
pub async fn start_zeroconf_login(
    device_id: String,
    name: Option<String>,
    timeout_ms: Option<u32>,
) -> Result<CredentialsResult> {
    let device_id = device_id.trim();
    if device_id.is_empty() {
        return Err(Error::from_reason("device_id is required"));
    }
    let mut discovery = Discovery::builder(device_id.to_string(), device_id.to_string())
        .name(name.unwrap_or_else(|| "LoxAudio".into()))
        .device_type(DeviceType::Speaker)
        .launch()
        .map_err(|e| Error::from_reason(format!("zeroconf launch failed: {e}")))?;

    let wait_for = timeout_ms.unwrap_or(120_000); // default 120s
    let creds = timeout(Duration::from_millis(wait_for as u64), async {
        discovery.next().await
    })
    .await
    .map_err(|_| Error::from_reason("zeroconf login timed out"))?
    .ok_or_else(|| Error::from_reason("zeroconf ended without credentials"))?;

    let credentials_json =
        serde_json::to_string_pretty(&creds).map_err(|e| Error::from_reason(format!("{e}")))?;

    Ok(CredentialsResult {
        username: creds.username.clone().unwrap_or_default(),
        credentials_json,
    })
}

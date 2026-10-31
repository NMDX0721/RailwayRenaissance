/* Auto-generated from the addon shape; keep in sync with N-API exports. */

/** Options used to create a librespot session. */
export interface CreateSessionOpts {
  accessToken?: string;
  clientId?: string;
  deviceName?: string;
  /** Directory for the audio file cache.  When set, decoded audio is stored so
   *  subsequent plays of the same track skip the CDN download. */
  cacheDir?: string;
  /** Maximum size of the audio cache in megabytes.  Only meaningful when
   *  `cacheDir` is set.  Omit for no limit. */
  cacheSizeLimitMb?: number;
}

/** Options for streaming a track. */
export interface StreamTrackOpts {
  uri: string;
  startPositionMs?: number;
  bitrate?: number;
  output?: string;
  emitEvents?: boolean;
}

/** Options for downloading raw (decrypted) audio bytes. */
export interface DownloadTrackOpts {
  uri: string;
  bitrate?: number;
}

/** Result of resolving a track's CDN location + decryption key (no download). */
export interface ResolveAudioFileResult {
  /** Signed, expiring CDN URL for the encrypted audio file (GET with Range). */
  cdnUrl: string;
  /** 16-byte AES-128 audio key, hex-encoded (lowercase, 32 chars). */
  keyHex: string;
  /** Chosen Spotify audio format, e.g. "OGG_VORBIS_320" or "MP3_320". */
  format: string;
}

/**
 * First-party session tokens for direct pathfinder/spclient calls from the app.
 * Both are refreshed/cached internally by librespot.
 */
export interface SessionTokens {
  accessToken: string;
  tokenType: string;
  clientToken: string;
  /** Access-token lifetime in milliseconds. */
  expiresInMs: number;
}

/** Result of a credentials login flow. */
export interface CredentialsResult {
  username: string;
  credentialsJson: string;
  credentials_json?: string;
}

/** Helper signature for OAuth-based credentials minting. */
export type LoginWithAccessToken = (
  accessToken: string,
  deviceName?: string,
) => Promise<CredentialsResult>;

export type LibrespotEventType =
  | 'playing'
  | 'paused'
  | 'loading'
  | 'stopped'
  | 'end_of_track'
  | 'unavailable'
  | 'volume'
  | 'position_correction'
  | 'health'
  | 'error'
  | 'metric'
  | 'preloading'
  | 'time_to_preload'
  | 'play_request_id'
  | 'credentials_changed'
  | 'other';

export type LibrespotErrorCode =
  | 'audio_key_error'
  | 'no_pcm'
  | 'end_of_track'
  | 'pcm_missing'
  | 'pcm_stalled'
  | 'pcm_ok'
  | 'unavailable'
  | 'unknown';

/** Event payload emitted by the connect host. */
export interface ConnectEvent {
  type: LibrespotEventType;
  deviceId?: string;
  sessionId?: string;
  trackId?: string;
  uri?: string;
  title?: string;
  artist?: string;
  album?: string;
  durationMs?: number;
  positionMs?: number;
  volume?: number;
  errorCode?: LibrespotErrorCode;
  errorMessage?: string;
  metricName?: string;
  metricValueMs?: number;
  metricMessage?: string;
  credentialsJson?: string;
}

/** Log payload emitted by the native module. */
export interface LogEvent {
  level: string;
  message: string;
  scope?: string;
  deviceId?: string;
  sessionId?: string;
}

/** Handle returned when hosting a connect device. */
export interface ConnectHandle {
  stop(): void;
  shutdown(): void;
  close(): void;
  play(): void;
  pause(): void;
  next(): void;
  prev(): void;
  sampleRate: number;
  channels: number;
}

/** Handle to a librespot session. */
export interface LibrespotSession {
  streamTrack(
    opts: StreamTrackOpts,
    onChunk: (chunk: Buffer) => void,
    onEvent?: (event: ConnectEvent) => void,
    onLog?: (event: LogEvent) => void,
  ): StreamHandle;
  /** Resolve CDN URL + AES key for a track without downloading audio. */
  resolveAudioFile(opts: DownloadTrackOpts): ResolveAudioFileResult;
  /**
   * Async variant of {@link resolveAudioFile}: the blocking CDN/key lookup runs
   * on the libuv threadpool, so it never stalls the Node event loop. Prefer this
   * on the playback hot path.
   */
  resolveAudioFileAsync(opts: DownloadTrackOpts): Promise<ResolveAudioFileResult>;
  /**
   * Mint the first-party session tokens (login5 bearer + client-token) so the
   * app can call pathfinder/spclient itself. Both are refreshed/cached by
   * librespot; call whenever a fresh pair is needed.
   */
  getTokens(): Promise<SessionTokens>;
  close(): Promise<void>;
}

export interface StreamHandle {
  stop(): void;
  sampleRate: number;
  channels: number;
}

/** Handle for a raw download stream. */
export interface DownloadHandle {
  stop(): void;
}

export declare function setLogLevel(level: string): void;

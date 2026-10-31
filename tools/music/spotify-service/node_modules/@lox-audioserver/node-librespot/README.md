# node-librespot

Native Node.js bindings for [librespot](https://github.com/librespot-org/librespot) (Spotify Connect) using N-API. Streams PCM directly to JavaScript without spawning the librespot binary.

Used by [lox-audioserver](https://github.com/rudyberends/lox-audioserver) to handle spotify traffic, but can be used by other software.

## Features
- Stream a Spotify track/episode to PCM buffers (`streamTrack`) using a Web API **access token**.
- Pull raw decrypted audio bytes (Ogg/MP3) for a track/episode without decoding (`downloadTrack`).
- Resolve a track's signed CDN URL + AES audio key without downloading, so the caller can fetch (HTTP Range) and decrypt itself (`resolveAudioFile`, or `resolveAudioFileAsync` to run the blocking lookup off the Node event loop).
- Mint first-party session tokens — the login5 bearer + client-token (`getTokens`) — so the consumer can call Spotify's own endpoints (e.g. pathfinder) itself for browse/metadata, independent of the Web API. Both are refreshed/cached internally.
- Host a Spotify Connect endpoint with a Web API access token + your own Spotify app client id (`startConnectDeviceWithToken`).
- No disk cache required; everything stays in-memory.
- Starts Connect hosts at max volume (volume control stays on the consumer side).
- `startConnectDevice` (credential-blob based) is deprecated and will error.

## Install
```bash
npm install
```

## Build
Requires Rust (stable) and `@napi-rs/cli` (installed via devDependencies). The
`librespot-*` crates are pulled as pinned git dependencies (a fixed upstream
commit), so a from-source build works on a fresh clone with no local checkout.
```bash
npm run build          # release build
# or
npm run build:debug    # debug build
# or (for publishing) generate host prebuild + JS/types
npm run build:prebuild
```

Runtime will load `librespot_addon.node` from `prebuilds/<platform-arch>/` when available, otherwise it falls back to `LOX_LIBRESPOT_ADDON_PATH` or the locally compiled `dist/librespot_addon.node`.

## Usage
```ts
import { createSession, setLogLevel } from 'node-librespot';

setLogLevel('debug'); // off|error|warn|info|debug|trace

const session = await createSession({
  accessToken,          // user Web API access token (PKCE/Authorization Code)
  clientId,             // your Spotify app client id
  deviceName: 'lox-zone-1',
});

const handle = session.streamTrack(
  { uri: 'spotify:track:...', bitrate: 320 },
  (chunk) => {
    // chunk is a Buffer with PCM s16le frames (44.1kHz stereo)
  },
  (event) => {
    // optional events: playing/paused/loading/stopped/end_of_track/error/health/metric
    // error codes include: audio_key_error, no_pcm, end_of_track, pcm_missing, pcm_stalled, unavailable
    // metric names include: first_pcm_ms, buffer_stall_ms, decode_error
  },
  (log) => {
    // optional logging callback { level, message, scope }
  },
);

// Host a Connect device with the same token
import { startConnectDeviceWithToken } from 'node-librespot';

const host = await startConnectDeviceWithToken(
  accessToken,
  clientId,
  'Lox-AudioServer', // publish name
  'lox-zone-1',      // device id
  (chunk) => { /* PCM s16le frames */ },
  (event) => { /* playing/paused/loading/end_of_track/error/health/metric with metadata */ },
  (log) => { /* optional log callback */ },
);

host.play();
host.pause();
host.next();
host.prev();
host.stop();
host.shutdown(); // alias for stop()
host.close();    // alias for stop()

// Or pull decrypted audio file bytes (Ogg/MP3) without decoding:
const download = await downloadTrack(
  { uri: 'spotify:track:...', bitrate: 320 },
  (chunk) => {
    // chunk is a Buffer with decrypted compressed audio data
  },
  (log) => {
    // optional logging callback { level, message, scope }
  },
);
// download.stop() to cancel
// Promise rejects on initial errors (invalid URI, unavailable track, key/file fetch failure).

// Or resolve the CDN url + AES key without downloading (you fetch + decrypt yourself):
const { cdnUrl, keyHex, format } = session.resolveAudioFile({ uri: 'spotify:track:...', bitrate: 320 });
// - cdnUrl: signed, expiring CDN url for the encrypted file (GET with Range)
// - keyHex: 16-byte AES-128 key, hex-encoded
// - format: e.g. "OGG_VORBIS_320" or "MP3_320"
// Decrypt with AES-128-CTR using the fixed Spotify audio IV (counter = byteOffset / 16).
// For OGG, strip up to the first 'OggS' page (Spotify prepends a ~167-byte header).
// resolveAudioFileAsync({ uri, bitrate }) is the same but returns a Promise and runs
// the blocking CDN/key lookup on the libuv threadpool (won't stall the event loop).

handle.stop();

// Mint first-party tokens to call Spotify's own endpoints (e.g. pathfinder) yourself:
const { accessToken, tokenType, clientToken, expiresInMs } = await session.getTokens();
// e.g. fetch('https://api-partner.spotify.com/pathfinder/v1/query', {
//   headers: { Authorization: `${tokenType} ${accessToken}`, 'client-token': clientToken } })
```

## Authentication
- Only Web API access tokens are supported (bring your own PKCE/Authorization Code flow).
- Supply your app’s client id via `clientId` or `LOX_LIBRESPOT_CLIENT_ID` env var.
- `startConnectDevice` is removed; use `startConnectDeviceWithToken` instead.

If the token is invalid/expired, playback/Connect hosting will fail; refresh the token in your host app and retry.

## Development / publish
- CI: `.github/workflows/prebuild.yml` runs on release creation, building prebuild artifacts for Linux x64 (glibc), Linux arm64 (glibc), and macOS arm64 before publishing to npm (requires `NPM_TOKEN` secret).
- Local publish flow: `npm run build:prebuild` to generate host prebuild + JS/types, then `npm publish`.
- Install from source: `npm install` will try to use an included prebuild; if none matches the host it will build from source automatically (set `LOX_LIBRESPOT_SKIP_BUILD=1` to skip).

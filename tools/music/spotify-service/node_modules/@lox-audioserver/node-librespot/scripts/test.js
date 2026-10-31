#!/usr/bin/env node

const fs = require('node:fs');
const path = require('node:path');

const ROOT = path.resolve(__dirname, '..');
const DIST_ENTRY = path.join(ROOT, 'dist', 'index.js');

function loadLibrary() {
  try {
    return require(DIST_ENTRY);
  } catch (err) {
    console.error(
      'Kan dist/index.js niet laden. Draai eerst `npm run build:debug` of `npm run build` zodat het native addon beschikbaar is.',
    );
    throw err;
  }
}

const { createSession, loginWithAccessToken, setLogLevel } = loadLibrary();

function readCredentials(filePath) {
  const resolved = path.resolve(filePath);
  try {
    return fs.readFileSync(resolved, 'utf8');
  } catch (err) {
    throw new Error(`Kon credentials niet lezen van ${resolved}: ${err.message}`);
  }
}

async function resolveCredentials(deviceName) {
  if (process.env.SPOTIFY_CREDENTIALS_JSON) {
    return process.env.SPOTIFY_CREDENTIALS_JSON;
  }

  if (process.env.SPOTIFY_CREDENTIALS_PATH) {
    return readCredentials(process.env.SPOTIFY_CREDENTIALS_PATH);
  }

  if (process.env.SPOTIFY_ACCESS_TOKEN) {
    console.info('Maak credentials aan via SPOTIFY_ACCESS_TOKEN...');
    const res = await loginWithAccessToken(process.env.SPOTIFY_ACCESS_TOKEN, deviceName);
    console.info(`Gebruiker: ${res.username}`);
    return res.credentialsJson;
  }

  throw new Error(
    'Geef credentials door via SPOTIFY_CREDENTIALS_JSON, SPOTIFY_CREDENTIALS_PATH of SPOTIFY_ACCESS_TOKEN.',
  );
}

async function main() {
  const trackUri = process.env.SPOTIFY_TRACK_URI;
  if (!trackUri) {
    throw new Error('Zet SPOTIFY_TRACK_URI op een Spotify track/episode URI om te testen.');
  }

  const deviceName = process.env.SPOTIFY_DEVICE_NAME || 'node-librespot-test';
  const bitrate = Number(process.env.SPOTIFY_BITRATE) || 160;
  const durationMs = Number(process.env.SPOTIFY_TEST_DURATION_MS) || 5000;

  setLogLevel(process.env.SPOTIFY_LOG_LEVEL || 'info');

  const credentialsJson = await resolveCredentials(deviceName);
  console.info('Sessies opzetten...');
  const session = await createSession({ credentialsJson, deviceName });
  console.info(`Stream ${trackUri} op ${bitrate} kbps voor ~${durationMs}ms`);

  let chunkCount = 0;
  let bytes = 0;
  const streamHandle = session.streamTrack(
    { uri: trackUri, bitrate, emitEvents: true },
    (chunk) => {
      chunkCount += 1;
      bytes += chunk.length;
    },
    (event) => {
      if (event.type === 'error') {
        console.error('Event (error):', event);
      } else if (event.type === 'metric' || event.type === 'health') {
        console.info('Event:', event);
      }
    },
    (log) => {
      if (log.level === 'error' || log.level === 'warn') {
        console.info('Log:', log);
      }
    },
  );

  const stop = async () => {
    console.info(
      `Stop na ${durationMs}ms; ${chunkCount} chunks ontvangen (~${(bytes / 1024).toFixed(1)} KiB).`,
    );
    streamHandle.stop();
    await session.close();
    console.info('Klaar.');
  };

  const timeout = setTimeout(stop, durationMs);

  process.on('SIGINT', async () => {
    clearTimeout(timeout);
    await stop();
    process.exit(1);
  });
}

main().catch((err) => {
  console.error('Test mislukt:', err);
  process.exit(1);
});

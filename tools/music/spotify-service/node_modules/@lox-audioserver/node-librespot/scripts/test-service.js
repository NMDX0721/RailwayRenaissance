#!/usr/bin/env node

const fs = require('node:fs');
const path = require('node:path');

const ROOT = path.resolve(__dirname, '..');
const DIST_ENTRY = path.join(ROOT, 'dist', 'index.js');

function loadLibrary() {
  try {
    // eslint-disable-next-line import/no-dynamic-require, global-require
    return require(DIST_ENTRY);
  } catch (err) {
    console.error(
      'Kan dist/index.js niet laden. Draai eerst `npm run build:debug` of `npm run build` zodat het native addon beschikbaar is.',
    );
    throw err;
  }
}

const { startConnectDevice, createSession, loginWithAccessToken, setLogLevel } = loadLibrary();

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

function scheduleRestart(attempt, fn) {
  const restartBackoffMs = [2000, 4000, 8000];
  const delay = restartBackoffMs[Math.min(attempt, restartBackoffMs.length - 1)];
  console.info(`Herstart Connect host over ${delay}ms (poging ${attempt + 1})`);
  setTimeout(fn, delay);
}

async function runConnectHost(credentialsJson, deviceName, deviceId, durationMs) {
  let handle = null;
  let chunkCount = 0;
  let bytes = 0;
  let events = 0;
  let restartAttempt = 0;
  let stopping = false;

  const start = async () => {
    console.info(`Start Spotify Connect host voor "${deviceName}" (${deviceId})`);
    handle = await startConnectDevice(
      credentialsJson,
      deviceName,
      deviceId,
      (chunk) => {
        chunkCount += 1;
        bytes += chunk.length;
      },
      (event) => {
        events += 1;
        if (event.type === 'error') {
          console.error('Event (error):', event);
          if (!stopping) {
            handle.stop();
            handle = null;
            scheduleRestart(restartAttempt, start);
            restartAttempt += 1;
          }
          return;
        }
        if (event.type !== 'metric' && event.type !== 'health') {
          console.info('Event:', event);
        }
      },
      (log) => {
        if (log.level === 'error' || log.level === 'warn') {
          console.info('Log:', log);
        }
      },
    );
    restartAttempt = 0;
  };

  await start();

  const stop = async (reason) => {
    if (stopping) {
      return;
    }
    stopping = true;
    if (handle) {
      try {
        handle.stop();
        handle.shutdown?.();
        handle.close?.();
      } catch {
        /* ignore */
      }
    }
    console.info(
      `${reason}; ${chunkCount} chunks ontvangen (~${(bytes / 1024).toFixed(
        1,
      )} KiB), ${events} events.`,
    );
  };

  const timeout = setTimeout(() => stop(`Stop na ${durationMs}ms`), durationMs);

  process.on('SIGINT', async () => {
    clearTimeout(timeout);
    await stop('Stop door SIGINT');
    process.exit(1);
  });
}

async function runDirectStream(credentialsJson, deviceName, trackUri, durationMs) {
  const bitrate = Number(process.env.SPOTIFY_BITRATE) || 160;
  console.info(`Maak directe stream voor ${trackUri} op ${bitrate} kbps`);
  const session = await createSession({ credentialsJson, deviceName });
  let chunkCount = 0;
  let bytes = 0;

  const handle = session.streamTrack(
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

  const stop = async (reason) => {
    console.info(
      `${reason}; ${chunkCount} chunks ontvangen (~${(bytes / 1024).toFixed(1)} KiB).`,
    );
    handle.stop();
    await session.close();
  };

  const timeout = setTimeout(() => stop(`Stop na ${durationMs}ms`), durationMs);
  process.on('SIGINT', async () => {
    clearTimeout(timeout);
    await stop('Stop door SIGINT');
    process.exit(1);
  });
}

async function main() {
  const deviceName = process.env.SPOTIFY_DEVICE_NAME || 'node-librespot-service';
  const deviceId = process.env.SPOTIFY_DEVICE_ID || 'node-librespot-service-id';
  const durationMs =
    Number(process.env.SPOTIFY_SERVICE_DURATION_MS || process.env.SPOTIFY_TEST_DURATION_MS) ||
    30000;
  const trackUri = process.env.SPOTIFY_TRACK_URI;
  const mode = process.env.SPOTIFY_SERVICE_MODE || (trackUri ? 'stream' : 'connect');

  setLogLevel(process.env.SPOTIFY_LOG_LEVEL || 'info');

  const credentialsJson = await resolveCredentials(deviceName);

  if (mode === 'stream') {
    if (!trackUri) {
      throw new Error('Zet SPOTIFY_TRACK_URI voor stream mode.');
    }
    await runDirectStream(credentialsJson, deviceName, trackUri, durationMs);
    return;
  }

  await runConnectHost(credentialsJson, deviceName, deviceId, durationMs);
}

main().catch((err) => {
  console.error('Service test mislukt:', err);
  process.exit(1);
});

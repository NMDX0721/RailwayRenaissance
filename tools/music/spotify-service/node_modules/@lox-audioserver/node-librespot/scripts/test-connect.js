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

const { startConnectDevice, loginWithAccessToken, setLogLevel } = loadLibrary();

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
  const deviceName = process.env.SPOTIFY_DEVICE_NAME || 'node-librespot-connect';
  const deviceId = process.env.SPOTIFY_DEVICE_ID || 'node-librespot-connect-id';
  const durationMs =
    Number(process.env.SPOTIFY_CONNECT_DURATION_MS || process.env.SPOTIFY_TEST_DURATION_MS) ||
    30000;

  setLogLevel(process.env.SPOTIFY_LOG_LEVEL || 'info');

  const credentialsJson = await resolveCredentials(deviceName);
  console.info(`Start Spotify Connect host voor "${deviceName}" (${deviceId})`);

  let chunkCount = 0;
  let bytes = 0;
  let events = 0;

  const handle = await startConnectDevice(
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
      } else if (event.type !== 'metric' && event.type !== 'health') {
        console.info('Event:', event);
      }
    },
    (log) => {
      if (log.level === 'error' || log.level === 'warn') {
        console.info('Log:', log);
      }
    },
  );

  const stop = () => {
    console.info(
      `Stop Connect host na ~${durationMs}ms; ${chunkCount} chunks ontvangen (~${(bytes / 1024).toFixed(1)} KiB), ${events} events.`,
    );
    handle.stop();
    handle.shutdown?.();
    handle.close?.();
  };

  const timeout = setTimeout(stop, durationMs);

  process.on('SIGINT', () => {
    clearTimeout(timeout);
    stop();
    process.exit(1);
  });
}

main().catch((err) => {
  console.error('Connect test mislukt:', err);
  process.exit(1);
});

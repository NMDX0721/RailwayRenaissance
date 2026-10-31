const fs = require('node:fs');
const path = require('node:path');
const { spawnSync } = require('node:child_process');

const ROOT = path.resolve(__dirname, '..');
const ADDON_NAME = 'librespot_addon.node';

function detectLibc() {
  const glibcVersionRuntime =
    // @ts-expect-error
    process.report?.getReport?.()?.header?.glibcVersionRuntime;
  return glibcVersionRuntime ? 'gnu' : 'musl';
}

function platformArchABI() {
  const { platform, arch } = process;
  if (platform === 'linux') return `linux-${arch}-${detectLibc()}`;
  if (platform === 'darwin') return `darwin-${arch}`;
  if (platform === 'win32') return `win32-${arch}-msvc`;
  return `${platform}-${arch}`;
}

function resolveAddonPaths() {
  const prebuildPath = path.join(ROOT, 'prebuilds', platformArchABI(), ADDON_NAME);
  const distPath = path.join(ROOT, 'dist', ADDON_NAME);
  return { prebuildPath, distPath };
}

function addonExists() {
  const { prebuildPath, distPath } = resolveAddonPaths();
  return fs.existsSync(prebuildPath) || fs.existsSync(distPath);
}

function buildNative() {
  const res = spawnSync('npm', ['run', 'build'], {
    stdio: 'inherit',
    cwd: ROOT,
    env: process.env,
  });
  if (res.status !== 0) {
    throw new Error(`npm run build failed with status ${res.status ?? 'unknown'}`);
  }
}

function main() {
  if (process.env.LOX_LIBRESPOT_SKIP_BUILD) {
    console.warn('node-librespot: skipping native build because LOX_LIBRESPOT_SKIP_BUILD is set.');
    return;
  }

  if (addonExists()) {
    return;
  }

  console.info('node-librespot: no prebuild found for this platform, building from source...');
  buildNative();
}

main();

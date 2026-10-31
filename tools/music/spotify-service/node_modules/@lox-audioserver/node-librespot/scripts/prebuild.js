const { execSync } = require('node:child_process');
const path = require('node:path');
const { preparePrebuilds } = require('./prepare-prebuilds');

const run = (cmd, env = {}) =>
  execSync(cmd, {
    stdio: 'inherit',
    env: { ...process.env, ...env },
  });

run('npm run clean');

const args = ['napi', 'build', '--release', '--platform', '--strip'];
if (process.env.USE_ZIG === '1') {
  args.push('--zig');
}
if (process.env.RUST_TARGET) {
  args.push('--target', process.env.RUST_TARGET);
}

run(args.join(' '));
preparePrebuilds(path.resolve(process.cwd()));
run('npm run build:types');

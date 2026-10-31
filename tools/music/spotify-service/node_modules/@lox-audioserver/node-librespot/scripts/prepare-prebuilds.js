const fs = require('node:fs');
const path = require('node:path');

const BINARY_NAME = 'librespot_addon';

function preparePrebuilds(cwd = process.cwd()) {
  const files = fs
    .readdirSync(cwd)
    .filter((file) => file.startsWith(`${BINARY_NAME}.`) && file.endsWith('.node'));

  const moved = [];

  for (const file of files) {
    if (file === `${BINARY_NAME}.node`) {
      continue;
    }

    const suffix = file.slice(BINARY_NAME.length + 1, -'.node'.length);
    if (!suffix) {
      continue;
    }

    const destDir = path.join(cwd, 'prebuilds', suffix);
    fs.mkdirSync(destDir, { recursive: true });

    const sourcePath = path.join(cwd, file);
    const destPath = path.join(destDir, `${BINARY_NAME}.node`);
    fs.renameSync(sourcePath, destPath);

    moved.push(path.relative(cwd, destPath));
  }

  if (!moved.length) {
    throw new Error(`No prebuild outputs found for ${BINARY_NAME}. Run "napi build --platform --release" first.`);
  }

  console.log(`Prebuilds ready:\n${moved.map((p) => ` - ${p}`).join('\n')}`);
}

if (require.main === module) {
  preparePrebuilds();
}

module.exports = { preparePrebuilds };

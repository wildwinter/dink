const fs = require('fs');
const path = require('path');

const version = process.env.npm_package_version || require('./package.json').version;
const distDir = path.join(__dirname, 'dist', `v${version}`);
const srcDir = path.join(__dirname, 'csharp', 'dist');

if (fs.existsSync(distDir)) fs.rmSync(distDir, { recursive: true, force: true });
fs.mkdirSync(distDir, { recursive: true });

for (const f of fs.readdirSync(srcDir).filter(f => f.endsWith('.zip'))) {
    fs.copyFileSync(path.join(srcDir, f), path.join(distDir, f));
    console.log(`Copied ${f} -> dist/v${version}/`);
}

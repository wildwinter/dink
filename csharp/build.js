const { execSync } = require('child_process');
const fs = require('fs');
const path = require('path');

const version = process.env.npm_package_version;
if (!version) { console.error('npm_package_version not set'); process.exit(1); }

const distDir = path.join(__dirname, 'dist');
const rootDir = path.join(__dirname, '..');
const targets = ['osx-arm64', 'osx-x64', 'win-x86', 'win-x64'];
const tools = ['DinkCompiler', 'DinkVoiceExport', 'DinkViewer'];

function zip(sourceDir, outputZip) {
    if (process.platform === 'win32') {
        execSync(`powershell -NoProfile -Command "Compress-Archive -Path '${path.join(sourceDir, '*')}' -DestinationPath '${outputZip}' -Force"`, { stdio: 'inherit' });
    } else {
        execSync(`zip -r "${outputZip}" .`, { cwd: sourceDir, stdio: 'inherit' });
    }
}

if (fs.existsSync(distDir)) fs.rmSync(distDir, { recursive: true, force: true });

for (const target of targets) {
    const targetDir = path.join(distDir, target);

    for (const tool of tools) {
        console.log(`Publishing ${tool} for ${target}...`);
        execSync(`dotnet publish -c Release -r ${target} -o ../dist/${target}`, {
            cwd: path.join(__dirname, tool),
            stdio: 'inherit',
        });
    }

    for (const file of fs.readdirSync(targetDir)) {
        if (file.endsWith('.pdb')) fs.rmSync(path.join(targetDir, file));
    }

    fs.copyFileSync(path.join(rootDir, 'LICENSE'), path.join(targetDir, 'LICENSE'));
    fs.copyFileSync(path.join(rootDir, 'README.md'), path.join(targetDir, 'README.md'));

    const appleId = process.env.APPLE_CODESIGN_ID || '';
    if (target.startsWith('osx-') && appleId) {
        console.log(`Signing macOS binaries for ${target}...`);
        for (const binary of tools) {
            execSync(`codesign --sign "${appleId}" --entitlements entitlements.plist --options runtime --timestamp --force "./dist/${target}/${binary}"`, { cwd: __dirname, stdio: 'inherit' });
        }
        for (const binary of tools) {
            execSync(`codesign --verify --strict "./dist/${target}/${binary}"`, { cwd: __dirname, stdio: 'inherit' });
        }
    }

    const zipPath = path.join(distDir, `Dink-${target}-${version}.zip`);
    zip(targetDir, zipPath);
    console.log(`Created: Dink-${target}-${version}.zip`);
}

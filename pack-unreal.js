const { execSync } = require('child_process');
const fs = require('fs');
const path = require('path');

const version = process.env.npm_package_version || require('./package.json').version;
const distDir = path.join(__dirname, 'dist', `v${version}`);
const unrealDir = path.join(__dirname, 'unreal');
const tempDir = path.join(__dirname, 'dist', 'unreal_tmp');
const platforms = ['osx-arm64', 'win-x64'];

function zip(sourceDir, outputZip) {
    if (process.platform === 'win32') {
        execSync(`powershell -NoProfile -Command "Compress-Archive -Path '${path.join(sourceDir, '*')}' -DestinationPath '${outputZip}' -Force"`, { stdio: 'inherit' });
    } else {
        execSync(`zip -r "${outputZip}" .`, { cwd: sourceDir, stdio: 'inherit' });
    }
}

function unzip(zipFile, destDir) {
    if (process.platform === 'win32') {
        execSync(`powershell -NoProfile -Command "Expand-Archive -Path '${zipFile}' -DestinationPath '${destDir}' -Force"`, { stdio: 'inherit' });
    } else {
        execSync(`unzip -q "${zipFile}" -d "${destDir}"`, { stdio: 'inherit' });
    }
}

const upluginPath = path.join(unrealDir, 'Dink', 'Dink.uplugin');
const [major, minor, patch] = version.split('.').map(Number);
const versionInt = major * 10000 + minor * 100 + patch;
console.log(`Updating Dink.uplugin to Version=${versionInt}, VersionName=${version}...`);
let uplugin = fs.readFileSync(upluginPath, 'utf8');
uplugin = uplugin.replace(/"Version": \d+/, `"Version": ${versionInt}`);
uplugin = uplugin.replace(/"VersionName": "[^"]*"/, `"VersionName": "${version}"`);
fs.writeFileSync(upluginPath, uplugin);

for (const platform of platforms) {
    console.log(`Creating Unreal zip for ${platform}...`);

    const platformTemp = path.join(tempDir, platform);
    if (fs.existsSync(platformTemp)) fs.rmSync(platformTemp, { recursive: true, force: true });
    fs.mkdirSync(platformTemp, { recursive: true });

    fs.cpSync(unrealDir, platformTemp, { recursive: true });

    const thirdPartyDir = path.join(platformTemp, 'Dink', 'ThirdParty', 'Dink');
    fs.mkdirSync(thirdPartyDir, { recursive: true });

    const platformZip = path.join(distDir, `Dink-${platform}-${version}.zip`);
    if (!fs.existsSync(platformZip)) {
        console.error(`Error: Platform zip not found: ${platformZip}`);
        process.exit(1);
    }

    unzip(platformZip, thirdPartyDir);

    const outputZip = path.join(distDir, `Dink-unreal-${platform}-${version}.zip`);
    zip(platformTemp, outputZip);
    console.log(`Created: ${path.basename(outputZip)}`);
}

fs.rmSync(tempDir, { recursive: true, force: true });
console.log('Unreal zip creation complete.');

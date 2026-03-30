#!/usr/bin/env node
// Syncs the version in all .csproj files to match the root package.json version.

const fs = require('fs');
const path = require('path');

const rootPackage = JSON.parse(fs.readFileSync(path.join(__dirname, '..', 'package.json'), 'utf8'));
const version = rootPackage.version;

function findCsproj(dir) {
    const results = [];
    for (const entry of fs.readdirSync(dir, { withFileTypes: true })) {
        const full = path.join(dir, entry.name);
        if (entry.isDirectory()) {
            results.push(...findCsproj(full));
        } else if (entry.name.endsWith('.csproj')) {
            results.push(full);
        }
    }
    return results;
}

const csprojFiles = findCsproj(__dirname);
for (const file of csprojFiles) {
    let content = fs.readFileSync(file, 'utf8');
    const updated = content.replace(/<Version>.*?<\/Version>/, `<Version>${version}</Version>`);
    if (updated !== content) {
        fs.writeFileSync(file, updated, 'utf8');
        console.log(`Updated version to ${version} in ${path.relative(__dirname, file)}`);
    }
}

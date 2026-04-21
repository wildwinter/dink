// Usage: dotnet package search <pkg> --exact-match --format json | node upgrade-package.js <pkg> <file1> [file2...]
const pkg = process.argv[2];
const files = process.argv.slice(3);
const fs = require('fs');
let input = '';
process.stdin.on('data', d => input += d);
process.stdin.on('end', () => {
  const d = JSON.parse(input);
  const source = d.searchResult.find(s => s.packages && s.packages.length > 0);
  if (!source) { console.error('No packages found for ' + pkg); process.exit(1); }
  const pkgs = source.packages;
  const v = pkgs[pkgs.length - 1].version;
  const escaped = pkg.replace(/\./g, '\\.');
  const regex = new RegExp(escaped + '" Version="[^"]+"');
  files.forEach(f => {
    const s = fs.readFileSync(f, 'utf8');
    fs.writeFileSync(f, s.replace(regex, pkg + '" Version="' + v + '"'));
    console.log(f + ' -> ' + v);
  });
});

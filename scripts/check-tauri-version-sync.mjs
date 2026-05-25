import { readFileSync } from 'node:fs';
import path from 'node:path';
import process from 'node:process';

function fail(message) {
  console.error(`\nTauri version sync check failed:\n${message}\n`);
  process.exit(1);
}

function readJson(filePath) {
  return JSON.parse(readFileSync(filePath, 'utf8'));
}

function escapeRegex(value) {
  return value.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
}

function readPackageVersion(packageJson, packageName) {
  const version = packageJson.dependencies?.[packageName] ?? packageJson.devDependencies?.[packageName];
  if (!version) {
    fail(`- Missing package.json dependency: ${packageName}`);
  }
  return version;
}

function readCargoVersion(cargoToml, crateName) {
  const escapedName = escapeRegex(crateName);
  const inlineTable = cargoToml.match(
    new RegExp(`^${escapedName}\\s*=\\s*\\{[^\n]*version\\s*=\\s*"([^"]+)"`, 'm'),
  );
  if (inlineTable?.[1]) {
    return inlineTable[1];
  }

  const plainEntry = cargoToml.match(new RegExp(`^${escapedName}\\s*=\\s*"([^"]+)"`, 'm'));
  if (plainEntry?.[1]) {
    return plainEntry[1];
  }

  fail(`- Missing Cargo.toml dependency: ${crateName}`);
}

function parseVersion(label, rawVersion) {
  const match = rawVersion.match(/(\d+)\.(\d+)\.(\d+)/);
  if (!match) {
    fail(`- ${label} has an unsupported version format: ${rawVersion}`);
  }

  const [, major, minor, patch] = match;
  return {
    raw: rawVersion,
    major: Number(major),
    minor: Number(minor),
    patch: Number(patch),
  };
}

function toMinor(version) {
  return `${version.major}.${version.minor}`;
}

function toExact(version) {
  return `${version.major}.${version.minor}.${version.patch}`;
}

const projectRoot = process.cwd();
const packageJsonPath = path.join(projectRoot, 'package.json');
const cargoTomlPath = path.join(projectRoot, 'src-tauri', 'Cargo.toml');

const packageJson = readJson(packageJsonPath);
const cargoToml = readFileSync(cargoTomlPath, 'utf8');

const versions = {
  jsApi: parseVersion('@tauri-apps/api', readPackageVersion(packageJson, '@tauri-apps/api')),
  jsCli: parseVersion('@tauri-apps/cli', readPackageVersion(packageJson, '@tauri-apps/cli')),
  jsDialog: parseVersion(
    '@tauri-apps/plugin-dialog',
    readPackageVersion(packageJson, '@tauri-apps/plugin-dialog'),
  ),
  jsLog: parseVersion('@tauri-apps/plugin-log', readPackageVersion(packageJson, '@tauri-apps/plugin-log')),
  rustTauri: parseVersion('tauri', readCargoVersion(cargoToml, 'tauri')),
  rustBuild: parseVersion('tauri-build', readCargoVersion(cargoToml, 'tauri-build')),
  rustDialog: parseVersion(
    'tauri-plugin-dialog',
    readCargoVersion(cargoToml, 'tauri-plugin-dialog'),
  ),
  rustLog: parseVersion('tauri-plugin-log', readCargoVersion(cargoToml, 'tauri-plugin-log')),
};

const errors = [];

if (toMinor(versions.jsApi) !== toMinor(versions.rustTauri)) {
  errors.push(
    `- @tauri-apps/api (${versions.jsApi.raw}) and tauri (${versions.rustTauri.raw}) must share the same major.minor line.`,
  );
}

if (toMinor(versions.jsCli) !== toMinor(versions.rustTauri)) {
  errors.push(
    `- @tauri-apps/cli (${versions.jsCli.raw}) and tauri (${versions.rustTauri.raw}) must share the same major.minor line.`,
  );
}

if (toExact(versions.jsDialog) !== toExact(versions.rustDialog)) {
  errors.push(
    `- @tauri-apps/plugin-dialog (${versions.jsDialog.raw}) and tauri-plugin-dialog (${versions.rustDialog.raw}) must match exactly.`,
  );
}

if (toExact(versions.jsLog) !== toExact(versions.rustLog)) {
  errors.push(
    `- @tauri-apps/plugin-log (${versions.jsLog.raw}) and tauri-plugin-log (${versions.rustLog.raw}) must match exactly.`,
  );
}

if (errors.length > 0) {
  fail(errors.join('\n'));
}

console.log('Tauri dependency alignment OK');
console.log(`- @tauri-apps/api      ${versions.jsApi.raw}`);
console.log(`- @tauri-apps/cli      ${versions.jsCli.raw}`);
console.log(`- tauri                ${versions.rustTauri.raw}`);
console.log(`- tauri-build          ${versions.rustBuild.raw} (reported, not line-enforced)`);
console.log(`- plugin-dialog        ${versions.jsDialog.raw} / ${versions.rustDialog.raw}`);
console.log(`- plugin-log           ${versions.jsLog.raw} / ${versions.rustLog.raw}`);

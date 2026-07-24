import { readFile, writeFile } from "node:fs/promises";

const version = process.argv[2]?.replace(/^v/, "");
if (!version || !/^\d+\.\d+\.\d+(?:-[0-9A-Za-z.-]+)?$/.test(version)) {
  throw new Error("Expected a semantic version such as v1.2.3.");
}

async function updateJsonVersion(path: string): Promise<void> {
  const contents = await readFile(path, "utf8");
  const updated = contents.replace(/("version"\s*:\s*")[^"]+(")/, `$1${version}$2`);
  if (updated === contents && !contents.includes(`"version": "${version}"`)) {
    throw new Error(`No version field was found in ${path}.`);
  }
  await writeFile(path, updated);
}

await updateJsonVersion("package.json");
await updateJsonVersion("src-tauri/tauri.conf.json");

const cargoPath = "src-tauri/Cargo.toml";
const cargoToml = await readFile(cargoPath, "utf8");
await writeFile(
  cargoPath,
  cargoToml.replace(/(\[package\][\s\S]*?\nversion = )"[^"]+"/, `$1"${version}"`),
);

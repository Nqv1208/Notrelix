#!/usr/bin/env node
import { createHash } from "node:crypto";
import { existsSync, mkdirSync, readFileSync, writeFileSync } from "node:fs";
import {
  dirname,
  isAbsolute,
  normalize,
  relative,
  resolve,
  sep,
} from "node:path";
import { spawnSync } from "node:child_process";
function arg(name) {
  const i = process.argv.indexOf(name);
  if (i < 0 || i + 1 >= process.argv.length) throw new Error(`missing ${name}`);
  return process.argv[i + 1];
}
function safe(v) {
  if (!v || isAbsolute(v)) return false;
  const n = normalize(v);
  return n !== ".." && !n.startsWith(`..${sep}`);
}
const component = arg("--component"),
  paths = JSON.parse(arg("--paths-json")),
  output = resolve(arg("--output")),
  manifestPath = resolve(arg("--manifest")),
  root = process.cwd();
if (!Array.isArray(paths) || !paths.length)
  throw new Error("artifact paths required");
for (const item of paths) {
  if (typeof item !== "string" || !safe(item))
    throw new Error(`unsafe path ${item}`);
  const target = resolve(root, item),
    rel = relative(root, target);
  if (!safe(rel) || !existsSync(target))
    throw new Error(`missing/unsafe path ${item}`);
}
mkdirSync(dirname(output), { recursive: true });
mkdirSync(dirname(manifestPath), { recursive: true });
const tar = spawnSync(
  "tar",
  ["--dereference", "-czf", output, "--", ...paths],
  { cwd: root, stdio: "inherit" },
);
if (tar.status !== 0) process.exit(tar.status ?? 1);
const sha256 = createHash("sha256").update(readFileSync(output)).digest("hex");
const manifest = {
  api_version: "delivery.notrelix.dev/v1",
  kind: "FrontendHostArtifact",
  component_id: component,
  archive: output.split(sep).pop(),
  sha256,
  paths,
};
writeFileSync(manifestPath, JSON.stringify(manifest, null, 2) + "\n");

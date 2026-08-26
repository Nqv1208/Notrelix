#!/usr/bin/env node
import { createHash } from "node:crypto";
import { readFileSync } from "node:fs";
import { isAbsolute, normalize, resolve, sep } from "node:path";
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
  archive = resolve(arg("--archive")),
  manifestPath = resolve(arg("--manifest")),
  destination = resolve(arg("--destination"));
const m = JSON.parse(readFileSync(manifestPath, "utf8"));
if (
  m.api_version !== "delivery.notrelix.dev/v1" ||
  m.kind !== "FrontendHostArtifact" ||
  m.component_id !== component
)
  throw new Error("artifact manifest identity mismatch");
const digest = createHash("sha256").update(readFileSync(archive)).digest("hex");
if (digest !== m.sha256) throw new Error("artifact hash mismatch");

// Plain listing drives the path allow-list (portable across GNU/bsdtar).
const listing = spawnSync("tar", ["-tzf", archive], { encoding: "utf8" });
if (listing.status !== 0) process.exit(listing.status ?? 1);
const allowed = m.paths.map((p) =>
  normalize(p).replaceAll("\\", "/").replace(/\/+$/, ""),
);
for (const raw of listing.stdout.split(/\r?\n/).filter(Boolean)) {
  const member = raw.replaceAll("\\", "/").replace(/\/+$/, "");
  if (
    !safe(member) ||
    !allowed.some((p) => member === p || member.startsWith(`${p}/`))
  )
    throw new Error(`unsafe/out-of-contract tar member ${raw}`);
}

// Verbose listing is required only to see symlink members: plain -t hides
// them, which would make the contract blind to preserved pnpm/Next links.
// Only lines containing " -> " carry a link; metadata column layout differs
// between tar implementations, so the member name is located by matching it
// against the allowed prefixes instead of parsing date fields.
const verbose = spawnSync("tar", ["-tvzf", archive], { encoding: "utf8" });
if (verbose.status !== 0) process.exit(verbose.status ?? 1);
function locateMember(left) {
  let best = null;
  for (const prefix of allowed) {
    const i = left.lastIndexOf(`${prefix}/`);
    if (left === prefix) return { name: prefix, at: left.indexOf(prefix) };
    if (i >= 0 && (best === null || i > best.at))
      best = { name: left.slice(i).replace(/\/+$/, ""), at: i };
  }
  return best;
}
for (const line of verbose.stdout.split(/\r?\n/).filter(Boolean)) {
  const arrow = line.indexOf(" -> ");
  if (arrow < 0) continue;
  const left = line.slice(0, arrow);
  const target = line
    .slice(arrow + 4)
    .trim()
    .replaceAll("\\", "/");
  const found = locateMember(left.trim());
  if (!found || !safe(found.name))
    throw new Error(`unsafe/out-of-contract tar member ${line}`);
  // Symlink safety: relative targets must resolve inside the destination;
  // absolute targets can only point outside of it and are rejected outright.
  const resolved = resolve(resolve(destination, found.name, ".."), target);
  if (isAbsolute(target) || !resolved.startsWith(destination + sep))
    throw new Error(
      `unsafe symlink escape in artifact: ${found.name} -> ${target}`,
    );
}

const ex = spawnSync("tar", ["-xzf", archive, "-C", destination], {
  stdio: "inherit",
});
if (ex.status !== 0) process.exit(ex.status ?? 1);

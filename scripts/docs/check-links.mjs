#!/usr/bin/env node

import {
  existsSync,
  readdirSync,
  readFileSync,
  realpathSync,
} from "node:fs";
import {
  dirname,
  extname,
  isAbsolute,
  join,
  relative,
  resolve,
  sep,
} from "node:path";
import process from "node:process";
import { fileURLToPath } from "node:url";

const SCRIPT_PATH = fileURLToPath(import.meta.url);
const DEFAULT_REPO_ROOT = resolve(dirname(SCRIPT_PATH), "../..");
const REPO_ROOT = process.env.DOCS_ROOT
  ? resolve(process.env.DOCS_ROOT)
  : DEFAULT_REPO_ROOT;

const ROOT_DOC_NAMES = new Set([
  "README.md",
  "PRODUCT.md",
  "RULE.md",
  "AGENTS.md",
  "DESIGN.md",
  "CONTEXT.md",
  "CONTEXT-MAP.md",
]);

const IGNORED_DIRECTORY_NAMES = new Set([
  ".git",
  ".agents",
  ".claude",
  ".codex",
  ".cursor",
  ".gemini",
  ".gstack",
  ".mimocode",
  ".opencode",
  ".qwen",
  ".turbo",
  ".next",
  "node_modules",
  "artifacts",
  "bin",
  "obj",
  "coverage",
  "dist",
]);

const RETIRED_DOC_TREES = [
  "docs/engineering",
  "docs-refoundation",
];

const failures = [];

let markdownFileCount = 0;
let localLinkCount = 0;
let anchorCheckCount = 0;
let referenceUseCount = 0;

function fail(message) {
  failures.push(message);
}

function toPosix(value) {
  return value.split(sep).join("/");
}

function displayPath(absolutePath) {
  return toPosix(relative(REPO_ROOT, absolutePath)) || ".";
}

function pathIsInside(parent, candidate) {
  const rel = relative(parent, candidate);

  return (
    rel === "" ||
    (!rel.startsWith(`..${sep}`) &&
      rel !== ".." &&
      !isAbsolute(rel))
  );
}

function repoPath(relativePath) {
  return resolve(REPO_ROOT, relativePath);
}

function isRetiredDocTree(absolutePath) {
  return RETIRED_DOC_TREES.some((tree) => {
    const base = repoPath(tree);

    return (
      absolutePath === base ||
      absolutePath.startsWith(`${base}${sep}`)
    );
  });
}

function isCanonicalMarkdownPath(absolutePath) {
  const rel = toPosix(relative(REPO_ROOT, absolutePath));

  if (!rel || rel.startsWith("../")) {
    return false;
  }

  if (isRetiredDocTree(absolutePath)) {
    return false;
  }

  if (!rel.includes("/")) {
    return ROOT_DOC_NAMES.has(rel);
  }

  if (rel.startsWith("docs/")) {
    return true;
  }

  if (
    rel === "backend/README.md" ||
    rel === "backend/AGENTS.md" ||
    rel === "backend/CONTEXT.md" ||
    rel === "backend/tests/AGENTS.md" ||
    rel === "backend/docs/README.md"
  ) {
    return true;
  }

  if (
    rel.startsWith("backend/docs/architecture/") ||
    rel.startsWith("backend/docs/operations/") ||
    rel.startsWith("backend/docs/decisions/") ||
    rel.startsWith("backend/docs/generated/")
  ) {
    return true;
  }

  if (
    rel === "frontend/README.md" ||
    rel === "frontend/AGENTS.md" ||
    rel === "frontend/CONTEXT.md" ||
    rel === "frontend/docs/README.md"
  ) {
    return true;
  }

  return (
    rel.startsWith("frontend/docs/architecture/") ||
    rel.startsWith("frontend/docs/decisions/") ||
    rel.startsWith("frontend/docs/generated/")
  );
}

function walkMarkdown(directory, acc = []) {
  if (!existsSync(directory)) {
    return acc;
  }

  for (const entry of readdirSync(directory, {
    withFileTypes: true,
  })) {
    if (
      entry.isDirectory() &&
      IGNORED_DIRECTORY_NAMES.has(entry.name)
    ) {
      continue;
    }

    const absolutePath = join(directory, entry.name);

    if (entry.isDirectory()) {
      if (isRetiredDocTree(absolutePath)) {
        continue;
      }

      walkMarkdown(absolutePath, acc);
      continue;
    }

    if (
      entry.isFile() &&
      entry.name.endsWith(".md") &&
      isCanonicalMarkdownPath(absolutePath)
    ) {
      acc.push(absolutePath);
    }
  }

  return acc;
}

/**
 * Remove fenced and inline code from Markdown while preserving
 * character offsets.
 *
 * This prevents examples such as:
 *
 *   file:///Users/example/project
 *
 * inside code blocks from being interpreted as real links.
 */
function stripCode(text) {
  const lines = text.split(/\r?\n/);
  let fence = null;

  return lines
    .map((line) => {
      const trimmed = line.trimStart();
      const fenceMatch = trimmed.match(/^(```+|~~~+)/);

      if (fenceMatch) {
        const marker = fenceMatch[1][0];
        const width = fenceMatch[1].length;

        if (fence == null) {
          fence = {
            marker,
            width,
          };
        } else if (
          fence.marker === marker &&
          fenceMatch[1].length >= fence.width
        ) {
          fence = null;
        }

        return " ".repeat(line.length);
      }

      if (fence != null) {
        return " ".repeat(line.length);
      }

      // Remove inline code spans while preserving offsets.
      let result = "";

      for (let index = 0; index < line.length; ) {
        if (line[index] !== "`") {
          result += line[index];
          index += 1;
          continue;
        }

        let tickCount = 1;

        while (line[index + tickCount] === "`") {
          tickCount += 1;
        }

        const delimiter = "`".repeat(tickCount);
        const close = line.indexOf(
          delimiter,
          index + tickCount,
        );

        if (close === -1) {
          result += " ".repeat(tickCount);
          index += tickCount;
          continue;
        }

        const length = close + tickCount - index;

        result += " ".repeat(length);
        index = close + tickCount;
      }

      return result;
    })
    .join("\n");
}

function lineNumberAt(text, offset) {
  let line = 1;

  for (let index = 0; index < offset; index += 1) {
    if (text.charCodeAt(index) === 10) {
      line += 1;
    }
  }

  return line;
}

/**
 * Extract inline Markdown links:
 *
 *   [label](path)
 *   [label](path "title")
 *   [label](<path with spaces>)
 *
 * This is intentionally a Markdown-oriented parser rather than a
 * generic URL regex.
 */
function extractInlineLinks(text) {
  const links = [];

  for (
    let cursor = 0;
    cursor < text.length - 1;
    cursor += 1
  ) {
    if (
      text[cursor] !== "]" ||
      text[cursor + 1] !== "("
    ) {
      continue;
    }

    const openBracket = text.lastIndexOf("[", cursor);

    if (openBracket === -1) {
      continue;
    }

    if (
      openBracket > 0 &&
      text[openBracket - 1] === "\\"
    ) {
      continue;
    }

    let depth = 1;
    let index = cursor + 2;
    let escaped = false;
    let angle = false;

    while (
      index < text.length &&
      depth > 0
    ) {
      const char = text[index];

      if (escaped) {
        escaped = false;
        index += 1;
        continue;
      }

      if (char === "\\") {
        escaped = true;
        index += 1;
        continue;
      }

      if (
        char === "<" &&
        depth === 1
      ) {
        angle = true;
        index += 1;
        continue;
      }

      if (
        char === ">" &&
        angle
      ) {
        angle = false;
        index += 1;
        continue;
      }

      if (
        !angle &&
        char === "("
      ) {
        depth += 1;
      }

      if (
        !angle &&
        char === ")"
      ) {
        depth -= 1;
      }

      index += 1;
    }

    if (depth !== 0) {
      continue;
    }

    const body = text
      .slice(cursor + 2, index - 1)
      .trim();

    const target = parseInlineDestination(body);

    if (target) {
      links.push({
        target,
        line: lineNumberAt(text, cursor),
      });
    }

    cursor = index - 1;
  }

  return links;
}

function parseInlineDestination(body) {
  if (!body) {
    return null;
  }

  if (body.startsWith("<")) {
    const close = body.indexOf(">");

    return close === -1
      ? null
      : body.slice(1, close);
  }

  let escaped = false;
  let parenDepth = 0;

  for (
    let index = 0;
    index < body.length;
    index += 1
  ) {
    const char = body[index];

    if (escaped) {
      escaped = false;
      continue;
    }

    if (char === "\\") {
      escaped = true;
      continue;
    }

    if (char === "(") {
      parenDepth += 1;
    } else if (char === ")") {
      parenDepth = Math.max(
        0,
        parenDepth - 1,
      );
    } else if (
      /\s/.test(char) &&
      parenDepth === 0
    ) {
      return body.slice(0, index);
    }
  }

  return body;
}

function extractReferenceDefinitions(text) {
  const definitions = new Map();

  const pattern =
    /^\s{0,3}\[([^\]]+)\]:\s*(?:<([^>]+)>|(\S+))/gm;

  for (const match of text.matchAll(pattern)) {
    const label = normalizeReferenceLabel(
      match[1],
    );

    definitions.set(label, {
      target: match[2] ?? match[3],
      line: lineNumberAt(
        text,
        match.index ?? 0,
      ),
    });
  }

  return definitions;
}

function extractReferenceUses(text) {
  const uses = [];

  const pattern =
    /(?<!!)\[([^\]\n]+)\]\[([^\]\n]*)\]/g;

  for (const match of text.matchAll(pattern)) {
    const label = normalizeReferenceLabel(
      match[2] || match[1],
    );

    uses.push({
      label,
      line: lineNumberAt(
        text,
        match.index ?? 0,
      ),
    });
  }

  return uses;
}

function normalizeReferenceLabel(value) {
  return value
    .trim()
    .replace(/\s+/g, " ")
    .toLowerCase();
}

function isRemoteOrNonFileTarget(target) {
  if (/^file:/i.test(target)) {
    return false;
  }

  /**
   * Any other URI scheme is external/non-repository from the
   * perspective of this checker.
   *
   * HTTP availability is intentionally not a docs-build gate.
   */
  return /^[A-Za-z][A-Za-z0-9+.-]*:/.test(
    target,
  );
}

function looksLikeDeveloperAbsolutePath(target) {
  return (
    /^file:\/\//i.test(target) ||
    /^\/(?:Users|home|private|Volumes)\//.test(
      target,
    ) ||
    /^[A-Za-z]:[\\/]/.test(target) ||
    /^\\\\/.test(target)
  );
}

function splitTarget(rawTarget) {
  const hashIndex = rawTarget.indexOf("#");

  const pathAndQuery =
    hashIndex === -1
      ? rawTarget
      : rawTarget.slice(0, hashIndex);

  const fragment =
    hashIndex === -1
      ? ""
      : rawTarget.slice(hashIndex + 1);

  const queryIndex =
    pathAndQuery.indexOf("?");

  const pathPart =
    queryIndex === -1
      ? pathAndQuery
      : pathAndQuery.slice(0, queryIndex);

  return {
    pathPart,
    fragment,
  };
}

function safeDecode(value, context) {
  try {
    return decodeURIComponent(value);
  } catch {
    fail(
      `${context}: invalid percent-encoding in Markdown target: ${value}`,
    );

    return null;
  }
}

/**
 * Build a GitHub-like heading anchor set.
 *
 * Explicit <a id="..."> / <a name="..."> anchors are also accepted.
 */
function markdownHeadingAnchors(file) {
  const text = stripCode(
    readFileSync(file, "utf8"),
  );

  const anchors = new Set();
  const slugCounts = new Map();

  for (const line of text.split(/\r?\n/)) {
    const explicitAnchorPattern =
      /<a\s+(?:[^>]*?\s)?(?:id|name)=["']([^"']+)["'][^>]*>/gi;

    for (
      const match of line.matchAll(
        explicitAnchorPattern,
      )
    ) {
      anchors.add(match[1]);
    }

    const heading = line.match(
      /^\s{0,3}#{1,6}\s+(.+?)\s*#*\s*$/,
    );

    if (!heading) {
      continue;
    }

    const base = githubLikeSlug(
      heading[1],
    );

    if (!base) {
      continue;
    }

    const count =
      slugCounts.get(base) ?? 0;

    const slug =
      count === 0
        ? base
        : `${base}-${count}`;

    slugCounts.set(
      base,
      count + 1,
    );

    anchors.add(slug);
  }

  return anchors;
}

function githubLikeSlug(heading) {
  let value = heading
    .replace(
      /!\[([^\]]*)\]\([^)]+\)/g,
      "$1",
    )
    .replace(
      /\[([^\]]+)\]\([^)]+\)/g,
      "$1",
    )
    .replace(/<[^>]+>/g, "")
    .replace(/[`*_~]/g, "")
    .trim()
    .toLowerCase();

  value = value
    .normalize("NFKC")
    .replace(
      /[^\p{L}\p{N}\p{M}\s_-]/gu,
      "",
    )
    .replace(/\s+/g, "-");

  return value;
}

function validateLocalTarget(
  sourceFile,
  rawTarget,
  line,
) {
  const context =
    `${displayPath(sourceFile)}:${line}`;

  if (!rawTarget) {
    return;
  }

  if (
    looksLikeDeveloperAbsolutePath(rawTarget)
  ) {
    fail(
      `${context}: forbidden absolute/local workstation link: ${rawTarget}`,
    );

    return;
  }

  if (
    isRemoteOrNonFileTarget(rawTarget)
  ) {
    return;
  }

  const {
    pathPart,
    fragment,
  } = splitTarget(rawTarget);

  if (!pathPart && !fragment) {
    return;
  }

  let targetFile = sourceFile;

  if (pathPart) {
    if (pathPart.startsWith("/")) {
      fail(
        `${context}: repository link must be relative, not absolute: ${rawTarget}`,
      );

      return;
    }

    const decoded = safeDecode(
      pathPart,
      context,
    );

    if (decoded == null) {
      return;
    }

    targetFile = resolve(
      dirname(sourceFile),
      decoded,
    );

    if (
      !pathIsInside(
        REPO_ROOT,
        targetFile,
      )
    ) {
      fail(
        `${context}: link escapes repository: ${rawTarget}`,
      );

      return;
    }

    if (!existsSync(targetFile)) {
      fail(
        `${context}: broken relative link: ${rawTarget}`,
      );

      return;
    }

    /**
     * Resolve the final real path even when only an intermediate
     * directory is a symlink.
     *
     * A repository-local-looking path must not escape the repository
     * through symlink traversal.
     */
    const resolvedRealPath =
      realpathSync(targetFile);

    if (
      !pathIsInside(
        REPO_ROOT,
        resolvedRealPath,
      )
    ) {
      fail(
        `${context}: symlink-resolved target escapes repository: ${rawTarget}`,
      );

      return;
    }
  }

  localLinkCount += 1;

  if (!fragment) {
    return;
  }

  if (
    extname(targetFile).toLowerCase() !==
    ".md"
  ) {
    return;
  }

  const decodedFragment = safeDecode(
    fragment,
    context,
  );

  if (
    decodedFragment == null ||
    !decodedFragment
  ) {
    return;
  }

  anchorCheckCount += 1;

  const anchors =
    markdownHeadingAnchors(targetFile);

  if (!anchors.has(decodedFragment)) {
    fail(
      `${context}: broken Markdown anchor #${decodedFragment} in ` +
        `${displayPath(targetFile)} (from ${rawTarget})`,
    );
  }
}

function validateFile(file) {
  markdownFileCount += 1;

  const raw = readFileSync(
    file,
    "utf8",
  );

  const text = stripCode(raw);

  /**
   * Examples inside fenced/inline code may document the forbidden
   * pattern.
   *
   * Only an authored non-code reference is a violation.
   */
  if (/file:\/\//i.test(text)) {
    fail(
      `${displayPath(file)}: contains forbidden file:/// reference`,
    );
  }

  const definitions =
    extractReferenceDefinitions(text);

  const uses =
    extractReferenceUses(text);

  for (
    const link of extractInlineLinks(text)
  ) {
    validateLocalTarget(
      file,
      link.target,
      link.line,
    );
  }

  for (
    const definition of definitions.values()
  ) {
    validateLocalTarget(
      file,
      definition.target,
      definition.line,
    );
  }

  for (const use of uses) {
    referenceUseCount += 1;

    if (
      !definitions.has(use.label)
    ) {
      fail(
        `${displayPath(file)}:${use.line}: undefined Markdown reference label ` +
          `[${use.label}]`,
      );
    }
  }
}

if (!existsSync(REPO_ROOT)) {
  console.error(
    `[docs-links] repository root does not exist: ${REPO_ROOT}`,
  );

  process.exit(1);
}

const markdownFiles =
  walkMarkdown(REPO_ROOT).sort(
    (a, b) =>
      displayPath(a).localeCompare(
        displayPath(b),
        "en",
      ),
  );

for (const file of markdownFiles) {
  validateFile(file);
}

if (failures.length > 0) {
  console.error(
    `[docs-links] FAIL — ${failures.length} documentation link violation(s):`,
  );

  for (const failure of failures) {
    console.error(
      `- ${failure}`,
    );
  }

  process.exit(1);
}

console.log(
  `[docs-links] PASS — ${markdownFileCount} Markdown file(s), ` +
    `${localLinkCount} local target(s), ` +
    `${anchorCheckCount} anchor check(s), ` +
    `${referenceUseCount} reference-style use(s).`,
);
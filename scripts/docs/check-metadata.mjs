#!/usr/bin/env node

import {
  existsSync,
  readdirSync,
  readFileSync,
} from "node:fs";
import {
  dirname,
  join,
  relative,
  resolve,
  sep,
} from "node:path";
import process from "node:process";
import { fileURLToPath } from "node:url";

const SCRIPT_PATH =
  fileURLToPath(import.meta.url);

const DEFAULT_REPO_ROOT =
  resolve(
    dirname(SCRIPT_PATH),
    "../..",
  );

const REPO_ROOT =
  process.env.DOCS_ROOT
    ? resolve(process.env.DOCS_ROOT)
    : DEFAULT_REPO_ROOT;

const REQUIRED_FIELDS = [
  "document_id",
  "document_type",
  "status",
  "owner",
  "applies_to",
  "evidence",
  "review_on",
];

const LIST_FIELDS = new Set([
  "applies_to",
  "evidence",
  "review_on",
]);

const LIFECYCLE_STATUSES = new Set([
  "draft",
  "active",
  "superseded",
  "generated",
]);

const ADR_STATUSES = new Set([
  "Proposed",
  "Accepted",
  "Superseded",
  "Rejected",
  "Deprecated",
]);

const IGNORED_DIRECTORY_NAMES =
  new Set([
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

/**
 * These trees are intentionally not metadata-scanned.
 *
 * Their existence is a documentation-authority concern owned by:
 *
 *   check-authority.mjs
 */
const AUTHORITY_OWNED_EXCLUSIONS = [
  "docs/engineering",
  "docs-refoundation",
];

const failures = [];

const documentIds =
  new Map();

let checkedFileCount = 0;

function fail(message) {
  failures.push(message);
}

function toPosix(value) {
  return value
    .split(sep)
    .join("/");
}

function displayPath(absolutePath) {
  return (
    toPosix(
      relative(
        REPO_ROOT,
        absolutePath,
      ),
    ) || "."
  );
}

function repoPath(relativePath) {
  return resolve(
    REPO_ROOT,
    relativePath,
  );
}

function isAuthorityOwnedExclusion(
  absolutePath,
) {
  return AUTHORITY_OWNED_EXCLUSIONS.some(
    (tree) => {
      const base = repoPath(tree);

      return (
        absolutePath === base ||
        absolutePath.startsWith(
          `${base}${sep}`,
        )
      );
    },
  );
}

/**
 * Metadata is mandatory for canonical authored/generated documentation
 * planes.
 *
 * Root onboarding/governance entry documents remain intentionally free
 * from mandatory YAML frontmatter.
 */
function isCanonicalMetadataPath(
  absolutePath,
) {
  const rel = toPosix(
    relative(
      REPO_ROOT,
      absolutePath,
    ),
  );

  if (
    !rel ||
    rel.startsWith("../") ||
    !rel.endsWith(".md")
  ) {
    return false;
  }

  if (
    isAuthorityOwnedExclusion(
      absolutePath,
    )
  ) {
    return false;
  }

  /**
   * Router/index README files do not own lifecycle metadata.
   *
   * They route readers into canonical authorities rather than acting as
   * normative documents themselves.
   */
  if (
    rel === "docs/README.md" ||
    rel === "docs/templates/README.md"
  ) {
    return false;
  }

  /**
   * Repository-wide canonical authored/generated documentation.
   */
  if (
    rel.startsWith("docs/governance/") ||
    rel.startsWith("docs/architecture/") ||
    rel.startsWith("docs/product/") ||
    rel.startsWith("docs/quality/") ||
    rel.startsWith("docs/delivery/") ||
    rel.startsWith("docs/operations/") ||
    rel.startsWith("docs/infrastructure/") ||
    rel.startsWith("docs/decisions/") ||
    rel.startsWith("docs/templates/") ||
    rel.startsWith("docs/generated/")
  ) {
    return true;
  }

  /**
   * Backend canonical authored/generated plane.
   *
   * backend/docs/README.md is intentionally excluded.
   */
  if (
    rel.startsWith(
      "backend/docs/architecture/",
    ) ||
    rel.startsWith(
      "backend/docs/operations/",
    ) ||
    rel.startsWith(
      "backend/docs/decisions/",
    ) ||
    rel.startsWith(
      "backend/docs/generated/",
    )
  ) {
    return true;
  }

  /**
   * Frontend canonical authored/generated plane.
   *
   * frontend/docs/README.md is intentionally excluded.
   */
  if (
    rel.startsWith(
      "frontend/docs/architecture/",
    ) ||
    rel.startsWith(
      "frontend/docs/decisions/",
    ) ||
    rel.startsWith(
      "frontend/docs/generated/",
    )
  ) {
    return true;
  }

  return false;
}

function walkMetadataDocs(
  directory,
  acc = [],
) {
  if (!existsSync(directory)) {
    return acc;
  }

  for (
    const entry of readdirSync(
      directory,
      {
        withFileTypes: true,
      },
    )
  ) {
    if (
      entry.isDirectory() &&
      IGNORED_DIRECTORY_NAMES.has(
        entry.name,
      )
    ) {
      continue;
    }

    const absolutePath =
      join(
        directory,
        entry.name,
      );

    if (entry.isDirectory()) {
      if (
        isAuthorityOwnedExclusion(
          absolutePath,
        )
      ) {
        continue;
      }

      walkMetadataDocs(
        absolutePath,
        acc,
      );

      continue;
    }

    if (
      entry.isFile() &&
      isCanonicalMetadataPath(
        absolutePath,
      )
    ) {
      acc.push(absolutePath);
    }
  }

  return acc;
}

/**
 * Parse the intentionally small YAML subset used by canonical
 * documentation metadata.
 *
 * Supported:
 *
 *   scalar: value
 *
 *   list:
 *     - item
 *
 * Complex nested YAML is intentionally rejected. Documentation
 * metadata should remain machine-readable and predictable.
 */
function parseFrontmatter(
  file,
  text,
) {
  const normalized =
    text.replace(
      /^\uFEFF/,
      "",
    );

  if (
    !normalized.startsWith("---\n") &&
    !normalized.startsWith("---\r\n")
  ) {
    fail(
      `${displayPath(file)}: missing YAML frontmatter at start of file`,
    );

    return null;
  }

  const lines =
    normalized.split(/\r?\n/);

  if (lines[0] !== "---") {
    fail(
      `${displayPath(file)}: malformed opening frontmatter delimiter`,
    );

    return null;
  }

  let endIndex = -1;

  for (
    let index = 1;
    index < lines.length;
    index += 1
  ) {
    if (lines[index] === "---") {
      endIndex = index;
      break;
    }
  }

  if (endIndex === -1) {
    fail(
      `${displayPath(file)}: missing closing frontmatter delimiter`,
    );

    return null;
  }

  const data = new Map();

  let currentListKey = null;

  for (
    let index = 1;
    index < endIndex;
    index += 1
  ) {
    const rawLine =
      lines[index];

    const lineNumber =
      index + 1;

    if (
      !rawLine.trim() ||
      rawLine
        .trimStart()
        .startsWith("#")
    ) {
      continue;
    }

    const listItem =
      rawLine.match(
        /^\s+-\s+(.+?)\s*$/,
      );

    if (listItem) {
      if (
        currentListKey == null
      ) {
        fail(
          `${displayPath(file)}:${lineNumber}: list item has no owning metadata key`,
        );

        continue;
      }

      const existing =
        data.get(currentListKey);

      if (
        !Array.isArray(existing)
      ) {
        fail(
          `${displayPath(file)}:${lineNumber}: metadata field ` +
            `${currentListKey} is not a list`,
        );

        continue;
      }

      existing.push(
        unquote(listItem[1]),
      );

      continue;
    }

    const topLevel =
      rawLine.match(
        /^([A-Za-z_][A-Za-z0-9_-]*):(?:\s*(.*))?$/,
      );

    if (!topLevel) {
      fail(
        `${displayPath(file)}:${lineNumber}: unsupported frontmatter syntax; ` +
          `canonical metadata must use top-level scalar/list fields`,
      );

      currentListKey = null;
      continue;
    }

    const key =
      topLevel[1];

    const rawValue =
      (topLevel[2] ?? "")
        .trim();

    if (data.has(key)) {
      fail(
        `${displayPath(file)}:${lineNumber}: duplicate metadata key ${key}`,
      );

      currentListKey = null;
      continue;
    }

    if (!rawValue) {
      data.set(
        key,
        [],
      );

      currentListKey = key;
      continue;
    }

    data.set(
      key,
      unquote(
        stripInlineComment(
          rawValue,
        ),
      ),
    );

    currentListKey = null;
  }

  return data;
}

function stripInlineComment(value) {
  /**
   * Preserve # inside quoted strings.
   */
  if (
    (
      value.startsWith('"') &&
      value.endsWith('"')
    ) ||
    (
      value.startsWith("'") &&
      value.endsWith("'")
    )
  ) {
    return value;
  }

  const index =
    value.indexOf(" #");

  return index === -1
    ? value
    : value
        .slice(0, index)
        .trimEnd();
}

function unquote(value) {
  const trimmed =
    value.trim();

  if (
    (
      trimmed.startsWith('"') &&
      trimmed.endsWith('"')
    ) ||
    (
      trimmed.startsWith("'") &&
      trimmed.endsWith("'")
    )
  ) {
    return trimmed.slice(
      1,
      -1,
    );
  }

  return trimmed;
}

function expectScalar(
  file,
  metadata,
  field,
) {
  const value =
    metadata.get(field);

  if (value == null) {
    fail(
      `${displayPath(file)}: missing required metadata field ${field}`,
    );

    return null;
  }

  if (Array.isArray(value)) {
    fail(
      `${displayPath(file)}: metadata field ${field} must be a scalar`,
    );

    return null;
  }

  if (!value.trim()) {
    fail(
      `${displayPath(file)}: metadata field ${field} must not be empty`,
    );

    return null;
  }

  return value;
}

function expectNonEmptyList(
  file,
  metadata,
  field,
) {
  const value =
    metadata.get(field);

  if (value == null) {
    fail(
      `${displayPath(file)}: missing required metadata field ${field}`,
    );

    return null;
  }

  if (!Array.isArray(value)) {
    fail(
      `${displayPath(file)}: metadata field ${field} must be a YAML list`,
    );

    return null;
  }

  const emptyIndex =
    value.findIndex(
      (entry) =>
        !String(entry).trim(),
    );

  if (emptyIndex !== -1) {
    fail(
      `${displayPath(file)}: metadata field ${field} contains an empty list item`,
    );
  }

  if (value.length === 0) {
    fail(
      `${displayPath(file)}: metadata field ${field} must not be empty`,
    );

    return null;
  }

  return value;
}

function isGeneratedPath(file) {
  const rel = toPosix(
    relative(
      REPO_ROOT,
      file,
    ),
  );

  return (
    rel.startsWith(
      "docs/generated/",
    ) ||
    rel.startsWith(
      "backend/docs/generated/",
    ) ||
    rel.startsWith(
      "frontend/docs/generated/",
    )
  );
}

function validateStatus(
  file,
  documentType,
  status,
) {
  if (
    documentType ===
    "architecture-decision"
  ) {
    if (
      !ADR_STATUSES.has(status)
    ) {
      fail(
        `${displayPath(file)}: architecture-decision status must be one of ` +
          `${[
            ...ADR_STATUSES,
          ].join(", ")}; got ${status}`,
      );
    }

    return;
  }

  if (
    !LIFECYCLE_STATUSES.has(
      status,
    )
  ) {
    fail(
      `${displayPath(file)}: document lifecycle status must be one of ` +
        `${[
          ...LIFECYCLE_STATUSES,
        ].join(", ")}; got ${status}`,
    );
  }
}

function validateGeneratedContract(
  file,
  documentType,
  status,
) {
  const generatedPath =
    isGeneratedPath(file);

  if (generatedPath) {
    if (
      documentType !== "generated"
    ) {
      fail(
        `${displayPath(file)}: generated-path document_type must be generated; ` +
          `got ${documentType}`,
      );
    }

    if (
      status !== "generated"
    ) {
      fail(
        `${displayPath(file)}: generated-path status must be generated; got ${status}`,
      );
    }

    return;
  }

  if (
    documentType === "generated" ||
    status === "generated"
  ) {
    fail(
      `${displayPath(file)}: generated metadata is only valid under a canonical ` +
        `generated/ directory`,
    );
  }
}

function validateNoStaleSha(
  file,
  metadata,
) {
  if (
    metadata.has(
      "last_verified_sha",
    )
  ) {
    fail(
      `${displayPath(file)}: last_verified_sha is forbidden canonical metadata; ` +
        `CI/source checks own current verification`,
    );
  }
}

function validateDocumentId(
  file,
  documentId,
) {
  if (
    !/^[A-Z][A-Z0-9]*(?:-[A-Z0-9]+)*$/.test(
      documentId,
    )
  ) {
    fail(
      `${displayPath(file)}: document_id must be a stable uppercase hyphenated ID; ` +
        `got ${documentId}`,
    );
  }

  const previous =
    documentIds.get(documentId);

  if (previous) {
    fail(
      `${displayPath(file)}: duplicate document_id ${documentId}; already used by ` +
        `${displayPath(previous)}`,
    );
  } else {
    documentIds.set(
      documentId,
      file,
    );
  }
}

function validateFile(file) {
  checkedFileCount += 1;

  const text =
    readFileSync(
      file,
      "utf8",
    );

  const metadata =
    parseFrontmatter(
      file,
      text,
    );

  if (!metadata) {
    return;
  }

  const missingFields =
    REQUIRED_FIELDS.filter(
      (field) =>
        !metadata.has(field),
    );

  for (
    const field of missingFields
  ) {
    fail(
      `${displayPath(file)}: missing required metadata field ${field}`,
    );
  }

  /**
   * Avoid duplicate diagnostics such as:
   *
   *   missing owner
   *   owner must be scalar
   */
  if (
    missingFields.length > 0
  ) {
    return;
  }

  for (
    const field of LIST_FIELDS
  ) {
    expectNonEmptyList(
      file,
      metadata,
      field,
    );
  }

  const documentId =
    expectScalar(
      file,
      metadata,
      "document_id",
    );

  const documentType =
    expectScalar(
      file,
      metadata,
      "document_type",
    );

  const status =
    expectScalar(
      file,
      metadata,
      "status",
    );

  expectScalar(
    file,
    metadata,
    "owner",
  );

  validateNoStaleSha(
    file,
    metadata,
  );

  if (documentId) {
    validateDocumentId(
      file,
      documentId,
    );
  }

  if (
    documentType &&
    status
  ) {
    validateStatus(
      file,
      documentType,
      status,
    );

    validateGeneratedContract(
      file,
      documentType,
      status,
    );
  }
}

if (!existsSync(REPO_ROOT)) {
  console.error(
    `[docs-metadata] repository root does not exist: ${REPO_ROOT}`,
  );

  process.exit(1);
}

const files =
  walkMetadataDocs(
    REPO_ROOT,
  ).sort(
    (a, b) =>
      displayPath(a).localeCompare(
        displayPath(b),
        "en",
      ),
  );

for (const file of files) {
  validateFile(file);
}

if (failures.length > 0) {
  console.error(
    `[docs-metadata] FAIL — ${failures.length} metadata violation(s):`,
  );

  for (
    const failure of failures
  ) {
    console.error(
      `- ${failure}`,
    );
  }

  process.exit(1);
}

console.log(
  `[docs-metadata] PASS — ${checkedFileCount} canonical document(s), ` +
    `${documentIds.size} unique document_id value(s).`,
);
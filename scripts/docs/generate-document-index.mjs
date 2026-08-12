#!/usr/bin/env node

import {
  existsSync,
  mkdirSync,
  readdirSync,
  readFileSync,
  writeFileSync,
} from "node:fs";
import {
  dirname,
  join,
  relative,
  resolve,
  sep,
} from "node:path";
import {
  spawnSync,
} from "node:child_process";
import process from "node:process";
import { fileURLToPath } from "node:url";

const SCRIPT_PATH =
  fileURLToPath(import.meta.url);

const SCRIPT_DIR =
  dirname(SCRIPT_PATH);

const DEFAULT_REPO_ROOT =
  resolve(
    SCRIPT_DIR,
    "../..",
  );

const REPO_ROOT =
  process.env.DOCS_ROOT
    ? resolve(process.env.DOCS_ROOT)
    : DEFAULT_REPO_ROOT;

const OUTPUT_PATH =
  join(
    REPO_ROOT,
    "docs",
    "generated",
    "document-index.md",
  );

const METADATA_CHECKER =
  join(
    SCRIPT_DIR,
    "check-metadata.mjs",
  );

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
 * These trees are not part of the canonical metadata plane.
 *
 * Their existence/removal is owned by:
 *
 *   check-authority.mjs
 */
const AUTHORITY_OWNED_EXCLUSIONS = [
  "docs/engineering",
  "docs-refoundation",
];

function toPosix(value) {
  return value
    .split(sep)
    .join("/");
}

function displayPath(
  absolutePath,
) {
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
      const base =
        repoPath(tree);

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
 * Keep this scope aligned with check-metadata.mjs.
 *
 * Root routing documents intentionally do not require metadata and are
 * therefore not part of the generated metadata index.
 */
function isCanonicalMetadataPath(
  absolutePath,
) {
  const rel =
    displayPath(
      absolutePath,
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

  if (
    rel === "docs/README.md" ||
    rel === "docs/templates/README.md"
  ) {
    return false;
  }

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
  if (
    !existsSync(directory)
  ) {
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

    if (
      entry.isDirectory()
    ) {
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
      /**
       * The document index intentionally excludes itself.
       *
       * Otherwise its generated contents would depend on metadata read from
       * the previous version of itself.
       */
      if (
        resolve(absolutePath) ===
        resolve(OUTPUT_PATH)
      ) {
        continue;
      }

      acc.push(
        absolutePath,
      );
    }
  }

  return acc;
}

/**
 * The generator does not duplicate metadata validation.
 *
 * check-metadata.mjs remains the contract owner.
 *
 * Generation only proceeds after the canonical metadata plane passes that
 * checker.
 */
function runMetadataChecker() {
  if (
    !existsSync(
      METADATA_CHECKER,
    )
  ) {
    throw new Error(
      `Metadata checker is missing: ${METADATA_CHECKER}`,
    );
  }

  const result =
    spawnSync(
      process.execPath,
      [
        METADATA_CHECKER,
      ],
      {
        cwd:
          REPO_ROOT,

        env: {
          ...process.env,

          DOCS_ROOT:
            REPO_ROOT,

          FORCE_COLOR:
            "0",
        },

        encoding:
          "utf8",

        stdio: [
          "ignore",
          "pipe",
          "pipe",
        ],
      },
    );

  if (
    result.error
  ) {
    throw new Error(
      `Unable to execute metadata checker: ${result.error.message}`,
    );
  }

  if (
    result.status !== 0
  ) {
    const stdout =
      String(
        result.stdout ?? "",
      ).trim();

    const stderr =
      String(
        result.stderr ?? "",
      ).trim();

    const details =
      [
        stdout,
        stderr,
      ]
        .filter(Boolean)
        .join("\n");

    throw new Error(
      `Canonical metadata validation failed before document-index generation.` +
        (
          details
            ? `\n${details}`
            : ""
        ),
    );
  }
}

/**
 * Parse the deliberately small metadata YAML subset used by canonical
 * documentation.
 *
 * Complex YAML is unnecessary here because check-metadata.mjs has already
 * validated the contract.
 */
function parseFrontmatter(
  file,
) {
  const text =
    readFileSync(
      file,
      "utf8",
    ).replace(
      /^\uFEFF/,
      "",
    );

  const lines =
    text.split(/\r?\n/);

  if (
    lines[0] !== "---"
  ) {
    throw new Error(
      `${displayPath(file)} does not begin with canonical frontmatter`,
    );
  }

  const data =
    new Map();

  let currentListKey =
    null;

  let endFound =
    false;

  for (
    let index = 1;
    index < lines.length;
    index += 1
  ) {
    const rawLine =
      lines[index];

    if (
      rawLine === "---"
    ) {
      endFound = true;
      break;
    }

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
        continue;
      }

      const list =
        data.get(
          currentListKey,
        );

      if (
        Array.isArray(list)
      ) {
        list.push(
          unquote(
            listItem[1],
          ),
        );
      }

      continue;
    }

    const scalar =
      rawLine.match(
        /^([A-Za-z_][A-Za-z0-9_-]*):(?:\s*(.*))?$/,
      );

    if (!scalar) {
      currentListKey =
        null;

      continue;
    }

    const key =
      scalar[1];

    const rawValue =
      (
        scalar[2] ??
        ""
      ).trim();

    if (!rawValue) {
      data.set(
        key,
        [],
      );

      currentListKey =
        key;

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

    currentListKey =
      null;
  }

  if (!endFound) {
    throw new Error(
      `${displayPath(file)} has no closing frontmatter delimiter`,
    );
  }

  return data;
}

function stripInlineComment(
  value,
) {
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
    value.indexOf(
      " #",
    );

  return index === -1
    ? value
    : value
        .slice(
          0,
          index,
        )
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

function scalar(
  metadata,
  key,
) {
  const value =
    metadata.get(key);

  if (
    value == null ||
    Array.isArray(value)
  ) {
    throw new Error(
      `Expected scalar metadata field: ${key}`,
    );
  }

  return value;
}

function list(
  metadata,
  key,
) {
  const value =
    metadata.get(key);

  if (
    !Array.isArray(value)
  ) {
    throw new Error(
      `Expected list metadata field: ${key}`,
    );
  }

  return value;
}

function markdownEscape(
  value,
) {
  return String(value)
    .replace(
      /\|/g,
      "\\|",
    )
    .replace(
      /\r?\n/g,
      " ",
    );
}

function inlineCodeList(
  values,
) {
  if (
    values.length === 0
  ) {
    return "_(none)_";
  }

  return values
    .map(
      (value) =>
        `\`${markdownEscape(value)}\``,
    )
    .join(", ");
}

function sourceMarkdownLink(
  file,
) {
  const relativeFromOutput =
    toPosix(
      relative(
        dirname(OUTPUT_PATH),
        file,
      ),
    );

  const normalized =
    relativeFromOutput.startsWith(".")
      ? relativeFromOutput
      : `./${relativeFromOutput}`;

  return (
    `[\`${displayPath(file)}\`]` +
    `(${normalized})`
  );
}

function collectDocuments() {
  const files =
    walkMetadataDocs(
      REPO_ROOT,
    );

  const documents =
    files.map(
      (file) => {
        const metadata =
          parseFrontmatter(
            file,
          );

        return {
          file,

          path:
            displayPath(file),

          documentId:
            scalar(
              metadata,
              "document_id",
            ),

          documentType:
            scalar(
              metadata,
              "document_type",
            ),

          status:
            scalar(
              metadata,
              "status",
            ),

          owner:
            scalar(
              metadata,
              "owner",
            ),

          appliesTo:
            list(
              metadata,
              "applies_to",
            ),

          evidence:
            list(
              metadata,
              "evidence",
            ),

          reviewOn:
            list(
              metadata,
              "review_on",
            ),
        };
      },
    );

  documents.sort(
    (left, right) => {
      const idCompare =
        left.documentId.localeCompare(
          right.documentId,
          "en",
          {
            numeric: true,
          },
        );

      if (
        idCompare !== 0
      ) {
        return idCompare;
      }

      return left.path.localeCompare(
        right.path,
        "en",
      );
    },
  );

  return documents;
}

function countBy(
  documents,
  selector,
) {
  const counts =
    new Map();

  for (
    const document of
      documents
  ) {
    const key =
      selector(document);

    counts.set(
      key,
      (
        counts.get(key) ??
        0
      ) + 1,
    );
  }

  return [
    ...counts.entries(),
  ].sort(
    ([a], [b]) =>
      a.localeCompare(
        b,
        "en",
      ),
  );
}

function renderSummaryTable(
  title,
  entries,
) {
  const lines = [
    `## ${title}`,
    "",
    "| Value | Count |",
    "|:---|---:|",
  ];

  for (
    const [
      value,
      count,
    ] of entries
  ) {
    lines.push(
      `| \`${markdownEscape(value)}\` | ${count} |`,
    );
  }

  lines.push("");

  return lines;
}

function generateDocumentIndex() {
  const documents =
    collectDocuments();

  const lines = [
    "---",
    "document_id: DOC-GEN-DOCUMENT-INDEX",
    "document_type: generated",
    "status: generated",
    "owner: documentation-governance",
    "applies_to:",
    "  - repository-documentation-inventory",
    "  - documentation-discovery",
    "  - documentation-governance-evidence",
    "evidence:",
    "  - scripts/docs/generate-document-index.mjs",
    "  - scripts/docs/check-metadata.mjs",
    "  - docs/governance/documentation-authority.md",
    "  - docs/governance/documentation-lifecycle.md",
    "review_on:",
    "  - canonical-document-added",
    "  - canonical-document-removed",
    "  - document-metadata-change",
    "  - documentation-index-generator-change",
    "---",
    "",
    "# Notrelix Documentation Index",
    "",
    "<!-- GENERATED FILE — DO NOT EDIT. -->",
    "<!-- Producer: scripts/docs/generate-document-index.mjs -->",
    "<!-- Source: canonical document frontmatter -->",
    "<!-- Regenerate: node scripts/docs/generate-document-index.mjs -->",
    "<!-- Check drift: node scripts/docs/generate-document-index.mjs --check -->",
    "",
    "> This file is generated discovery evidence.",
    "> It does not replace the canonical authority described by each source document.",
    "",
    `Document count: ${documents.length}`,
    "",
  ];

  lines.push(
    ...renderSummaryTable(
      "Documents by type",
      countBy(
        documents,
        (document) =>
          document.documentType,
      ),
    ),
  );

  lines.push(
    ...renderSummaryTable(
      "Documents by status",
      countBy(
        documents,
        (document) =>
          document.status,
      ),
    ),
  );

  lines.push(
    "## Document inventory",
    "",
    "| Document ID | Type | Status | Owner | Applies to | Source |",
    "|:---|:---|:---|:---|:---|:---|",
  );

  for (
    const document of
      documents
  ) {
    lines.push(
      `| \`${markdownEscape(document.documentId)}\` ` +
        `| \`${markdownEscape(document.documentType)}\` ` +
        `| \`${markdownEscape(document.status)}\` ` +
        `| \`${markdownEscape(document.owner)}\` ` +
        `| ${inlineCodeList(document.appliesTo)} ` +
        `| ${sourceMarkdownLink(document.file)} |`,
    );
  }

  lines.push(
    "",
    "## Generation contract",
    "",
    "This index is derived from canonical document frontmatter.",
    "",
    "To change a row:",
    "",
    "```text",
    "edit the canonical source document metadata",
    "→ run check-metadata.mjs",
    "→ regenerate this index",
    "```",
    "",
    "Do not edit this generated table manually.",
    "",
  );

  return lines.join(
    "\n",
  );
}

function ensureOutputDirectory() {
  mkdirSync(
    dirname(
      OUTPUT_PATH,
    ),
    {
      recursive:
        true,
    },
  );
}

function writeOutput(
  content,
) {
  ensureOutputDirectory();

  writeFileSync(
    OUTPUT_PATH,
    content,
    "utf8",
  );

  console.log(
    `[docs-document-index] WROTE ${displayPath(OUTPUT_PATH)}`,
  );
}

function checkOutput(
  expected,
) {
  if (
    !existsSync(
      OUTPUT_PATH,
    )
  ) {
    console.error(
      `[docs-document-index] FAIL — generated target is missing: ` +
        `${displayPath(OUTPUT_PATH)}`,
    );

    process.exit(1);
  }

  const actual =
    readFileSync(
      OUTPUT_PATH,
      "utf8",
    );

  if (
    actual !== expected
  ) {
    console.error(
      `[docs-document-index] FAIL — ${displayPath(OUTPUT_PATH)} is stale.`,
    );

    console.error(
      `Run: node scripts/docs/generate-document-index.mjs`,
    );

    process.exit(1);
  }

  console.log(
    `[docs-document-index] PASS — ${displayPath(OUTPUT_PATH)} is in sync.`,
  );
}

function main() {
  const checkMode =
    process.argv.includes(
      "--check",
    );

  const stdoutMode =
    process.argv.includes(
      "--stdout",
    );

  if (
    checkMode &&
    stdoutMode
  ) {
    console.error(
      `[docs-document-index] --check and --stdout are mutually exclusive.`,
    );

    process.exit(2);
  }

  runMetadataChecker();

  const content =
    generateDocumentIndex();

  if (
    stdoutMode
  ) {
    process.stdout.write(
      content,
    );

    return;
  }

  if (
    checkMode
  ) {
    checkOutput(
      content,
    );

    return;
  }

  writeOutput(
    content,
  );
}

main();
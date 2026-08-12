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
    "rule-index.md",
  );

const RULE_CHECKER =
  join(
    SCRIPT_DIR,
    "check-rule-ids.mjs",
  );

/**
 * Root documents that may own stable rule declarations.
 *
 * Keep this aligned with check-rule-ids.mjs.
 */
const ROOT_RULE_SOURCE_NAMES =
  new Set([
    "RULE.md",
    "DESIGN.md",
    "CONTEXT-MAP.md",
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

    /**
     * Generated files must never become rule-definition authority.
     */
    "generated",

    /**
     * Templates may intentionally show example/placeholder IDs.
     */
    "templates",
  ]);

const RETIRED_DOC_TREES = [
  "docs/engineering",
  "docs-refoundation",

  "frontend/docs/client",
  "frontend/docs/client-architecture",
  "frontend/docs/plans",
];

/**
 * ADR IDs are historical decision IDs, not rule IDs.
 */
const DECISION_ID_PREFIXES =
  new Set([
    "ADR",
    "SYS-ADR",
    "FE-ADR",
  ]);

/**
 * Only headings beginning with one of these governance stems can become
 * generated rule-index entries.
 *
 * Validation of the complete prefix belongs to check-rule-ids.mjs.
 */
const RULE_NAMESPACE_STEMS =
  new Set([
    "NRX",
    "DOC",

    "SYS",
    "PROD",
    "DEC",

    "ACC",
    "ID",
    "WSP",
    "GOV",
    "WM",
    "DCT",
    "COL",
    "AUT",
    "INT",
    "BIL",
    "ANA",

    "BE",
    "FE",

    "QLT",
    "DEL",
    "OPS",
    "INFRA",
  ]);

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

function isRetiredPath(
  absolutePath,
) {
  return RETIRED_DOC_TREES.some(
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
 * Keep rule-source scope aligned with check-rule-ids.mjs.
 */
function isCanonicalRuleSource(
  absolutePath,
) {
  const rel =
    displayPath(
      absolutePath,
    );

  if (
    isRetiredPath(
      absolutePath,
    )
  ) {
    return false;
  }

  if (
    !rel.includes("/")
    ) {
    return ROOT_RULE_SOURCE_NAMES.has(
        rel,
    );
  }

  if (
    rel.startsWith(
      "docs/",
    )
  ) {
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
    rel.startsWith(
      "backend/docs/architecture/",
    ) ||
    rel.startsWith(
      "backend/docs/operations/",
    ) ||
    rel.startsWith(
      "backend/docs/decisions/",
    )
  ) {
    return true;
  }

  if (
    rel === "frontend/README.md" ||
    rel === "frontend/AGENTS.md" ||
    rel === "frontend/docs/README.md"
  ) {
    return true;
  }

  return (
    rel.startsWith(
      "frontend/docs/architecture/",
    ) ||
    rel.startsWith(
      "frontend/docs/decisions/",
    )
  );
}

function walkMarkdown(
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
        withFileTypes:
          true,
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
        isRetiredPath(
          absolutePath,
        )
      ) {
        continue;
      }

      walkMarkdown(
        absolutePath,
        acc,
      );

      continue;
    }

    if (
      entry.isFile() &&
      entry.name.endsWith(
        ".md",
      ) &&
      isCanonicalRuleSource(
        absolutePath,
      )
    ) {
      acc.push(
        absolutePath,
      );
    }
  }

  return acc;
}

/**
 * Rule validation remains owned by check-rule-ids.mjs.
 *
 * The generator refuses to produce an index from invalid/duplicate rule
 * definitions.
 */
function runRuleChecker() {
  if (
    !existsSync(
      RULE_CHECKER,
    )
  ) {
    throw new Error(
      `Rule checker is missing: ${RULE_CHECKER}`,
    );
  }

  const result =
    spawnSync(
      process.execPath,
      [
        RULE_CHECKER,
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
      `Unable to execute rule checker: ${result.error.message}`,
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
      `Rule-ID validation failed before rule-index generation.` +
        (
          details
            ? `\n${details}`
            : ""
        ),
    );
  }
}

function stripInlineCode(
  value,
) {
  let result = "";

  for (
    let index = 0;
    index < value.length;
  ) {
    if (
      value[index] !== "`"
    ) {
      result += value[index];
      index += 1;
      continue;
    }

    let tickCount = 1;

    while (
      value[index + tickCount] ===
      "`"
    ) {
      tickCount += 1;
    }

    const delimiter =
      "`".repeat(
        tickCount,
      );

    const close =
      value.indexOf(
        delimiter,
        index + tickCount,
      );

    if (
      close === -1
    ) {
      result +=
        value.slice(index);

      break;
    }

    result +=
      value.slice(
        index + tickCount,
        close,
      );

    index =
      close + tickCount;
  }

  return result;
}

/**
 * Extract ATX headings outside fenced code.
 *
 * A stable rule declaration must be an explicit Markdown heading.
 */
function headingLines(
  text,
) {
  const lines =
    text.split(/\r?\n/);

  const headings = [];

  let fence = null;

  for (
    let index = 0;
    index < lines.length;
    index += 1
  ) {
    const line =
      lines[index];

    const trimmed =
      line.trimStart();

    const fenceMatch =
      trimmed.match(
        /^(```+|~~~+)/,
      );

    if (fenceMatch) {
      const marker =
        fenceMatch[1][0];

      const width =
        fenceMatch[1].length;

      if (
        fence == null
      ) {
        fence = {
          marker,
          width,
        };
      } else if (
        fence.marker ===
          marker &&
        fenceMatch[1].length >=
          fence.width
      ) {
        fence = null;
      }

      continue;
    }

    if (
      fence != null
    ) {
      continue;
    }

    const heading =
      line.match(
        /^\s{0,3}#{1,6}\s+(.+?)\s*#*\s*$/,
      );

    if (!heading) {
      continue;
    }

    headings.push({
      line:
        index + 1,

      text:
        stripInlineCode(
          heading[1],
        )
          .replace(
            /[*_~]/g,
            "",
          )
          .trim(),
    });
  }

  return headings;
}

/**
 * Strip optional document-section numbering:
 *
 *   12. FE-STATE-001 — ...
 *   12.4. FE-STATE-001 — ...
 *
 * →
 *
 *   FE-STATE-001 — ...
 */
function stripSectionNumber(
  heading,
) {
  return heading.replace(
    /^\d+(?:\.\d+)*\.?\s+/,
    "",
  );
}

/**
 * Parse the leading stable rule ID.
 *
 * Decision IDs such as FE-ADR-005 are deliberately excluded later.
 */
function parseRuleHeading(
  heading,
) {
  const normalized =
    stripSectionNumber(
      heading,
    );

  const match =
    normalized.match(
      /^([A-Z][A-Z0-9]*(?:-[A-Z0-9]+)*)-(\d{3})(?=\s|—|–|-|:|$)/,
    );

  if (!match) {
    return null;
  }

  const prefix =
    match[1];

  const number =
    match[2];

  const full =
    `${prefix}-${number}`;

  if (
    DECISION_ID_PREFIXES.has(
      prefix,
    )
  ) {
    return null;
  }

  const stem =
    prefix.split(
      "-",
      1,
    )[0];

  if (
    !RULE_NAMESPACE_STEMS.has(
      stem,
    )
  ) {
    return null;
  }

  const remainder =
    normalized
      .slice(
        match[0].length,
      )
      .replace(
        /^\s*(?:—|–|-|:)\s*/,
        "",
      )
      .trim();

  return {
    id:
      full,

    prefix,

    number:
      Number(number),

    title:
      remainder ||
      "_Untitled rule_",
  };
}

function sourceMarkdownLink(
  file,
) {
  const relativeFromOutput =
    toPosix(
      relative(
        dirname(
          OUTPUT_PATH,
        ),
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

function collectRules() {
  const files =
    walkMarkdown(
      REPO_ROOT,
    );

  const rules = [];

  for (
    const file of files
  ) {
    const text =
      readFileSync(
        file,
        "utf8",
      );

    for (
      const heading of
        headingLines(text)
    ) {
      const parsed =
        parseRuleHeading(
          heading.text,
        );

      if (!parsed) {
        continue;
      }

      rules.push({
        ...parsed,

        file,

        path:
          displayPath(file),

        line:
          heading.line,
      });
    }
  }

  /**
   * check-rule-ids.mjs has already proven uniqueness.
   *
   * This defensive check protects against future parser divergence.
   */
  const ids =
    new Map();

  for (
    const rule of rules
  ) {
    const previous =
      ids.get(
        rule.id,
      );

    if (previous) {
      throw new Error(
        `Rule-index parser found duplicate ${rule.id}: ` +
          `${previous.path}:${previous.line} and ` +
          `${rule.path}:${rule.line}`,
      );
    }

    ids.set(
      rule.id,
      rule,
    );
  }

  rules.sort(
    (left, right) => {
      const prefixCompare =
        left.prefix.localeCompare(
          right.prefix,
          "en",
        );

      if (
        prefixCompare !== 0
      ) {
        return prefixCompare;
      }

      if (
        left.number !==
        right.number
      ) {
        return (
          left.number -
          right.number
        );
      }

      return left.path.localeCompare(
        right.path,
        "en",
      );
    },
  );

  return rules;
}

function countByPrefix(
  rules,
) {
  const counts =
    new Map();

  for (
    const rule of rules
  ) {
    counts.set(
      rule.prefix,
      (
        counts.get(
          rule.prefix,
        ) ??
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

function generateRuleIndex() {
  const rules =
    collectRules();

  const prefixCounts =
    countByPrefix(
      rules,
    );

  const lines = [
    "---",
    "document_id: DOC-GEN-RULE-INDEX",
    "document_type: generated",
    "status: generated",
    "owner: documentation-governance",
    "applies_to:",
    "  - repository-rule-inventory",
    "  - architecture-rule-discovery",
    "  - documentation-governance-evidence",
    "evidence:",
    "  - scripts/docs/generate-rule-index.mjs",
    "  - scripts/docs/check-rule-ids.mjs",
    "  - docs/governance/documentation-authority.md",
    "  - docs/governance/documentation-quality-gates.md",
    "review_on:",
    "  - canonical-rule-added",
    "  - canonical-rule-removed",
    "  - canonical-rule-renamed",
    "  - rule-namespace-change",
    "  - rule-index-generator-change",
    "---",
    "",
    "# Notrelix Rule Index",
    "",
    "<!-- GENERATED FILE — DO NOT EDIT. -->",
    "<!-- Producer: scripts/docs/generate-rule-index.mjs -->",
    "<!-- Source: stable rule headings in canonical authored documentation -->",
    "<!-- Regenerate: node scripts/docs/generate-rule-index.mjs -->",
    "<!-- Check drift: node scripts/docs/generate-rule-index.mjs --check -->",
    "",
    "> This file is generated discovery evidence.",
    "> Rule meaning remains owned by the canonical source document in which the rule is declared.",
    "",
    `Rule count: ${rules.length}`,
    "",
    "## Namespace summary",
    "",
    "| Namespace | Count |",
    "|:---|---:|",
  ];

  for (
    const [
      prefix,
      count,
    ] of prefixCounts
  ) {
    lines.push(
      `| \`${prefix}\` | ${count} |`,
    );
  }

  lines.push(
    "",
    "## Rule inventory",
    "",
    "| Rule ID | Namespace | Rule | Source |",
    "|:---|:---|:---|:---|",
  );

  for (
    const rule of rules
  ) {
    lines.push(
      `| \`${rule.id}\` ` +
        `| \`${rule.prefix}\` ` +
        `| ${markdownEscape(rule.title)} ` +
        `| ${sourceMarkdownLink(rule.file)} |`,
    );
  }

  lines.push(
    "",
    "## Generation contract",
    "",
    "Stable rules are declared in authored canonical Markdown headings.",
    "",
    "Example:",
    "",
    "```markdown",
    "## FE-STATE-001 — Server state remains backend-authoritative",
    "```",
    "",
    "To change the rule index:",
    "",
    "```text",
    "change the canonical source rule",
    "→ run check-rule-ids.mjs",
    "→ regenerate this index",
    "```",
    "",
    "Do not edit this generated index manually.",
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
    `[docs-rule-index] WROTE ${displayPath(OUTPUT_PATH)}`,
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
      `[docs-rule-index] FAIL — generated target is missing: ` +
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
      `[docs-rule-index] FAIL — ${displayPath(OUTPUT_PATH)} is stale.`,
    );

    console.error(
      `Run: node scripts/docs/generate-rule-index.mjs`,
    );

    process.exit(1);
  }

  console.log(
    `[docs-rule-index] PASS — ${displayPath(OUTPUT_PATH)} is in sync.`,
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
      `[docs-rule-index] --check and --stdout are mutually exclusive.`,
    );

    process.exit(2);
  }

  runRuleChecker();

  const content =
    generateRuleIndex();

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
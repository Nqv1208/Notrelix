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

/**
 * Root documents allowed to DECLARE stable rule IDs.
 *
 * Not every root documentation file is a rule-definition authority.
 *
 * PRODUCT.md is intentionally excluded:
 *
 *   PRODUCT.md
 *     → product entry/constitution
 *
 *   docs/product/product-model.md
 *     → canonical PROD-* rule owner
 *
 * README / AGENTS / CONTEXT are routing/context documents and should not
 * accidentally become stable rule-definition authorities either.
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
     * Generated documents repeat/reference canonical information and must not
     * become rule-definition authority.
     */
    "generated",

    /**
     * Templates may contain example/placeholder rule IDs.
     *
     * They are not live rule declarations.
     */
    "templates",
  ]);

/**
 * Legacy authority trees are excluded from rule scanning.
 *
 * Their existence is handled by check-authority.mjs.
 */
const RETIRED_DOC_TREES = [
  "docs/engineering",
  "docs-refoundation",

  "frontend/docs/client",
  "frontend/docs/client-architecture",
  "frontend/docs/plans",
];

/**
 * Explicit live rule namespaces.
 *
 * This registry is intentionally stricter than:
 *
 *   /^[A-Z-]+-\d+$/
 *
 * because a typo such as:
 *
 *   FE-STAE-001
 *
 * must not silently become a new architecture namespace.
 *
 * Some sub-families exist because approved canonical documents already
 * divide the larger governance plane into stable semantic groups.
 */
const ALLOWED_RULE_PREFIXES =
  new Set([
    /**
     * Repository / documentation governance.
     */
    "NRX",
    "DOC",

    /**
     * System architecture.
     */
    "SYS",
    "SYS-CTX",
    "SYS-CON",
    "SYS-DATA",
    "SYS-EVT",
    "SYS-RT",
    "SYS-EXT",
    "SYS-ACT",
    "SYS-NOTIF",
    "SYS-AUD",
    "SYS-OBS",

    /**
     * Product.
     */
    "PROD",
    "PROD-UX",

    /**
     * System decision governance.
     */
    "DEC",

    /**
     * Product bounded-context local rules.
     */
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

    /**
     * Backend architecture.
     */
    "BE-DOM",
    "BE-APP",
    "BE-INF",
    "BE-PLT",
    "BE-API",
    "BE-SEC",
    "BE-TST",

    /**
     * Backend decision / operations governance.
     */
    "BE-DEC",
    "BE-OPS-CFG",
    "BE-OPS-DATA",

    /**
     * Frontend.
     */
    "FE-ARCH",
    "FE-ARCH-CHG",
    "FE-DEP",
    "FE-HOST",
    "FE-API",
    "FE-STATE",
    "FE-RT",
    "FE-UI",
    "FE-TST",
    "FE-DEC",

    /**
     * Quality.
     */
    "QLT",
    "QLT-TST",
    "QLT-SEC",
    "QLT-A11Y",
    "QLT-PERF",

    /**
     * Delivery.
     */
    "DEL",
    "DEL-CHG",
    "DEL-CON",
    "DEL-DONE",
    "DEL-REL",
    "DEL-MIG",
    "DEL-OWN",
    "DEL-DEV",

    /**
     * Operations.
     */
    "OPS",
    "OPS-OBS",
    "OPS-INC",
    "OPS-REC",
    "OPS-DEG",

    /**
     * Infrastructure.
     */
    "INFRA",
    "INFRA-ENV",
    "INFRA-RUN",
    "INFRA-CTR",
  ]);

/**
 * ADR identifiers are decision IDs, not rule IDs.
 *
 * Examples:
 *
 *   ADR-001
 *   SYS-ADR-001
 *   FE-ADR-001
 *
 * They are governed by check-authority.mjs / decision registry logic.
 */
const DECISION_ID_PREFIXES =
  new Set([
    "ADR",
    "SYS-ADR",
    "FE-ADR",
  ]);

/**
 * Used to distinguish:
 *
 *   FE-BAD-001
 *
 * from an unrelated uppercase heading such as:
 *
 *   HTTP-200
 *
 * Only known governance-family stems are treated as attempted rule
 * namespaces.
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

const PRODUCT_CONTEXT_PREFIXES =
  new Set([
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
  ]);

const failures = [];

/**
 * Full rule ID:
 *
 *   FE-STATE-001
 *
 * →
 *
 *   {
 *     file,
 *     line
 *   }
 */
const declarations =
  new Map();

/**
 * Used only for success diagnostics.
 */
const countsByPrefix =
  new Map();

let markdownFileCount = 0;
let ruleCount = 0;

function fail(message) {
  failures.push(message);
}

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
 * Rule definitions are scanned only from authored canonical documentation.
 *
 * Generated outputs and templates are excluded because they may repeat or
 * demonstrate rule IDs without owning them.
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

  if (!rel.includes("/")) {
    return ROOT_RULE_SOURCE_NAMES.has(
        rel,
    );
    }

  /**
   * Repository/system authored docs.
   */
  if (
    rel.startsWith("docs/")
  ) {
    return true;
  }

  /**
   * Backend root/local guidance.
   */
  if (
    rel === "backend/README.md" ||
    rel === "backend/AGENTS.md" ||
    rel === "backend/CONTEXT.md" ||
    rel === "backend/tests/AGENTS.md" ||
    rel === "backend/docs/README.md"
  ) {
    return true;
  }

  /**
   * Backend canonical authored plane.
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
    )
  ) {
    return true;
  }

  /**
   * Frontend root guidance.
   */
  if (
    rel === "frontend/README.md" ||
    rel === "frontend/AGENTS.md" ||
    rel === "frontend/docs/README.md"
  ) {
    return true;
  }

  /**
   * Frontend canonical authored plane.
   */
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
      entry.name.endsWith(".md") &&
      isCanonicalRuleSource(
        absolutePath,
      )
    ) {
      acc.push(absolutePath);
    }
  }

  return acc;
}

/**
 * A rule heading may wrap the rule ID in inline-code formatting:
 *
 *   ## `DOC-001` — One authority
 *
 * We want:
 *
 *   DOC-001 — One authority
 *
 * for declaration parsing.
 */
function stripInlineCode(value) {
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
      "`".repeat(tickCount);

    const close =
      value.indexOf(
        delimiter,
        index + tickCount,
      );

    if (close === -1) {
      result += value.slice(index);
      break;
    }

    /**
     * Keep the content, remove only the Markdown ticks.
     */
    result += value.slice(
      index + tickCount,
      close,
    );

    index =
      close + tickCount;
  }

  return result;
}

/**
 * Return ATX headings outside fenced code blocks.
 *
 * Setext headings are intentionally not accepted as rule declarations.
 *
 * Stable rule definitions should be explicit:
 *
 *   ## DOC-001 — ...
 *
 * rather than:
 *
 *   DOC-001
 *   -------
 */
function headingLines(text) {
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

      if (fence == null) {
        fence = {
          marker,
          width,
        };
      } else if (
        fence.marker === marker &&
        fenceMatch[1].length >=
          fence.width
      ) {
        fence = null;
      }

      continue;
    }

    if (fence != null) {
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
 * Rule declarations must begin the heading after an optional numeric
 * documentation section:
 *
 *   # DOC-001 — ...
 *
 *   # 12. DOC-001 — ...
 *
 *   # 12.4. DOC-001 — ...
 *
 * This intentionally does NOT treat:
 *
 *   # Relationship between DOC-001 and DOC-002
 *
 * as another rule declaration.
 */
function declarationCandidate(
  headingText,
) {
  const normalized =
    headingText.replace(
      /^\d+(?:\.\d+)*\.?\s+/,
      "",
    );

  const match =
    normalized.match(
      /^([A-Z][A-Z0-9]*(?:-[A-Z0-9]+)*)-(\d+)(?=\s|—|–|-|:|$)/,
    );

  if (!match) {
    return null;
  }

  return {
    prefix:
      match[1],

    number:
      match[2],

    full:
      `${match[1]}-${match[2]}`,
  };
}

function topLevelStem(prefix) {
  return prefix
    .split("-", 1)[0];
}

/**
 * A stable rule namespace has an ownership plane.
 *
 * The checker does not attempt semantic NLP.
 *
 * It simply prevents obvious authority leakage such as:
 *
 *   FE-STATE-001
 *
 * being declared in:
 *
 *   backend/docs/...
 */
function validatePlacement(
  prefix,
  file,
) {
  const rel =
    displayPath(file);

  const allowed = (() => {
    /**
     * Repository rules belong to root docs.
     */
    if (prefix === "NRX") {
      return !rel.includes("/");
    }

    /**
     * Documentation governance.
     */
    if (prefix === "DOC") {
      return rel.startsWith(
        "docs/governance/",
      );
    }

    /**
     * System architecture.
     */
    if (
      prefix === "SYS" ||
      prefix.startsWith("SYS-")
    ) {
      return (
        rel.startsWith(
          "docs/architecture/",
        ) ||
        rel === "DESIGN.md" ||
        rel === "CONTEXT-MAP.md"
      );
    }

    /**
     * Product-global rules.
     */
    if (
      prefix === "PROD" ||
      prefix.startsWith("PROD-")
    ) {
      return (
        rel.startsWith(
          "docs/product/",
        ) ||
        rel === "PRODUCT.md"
      );
    }

    /**
     * Bounded-context product rules.
     */
    if (
      PRODUCT_CONTEXT_PREFIXES.has(
        prefix,
      )
    ) {
      return rel.startsWith(
        "docs/product/",
      );
    }

    /**
     * System decision governance.
     *
     * DEC-* belongs to the system decision registry, while frontend
     * decision governance uses FE-DEC-*.
     */
    if (prefix === "DEC") {
      return rel.startsWith(
        "docs/decisions/",
      );
    }

    /**
     * Backend decision governance.
     */
    if (
    prefix === "BE-DEC"
    ) {
    return rel.startsWith(
        "backend/docs/decisions/",
    );
    }

    /**
     * Backend runtime/configuration operations.
     */
    if (
    prefix === "BE-OPS-CFG"
    ) {
    return (
        rel ===
        "backend/docs/operations/configuration-and-runtime.md"
    );
    }

    /**
     * Backend data/migration operations.
     */
    if (
    prefix === "BE-OPS-DATA"
    ) {
    return (
        rel ===
        "backend/docs/operations/migrations-and-data-change.md"
    );
    }

    /**
     * Remaining backend architecture rules.
     */
    if (
    prefix.startsWith("BE-")
    ) {
    return rel.startsWith(
        "backend/",
    );
    }

    /**
     * Frontend decision governance gets the tighter decision path.
     */
    if (
      prefix === "FE-DEC"
    ) {
      return rel.startsWith(
        "frontend/docs/decisions/",
      );
    }

    /**
     * Other frontend architecture rules.
     */
    if (
      prefix.startsWith("FE-")
    ) {
      return rel.startsWith(
        "frontend/",
      );
    }

    /**
     * Quality.
     */
    if (
      prefix === "QLT" ||
      prefix.startsWith("QLT-")
    ) {
      return rel.startsWith(
        "docs/quality/",
      );
    }

    /**
     * Delivery.
     */
    if (
      prefix === "DEL" ||
      prefix.startsWith("DEL-")
    ) {
      return rel.startsWith(
        "docs/delivery/",
      );
    }

    /**
     * Operations.
     */
    if (
      prefix === "OPS" ||
      prefix.startsWith("OPS-")
    ) {
      return rel.startsWith(
        "docs/operations/",
      );
    }

    /**
     * Infrastructure.
     */
    if (
      prefix === "INFRA" ||
      prefix.startsWith("INFRA-")
    ) {
      return rel.startsWith(
        "docs/infrastructure/",
      );
    }

    return true;
  })();

  if (!allowed) {
    fail(
      `[RULE_PLACEMENT] ${rel}: rule prefix ${prefix} ` +
        `is not owned by this documentation plane`,
    );
  }
}

function validateFile(file) {
  markdownFileCount += 1;

  const text =
    readFileSync(
      file,
      "utf8",
    );

  for (
    const heading of
      headingLines(text)
  ) {
    const candidate =
      declarationCandidate(
        heading.text,
      );

    if (!candidate) {
      continue;
    }

    /**
     * Decision IDs are not rule IDs.
     *
     * This also safely ignores headings such as:
     *
     *   FE-ADR-005-D1
     *
     * because the rule candidate is FE-ADR-005 and FE-ADR is explicitly
     * decision-owned.
     */
    if (
      DECISION_ID_PREFIXES.has(
        candidate.prefix,
      )
    ) {
      continue;
    }

    const stem =
      topLevelStem(
        candidate.prefix,
      );

    /**
     * Do not interpret unrelated uppercase identifiers as documentation
     * rules.
     */
    if (
      !RULE_NAMESPACE_STEMS.has(
        stem,
      )
    ) {
      continue;
    }

    /**
     * A recognized governance stem with an unknown full prefix is almost
     * certainly a typo or unauthorized namespace.
     *
     * Example:
     *
     *   FE-STAE-001
     */
    if (
      !ALLOWED_RULE_PREFIXES.has(
        candidate.prefix,
      )
    ) {
      fail(
        `[RULE_NAMESPACE] ${displayPath(file)}:${heading.line}: ` +
          `unsupported rule prefix ${candidate.prefix} in ${candidate.full}`,
      );

      continue;
    }

    /**
     * Stable rules use three-digit suffixes:
     *
     *   DOC-001
     *
     * not:
     *
     *   DOC-1
     *   DOC-01
     *   DOC-0001
     */
    if (
      !/^\d{3}$/.test(
        candidate.number,
      )
    ) {
      fail(
        `[RULE_FORMAT] ${displayPath(file)}:${heading.line}: ` +
          `rule id ${candidate.full} must use a three-digit numeric suffix`,
      );

      continue;
    }

    ruleCount += 1;

    validatePlacement(
      candidate.prefix,
      file,
    );

    const previous =
      declarations.get(
        candidate.full,
      );

    if (previous) {
      fail(
        `[RULE_DUPLICATE] ${candidate.full} declared at ` +
          `${previous.file}:${previous.line} and ` +
          `${displayPath(file)}:${heading.line}`,
      );
    } else {
      declarations.set(
        candidate.full,
        {
          file:
            displayPath(file),

          line:
            heading.line,
        },
      );
    }

    countsByPrefix.set(
      candidate.prefix,
      (
        countsByPrefix.get(
          candidate.prefix,
        ) ?? 0
      ) + 1,
    );
  }
}

if (
  !existsSync(REPO_ROOT)
) {
  console.error(
    `[docs-rule-ids] repository root does not exist: ${REPO_ROOT}`,
  );

  process.exit(1);
}

const files =
  walkMarkdown(
    REPO_ROOT,
  ).sort(
    (a, b) =>
      displayPath(a).localeCompare(
        displayPath(b),
        "en",
      ),
  );

for (
  const file of files
) {
  validateFile(file);
}

if (
  failures.length > 0
) {
  console.error(
    `[docs-rule-ids] FAIL — ${failures.length} rule-id violation(s):`,
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

const prefixSummary =
  [
    ...countsByPrefix.entries(),
  ]
    .sort(
      ([a], [b]) =>
        a.localeCompare(
          b,
          "en",
        ),
    )
    .map(
      ([prefix, count]) =>
        `${prefix}=${count}`,
    )
    .join(", ");

console.log(
  `[docs-rule-ids] PASS — ` +
    `${markdownFileCount} Markdown file(s), ` +
    `${ruleCount} rule declaration(s), ` +
    `${declarations.size} unique rule id(s)` +
    `${prefixSummary ? `; ${prefixSummary}` : ""}.`,
);
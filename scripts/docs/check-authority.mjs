#!/usr/bin/env node

import {
  existsSync,
  readdirSync,
  readFileSync,
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

const SCRIPT_PATH =
  fileURLToPath(import.meta.url);

const DEFAULT_REPO_ROOT =
  resolve(dirname(SCRIPT_PATH), "../..");

const REPO_ROOT =
  process.env.DOCS_ROOT
    ? resolve(process.env.DOCS_ROOT)
    : DEFAULT_REPO_ROOT;

/**
 * Authored canonical documentation required by the finalized
 * documentation architecture.
 *
 * Generated documents are intentionally NOT required here.
 *
 * Generated artifact existence/drift belongs to:
 *
 *   check-generated.mjs
 *
 * This checker owns authored authority presence and retired-authority
 * rejection.
 */
const REQUIRED_AUTHORED_PATHS = [
  /**
   * Root authority / routing documents.
   */
  "README.md",
  "PRODUCT.md",
  "RULE.md",
  "AGENTS.md",
  "DESIGN.md",
  "CONTEXT.md",
  "CONTEXT-MAP.md",

  /**
   * Repository documentation router + governance.
   */
  "docs/README.md",

  "docs/governance/documentation-authority.md",
  "docs/governance/documentation-lifecycle.md",
  "docs/governance/topic-authority-map.md",
  "docs/governance/decision-and-exception-policy.md",
  "docs/governance/documentation-quality-gates.md",

  /**
   * System architecture.
   */
  "docs/architecture/system-overview.md",
  "docs/architecture/bounded-context-map.md",
  "docs/architecture/contract-boundaries.md",
  "docs/architecture/data-ownership-and-consistency.md",
  "docs/architecture/events-realtime-and-delivery-boundary.md",
  "docs/architecture/capability-extraction-strategy.md",

  /**
   * Product authority.
   */
  "docs/product/README.md",
  "docs/product/product-model.md",
  "docs/product/product-experience.md",

  "docs/product/accounts.md",
  "docs/product/identity.md",
  "docs/product/workspaces.md",
  "docs/product/governance.md",
  "docs/product/work-management.md",
  "docs/product/documents.md",
  "docs/product/collaboration.md",
  "docs/product/automation.md",
  "docs/product/integrations.md",
  "docs/product/billing.md",
  "docs/product/analytics.md",

  /**
   * Quality.
   */
  "docs/quality/engineering-quality-standard.md",
  "docs/quality/testing-strategy.md",
  "docs/quality/security-quality-standard.md",
  "docs/quality/accessibility-standard.md",
  "docs/quality/performance-and-scalability.md",

  /**
   * Delivery.
   */
  "docs/delivery/change-classification.md",
  "docs/delivery/contract-first-delivery.md",
  "docs/delivery/definition-of-done.md",
  "docs/delivery/release-and-rollout.md",
  "docs/delivery/migration-policy.md",
  "docs/delivery/team-ownership.md",
  "docs/delivery/local-development.md",

  /**
   * Operations.
   */
  "docs/operations/observability.md",
  "docs/operations/incident-readiness.md",
  "docs/operations/recovery-and-data-safety.md",
  "docs/operations/service-degradation.md",

  /**
   * Infrastructure.
   */
  "docs/infrastructure/environment-model.md",
  "docs/infrastructure/deployment-runtime.md",
  "docs/infrastructure/containerization-and-local-services.md",

  /**
   * System decisions.
   */
  "docs/decisions/README.md",

  /**
   * Canonical templates.
   */
  "docs/templates/adr-template.md",
  "docs/templates/architecture-change-template.md",
  "docs/templates/feature-spec-template.md",
  "docs/templates/migration-plan-template.md",
  "docs/templates/incident-template.md",
  "docs/templates/pr-checklist.md",

  /**
   * Backend root/local guidance.
   */
  "backend/README.md",
  "backend/AGENTS.md",
  "backend/CONTEXT.md",
  "backend/tests/AGENTS.md",

  /**
   * Backend architecture.
   */
  "backend/docs/architecture/backend-overview.md",
  "backend/docs/architecture/domain-modeling.md",
  "backend/docs/architecture/application-model.md",
  "backend/docs/architecture/infrastructure-and-data.md",
  "backend/docs/architecture/platform-and-messaging.md",
  "backend/docs/architecture/api-and-contracts.md",
  "backend/docs/architecture/security-tenancy-authorization.md",
  "backend/docs/architecture/testing-and-quality-gates.md",

  /**
   * Backend operations.
   */
  "backend/docs/operations/configuration-and-runtime.md",
  "backend/docs/operations/migrations-and-data-change.md",

  /**
   * Backend decisions.
   */
  "backend/docs/decisions/README.md",

  /**
   * Frontend root guidance.
   *
   * frontend/CONTEXT.md is intentionally NOT part of the architecture.
   */
  "frontend/README.md",
  "frontend/AGENTS.md",
  "frontend/docs/README.md",

  /**
   * Frontend architecture.
   */
  "frontend/docs/architecture/frontend-overview.md",
  "frontend/docs/architecture/dependency-boundaries.md",
  "frontend/docs/architecture/hosts-composition-routing.md",
  "frontend/docs/architecture/api-and-contracts.md",
  "frontend/docs/architecture/state-query-mutations.md",
  "frontend/docs/architecture/realtime.md",
  "frontend/docs/architecture/ui-and-design-system.md",
  "frontend/docs/architecture/testing-and-quality-gates.md",
  "frontend/docs/architecture/architecture-change-policy.md",

  /**
   * Frontend decisions.
   */
  "frontend/docs/decisions/README.md",
];

/**
 * Authorities that must not remain after documentation migration.
 *
 * This list intentionally contains known duplicate/legacy authority
 * paths rather than trying to ban arbitrary Markdown files.
 *
 * A local/package README may remain when it contains genuinely local
 * operational knowledge and does not compete with canonical authority.
 */
const FORBIDDEN_AUTHORITY_PATHS = [
  /**
   * Root retired/unused authority.
   */
  "CLAUDE.md",
  "SKILL.md",
  "MEMORY.md",

  /**
   * Repository legacy documentation trees.
   */
  "docs/engineering",
  "docs-refoundation",

  /**
   * Backend legacy root authorities.
   */
  "backend/RULE.md",
  "backend/PROMPT.md",
  "backend/CONFIGURATION.md",
  "backend/PROJECT-MAP.md",

  /**
   * Backend legacy documentation trees.
   */
  "backend/docs/ADR",
  "backend/docs/api",
  "backend/docs/application",
  "backend/docs/caching",
  "backend/docs/concurrency",
  "backend/docs/database",
  "backend/docs/domain",
  "backend/docs/infrastructure/rules",
  "backend/docs/issues",
  "backend/docs/messaging",
  "backend/docs/security",
  "backend/docs/superpowers",
  "backend/docs/testing",

  /**
   * Frontend legacy root authorities.
   */
  "frontend/ARCHITECTURE.md",
  "frontend/RULES.md",
  "frontend/MIGRATION_TRACKER.md",

  /**
   * No frontend/CONTEXT.md authority plane is part of the target
   * documentation architecture.
   */
  "frontend/CONTEXT.md",

  /**
   * Frontend legacy documentation trees.
   */
  "frontend/docs/adr",
  "frontend/docs/client",
  "frontend/docs/client-architecture",
  "frontend/docs/plans",

  /**
   * Legacy freeze / architecture snapshots.
   */
  "frontend/docs/FRONTEND_PLATFORM_FREEZE_SPEC.md",
  "frontend/docs/WAVE_F2_RUNTIME_MIGRATION_SPEC.md",
  "frontend/docs/notrelix-client-technical-project-structure.md",
];

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
 * ADR namespaces are separate by authority plane.
 *
 * System:
 *
 *   SYS-ADR-NNN
 *
 * Backend:
 *
 *   ADR-NNN
 *
 * Frontend:
 *
 *   FE-ADR-NNN
 */
const DECISION_NAMESPACES = [
  {
    directory: "docs/decisions",

    filename:
      /^SYS-ADR-(\d{3})-[a-z0-9][a-z0-9-]*\.md$/,

    id:
      /^SYS-ADR-\d{3}$/,

    label:
      "system",
  },

  {
    directory:
      "backend/docs/decisions",

    filename:
      /^ADR-(\d{3})-[a-z0-9][a-z0-9-]*\.md$/,

    id:
      /^ADR-\d{3}$/,

    label:
      "backend",
  },

  {
    directory:
      "frontend/docs/decisions",

    filename:
      /^FE-ADR-(\d{3})-[a-z0-9][a-z0-9-]*\.md$/,

    id:
      /^FE-ADR-\d{3}$/,

    label:
      "frontend",
  },
];

const failures = [];

let markdownFileCount = 0;
let decisionFileCount = 0;
let legacyReferenceCount = 0;

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

function pathIsInside(
  parent,
  candidate,
) {
  const rel =
    relative(
      parent,
      candidate,
    );

  return (
    rel === "" ||
    (
      !rel.startsWith(`..${sep}`) &&
      rel !== ".." &&
      !isAbsolute(rel)
    )
  );
}

function matchesForbiddenPath(
  repositoryRelativePath,
) {
  const normalized =
    toPosix(
      repositoryRelativePath,
    ).replace(
      /^\.\//,
      "",
    );

  return FORBIDDEN_AUTHORITY_PATHS.find(
    (forbidden) =>
      normalized === forbidden ||
      normalized.startsWith(
        `${forbidden}/`,
      ),
  );
}

/**
 * Walk Markdown files for authority-reference checks.
 *
 * Retired authority trees are intentionally skipped after their existence
 * has been reported once.
 *
 * Otherwise one legacy tree could generate hundreds of secondary
 * violations and obscure the real migration failure.
 */
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
      const relativeDirectory =
        displayPath(absolutePath);

      if (
        matchesForbiddenPath(
          relativeDirectory,
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
      entry.name.endsWith(".md")
    ) {
      acc.push(absolutePath);
    }
  }

  return acc;
}

/**
 * Strip fenced and inline code while preserving enough structure for
 * Markdown-link scanning.
 *
 * Retired paths may legitimately be mentioned inside code examples:
 *
 *   frontend/ARCHITECTURE.md
 *
 * That is not a live authority reference.
 */
function stripCode(text) {
  const lines =
    text.split(/\r?\n/);

  let fence = null;

  return lines
    .map((line) => {
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

        return " ".repeat(
          line.length,
        );
      }

      if (fence != null) {
        return " ".repeat(
          line.length,
        );
      }

      let result = "";

      for (
        let index = 0;
        index < line.length;
      ) {
        if (
          line[index] !== "`"
        ) {
          result += line[index];
          index += 1;
          continue;
        }

        let tickCount = 1;

        while (
          line[index + tickCount] ===
          "`"
        ) {
          tickCount += 1;
        }

        const delimiter =
          "`".repeat(tickCount);

        const close =
          line.indexOf(
            delimiter,
            index + tickCount,
          );

        if (close === -1) {
          result += " ".repeat(
            tickCount,
          );

          index += tickCount;
          continue;
        }

        const length =
          close +
          tickCount -
          index;

        result += " ".repeat(
          length,
        );

        index =
          close + tickCount;
      }

      return result;
    })
    .join("\n");
}

/**
 * Authority checking only needs actual Markdown targets.
 *
 * Full link integrity, anchors and reference-label correctness belong to:
 *
 *   check-links.mjs
 *
 * This parser therefore intentionally extracts only enough information to
 * identify links that point to retired authorities.
 */
function extractLinkTargets(text) {
  const targets = [];

  const inlinePattern =
    /!?\[[^\]\n]*\]\((?:<([^>]+)>|([^\s)]+))(?:\s+[^)]*)?\)/g;

  for (
    const match of text.matchAll(
      inlinePattern,
    )
  ) {
    targets.push(
      match[1] ??
      match[2],
    );
  }

  const referenceDefinitionPattern =
    /^\s{0,3}\[[^\]]+\]:\s*(?:<([^>]+)>|(\S+))/gm;

  for (
    const match of text.matchAll(
      referenceDefinitionPattern,
    )
  ) {
    targets.push(
      match[1] ??
      match[2],
    );
  }

  return targets.filter(Boolean);
}

function resolveRepositoryLink(
  sourceFile,
  rawTarget,
) {
  if (!rawTarget) {
    return null;
  }

  /**
   * External protocols are outside authority-path checking.
   */
  if (
    /^[A-Za-z][A-Za-z0-9+.-]*:/.test(
      rawTarget,
    )
  ) {
    return null;
  }

  if (
    rawTarget.startsWith("#")
  ) {
    return null;
  }

  const pathPart =
    rawTarget
      .split("#", 1)[0]
      .split("?", 1)[0];

  if (!pathPart) {
    return null;
  }

  let decoded;

  try {
    decoded =
      decodeURIComponent(
        pathPart,
      );
  } catch {
    /**
     * Invalid link syntax belongs to check-links.mjs.
     */
    return null;
  }

  const absoluteTarget =
    resolve(
      dirname(sourceFile),
      decoded,
    );

  if (
    !pathIsInside(
      REPO_ROOT,
      absoluteTarget,
    )
  ) {
    /**
     * Repository escape belongs to check-links.mjs.
     */
    return null;
  }

  return toPosix(
    relative(
      REPO_ROOT,
      absoluteTarget,
    ),
  );
}

/**
 * Every authored canonical owner required by the target architecture must
 * exist.
 *
 * This protects against accidentally deleting the canonical replacement
 * while retiring the old documentation tree.
 */
function validateRequiredAuthorities() {
  for (
    const relativePath of
      REQUIRED_AUTHORED_PATHS
  ) {
    if (
      !existsSync(
        repoPath(relativePath),
      )
    ) {
      fail(
        `[MISSING_AUTHORITY] required canonical document is missing: ` +
          `${relativePath}`,
      );
    }
  }
}

/**
 * Legacy or competing authorities must not coexist with the new canonical
 * plane.
 */
function validateRetiredAuthorities() {
  for (
    const relativePath of
      FORBIDDEN_AUTHORITY_PATHS
  ) {
    if (
      existsSync(
        repoPath(relativePath),
      )
    ) {
      fail(
        `[RETIRED_AUTHORITY] forbidden legacy/duplicate authority still exists: ` +
          `${relativePath}`,
      );
    }
  }
}

/**
 * Bare textual historical mentions are allowed.
 *
 * Live Markdown links to retired authority are not.
 *
 * This allows canonical migration/governance docs to say:
 *
 *   "frontend/ARCHITECTURE.md is retired"
 *
 * without treating that sentence as a canonical route.
 */
function validateLegacyLinks(
  markdownFiles,
) {
  for (
    const file of markdownFiles
  ) {
    markdownFileCount += 1;

    const text =
      stripCode(
        readFileSync(
          file,
          "utf8",
        ),
      );

    for (
      const rawTarget of
        extractLinkTargets(text)
    ) {
      const resolved =
        resolveRepositoryLink(
          file,
          rawTarget,
        );

      if (!resolved) {
        continue;
      }

      const forbidden =
        matchesForbiddenPath(
          resolved,
        );

      if (!forbidden) {
        continue;
      }

      legacyReferenceCount += 1;

      fail(
        `[RETIRED_REFERENCE] ${displayPath(file)} links to retired authority ` +
          `${resolved} (retired root: ${forbidden})`,
      );
    }
  }
}

/**
 * Metadata parsing here is intentionally minimal.
 *
 * Full metadata validation belongs to:
 *
 *   check-metadata.mjs
 *
 * Authority only needs document_id to prove ADR filename/metadata
 * identity consistency.
 */
function readFrontmatterDocumentId(
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
    return null;
  }

  for (
    let index = 1;
    index < lines.length;
    index += 1
  ) {
    if (
      lines[index] === "---"
    ) {
      break;
    }

    const match =
      lines[index].match(
        /^document_id:\s*["']?([^"'\s]+)["']?\s*$/,
      );

    if (match) {
      return match[1];
    }
  }

  return null;
}

function escapeRegex(value) {
  return value.replace(
    /[.*+?^${}()|[\]\\]/g,
    "\\$&",
  );
}

/**
 * Enforce one ADR namespace per decision plane.
 *
 * Also enforce:
 *
 *   filename ID
 *     ==
 *   frontmatter document_id
 *
 * and ensure the decision registry indexes every actual ADR file.
 */
function validateDecisionNamespace(
  namespace,
) {
  const directory =
    repoPath(
      namespace.directory,
    );

  if (
    !existsSync(directory)
  ) {
    return;
  }

  const registryPath =
    join(
      directory,
      "README.md",
    );

  const registry =
    existsSync(registryPath)
      ? readFileSync(
          registryPath,
          "utf8",
        )
      : "";

  const idsSeen =
    new Map();

  for (
    const entry of readdirSync(
      directory,
      {
        withFileTypes: true,
      },
    )
  ) {
    if (
      !entry.isFile() ||
      extname(entry.name) !== ".md" ||
      entry.name === "README.md"
    ) {
      continue;
    }

    decisionFileCount += 1;

    const filenameMatch =
      entry.name.match(
        namespace.filename,
      );

    if (!filenameMatch) {
      fail(
        `[ADR_FILENAME] ${namespace.label} decision file does not follow its namespace: ` +
          `${toPosix(
            relative(
              REPO_ROOT,
              join(
                directory,
                entry.name,
              ),
            ),
          )}`,
      );

      continue;
    }

    /**
     * Derive the ID from the namespace-specific filename rather than from
     * generic string slicing.
     *
     * System/backend/frontend namespaces differ.
     */
    const normalizedId =
      namespace.label === "system"
        ? `SYS-ADR-${filenameMatch[1]}`
        : namespace.label ===
            "frontend"
          ? `FE-ADR-${filenameMatch[1]}`
          : `ADR-${filenameMatch[1]}`;

    if (
      !namespace.id.test(
        normalizedId,
      )
    ) {
      fail(
        `[ADR_ID] invalid ${namespace.label} ADR id derived from ` +
          `${entry.name}: ${normalizedId}`,
      );

      continue;
    }

    const currentPath =
      toPosix(
        relative(
          REPO_ROOT,
          join(
            directory,
            entry.name,
          ),
        ),
      );

    const previous =
      idsSeen.get(
        normalizedId,
      );

    if (previous) {
      fail(
        `[ADR_DUPLICATE] duplicate ${namespace.label} ADR id ${normalizedId}: ` +
          `${previous} and ${currentPath}`,
      );
    } else {
      idsSeen.set(
        normalizedId,
        currentPath,
      );
    }

    const documentId =
      readFrontmatterDocumentId(
        join(
          directory,
          entry.name,
        ),
      );

    if (
      documentId !==
      normalizedId
    ) {
      fail(
        `[ADR_METADATA] ${currentPath} must declare ` +
          `document_id: ${normalizedId}; got ${documentId ?? "<missing>"}`,
      );
    }

    /**
     * We only require actual ADR files to appear in the registry.
     *
     * We intentionally do NOT reject additional ADR-looking text in README
     * because the registry may legitimately say:
     *
     *   next normally available ID: FE-ADR-006
     *
     * without that ADR existing yet.
     */
    const idMention =
      new RegExp(
        `(^|[^A-Z0-9-])${escapeRegex(normalizedId)}([^A-Z0-9-]|$)`,
      );

    if (
      !idMention.test(registry)
    ) {
      fail(
        `[ADR_REGISTRY] ${namespace.directory}/README.md does not index ` +
          `${normalizedId}`,
      );
    }
  }
}

if (
  !existsSync(REPO_ROOT)
) {
  console.error(
    `[docs-authority] repository root does not exist: ${REPO_ROOT}`,
  );

  process.exit(1);
}

validateRequiredAuthorities();

validateRetiredAuthorities();

const markdownFiles =
  walkMarkdown(
    REPO_ROOT,
  ).sort(
    (a, b) =>
      displayPath(a).localeCompare(
        displayPath(b),
        "en",
      ),
  );

validateLegacyLinks(
  markdownFiles,
);

for (
  const namespace of
    DECISION_NAMESPACES
) {
  validateDecisionNamespace(
    namespace,
  );
}

if (
  failures.length > 0
) {
  console.error(
    `[docs-authority] FAIL — ${failures.length} authority violation(s):`,
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
  `[docs-authority] PASS — ` +
    `${REQUIRED_AUTHORED_PATHS.length} required authored authority path(s), ` +
    `${markdownFileCount} Markdown file(s) scanned, ` +
    `${decisionFileCount} ADR file(s), ` +
    `${legacyReferenceCount} retired link(s).`,
);
#!/usr/bin/env node

import {
  existsSync,
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

const DEFAULT_REPO_ROOT =
  resolve(
    dirname(SCRIPT_PATH),
    "../..",
  );

const REPO_ROOT =
  process.env.DOCS_ROOT
    ? resolve(process.env.DOCS_ROOT)
    : DEFAULT_REPO_ROOT;

const FRONTEND_ROOT =
  join(
    REPO_ROOT,
    "frontend",
  );

const failures = [];

let executedCheckCount = 0;

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

function fail(message) {
  failures.push(message);
}

function rootPath(
  relativePath,
) {
  return join(
    REPO_ROOT,
    relativePath,
  );
}

function pnpmExecutable() {
  return process.platform ===
    "win32"
    ? "pnpm.cmd"
    : "pnpm";
}

/**
 * Generated documentation registry.
 *
 * IMPORTANT:
 *
 * This file owns orchestration only.
 *
 * It MUST NOT contain generation logic for any of these artifacts.
 *
 * Every artifact keeps exactly one producer:
 *
 * document-index.md
 *   <- generate-document-index.mjs
 *
 * rule-index.md
 *   <- generate-rule-index.mjs
 *
 * backend project-map.md
 *   <- generate-backend-project-map.mjs
 *
 * frontend package-boundaries.md
 *   <- frontend dependency-rules generator
 */
const GENERATED_CHECKS = [
  {
    id:
      "document-index",

    label:
      "repository document index",

    producer:
      rootPath(
        "scripts/docs/generate-document-index.mjs",
      ),

    target:
      rootPath(
        "docs/generated/document-index.md",
      ),

    command:
      process.execPath,

    args: [
      rootPath(
        "scripts/docs/generate-document-index.mjs",
      ),
      "--check",
    ],

    cwd:
      REPO_ROOT,

    environment: {
      DOCS_ROOT:
        REPO_ROOT,
    },
  },

  {
    id:
      "rule-index",

    label:
      "repository rule index",

    producer:
      rootPath(
        "scripts/docs/generate-rule-index.mjs",
      ),

    target:
      rootPath(
        "docs/generated/rule-index.md",
      ),

    command:
      process.execPath,

    args: [
      rootPath(
        "scripts/docs/generate-rule-index.mjs",
      ),
      "--check",
    ],

    cwd:
      REPO_ROOT,

    environment: {
      DOCS_ROOT:
        REPO_ROOT,
    },
  },

  {
    id:
      "backend-project-map",

    label:
      "backend generated project map",

    producer:
      rootPath(
        "scripts/docs/generate-backend-project-map.mjs",
      ),

    target:
      rootPath(
        "backend/docs/generated/project-map.md",
      ),

    command:
      process.execPath,

    args: [
      rootPath(
        "scripts/docs/generate-backend-project-map.mjs",
      ),
      "--check",
    ],

    cwd:
      REPO_ROOT,

    environment: {
      DOCS_ROOT:
        REPO_ROOT,
    },
  },

  {
    id:
      "frontend-package-boundaries",

    label:
      "frontend generated package boundaries",

    producer:
      rootPath(
        "frontend/tooling/dependency-rules/src/generate-architecture-docs.ts",
      ),

    target:
      rootPath(
        "frontend/docs/generated/package-boundaries.md",
      ),

    command:
      pnpmExecutable(),

    args: [
      "--filter",
      "@notrelix/dependency-rules",
      "docs:check",
    ],

    cwd:
      FRONTEND_ROOT,

    environment: {
      /**
       * The frontend generator already supports GENERATOR_ROOT.
       *
       * Supplying it explicitly keeps fixture/test execution from
       * accidentally touching the real worktree.
       */
      GENERATOR_ROOT:
        FRONTEND_ROOT,
    },
  },
];

/**
 * A generated artifact cannot be checked when either:
 *
 *   producer missing
 *   target missing
 *
 * Missing target is a first-class failure rather than silently generating
 * it during a check.
 *
 * CI/check commands must remain side-effect free.
 */
function validateCheckInputs(
  check,
) {
  let valid = true;

  if (
    !existsSync(
      check.producer,
    )
  ) {
    fail(
      `[GENERATED_PRODUCER_MISSING] ${check.label} producer is missing: ` +
        `${displayPath(check.producer)}`,
    );

    valid = false;
  }

  if (
    !existsSync(
      check.target,
    )
  ) {
    fail(
      `[GENERATED_TARGET_MISSING] ${check.label} target is missing: ` +
        `${displayPath(check.target)}. Generate and commit the artifact.`,
    );

    valid = false;
  }

  if (
    !existsSync(
      check.cwd,
    )
  ) {
    fail(
      `[GENERATED_CWD_MISSING] ${check.label} working directory is missing: ` +
        `${displayPath(check.cwd)}`,
    );

    valid = false;
  }

  return valid;
}

function cleanOutput(
  value,
) {
  if (
    value == null
  ) {
    return "";
  }

  return String(value)
    .replace(
      /\r\n/g,
      "\n",
    )
    .trim();
}

function formatCommand(
  check,
) {
  const command =
    check.command ===
    process.execPath
      ? "node"
      : check.command;

  return [
    command,
    ...check.args,
  ]
    .map(
      (part) => {
        if (
          /\s/.test(part)
        ) {
          return JSON.stringify(
            part,
          );
        }

        return part;
      },
    )
    .join(" ");
}

/**
 * Execute the producer's own check mode.
 *
 * This function deliberately does NOT:
 *
 *   - regenerate files;
 *   - parse the producer's source;
 *   - reconstruct expected Markdown;
 *   - compare a second independently generated representation.
 *
 * Doing so would create a second producer/authority.
 */
function runGeneratedCheck(
  check,
) {
  if (
    !validateCheckInputs(
      check,
    )
  ) {
    return;
  }

  const result =
    spawnSync(
      check.command,
      check.args,
      {
        cwd:
          check.cwd,

        env: {
          ...process.env,

          ...check.environment,

          /**
           * Stable non-colored diagnostics in CI.
           */
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

        shell:
          false,
      },
    );

  executedCheckCount += 1;

  if (
    result.error
  ) {
    fail(
      `[GENERATED_CHECK_EXECUTION] ${check.label} could not execute ` +
        `${formatCommand(check)}: ${result.error.message}`,
    );

    return;
  }

  if (
    result.status !== 0
  ) {
    const stdout =
      cleanOutput(
        result.stdout,
      );

    const stderr =
      cleanOutput(
        result.stderr,
      );

    const diagnostic =
      [
        stdout
          ? `stdout:\n${stdout}`
          : "",

        stderr
          ? `stderr:\n${stderr}`
          : "",
      ]
        .filter(Boolean)
        .join("\n");

    fail(
      `[GENERATED_DRIFT] ${check.label} check failed ` +
        `(exit ${result.status ?? "unknown"}) using:\n` +
        `  ${formatCommand(check)}` +
        (
          diagnostic
            ? `\n${diagnostic}`
            : ""
        ),
    );

    return;
  }

  if (
    result.signal
  ) {
    fail(
      `[GENERATED_SIGNAL] ${check.label} check terminated by signal ` +
        `${result.signal}`,
    );
  }
}

/**
 * Ensure generated target paths are not duplicated in the orchestrator.
 *
 * A duplicate entry could cause one artifact to appear to have multiple
 * governance checks/producers.
 */
function validateRegistryUniqueness() {
  const ids =
    new Map();

  const targets =
    new Map();

  const producers =
    new Map();

  for (
    const check of
      GENERATED_CHECKS
  ) {
    const previousId =
      ids.get(
        check.id,
      );

    if (
      previousId
    ) {
      fail(
        `[GENERATED_REGISTRY_DUPLICATE_ID] generated check id ${check.id} ` +
          `is registered more than once`,
      );
    } else {
      ids.set(
        check.id,
        true,
      );
    }

    const normalizedTarget =
      resolve(
        check.target,
      );

    const previousTarget =
      targets.get(
        normalizedTarget,
      );

    if (
      previousTarget
    ) {
      fail(
        `[GENERATED_REGISTRY_DUPLICATE_TARGET] ${displayPath(normalizedTarget)} ` +
          `is registered by both ${previousTarget} and ${check.id}`,
      );
    } else {
      targets.set(
        normalizedTarget,
        check.id,
      );
    }

    /**
     * The same generic producer may theoretically emit several distinct
     * generated artifacts, so producer uniqueness is diagnostic only when
     * both producer AND target duplicate.
     *
     * We still retain the map to make accidental registry mistakes easier
     * to detect.
     */
    const producerKey =
      resolve(
        check.producer,
      );

    const existing =
      producers.get(
        producerKey,
      ) ?? [];

    existing.push(
      check.id,
    );

    producers.set(
      producerKey,
      existing,
    );
  }
}

if (
  !existsSync(REPO_ROOT)
) {
  console.error(
    `[docs-generated] repository root does not exist: ${REPO_ROOT}`,
  );

  process.exit(1);
}

validateRegistryUniqueness();

for (
  const check of
    GENERATED_CHECKS
) {
  runGeneratedCheck(
    check,
  );
}

if (
  failures.length > 0
) {
  console.error(
    `[docs-generated] FAIL — ${failures.length} generated-document violation(s):`,
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
  `[docs-generated] PASS — ${GENERATED_CHECKS.length} registered generated ` +
    `artifact(s), ${executedCheckCount} producer-owned drift check(s) executed.`,
);
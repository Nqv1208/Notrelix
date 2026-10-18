/**
 * WX-FND-003 design-token usage gate.
 *
 * Flags, in product code (`apps/web/src/**`, `packages/features/** /src`):
 *   hex-color             hex color literals (#fff, #6161ff, #00000080)
 *   palette-color         primitive Tailwind palette colors used for meaning
 *                         (bg-blue-500, text-emerald-600/80, hover:border-red-200)
 *   arbitrary-text-size   arbitrary font sizes (text-[11px], text-[0.8rem])
 *
 * Semantic tokens replace all three (FE-UI-004, FE-UI-021, FE-UI-078).
 *
 * Separately (not baselined), `raw-z-index` forbids numeric and arbitrary
 * z-index utilities (`z-50`, `z-[100]`) in UI source: stacking uses the
 * `@notrelix/ui-tokens` roles (`z-dropdown`, `z-sticky`, `z-backdrop`,
 * `z-modal`, `z-toast`, `z-tooltip`). `z-0`, `z-10` and `z-20` stay allowed
 * as local, in-component layering (focus ring over a sibling, a resize handle).
 * The rule also flags inline `zIndex: <number>` of 30 or more and Tailwind v4
 * `z-(--x)` variable forms unless they name a role (`z-(--z-modal)`); inline
 * styles use `"var(--z-<role>)"`.
 * Excluded: `packages/product/work-management/**` and the legacy
 * `shell/sidebar/sidebar.tsx`, both owned by other pending work.
 *
 * Ratchet: a committed baseline (`design-token-baseline.json`) records the
 * current offenders per file and rule. The gate fails only when a file gains
 * violations beyond its baseline or a new file has any. Fixing offenders lowers
 * the count; run with `--update-baseline` to record the lower numbers. Later
 * phases drive the baseline to zero.
 */
import { existsSync, readFileSync, readdirSync, writeFileSync } from "node:fs";
import { dirname, join, relative, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const __dirname = dirname(fileURLToPath(import.meta.url));
const DEFAULT_ROOT = resolve(__dirname, "../../..");
export const BASELINE_PATH = resolve(
  __dirname,
  "../design-token-baseline.json",
);

export const DESIGN_TOKEN_RULES = [
  "hex-color",
  "palette-color",
  "arbitrary-text-size",
] as const;
export type DesignTokenRule = (typeof DESIGN_TOKEN_RULES)[number];

const PALETTE_HUES =
  "slate|gray|zinc|neutral|stone|red|orange|amber|yellow|lime|green|emerald|teal|cyan|sky|blue|indigo|violet|purple|fuchsia|pink|rose";
const COLOR_UTILITIES =
  "bg|text|border|border-[trblxyse]|ring|ring-offset|outline|fill|stroke|from|via|to|decoration|divide|placeholder|caret|accent|shadow";

const PATTERNS: Record<DesignTokenRule, RegExp> = {
  "hex-color":
    /(?<![\w&/])#(?:[0-9a-fA-F]{8}|[0-9a-fA-F]{6}|[0-9a-fA-F]{3,4})(?![A-Za-z0-9-])/g,
  "palette-color": new RegExp(
    `(?<![\\w-])(?:${COLOR_UTILITIES})-(?:${PALETTE_HUES})-(?:50|100|200|300|400|500|600|700|800|900|950)(?:\\/\\d+)?(?![\\w-])`,
    "g",
  ),
  "arbitrary-text-size":
    /(?<![\w-])text-\[(?:length:)?\d*\.?\d+(?:px|rem|em)\]/g,
};

/** Roles that replace raw z-index utilities (`@notrelix/ui-tokens` `zIndex`). */
export const Z_INDEX_ROLE_UTILITIES = [
  "z-dropdown",
  "z-sticky",
  "z-backdrop",
  "z-modal",
  "z-toast",
  "z-tooltip",
] as const;

const RAW_Z_INDEX = /(?<![\w-])-?z-(?:\[[^\]\s]+\]|\d+)(?![\w-])/g;
const LOCAL_LAYERING_Z = /^-?z-(?:0|10|20)$/;
/** Tailwind v4 variable form: `z-(--layer)`; only `z-(--z-<role>)` is allowed. */
const RAW_Z_VARIABLE = /(?<![\w-])-?z-\(--[\w-]+\)/g;
const ROLE_Z_VARIABLE =
  /^-?z-\(--z-(?:dropdown|sticky|backdrop|modal|toast|tooltip)\)$/;
/** Inline `zIndex: <expression>` in a style object; numbers are checked below. */
const INLINE_Z_INDEX = /(?<![\w$])zIndex\s*:\s*[^,;}\n]+/g;
const INLINE_Z_NUMBER = /(?<![\w.-])-?\d+(?![\w.])/g;
/** Inline numeric z-index below this stays allowed as local layering. */
const INLINE_Z_LOCAL_MAX = 29;

/** Files excluded from the raw z-index rule (other sessions' pending work). */
export const RAW_Z_INDEX_EXCLUDED = [
  "packages/product/work-management/",
  "apps/web/src/shell/sidebar/sidebar.tsx",
] as const;

/**
 * Third-party brand marks whose colours are fixed by the vendor's branding
 * rules; they are exempt from every rule here (the only raw hex allowed).
 */
export const VENDOR_MARK_FILES = [
  "packages/features/auth/src/web/components/google-mark.tsx",
  "packages/features/auth/src/web/components/microsoft-mark.tsx",
] as const;

export type FileCounts = Partial<Record<DesignTokenRule, number>>;
export type CountsByFile = Record<string, FileCounts>;

export interface DesignTokenBaseline {
  readonly description: string;
  readonly files: CountsByFile;
}

const SOURCE_EXTENSIONS = /\.(tsx?|css)$/;
const EXCLUDED = /(\.test\.|\.spec\.|\/__tests__\/|routeTree\.gen\.ts$)/;

function walk(dir: string): string[] {
  if (!existsSync(dir)) return [];
  const out: string[] = [];
  for (const entry of readdirSync(dir, { withFileTypes: true })) {
    if (entry.name === "node_modules" || entry.name.startsWith(".")) continue;
    const full = join(dir, entry.name);
    if (entry.isDirectory()) out.push(...walk(full));
    else if (SOURCE_EXTENSIONS.test(entry.name)) out.push(full);
  }
  return out;
}

export function scanRoots(rootDir: string): string[] {
  const featuresDir = join(rootDir, "packages/features");
  const features = existsSync(featuresDir)
    ? readdirSync(featuresDir, { withFileTypes: true })
        .filter((entry) => entry.isDirectory())
        .map((entry) => join(featuresDir, entry.name, "src"))
    : [];
  return [join(rootDir, "apps/web/src"), ...features];
}

export function countViolations(source: string, isCss = false): FileCounts {
  const counts: FileCounts = {};
  for (const rule of DESIGN_TOKEN_RULES) {
    if (isCss && rule !== "hex-color") continue;
    const matches = source.match(PATTERNS[rule]);
    if (matches && matches.length > 0) counts[rule] = matches.length;
  }
  return counts;
}

export function findRawZIndex(source: string): string[] {
  const utilities = (source.match(RAW_Z_INDEX) ?? []).filter(
    (utility) => !LOCAL_LAYERING_Z.test(utility),
  );
  const variables = (source.match(RAW_Z_VARIABLE) ?? []).filter(
    (utility) => !ROLE_Z_VARIABLE.test(utility),
  );
  const inline = (source.match(INLINE_Z_INDEX) ?? []).filter((expression) =>
    (expression.match(INLINE_Z_NUMBER) ?? []).some(
      (n) => Math.abs(Number(n)) > INLINE_Z_LOCAL_MAX,
    ),
  );
  return [...utilities, ...variables, ...inline].map((found) =>
    found.replace(/\s+/g, " ").trim(),
  );
}

export function zIndexScanRoots(rootDir: string): string[] {
  const productDir = join(rootDir, "packages/product");
  const product = existsSync(productDir)
    ? readdirSync(productDir, { withFileTypes: true })
        .filter((entry) => entry.isDirectory())
        .flatMap((entry) => {
          const family = join(productDir, entry.name);
          return readdirSync(family, { withFileTypes: true })
            .filter((layer) => layer.isDirectory())
            .map((layer) => join(family, layer.name, "src"));
        })
    : [];
  return [
    ...scanRoots(rootDir),
    join(rootDir, "packages/ui/web/src"),
    ...product,
  ];
}

/** Raw z-index utilities per file, outside the exclusions. */
export function scanRawZIndex(
  rootDir: string = DEFAULT_ROOT,
): Record<string, string[]> {
  const result: Record<string, string[]> = {};
  for (const root of zIndexScanRoots(rootDir)) {
    for (const file of walk(root)) {
      const rel = relative(rootDir, file).replace(/\\/g, "/");
      if (EXCLUDED.test(`/${rel}`)) continue;
      if (RAW_Z_INDEX_EXCLUDED.some((prefix) => rel.startsWith(prefix))) {
        continue;
      }
      const found = findRawZIndex(readFileSync(file, "utf8"));
      if (found.length > 0) result[rel] = found;
    }
  }
  return result;
}

export function scanDesignTokenUsage(
  rootDir: string = DEFAULT_ROOT,
): CountsByFile {
  const result: CountsByFile = {};
  for (const root of scanRoots(rootDir)) {
    for (const file of walk(root)) {
      const rel = relative(rootDir, file).replace(/\\/g, "/");
      if (EXCLUDED.test(`/${rel}`)) continue;
      if ((VENDOR_MARK_FILES as readonly string[]).includes(rel)) continue;
      const counts = countViolations(
        readFileSync(file, "utf8"),
        file.endsWith(".css"),
      );
      if (Object.keys(counts).length > 0) result[rel] = counts;
    }
  }
  return Object.fromEntries(
    Object.entries(result).sort(([a], [b]) => a.localeCompare(b)),
  );
}

export interface BaselineComparison {
  /** Violations above the baseline: these fail the gate. */
  readonly regressions: string[];
  /** Files whose count dropped below the baseline (update the baseline). */
  readonly improvements: string[];
}

export function compareToBaseline(
  current: CountsByFile,
  baseline: CountsByFile,
): BaselineComparison {
  const regressions: string[] = [];
  const improvements: string[] = [];
  const files = new Set([...Object.keys(current), ...Object.keys(baseline)]);
  for (const file of [...files].sort()) {
    for (const rule of DESIGN_TOKEN_RULES) {
      const now = current[file]?.[rule] ?? 0;
      const allowed = baseline[file]?.[rule] ?? 0;
      if (now > allowed) {
        regressions.push(
          `${file}: ${rule} ${now} > baseline ${allowed} (use semantic tokens instead)`,
        );
      } else if (now < allowed) {
        improvements.push(`${file}: ${rule} ${now} < baseline ${allowed}`);
      }
    }
  }
  return { regressions, improvements };
}

export function totalViolations(counts: CountsByFile): number {
  return Object.values(counts).reduce(
    (sum, file) =>
      sum + Object.values(file).reduce((inner, n) => inner + (n ?? 0), 0),
    0,
  );
}

export function readBaseline(
  path: string = BASELINE_PATH,
): DesignTokenBaseline {
  return JSON.parse(readFileSync(path, "utf8")) as DesignTokenBaseline;
}

function writeBaseline(counts: CountsByFile, path: string = BASELINE_PATH) {
  const baseline: DesignTokenBaseline = {
    description:
      "WX-FND-003 ratchet: existing hex / primitive-palette / arbitrary-text-size offenders per file. Counts may only go down; regenerate with `pnpm check:design-tokens --update-baseline` after removing offenders.",
    files: counts,
  };
  writeFileSync(path, `${JSON.stringify(baseline, null, 2)}\n`);
}

if (process.argv[1]?.endsWith("check-design-tokens.ts")) {
  const rawZ = scanRawZIndex();
  if (Object.keys(rawZ).length > 0) {
    console.error(
      `[design-tokens] FAIL — raw z-index utilities (use ${Z_INDEX_ROLE_UTILITIES.join(", ")}):`,
    );
    for (const [file, found] of Object.entries(rawZ)) {
      console.error(`  - ${file}: ${found.join(", ")}`);
    }
    process.exitCode = 1;
  }
  const current = scanDesignTokenUsage();
  if (process.argv.includes("--update-baseline")) {
    writeBaseline(current);
    console.log(
      `[design-tokens] baseline written: ${Object.keys(current).length} file(s), ${totalViolations(current)} violation(s).`,
    );
  } else {
    const baseline = readBaseline();
    const { regressions, improvements } = compareToBaseline(
      current,
      baseline.files,
    );
    if (regressions.length > 0) {
      console.error(
        "[design-tokens] FAIL — new hard-coded design values (use @notrelix/ui-tokens roles):",
      );
      for (const line of regressions) console.error(`  - ${line}`);
      process.exitCode = 1;
    } else {
      console.log(
        `[design-tokens] PASS — ${Object.keys(current).length} baselined file(s), ${totalViolations(current)} remaining violation(s), 0 new.`,
      );
    }
    if (improvements.length > 0) {
      console.log(
        `[design-tokens] ${improvements.length} count(s) below baseline — run with --update-baseline to lock in:`,
      );
      for (const line of improvements) console.log(`  - ${line}`);
    }
  }
}

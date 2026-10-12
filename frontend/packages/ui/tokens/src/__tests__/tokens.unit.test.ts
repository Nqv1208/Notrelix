import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";
import { describe, expect, it } from "vitest";
import {
  ACCENT_IDS,
  ACCENT_ROLES,
  ELEVATION_ROLES,
  THEME_APPEARANCES,
  WORK_STATUS_IDS,
  Z_INDEX_ORDER,
  buildTokensCss,
  durationMs,
  easing,
  fontSizeScale,
  fonts,
  fromHex,
  oklch,
  radiusBase,
  resolveSemanticColors,
  resolveThemeVariables,
  semanticColorRoleNames,
  textRoles,
  toHex,
  zIndex,
  type ThemeAppearance,
} from "../index";

const here = dirname(fileURLToPath(import.meta.url));
const committedCss = readFileSync(resolve(here, "../css/index.css"), "utf8");
const packageJson = JSON.parse(
  readFileSync(resolve(here, "../../package.json"), "utf8"),
) as { exports: Record<string, string> };

describe("generated CSS (@notrelix/ui-tokens/css)", () => {
  it("matches the generator output (run `pnpm --filter @notrelix/ui-tokens generate:css`)", () => {
    expect(committedCss).toBe(buildTokensCss());
  });

  it("is the file the package export declares (FE-UI-064)", () => {
    expect(packageJson.exports["./css"]).toBe("./src/css/index.css");
  });

  it("declares every semantic role in every appearance block", () => {
    const blocks = {
      light: committedCss.split(":root,\n.light {")[1]!.split("\n}")[0]!,
      dark: committedCss.split("\n.dark {")[1]!.split("\n}")[0]!,
      "high-contrast": committedCss
        .split('[data-contrast="high"] {')[1]!
        .split("\n}")[0]!,
    };
    for (const appearance of THEME_APPEARANCES) {
      for (const role of semanticColorRoleNames()) {
        expect(blocks[appearance], `${appearance} --${role}`).toContain(
          `  --${role}: `,
        );
      }
      for (const role of ELEVATION_ROLES) {
        expect(blocks[appearance]).toContain(`--elevation-${role}: `);
      }
    }
  });

  it("emits one accent block per accent and routes accent roles through tones", () => {
    for (const accent of ACCENT_IDS) {
      expect(committedCss).toContain(`[data-accent="${accent}"] {`);
    }
    expect(committedCss).toContain(
      "--primary: var(--accent-tone-light-solid);",
    );
    expect(committedCss).toContain("--primary: var(--accent-tone-dark-solid);");
    expect(committedCss).toContain(
      "--primary: var(--accent-tone-high-contrast-solid);",
    );
    expect(Object.keys(ACCENT_ROLES)).toContain("ring");
  });

  it("contains no hex color literals (OKLCH only)", () => {
    expect(committedCss.match(/#[0-9a-fA-F]{3,8}\b/g) ?? []).toEqual([]);
  });

  it("collapses motion under prefers-reduced-motion", () => {
    const reduced = committedCss
      .split("@media (prefers-reduced-motion: reduce) {")[1]!
      .split("\n}\n")[0]!;
    for (const role of Object.keys(durationMs)) {
      expect(reduced).toContain(`--motion-duration-${role}: 0.01ms;`);
    }
  });
});

describe("typography", () => {
  it("uses Be Vietnam Pro as the only sans family (WX-DEC-013)", () => {
    expect(fonts.sans.startsWith('"Be Vietnam Pro",')).toBe(true);
    expect(
      fonts.sans.endsWith(
        'ui-sans-serif, system-ui, -apple-system, "Segoe UI", Roboto, "Helvetica Neue", Arial, sans-serif',
      ),
    ).toBe(true);
    expect(fonts.sans).not.toMatch(/Inter|Poppins/);
  });

  it("uses IBM Plex Mono for keys, code and IDs (WX-DEC-013)", () => {
    expect(fonts.mono.startsWith('"IBM Plex Mono",')).toBe(true);
    expect(
      fonts.mono.endsWith(
        "ui-monospace, SFMono-Regular, Menlo, Consolas, monospace",
      ),
    ).toBe(true);
  });

  it("keeps a fixed rem scale with a 1.125–1.2 ratio between steps", () => {
    const steps = Object.values(fontSizeScale);
    for (let i = 1; i < steps.length; i += 1) {
      const ratio = steps[i]! / steps[i - 1]!;
      expect(ratio, `step ${i}`).toBeGreaterThanOrEqual(1.125 - 1e-9);
      expect(ratio, `step ${i}`).toBeLessThanOrEqual(1.2 + 1e-9);
    }
  });

  it("defines the DESIGN.md §8.1 semantic roles on the scale", () => {
    for (const role of [
      "display",
      "page-title",
      "section-heading",
      "subheading",
      "body",
      "dense-body",
      "label",
      "metadata",
      "caption",
      "code",
    ]) {
      const spec = textRoles[role as keyof typeof textRoles];
      expect(spec, role).toBeDefined();
      expect(Object.keys(fontSizeScale)).toContain(spec.size);
    }
    expect(textRoles.metadata.tabular).toBe(true);
  });
});

describe("motion, stacking, shape", () => {
  it("keeps durations within 150–200 ms", () => {
    for (const [role, ms] of Object.entries(durationMs)) {
      expect(ms, role).toBeGreaterThanOrEqual(150);
      expect(ms, role).toBeLessThanOrEqual(200);
    }
  });

  it("uses decelerating (ease-out) curves only", () => {
    for (const [role, curve] of Object.entries(easing)) {
      const [x1, y1] = curve
        .replace(/cubic-bezier\(|\)/g, "")
        .split(",")
        .map(Number);
      // Ease-out: fast start — the first control point rises at least as fast as it advances.
      expect(y1!, role).toBeGreaterThanOrEqual(0);
      expect(x1!, role).toBeLessThanOrEqual(0.25);
    }
  });

  it("orders z-index dropdown → sticky → backdrop → modal → toast → tooltip", () => {
    expect(Z_INDEX_ORDER).toEqual([
      "dropdown",
      "sticky",
      "backdrop",
      "modal",
      "toast",
      "tooltip",
    ]);
    const values = Z_INDEX_ORDER.map((role) => zIndex[role]);
    expect([...values].sort((a, b) => a - b)).toEqual(values);
    expect(new Set(values).size).toBe(values.length);
  });

  it("keeps the shadcn radius base", () => {
    expect(radiusBase).toBe("0.625rem");
  });
});

/** Hex the browser renders for an emitted `--role: oklch(l c h)` value. */
function emittedHex(appearance: ThemeAppearance, role: string): string {
  const value = resolveThemeVariables(appearance, "blue")[`--${role}`];
  const match = /^oklch\(([\d.]+) ([\d.]+) ([\d.]+)\)$/.exec(value ?? "");
  if (!match) throw new Error(`--${role} (${appearance}) is not opaque oklch: ${value}`);
  return toHex(oklch(Number(match[1]), Number(match[2]), Number(match[3])));
}

/**
 * Approved mockup values (docs/design/mockups/2026-10, `.nx` / `.nx.mode-dem`).
 * Compared as the hex of the emitted CSS value, so the page renders the
 * mockup colour exactly. Light `work-status-review` is the one contrast
 * adjustment: the mockup #D99A00 is 2.43:1 on panel, lowered by L 0.01 steps
 * to #C18A12 (3.02:1).
 */
const MOCKUP_VALUES: Record<"light" | "dark", Record<string, string>> = {
  light: {
    border: "#e6e9ee",
    "muted-foreground": "#4f5866",
    "danger-text": "#c42340",
    "surface-canvas": "#f7f8fa",
    "surface-sidebar": "#eff1f4",
    "work-status-todo": "#7a8391",
    "work-status-doing": "#1e7be0",
    "work-status-review": "#c18a12",
    "work-status-done": "#2e8b57",
    "work-status-todo-text": "#4f5866",
    "work-status-doing-text": "#0f56ae",
    "work-status-review-text": "#8a5a00",
    "work-status-done-text": "#1d6b40",
  },
  dark: {
    "surface-canvas": "#0e1116",
    "surface-sidebar": "#13181e",
    "surface-panel": "#151a21",
    border: "#232b35",
    foreground: "#e6e9ee",
    "muted-foreground": "#aab3c0",
    "danger-text": "#ff6b81",
    "work-status-todo": "#8c95a3",
    "work-status-doing": "#5aa9ff",
    "work-status-review": "#f2c14e",
    "work-status-done": "#56c98a",
    "work-status-todo-text": "#8c95a3",
    "work-status-doing-text": "#5aa9ff",
    "work-status-review-text": "#f2c14e",
    "work-status-done-text": "#56c98a",
  },
};

describe("mockup colour values", () => {
  for (const appearance of ["light", "dark"] as const) {
    it.each(Object.entries(MOCKUP_VALUES[appearance]))(
      `${appearance} --%s renders %s`,
      (role, hex) => {
        expect(emittedHex(appearance, role)).toBe(hex);
      },
    );
  }

  it("names the four work-status roles (labels are Work Management copy, not tokens)", () => {
    expect(WORK_STATUS_IDS).toEqual(["todo", "doing", "review", "done"]);
    for (const appearance of THEME_APPEARANCES) {
      const variables = resolveThemeVariables(appearance, "blue");
      for (const id of WORK_STATUS_IDS) {
        expect(variables, `${appearance} ${id}`).toHaveProperty(
          `--work-status-${id}`,
        );
        expect(variables, `${appearance} ${id}`).toHaveProperty(
          `--work-status-${id}-text`,
        );
      }
    }
  });
});

describe("color model", () => {
  it("round-trips hex through OKLCH", () => {
    for (const hex of ["#1e90ff", "#fc744c", "#f9c942", "#8cada1", "#fe3154"]) {
      expect(toHex(fromHex(hex))).toBe(hex);
    }
  });

  it("resolves the same role set for every appearance × accent", () => {
    const reference = Object.keys(
      resolveSemanticColors("light", "blue"),
    ).sort();
    for (const appearance of THEME_APPEARANCES) {
      for (const accent of ACCENT_IDS) {
        expect(
          Object.keys(resolveSemanticColors(appearance, accent)).sort(),
        ).toEqual(reference);
      }
    }
  });
});

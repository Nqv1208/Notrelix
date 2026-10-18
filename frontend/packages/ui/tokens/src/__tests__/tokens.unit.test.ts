import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";
import { describe, expect, it } from "vitest";
import {
  ACCENT_IDS,
  ACCENT_ROLES,
  ELEVATION_ROLES,
  LABEL_TONE_IDS,
  MARK_TONE_IDS,
  THEME_APPEARANCES,
  WORK_STATUS_IDS,
  Z_INDEX_ORDER,
  buildTokensCss,
  durationMs,
  easing,
  fontSizeDense,
  fontSizeScale,
  fonts,
  deltaEOk,
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

  it("keeps the mockup's off-grid chrome sizes out of the modular scale", () => {
    const dense = Object.keys(fontSizeDense);
    for (const spec of Object.values(textRoles)) {
      const onScale = Object.keys(fontSizeScale).includes(spec.size);
      expect(onScale || dense.includes(spec.size), spec.size).toBe(true);
    }
    for (const step of dense) {
      expect(Object.keys(fontSizeScale)).not.toContain(step);
    }
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
  if (!match)
    throw new Error(`--${role} (${appearance}) is not opaque oklch: ${value}`);
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
    // Hover tiers under the mockup sidebar (`--hover` / `--hover2`).
    muted: "#f1f3f6",
    secondary: "#f1f3f6",
    accent: "#f1f3f6",
    "state-hover": "#f1f3f6",
    "table-row-hover": "#f1f3f6",
    "sidebar-accent": "#e6e9ee",
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
    muted: "#1c232c",
    secondary: "#1c232c",
    accent: "#1c232c",
    "state-hover": "#1c232c",
    "table-row-hover": "#1c232c",
    "sidebar-accent": "#232a34",
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

  it("lifts the sidebar hover off the sidebar in every appearance", () => {
    // Light uses the mockup pair as is: #EFF1F4 → #E6E9EE is ΔL 0.024.
    for (const appearance of THEME_APPEARANCES) {
      const colors = resolveSemanticColors(appearance, "blue");
      const delta = colors["sidebar"]!.l - colors["sidebar-accent"]!.l;
      expect(Math.abs(delta), appearance).toBeGreaterThanOrEqual(0.024 - 1e-9);
    }
  });

  it("selects with the mockup tint (blue accent; other accents share L and C)", () => {
    expect(
      deltaEOk(
        resolveSemanticColors("dark", "blue")["state-selected"]!,
        fromHex("#172334"),
      ),
    ).toBeLessThanOrEqual(0.005);
    // Light #EEF4FD is within ΔE_OK 0.02 (just noticeable) of the current tone.
    expect(
      deltaEOk(
        resolveSemanticColors("light", "blue")["state-selected"]!,
        fromHex("#eef4fd"),
      ),
    ).toBeLessThan(0.02);
  });

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

/**
 * Mark tones: workspace and department (Space) initials chips, the mockup
 * department colours (CN, KT, MK, NS, GĐ, PC), one fill in every appearance
 * with white initials. Sage #4E8574 is 4.26:1 under white, lowered by L 0.01
 * steps to #487F6E (4.63:1).
 */
const MARK_TONE_VALUES: Record<string, string> = {
  blue: "#1e6fd0",
  gold: "#9a6a12",
  coral: "#c2410c",
  sage: "#487f6e",
  crimson: "#c8143f",
  violet: "#5b5bd6",
};

describe("mark tones (WX-DEC-014)", () => {
  it("names the six mark tones in mockup order", () => {
    expect(MARK_TONE_IDS).toEqual([
      "blue",
      "gold",
      "coral",
      "sage",
      "crimson",
      "violet",
    ]);
  });

  for (const appearance of THEME_APPEARANCES) {
    it.each(Object.entries(MARK_TONE_VALUES))(
      `${appearance} --mark-%s renders %s with white initials`,
      (id, hex) => {
        expect(emittedHex(appearance, `mark-${id}`)).toBe(hex);
        expect(emittedHex(appearance, `mark-${id}-foreground`)).toBe("#ffffff");
      },
    );
  }
});

/**
 * Notrelix system rail (Final-Space `.rail`): dark in every appearance.
 * Light rail #16202E with ink #AFBACB; dark rail #05070A with ink #8C97A8;
 * high contrast black with white ink. Active white, badge #E8174B + white,
 * focus ring white (the app ring is < 3:1 on the rail in light and HC).
 */
const RAIL_VALUES: Record<ThemeAppearance, Record<string, string>> = {
  light: {
    rail: "#16202e",
    "rail-foreground": "#afbacb",
    "rail-active-foreground": "#ffffff",
    "rail-badge": "#e8174b",
    "rail-badge-foreground": "#ffffff",
    "rail-focus-ring": "#ffffff",
  },
  dark: {
    rail: "#05070a",
    "rail-foreground": "#8c97a8",
    "rail-active-foreground": "#ffffff",
    "rail-badge": "#e8174b",
    "rail-badge-foreground": "#ffffff",
    "rail-focus-ring": "#ffffff",
  },
  "high-contrast": {
    rail: "#000000",
    "rail-foreground": "#ffffff",
    "rail-active-foreground": "#ffffff",
    "rail-badge": "#e8174b",
    "rail-badge-foreground": "#ffffff",
    "rail-focus-ring": "#ffffff",
  },
};

describe("system rail roles", () => {
  for (const appearance of THEME_APPEARANCES) {
    it.each(Object.entries(RAIL_VALUES[appearance]))(
      `${appearance} --%s renders %s`,
      (role, hex) => {
        expect(emittedHex(appearance, role)).toBe(hex);
      },
    );

    it(`${appearance} rail hover and separator are white at 8 % and 14 %`, () => {
      const variables = resolveThemeVariables(appearance, "blue");
      expect(variables["--rail-hover"]).toBe("oklch(1 0 0 / 0.08)");
      expect(variables["--rail-separator"]).toBe("oklch(1 0 0 / 0.14)");
    });
  }
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

/**
 * Board Kanban (Final-Board): lane tints (`--lane-*`), label chips (`.t-*`)
 * and the high priority colour (`--high`), light and dark as the mockup.
 * Contrast adjustment: dark todo lane #151B23 is ΔL 0.004 from the dark
 * panel (cards would vanish into the lane); L raised in 0.01 steps from
 * 0.22 to 0.24 → #1A2028 (ΔL 0.024, the same side of the panel as the
 * other dark lanes). High contrast reuses the light lanes and label fills.
 */
const BOARD_VALUES: Record<"light" | "dark", Record<string, string>> = {
  light: {
    "work-status-todo-lane": "#eceef2",
    "work-status-doing-lane": "#e6effb",
    "work-status-review-lane": "#fdf3dc",
    "work-status-done-lane": "#e5f3ea",
    "label-data": "#e1eefc",
    "label-data-foreground": "#124c8e",
    "label-security": "#fde5ea",
    "label-security-foreground": "#9e1736",
    "label-training": "#e1f0ea",
    "label-training-foreground": "#2c5e4f",
    "label-report": "#fbf0d3",
    "label-report-foreground": "#6e4f0a",
    "label-doc": "#eceef2",
    "label-doc-foreground": "#3e4757",
    "label-test": "#eee7fb",
    "label-test-foreground": "#5532a8",
    "priority-high": "#b4500b",
    "priority-high-text": "#b4500b",
  },
  dark: {
    "work-status-todo-lane": "#1a2028",
    "work-status-doing-lane": "#13203a",
    "work-status-review-lane": "#272012",
    "work-status-done-lane": "#12251c",
    "label-data": "#16304f",
    "label-data-foreground": "#9cc8fa",
    "label-security": "#4a1a26",
    "label-security-foreground": "#ffa8b8",
    "label-training": "#173a30",
    "label-training-foreground": "#9ed6c2",
    "label-report": "#3c3014",
    "label-report-foreground": "#f2d58a",
    "label-doc": "#262e39",
    "label-doc-foreground": "#c3cbd6",
    "label-test": "#2e2350",
    "label-test-foreground": "#c9b6fa",
    "priority-high": "#ffa060",
    "priority-high-text": "#ffa060",
  },
};

describe("board lane, label and priority colours", () => {
  it("names the six label tones in mockup order", () => {
    expect(LABEL_TONE_IDS).toEqual([
      "data",
      "security",
      "training",
      "report",
      "doc",
      "test",
    ]);
  });

  for (const appearance of ["light", "dark"] as const) {
    it.each(Object.entries(BOARD_VALUES[appearance]))(
      `${appearance} --%s renders %s`,
      (role, hex) => {
        expect(emittedHex(appearance, role)).toBe(hex);
      },
    );
  }

  it("high contrast reuses the light lane tints and label fills", () => {
    for (const id of WORK_STATUS_IDS) {
      expect(emittedHex("high-contrast", `work-status-${id}-lane`)).toBe(
        emittedHex("light", `work-status-${id}-lane`),
      );
    }
    for (const id of LABEL_TONE_IDS) {
      expect(emittedHex("high-contrast", `label-${id}`)).toBe(
        emittedHex("light", `label-${id}`),
      );
    }
  });

  it("keeps the high priority orange visibly distinct from danger", () => {
    for (const appearance of THEME_APPEARANCES) {
      const colors = resolveSemanticColors(appearance, "blue");
      for (const role of ["priority-high", "priority-high-text"]) {
        for (const danger of ["danger", "danger-text"]) {
          expect(
            deltaEOk(colors[role]!, colors[danger]!),
            `${appearance} ${role} vs ${danger}`,
          ).toBeGreaterThanOrEqual(0.08);
        }
      }
    }
  });

  it("maps every new role to a Tailwind colour in the web globals", () => {
    const globals = readFileSync(
      resolve(here, "../../../../../apps/web/src/styles/globals.css"),
      "utf8",
    );
    const roles = [
      ...WORK_STATUS_IDS.map((id) => `work-status-${id}-lane`),
      ...LABEL_TONE_IDS.flatMap((id) => [
        `label-${id}`,
        `label-${id}-foreground`,
      ]),
      "priority-high",
      "priority-high-text",
    ];
    for (const role of roles) {
      expect(globals, role).toContain(`--color-${role}: var(--${role});`);
    }
  });
});

/**
 * Calendar (Final-Calendar): the day column tints (`--weekend`, `--muted-day`,
 * `--today-soft`), the status event fills (`--lane-*`), `--faint`, the review
 * dot and the drop tooltip, light and dark as the mockup. High contrast reuses
 * the light values.
 */
const CALENDAR_VALUES: Record<"light" | "dark", Record<string, string>> = {
  light: {
    "calendar-weekend": "#f4f5f8",
    "calendar-outside": "#fafbfc",
    "calendar-today": "#edf4fd",
    "calendar-event-todo": "#ffffff",
    "calendar-event-doing": "#e2ecfa",
    "calendar-event-review": "#fbf0d6",
    "calendar-event-done": "#e2f1e7",
    "calendar-event-overdue": "#fdecef",
    "calendar-dot-review": "#d99a00",
    "calendar-faint": "#686f7b",
    "calendar-tip": "#161a21",
    "calendar-tip-foreground": "#f2f4f7",
  },
  dark: {
    "calendar-weekend": "#12171d",
    "calendar-outside": "#11151b",
    "calendar-today": "#13213a",
    "calendar-event-todo": "#1e252f",
    "calendar-event-doing": "#16284a",
    "calendar-event-review": "#30281a",
    "calendar-event-done": "#153024",
    "calendar-dot-review": "#f2c14e",
    "calendar-faint": "#6f7986",
    "calendar-tip": "#2a323d",
    "calendar-tip-foreground": "#f2f4f7",
  },
};

describe("calendar colours", () => {
  for (const appearance of ["light", "dark"] as const) {
    it.each(Object.entries(CALENDAR_VALUES[appearance]))(
      `${appearance} --%s renders %s`,
      (role, hex) => {
        expect(emittedHex(appearance, role)).toBe(hex);
      },
    );
  }

  it("renders the dark overdue fill as the mockup's rgba(255,107,129,.14)", () => {
    const value = resolveThemeVariables("dark", "blue")[
      "--calendar-event-overdue"
    ];
    expect(value).toMatch(/^oklch\(0\.716 0\.18 13\.8 \/ 0\.14\)$/);
  });

  it("high contrast reuses the light calendar tints", () => {
    for (const role of Object.keys(CALENDAR_VALUES.light)) {
      expect(emittedHex("high-contrast", role)).toBe(emittedHex("light", role));
    }
  });

  it("maps every calendar role and elevation to the web globals", () => {
    const globals = readFileSync(
      resolve(here, "../../../../../apps/web/src/styles/globals.css"),
      "utf8",
    );
    for (const role of Object.keys(CALENDAR_VALUES.light).concat(
      "calendar-event-overdue",
    )) {
      expect(globals, role).toContain(`--color-${role}: var(--${role});`);
    }
    expect(globals).toContain("--shadow-segment: var(--elevation-segment);");
    for (const role of ["avatar-xs", "calendar-title", "hint"]) {
      expect(globals, role).toContain(
        `--text-${role}: var(--type-${role}-size);`,
      );
    }
  });
});

/**
 * Timeline (Final-Timeline): the bar fills (`--bar-*`), the Cần làm bar
 * border, the progress strips (`--st-*`), the dependency line (`--dep`), the
 * milestone diamond and the drag tooltip's second line, light and dark as the
 * mockup. High contrast reuses the light values.
 */
const TIMELINE_VALUES: Record<"light" | "dark", Record<string, string>> = {
  light: {
    "timeline-bar-todo": "#eceef2",
    "timeline-bar-todo-border": "#c9ced6",
    "timeline-bar-doing": "#d6e5f9",
    "timeline-bar-review": "#f7e6ba",
    "timeline-bar-done": "#d3ebdc",
    "timeline-progress-doing": "#1e7be0",
    "timeline-progress-review": "#d99a00",
    "timeline-progress-done": "#2e8b57",
    "timeline-link": "#8a93a0",
    "timeline-milestone": "#c8143f",
    "timeline-tip-note": "#c9ced6",
  },
  dark: {
    "timeline-bar-todo": "#252d38",
    "timeline-bar-todo-border": "#3a4452",
    "timeline-bar-doing": "#17345e",
    "timeline-bar-review": "#3a3018",
    "timeline-bar-done": "#173a2a",
    "timeline-progress-doing": "#5aa9ff",
    "timeline-progress-review": "#f2c14e",
    "timeline-progress-done": "#56c98a",
    "timeline-link": "#6f7986",
    "timeline-milestone": "#c8143f",
    "timeline-tip-note": "#c9ced6",
  },
};

describe("timeline colours", () => {
  for (const appearance of ["light", "dark"] as const) {
    it.each(Object.entries(TIMELINE_VALUES[appearance]))(
      `${appearance} --%s renders %s`,
      (role, hex) => {
        expect(emittedHex(appearance, role)).toBe(hex);
      },
    );
  }

  it("high contrast reuses the light timeline colours", () => {
    for (const role of Object.keys(TIMELINE_VALUES.light)) {
      expect(emittedHex("high-contrast", role)).toBe(emittedHex("light", role));
    }
  });

  it("maps every timeline role to the web globals", () => {
    const globals = readFileSync(
      resolve(here, "../../../../../apps/web/src/styles/globals.css"),
      "utf8",
    );
    for (const role of Object.keys(TIMELINE_VALUES.light)) {
      expect(globals, role).toContain(`--color-${role}: var(--${role});`);
    }
  });
});

/**
 * Item page (Final-Detail): the conflict notice border, the "added" mark and
 * the file type chips, light and dark as the mockup. High contrast reuses the
 * light values.
 */
const ITEM_PAGE_VALUES: Record<"light" | "dark", Record<string, string>> = {
  light: {
    "item-conflict-line": "#ebcb7a",
    "item-added": "#e5f3ea",
    "item-added-foreground": "#1f6b43",
    "item-file-sheet": "#e1f0ea",
    "item-file-sheet-foreground": "#1f6b43",
    "item-file-pdf": "#fde5ea",
    "item-file-pdf-foreground": "#9e1736",
  },
  dark: {
    "item-added-foreground": "#7fd9a6",
    "item-file-sheet": "#173a30",
    "item-file-sheet-foreground": "#9ed6c2",
    "item-file-pdf": "#4a1a26",
    "item-file-pdf-foreground": "#ffa8b8",
  },
};

describe("item page colours", () => {
  for (const appearance of ["light", "dark"] as const) {
    it.each(Object.entries(ITEM_PAGE_VALUES[appearance]))(
      `${appearance} --%s renders %s`,
      (role, hex) => {
        expect(emittedHex(appearance, role)).toBe(hex);
      },
    );
  }

  it("renders the dark notice border and added mark as the mockup's rgba values", () => {
    const variables = resolveThemeVariables("dark", "blue");
    expect(variables["--item-conflict-line"]).toMatch(
      /^oklch\(0\.834 0\.141 85\.4 \/ 0\.4\)$/,
    );
    expect(variables["--item-added"]).toMatch(/\/ 0\.14\)$/);
  });

  it("high contrast reuses the light item page colours", () => {
    for (const role of Object.keys(ITEM_PAGE_VALUES.light)) {
      expect(emittedHex("high-contrast", role)).toBe(emittedHex("light", role));
    }
  });

  it("maps every item page role and text role to the web globals", () => {
    const globals = readFileSync(
      resolve(here, "../../../../../apps/web/src/styles/globals.css"),
      "utf8",
    );
    for (const role of Object.keys(ITEM_PAGE_VALUES.light)) {
      expect(globals, role).toContain(`--color-${role}: var(--${role});`);
    }
    for (const role of [
      "item-title",
      "item-heading",
      "item-prose",
      "item-text",
      "item-comment",
      "item-ui",
      "item-small",
      "item-note",
    ]) {
      expect(globals, role).toContain(
        `--text-${role}: var(--type-${role}-size);`,
      );
    }
  });
});

/**
 * Home, My work and Inbox (Final-Home, Final-MyWork, Final-Inbox): the page
 * title roles the mockups draw above the shared roles.
 */
describe("Home, My work and Inbox type roles", () => {
  it("sizes the greeting as Final-Home's h1 (26px / 650, line 1.25)", () => {
    const role = textRoles.greeting;
    expect(fontSizeDense[role.size as keyof typeof fontSizeDense]).toBe(1.625);
    expect(role.lineHeight * 16).toBeCloseTo(32.5, 5);
    expect(role.weight).toBe(650);
    expect(role.letterSpacing).toBe("-0.015em");
  });

  it("sizes the My work title as Final-MyWork's h1 (24px / 650, line 1.25)", () => {
    const role = textRoles["list-title"];
    expect(fontSizeScale[role.size as keyof typeof fontSizeScale]).toBe(1.5);
    expect(role.lineHeight * 16).toBeCloseTo(30, 5);
    expect(role.weight).toBe(650);
    expect(role.letterSpacing).toBe("-0.015em");
  });

  it("sizes the empty-page title as Final-MyWork's `.emp` h2 (19px / 650, line 1.45)", () => {
    const role = textRoles["empty-title"];
    expect(fontSizeDense[role.size as keyof typeof fontSizeDense]).toBe(1.1875);
    expect(role.lineHeight * 16).toBeCloseTo(27.55, 5);
    expect(role.weight).toBe(650);
  });

  it("emits the inverse success and muted colours of the bulk bar (#8EE3B0, #A3ACB8)", () => {
    for (const appearance of ["light", "dark", "high-contrast"] as const) {
      expect(emittedHex(appearance, "inverse-success")).toBe("#8ee3b0");
      expect(emittedHex(appearance, "inverse-muted")).toBe("#a3acb8");
    }
    const globals = readFileSync(
      resolve(here, "../../../../../apps/web/src/styles/globals.css"),
      "utf8",
    );
    expect(globals).toContain(
      "--color-inverse-success: var(--inverse-success);",
    );
    expect(globals).toContain("--color-inverse-muted: var(--inverse-muted);");
  });

  it("maps every Home role to the web globals", () => {
    const globals = readFileSync(
      resolve(here, "../../../../../apps/web/src/styles/globals.css"),
      "utf8",
    );
    for (const role of ["greeting", "list-title", "empty-title"]) {
      expect(globals, role).toContain(
        `--text-${role}: var(--type-${role}-size);`,
      );
    }
  });
});

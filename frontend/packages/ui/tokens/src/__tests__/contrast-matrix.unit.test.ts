import { describe, expect, it } from "vitest";
import {
  ACCENT_IDS,
  IDENTITY_IDS,
  MARK_TONE_IDS,
  STATUS_IDS,
  THEME_APPEARANCES,
  WORK_STATUS_IDS,
  accents,
  contrastOverComposite,
  contrastRatio,
  deltaEOk,
  fromHex,
  isInSrgbGamut,
  resolveSemanticColors,
  statuses,
  toHex,
  withAlpha,
  type ThemeAppearance,
} from "../index";

/**
 * WX-FND-001 contrast matrix. Every appearance × accent is resolved from the
 * same token source the CSS is generated from, converted OKLCH → sRGB, and
 * checked against WCAG 2.x ratios:
 *   text/background pairs   ≥ 4.5:1  (high contrast ≥ 7:1)
 *   focus ring, icons, marks ≥ 3:1
 */

type Pair = readonly [foreground: string, background: string];

const SURFACES = ["background", "card", "popover", "sidebar"] as const;

const TEXT_PAIRS: readonly Pair[] = [
  ...SURFACES.map((s) => ["foreground", s] as const),
  ["foreground", "muted"],
  ["foreground", "accent"],
  ["foreground", "table-header"],
  ["foreground", "table-selected"],
  ...SURFACES.map((s) => ["muted-foreground", s] as const),
  ["muted-foreground", "muted"],
  ["muted-foreground", "accent"],
  ["secondary-foreground", "secondary"],
  ["accent-foreground", "accent"],
  ["sidebar-foreground", "sidebar"],
  ["sidebar-accent-foreground", "sidebar-accent"],
  ["primary-foreground", "primary"],
  ["sidebar-primary-foreground", "sidebar-primary"],
  ["brand-solid-foreground", "brand-solid"],
  ...SURFACES.map((s) => ["link", s] as const),
  ["brand-subtle-foreground", "brand-subtle"],
  ["state-selected-foreground", "state-selected"],
  ["destructive", "background"],
  ["destructive", "card"],
  ["destructive-foreground", "destructive"],
  ...STATUS_IDS.flatMap((s) => [
    [`${s}-foreground`, s] as const,
    [`${s}-text`, "background"] as const,
    [`${s}-text`, "card"] as const,
    [`${s}-subtle-foreground`, `${s}-subtle`] as const,
  ]),
  ...[
    "default",
    "done",
    "working",
    "stuck",
    "urgent",
    "high",
    "medium",
    "low",
  ].map((b) => [`badge-${b}-text`, `badge-${b}-bg`] as const),
];

const NON_TEXT_PAIRS: readonly Pair[] = [
  ...SURFACES.map((s) => ["ring", s] as const),
  ...SURFACES.map((s) => ["focus-ring", s] as const),
  ...(["background", "card", "sidebar"] as const).map(
    (s) => ["brand-mark", s] as const,
  ),
  ...(["background", "card", "sidebar", "popover"] as const).map(
    (s) => ["icon", s] as const,
  ),
];

/** Avatar initials / labels on identity fills: legible in every appearance. */
const IDENTITY_PAIRS: readonly Pair[] = IDENTITY_IDS.map(
  (id) => [`identity-${id}-foreground`, `identity-${id}`] as const,
);

/** Work-status text on the three page surfaces (canvas, panel, sidebar). */
const WORK_STATUS_TEXT_PAIRS: readonly Pair[] = WORK_STATUS_IDS.flatMap((id) =>
  (["surface-canvas", "surface-panel", "surface-sidebar"] as const).map(
    (s) => [`work-status-${id}-text`, s] as const,
  ),
);

/** Work-status dots/fills on panel: ≥ 3:1 (≥ 4.5:1 in high contrast). */
const WORK_STATUS_FILL_PAIRS: readonly Pair[] = WORK_STATUS_IDS.map(
  (id) => [`work-status-${id}`, "surface-panel"] as const,
);

/** White initials on workspace/department mark chips: ≥ 4.5:1 in every appearance. */
const MARK_PAIRS: readonly Pair[] = MARK_TONE_IDS.map(
  (id) => [`mark-${id}-foreground`, `mark-${id}`] as const,
);

function minimumText(appearance: ThemeAppearance): number {
  return appearance === "high-contrast" ? 7 : 4.5;
}

const matrix = THEME_APPEARANCES.flatMap((appearance) =>
  ACCENT_IDS.map((accent) => ({ appearance, accent })),
);

describe("contrast matrix (appearance × accent)", () => {
  it("covers every appearance and accent", () => {
    expect(matrix).toHaveLength(THEME_APPEARANCES.length * ACCENT_IDS.length);
    expect(matrix.length).toBe(15);
  });

  describe.each(matrix)("$appearance × $accent", ({ appearance, accent }) => {
    const colors = resolveSemanticColors(appearance, accent);
    const get = (role: string) => {
      const color = colors[role];
      if (!color) throw new Error(`missing role ${role}`);
      return color;
    };

    it("emits only in-gamut sRGB colors", () => {
      const outOfGamut = Object.entries(colors)
        .filter(([, color]) => !isInSrgbGamut(color))
        .map(([role]) => role);
      expect(outOfGamut).toEqual([]);
    });

    it.each(TEXT_PAIRS)("text %s on %s meets the text minimum", (fg, bg) => {
      const ratio = contrastRatio(get(fg), get(bg));
      expect(
        ratio,
        `${fg} ${toHex(get(fg))} on ${bg} ${toHex(get(bg))} = ${ratio.toFixed(2)}`,
      ).toBeGreaterThanOrEqual(minimumText(appearance));
    });

    // `text-primary` (primary used as text) is legible for accents whose fill
    // carries white text. Amber fills are light by design (dark text on
    // fills), so accent-colored text must use `link`/`brand-ink` instead.
    it.runIf(!accents[accent].lightFill || appearance === "dark")(
      "primary used as text stays legible on surfaces",
      () => {
        for (const bg of ["background", "card"]) {
          expect(
            contrastRatio(get("primary"), get(bg)),
            bg,
          ).toBeGreaterThanOrEqual(minimumText(appearance));
        }
      },
    );

    it.each(NON_TEXT_PAIRS)("non-text %s on %s ≥ 3:1", (fg, bg) => {
      const ratio = contrastRatio(get(fg), get(bg));
      expect(
        ratio,
        `${fg} on ${bg} = ${ratio.toFixed(2)}`,
      ).toBeGreaterThanOrEqual(3);
    });

    it.each(WORK_STATUS_TEXT_PAIRS)(
      "work-status text %s on %s meets the text minimum",
      (fg, bg) => {
        const ratio = contrastRatio(get(fg), get(bg));
        expect(
          ratio,
          `${fg} ${toHex(get(fg))} on ${bg} ${toHex(get(bg))} = ${ratio.toFixed(2)}`,
        ).toBeGreaterThanOrEqual(minimumText(appearance));
      },
    );

    it.each(WORK_STATUS_FILL_PAIRS)(
      "work-status fill %s on %s meets the non-text minimum",
      (fg, bg) => {
        const ratio = contrastRatio(get(fg), get(bg));
        expect(
          ratio,
          `${fg} ${toHex(get(fg))} on ${bg} ${toHex(get(bg))} = ${ratio.toFixed(2)}`,
        ).toBeGreaterThanOrEqual(appearance === "high-contrast" ? 4.5 : 3);
      },
    );

    it.each(MARK_PAIRS)("mark initials %s on %s ≥ 4.5:1", (fg, bg) => {
      const ratio = contrastRatio(get(fg), get(bg));
      expect(
        ratio,
        `${fg} ${toHex(get(fg))} on ${bg} ${toHex(get(bg))} = ${ratio.toFixed(2)}`,
      ).toBeGreaterThanOrEqual(4.5);
    });

    it("system rail ink, active ink and badge stay legible on the dark rail", () => {
      const rail = get("rail");
      const hover = get("rail-hover");
      for (const fg of ["rail-foreground", "rail-active-foreground"]) {
        const plain = contrastRatio(get(fg), rail);
        expect(plain, `${fg} on rail = ${plain.toFixed(2)}`).toBeGreaterThanOrEqual(
          minimumText(appearance),
        );
        const hovered = contrastOverComposite(get(fg), hover, rail);
        expect(
          hovered,
          `${fg} on rail-hover = ${hovered.toFixed(2)}`,
        ).toBeGreaterThanOrEqual(4.5);
      }
      const badge = contrastRatio(get("rail-badge-foreground"), get("rail-badge"));
      expect(badge, `badge = ${badge.toFixed(2)}`).toBeGreaterThanOrEqual(4.5);
      // The current-workspace ring (active foreground) is visible on the rail.
      expect(
        contrastRatio(get("rail-active-foreground"), rail),
      ).toBeGreaterThanOrEqual(3);
    });

    it("system rail focus ring reaches 3:1 on the rail and on its hover tint", () => {
      const ring = get("rail-focus-ring");
      const rail = get("rail");
      const plain = contrastRatio(ring, rail);
      expect(plain, `rail-focus-ring on rail = ${plain.toFixed(2)}`).toBeGreaterThanOrEqual(3);
      const hovered = contrastOverComposite(ring, get("rail-hover"), rail);
      expect(
        hovered,
        `rail-focus-ring on rail-hover = ${hovered.toFixed(2)}`,
      ).toBeGreaterThanOrEqual(3);
    });

    it.each(IDENTITY_PAIRS)("identity label %s on %s ≥ 4.5:1", (fg, bg) => {
      expect(contrastRatio(get(fg), get(bg))).toBeGreaterThanOrEqual(4.5);
    });

    it("destructive text stays legible on its own translucent tint (rendered button/badge)", () => {
      for (const alpha of [0.1, 0.2]) {
        const ratio = contrastOverComposite(
          get("destructive"),
          withAlpha(get("destructive"), alpha),
          get("background"),
        );
        expect(ratio, `alpha ${alpha}`).toBeGreaterThanOrEqual(
          appearance === "high-contrast" ? 7 : 4.5,
        );
      }
    });
  });
});

describe("palette identity", () => {
  it("derives Notrelix Blue from the logo hue (#1E90FF, OKLCH hue ≈ 253–255)", () => {
    const seed = fromHex("#1E90FF");
    expect(seed.h).toBeGreaterThan(252);
    expect(seed.h).toBeLessThan(256);
    const light = accents.blue.tones.light;
    expect(light.mark.l).toBeCloseTo(0.63, 2);
    expect(light.solid.l).toBeCloseTo(0.53, 2);
    expect(light.ink.l).toBeCloseTo(0.45, 2);
    expect(accents.blue.tones.dark.ink.l).toBeCloseTo(0.72, 2);
    expect(accents.blue.tones.light.solid.h).toBeCloseTo(seed.h, 1);
  });

  it("keeps Ember visibly distinct from the danger red", () => {
    for (const appearance of THEME_APPEARANCES) {
      const distance = deltaEOk(
        accents.ember.tones[appearance].solid,
        statuses.danger[appearance].solid,
      );
      expect(distance, appearance).toBeGreaterThanOrEqual(0.08);
    }
  });

  it("gives Amber fills dark text", () => {
    for (const appearance of THEME_APPEARANCES) {
      const tones = accents.amber.tones[appearance];
      expect(tones.onSolid.l).toBeLessThan(0.25);
    }
  });

  it("tints neutrals toward hue 255 with very low chroma (no cream/beige)", () => {
    const colors = resolveSemanticColors("light", "blue");
    for (const role of [
      "background",
      "surface-sidebar",
      "surface-panel",
      "border",
      "foreground",
    ]) {
      const color = colors[role]!;
      expect(color.c, role).toBeLessThanOrEqual(0.01);
      if (color.c > 0) expect(color.h, role).toBe(255);
    }
  });

  it("separates the four surface tiers in every appearance", () => {
    for (const appearance of THEME_APPEARANCES) {
      const colors = resolveSemanticColors(appearance, "blue");
      const tiers = [
        "surface-canvas",
        "surface-sidebar",
        "surface-panel",
        "surface-raised",
      ].map((role) => colors[role]!);
      expect(new Set(tiers.map(toHex)).size, appearance).toBeGreaterThan(
        appearance === "high-contrast" ? 1 : 3,
      );
    }
  });
});

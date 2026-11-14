/**
 * The pieces that fly out from the headline and float around it (see `HeroOrbit`).
 *
 * Every piece has a twin on the other side of the headline (same row, mirrored), so the layout is
 * symmetric. Three layouts, chosen by CSS from the viewport width:
 * - lg (1280px and up): `g` is how far across the gutter (0 = the text's edge, 1 = the page's
 *   edge) and `y` the offset from the headline's centre, in px.
 * - md (1024-1279px): one column per side (`md`).
 * - sm / xs (under 1024px): a row in the band between the header and the badge (`sm` / `xs` =
 *   -1..1 across the page width, mirrored for the twin).
 */
export type OrbitKind = "avatar" | "tile" | "chip";
export type OrbitTone = "blue" | "red" | "orange";

export interface OrbitItem {
  readonly id: string;
  readonly kind: OrbitKind;
  readonly side: -1 | 1;
  readonly emoji: string;
  readonly chip?: "dueToday" | "inReview";
  readonly tone: OrbitTone;
  /** A pointer arrives, hovers and clicks this piece now and then. */
  readonly click?: boolean;
  /** Size in px at the lg layout. */
  readonly size: number;
  /** How far it moves with the pointer: 0.3 (far) to 1 (near). */
  readonly depth: number;
  readonly g: number;
  readonly y: number;
  readonly md?: { readonly g: number; readonly y: number };
  readonly sm?: number;
  readonly xs?: number;
  /** Idle float: seconds, pixels and degrees. */
  readonly float: {
    readonly s: number;
    readonly amp: number;
    readonly rot: number;
  };
}

/** Distance of the top band's row from the headline's centre (px), for sm and xs. */
export const ORBIT_BAND_Y = -185;

const f = (s: number, amp: number, rot: number) => ({ s, amp, rot });

type Spot = Pick<
  OrbitItem,
  | "id"
  | "kind"
  | "side"
  | "emoji"
  | "chip"
  | "tone"
  | "click"
  | "size"
  | "depth"
  | "g"
  | "y"
  | "md"
  | "sm"
  | "xs"
  | "float"
>;

/** Scattered on purpose: no two pieces share a row, a column or a distance from the headline. */
export const ORBIT_ITEMS: readonly OrbitItem[] = [
  {
    id: "l-avatar",
    kind: "avatar",
    side: -1,
    emoji: "😎",
    tone: "red",
    size: 37,
    depth: 1,
    g: 0.62,
    y: -170,
    md: { g: 0.55, y: -150 },
    sm: 0.86,
    xs: 0.82,
    float: f(5.6, 10, 3),
  },
  {
    id: "l-rocket",
    kind: "tile",
    side: -1,
    emoji: "🚀",
    tone: "blue",
    click: true,
    size: 35,
    depth: 0.8,
    g: 0.3,
    y: -75,
    md: { g: 0.4, y: -40 },
    sm: 0.52,
    float: f(6.4, 8, 2),
  },
  {
    id: "l-due",
    kind: "chip",
    side: -1,
    emoji: "⏰",
    chip: "dueToday",
    tone: "orange",
    size: 22,
    depth: 0.6,
    g: 0.7,
    y: 35,
    float: f(8.1, 7, 1.5),
  },
  {
    id: "l-cal",
    kind: "tile",
    side: -1,
    emoji: "📅",
    tone: "orange",
    size: 32,
    depth: 0.7,
    g: 0.36,
    y: 150,
    md: { g: 0.62, y: 120 },
    sm: 0.18,
    float: f(7.8, 8, 2),
  },
  {
    id: "l-avatar2",
    kind: "avatar",
    side: -1,
    emoji: "🤩",
    tone: "orange",
    size: 32,
    depth: 0.5,
    g: 0.6,
    y: 262,
    md: { g: 0.35, y: 245 },
    xs: 0.4,
    float: f(8.6, 9, 3),
  },
  {
    id: "r-avatar",
    kind: "avatar",
    side: 1,
    emoji: "😄",
    tone: "blue",
    size: 34,
    depth: 1,
    g: 0.42,
    y: -140,
    md: { g: 0.42, y: -165 },
    sm: 0.8,
    xs: 0.7,
    float: f(6.2, 10, -3),
  },
  {
    id: "r-target",
    kind: "tile",
    side: 1,
    emoji: "🎯",
    tone: "blue",
    click: true,
    size: 35,
    depth: 0.8,
    g: 0.72,
    y: -55,
    md: { g: 0.6, y: -25 },
    sm: 0.46,
    float: f(7.2, 8, -2),
  },
  {
    id: "r-review",
    kind: "chip",
    side: 1,
    emoji: "👀",
    chip: "inReview",
    tone: "blue",
    size: 22,
    depth: 0.6,
    g: 0.3,
    y: 70,
    float: f(7.6, 7, -1.5),
  },
  {
    id: "r-avatar2",
    kind: "avatar",
    side: 1,
    emoji: "😍",
    tone: "red",
    size: 35,
    depth: 0.7,
    g: 0.68,
    y: 175,
    md: { g: 0.5, y: 125 },
    sm: 0.1,
    xs: 0.34,
    float: f(5.9, 9, -3),
  },
  {
    id: "r-chat",
    kind: "tile",
    side: 1,
    emoji: "💬",
    tone: "red",
    size: 32,
    depth: 0.5,
    g: 0.28,
    y: 268,
    md: { g: 0.4, y: 245 },
    float: f(6.1, 8, -2),
  },
] satisfies readonly Spot[];

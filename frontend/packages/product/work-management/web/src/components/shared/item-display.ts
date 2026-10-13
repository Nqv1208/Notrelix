/**
 * Item display rules shared by the Kanban card and the item side sheet:
 * label tone, calendar days and due state. Pure functions; the clock is
 * always injected. Calendar days use an optional IANA `timeZone`; without
 * one the runtime time zone is used, the same default everywhere.
 */

// ─── Person mark tone ───────────────────────────────────────────────────────

/** The identity tones of the design tokens, as literal classes for Tailwind. */
const IDENTITY_MARK_CLASSES = [
  "bg-identity-blue text-identity-blue-foreground",
  "bg-identity-coral text-identity-coral-foreground",
  "bg-identity-gold text-identity-gold-foreground",
  "bg-identity-sage text-identity-sage-foreground",
  "bg-identity-ember text-identity-ember-foreground",
] as const;

/**
 * A stable identity tone per person; the API's raw member colour is not used.
 * Local copy of `IdentityMark`/`identityToneFor` in features/workspace
 * (`web/shell/shell-link.tsx`, `core/model/navigation.ts`), the shared origin.
 */
export function identityMarkClass(seed: string): string {
  let hash = 0;
  for (let index = 0; index < seed.length; index += 1) {
    hash = (hash * 31 + seed.charCodeAt(index)) | 0;
  }
  return (
    IDENTITY_MARK_CLASSES[Math.abs(hash) % IDENTITY_MARK_CLASSES.length] ??
    IDENTITY_MARK_CLASSES[0]
  );
}

// ─── Label tone ─────────────────────────────────────────────────────────────

/** The six label tones of the design tokens (`--label-<tone>`). */
export type ItemLabelTone =
  "data" | "security" | "training" | "report" | "doc" | "test";

/** Literal tone classes, so Tailwind sees every one of them. */
export const ITEM_LABEL_TONE_CLASS: Readonly<Record<ItemLabelTone, string>> = {
  data: "bg-label-data text-label-data-foreground",
  security: "bg-label-security text-label-security-foreground",
  training: "bg-label-training text-label-training-foreground",
  report: "bg-label-report text-label-report-foreground",
  doc: "bg-label-doc text-label-doc-foreground",
  test: "bg-label-test text-label-test-foreground",
};

/** Whole-word name keywords (normalised: lower case, no diacritics). */
const LABEL_TONE_KEYWORDS: ReadonlyArray<
  readonly [ItemLabelTone, readonly string[]]
> = [
  ["data", ["du lieu", "data"]],
  ["security", ["bao mat", "an ninh", "security"]],
  ["training", ["dao tao", "training"]],
  ["report", ["bao cao", "report", "reports", "reporting"]],
  ["test", ["kiem thu", "test", "tests", "testing", "qa"]],
  [
    "doc",
    ["tai lieu", "doc", "docs", "documentation", "ha tang", "infrastructure"],
  ],
];

function normaliseLabelName(name: string): string {
  return name
    .normalize("NFD")
    .replace(/[̀-ͯ]/g, "")
    .replace(/đ/g, "d")
    .replace(/Đ/g, "d")
    .toLowerCase()
    .trim();
}

function hasWord(text: string, phrase: string): boolean {
  return ` ${text.replace(/[^a-z0-9]+/g, " ")} `.includes(` ${phrase} `);
}

/** Hue (0–360) and saturation (0–1) of a `#rgb` / `#rrggbb` colour. */
function hueOf(color: string): { hue: number; saturation: number } | null {
  const match = /^#([0-9a-f]{3}|[0-9a-f]{6})$/i.exec(color.trim());
  if (!match?.[1]) return null;
  const hex =
    match[1].length === 3
      ? Array.from(match[1], (digit) => digit + digit).join("")
      : match[1];
  const [r, g, b] = [0, 2, 4].map(
    (offset) => parseInt(hex.slice(offset, offset + 2), 16) / 255,
  ) as [number, number, number];
  const max = Math.max(r, g, b);
  const min = Math.min(r, g, b);
  const delta = max - min;
  const lightness = (max + min) / 2;
  const saturation =
    delta === 0 ? 0 : delta / (1 - Math.abs(2 * lightness - 1));
  if (delta === 0) return { hue: 0, saturation };
  let hue: number;
  if (max === r) hue = ((g - b) / delta) % 6;
  else if (max === g) hue = (b - r) / delta + 2;
  else hue = (r - g) / delta + 4;
  hue *= 60;
  if (hue < 0) hue += 360;
  return { hue, saturation };
}

/**
 * The label tone of a label, deterministic:
 * 1. by name — normalised (lower case, diacritics removed, đ → d) and matched
 *    as whole words against the keyword table (Dữ liệu/data, Bảo mật/security,
 *    Đào tạo/training, Báo cáo/report, Kiểm thử/test, Tài liệu/Hạ tầng/doc);
 * 2. otherwise by the hue of the label colour: grey (saturation < 0.15) → doc,
 *    red/pink → security, orange/yellow → report, green → training,
 *    blue → data, purple → test;
 * 3. otherwise doc. The raw label colour is never painted.
 */
export function itemLabelTone(label: {
  readonly name: string;
  readonly color?: string | null;
}): ItemLabelTone {
  const name = normaliseLabelName(label.name);
  for (const [tone, keywords] of LABEL_TONE_KEYWORDS) {
    if (keywords.some((keyword) => hasWord(name, keyword))) return tone;
  }
  const colour = label.color ? hueOf(label.color) : null;
  if (!colour || colour.saturation < 0.15) return "doc";
  const { hue } = colour;
  if (hue < 20 || hue >= 330) return "security";
  if (hue < 70) return "report";
  if (hue < 170) return "training";
  if (hue < 250) return "data";
  return "test";
}

// ─── Calendar days ──────────────────────────────────────────────────────────

export interface CalendarParts {
  readonly year: number;
  readonly month: number;
  readonly day: number;
  readonly hour: number;
  readonly minute: number;
}

const DAY_MS = 86_400_000;

export function toValidDate(value: string | Date): Date | null {
  const date = typeof value === "string" ? new Date(value) : value;
  return Number.isNaN(date.getTime()) ? null : date;
}

/** Wall-clock parts of `date` in `timeZone` (the runtime zone when omitted). */
export function calendarParts(date: Date, timeZone?: string): CalendarParts {
  const parts = new Intl.DateTimeFormat("en-GB", {
    timeZone,
    year: "numeric",
    month: "2-digit",
    day: "2-digit",
    hour: "2-digit",
    minute: "2-digit",
    hourCycle: "h23",
  }).formatToParts(date);
  const value = (type: Intl.DateTimeFormatPartTypes) =>
    Number(parts.find((part) => part.type === type)?.value ?? 0);
  return {
    year: value("year"),
    month: value("month"),
    day: value("day"),
    hour: value("hour"),
    minute: value("minute"),
  };
}

function dayNumber(parts: CalendarParts): number {
  return Math.round(Date.UTC(parts.year, parts.month - 1, parts.day) / DAY_MS);
}

export function pad2(value: number): string {
  return String(value).padStart(2, "0");
}

/** Calendar days from today to `value`: negative in the past, 0 today. */
export function calendarDaysFromToday(
  value: string | Date,
  now: Date,
  timeZone?: string,
): number | null {
  const date = toValidDate(value);
  if (!date) return null;
  return (
    dayNumber(calendarParts(date, timeZone)) -
    dayNumber(calendarParts(now, timeZone))
  );
}

/** "10/10", or "18/01/2027" when the year differs from `now`. */
export function formatItemDate(
  value: string | Date,
  now: Date,
  timeZone?: string,
): string | null {
  const date = toValidDate(value);
  if (!date) return null;
  const parts = calendarParts(date, timeZone);
  const today = calendarParts(now, timeZone);
  const dayMonth = `${pad2(parts.day)}/${pad2(parts.month)}`;
  return parts.year === today.year ? dayMonth : `${dayMonth}/${parts.year}`;
}

// ─── Due state ──────────────────────────────────────────────────────────────

export type ItemDueState =
  | { readonly kind: "overdue"; readonly days: number; readonly text: string }
  | { readonly kind: "today"; readonly text: string }
  | { readonly kind: "date"; readonly text: string };

/**
 * Due state by calendar day against `now`: before today → overdue by N days;
 * the same day → today (even after the hour); later → date. `text` is always
 * the date ("dd/MM", "/yyyy" when the year differs). An absent or
 * unparseable date gives `null`.
 */
export function itemDueState(
  dueIso: string | null | undefined,
  now: Date,
  timeZone?: string,
): ItemDueState | null {
  if (!dueIso) return null;
  const days = calendarDaysFromToday(dueIso, now, timeZone);
  const text = formatItemDate(dueIso, now, timeZone);
  if (days === null || text === null) return null;
  if (days < 0) return { kind: "overdue", days: -days, text };
  if (days === 0) return { kind: "today", text };
  return { kind: "date", text };
}

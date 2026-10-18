import * as React from "react";
import { cn } from "@notrelix/ui-web";
import { isPlainClick } from "../shell/shell-models";
import { BTN, BTN_PRIMARY, BTN_SM, FOCUS } from "../spaces/space-page-parts";

/** The panel of the Billing mockup (`.card`): 12 px radius, 1 px line. */
export const CARD = "rounded-[12px] border border-line bg-popover dark:bg-card";
/** A section box (`.box`): a panel with 14 px padding and a 10 px gap. */
export const BOX = cn(CARD, "flex flex-col gap-2.5 p-3.5");
export const BOX_TITLE =
  "m-0 text-compact leading-[1.45] font-semibold text-muted-foreground";
export const BOX_TEXT = "m-0 text-crumb leading-[1.55] text-muted-foreground";

const DISABLED =
  "disabled:cursor-not-allowed disabled:border-line-strong disabled:bg-state-hover-strong disabled:font-medium disabled:text-meta-foreground disabled:hover:bg-state-hover-strong";
export const BILLING_BTN = cn(BTN, FOCUS, DISABLED);
export const BILLING_BTN_PRIMARY = cn(BTN_PRIMARY, FOCUS, DISABLED);
export const BILLING_BTN_SM = cn(BTN_SM, FOCUS, DISABLED);
export const BILLING_BTN_SM_PRIMARY = cn(
  "inline-flex h-7 items-center gap-1.5 rounded-[7px] border border-primary bg-primary px-2.5 text-crumb leading-[1.45] font-semibold whitespace-nowrap text-primary-foreground hover:bg-primary/90",
  FOCUS,
  DISABLED,
);

/** Text link button (`.lnkb`). */
export const LNKB = cn(
  "inline cursor-pointer border-0 bg-transparent p-0 text-crumb leading-[1.45] font-semibold whitespace-nowrap text-brand-ink hover:underline",
  FOCUS,
);

export const MONO = "font-mono";

const GLYPHS = {
  back: ["M15 6l-6 6 6 6"],
  check: ["m5 12 5 5L20 7"],
  warn: ["M12 3 2 20h20z", "M12 10v4M12 17h.01"],
  info: ["circle:12,12,9", "M12 11v5M12 8h.01"],
  plus: ["M12 5v14M5 12h14"],
  x: ["M6 6l12 12M18 6 6 18"],
  bank: ["M3 10h18M5 10v8M9.5 10v8M14.5 10v8M19 10v8M3 20h18M12 3 3 8h18z"],
  download: ["M12 3v12M7 10l5 5 5-5M5 21h14"],
  mail: ["rect:3,5,18,14,2", "m3 7 9 6 9-6"],
  go: ["m9 6 6 6-6 6"],
  ext: [
    "M14 4h6v6M20 4l-9 9M18 14v5a1 1 0 0 1-1 1H5a1 1 0 0 1-1-1V7a1 1 0 0 1 1-1h5",
  ],
  lock: ["rect:5,11,14,9,2", "M8 11V8a4 4 0 0 1 8 0v3"],
  clock: ["circle:12,12,9", "M12 7v5l3 2"],
} as const;
export type BillingGlyphName = keyof typeof GLYPHS;

/** An inline stroke glyph at the mockup's 24 x 24 path data. */
export function BillingGlyph({
  name,
  className,
}: {
  name: BillingGlyphName;
  className?: string;
}) {
  return (
    <svg
      viewBox="0 0 24 24"
      aria-hidden="true"
      fill="none"
      stroke="currentColor"
      strokeWidth={1.8}
      strokeLinecap="round"
      strokeLinejoin="round"
      className={cn("size-4 shrink-0", className)}
    >
      {GLYPHS[name].map((part, index) => {
        if (part.startsWith("rect:")) {
          const [x, y, w, h, r] = part.slice(5).split(",").map(Number);
          return <rect key={index} x={x} y={y} width={w} height={h} rx={r} />;
        }
        if (part.startsWith("circle:")) {
          const [cx, cy, r] = part.slice(7).split(",").map(Number);
          return <circle key={index} cx={cx} cy={cy} r={r} />;
        }
        return <path key={index} d={part} />;
      })}
    </svg>
  );
}

const TAG_TONE = {
  neutral: "bg-state-hover-strong text-muted-foreground",
  ok: "bg-success-subtle text-success-text",
  warn: "bg-warning-subtle text-warning-text",
  acc: "bg-state-selected text-brand-ink",
} as const;

/** A status tag (`.tag`): 20 px, 11.5 px semibold. */
export function Tag({
  tone = "neutral",
  className,
  children,
}: {
  tone?: keyof typeof TAG_TONE;
  className?: string;
  children: React.ReactNode;
}) {
  return (
    <span
      data-f="tag"
      className={cn(
        "inline-flex h-5 items-center rounded-[5px] px-[7px] text-micro leading-[1.45] font-semibold whitespace-nowrap",
        TAG_TONE[tone],
        className,
      )}
    >
      {children}
    </span>
  );
}

/** A segmented meter (`.meter`): up to two segments of the account's workspaces. */
export function Meter({
  max,
  now,
  label,
  segments,
}: {
  max: number;
  now: number;
  label: string;
  segments: readonly { share: number; tone: "one" | "two" }[];
}) {
  return (
    <div
      role="meter"
      aria-valuemin={0}
      aria-valuemax={max}
      aria-valuenow={now}
      aria-label={label}
      data-f="meter"
      className="flex h-2 overflow-hidden rounded-full bg-state-hover-strong shadow-[inset_0_0_0_1px_var(--color-line)]"
    >
      {segments.map((segment, index) => (
        <span
          key={index}
          style={{ width: `${segment.share}%` }}
          className={cn(
            "block h-full",
            segment.tone === "one" ? "bg-primary" : "bg-mark-sage",
          )}
        />
      ))}
    </div>
  );
}

/** A legend dot (`.dot`). */
export function Dot({ tone }: { tone: "one" | "two" }) {
  return (
    <i
      aria-hidden="true"
      className={cn(
        "inline-block size-2 shrink-0 rounded-[2px]",
        tone === "one" ? "bg-primary" : "bg-mark-sage",
      )}
    />
  );
}

/** The grey note box (`.info`). */
export function InfoNote({
  children,
  className,
}: {
  children: React.ReactNode;
  className?: string;
}) {
  return (
    <div
      data-f="info"
      className={cn(
        "flex items-start gap-[9px] rounded-[8px] border border-line bg-background px-3 py-2.5 text-crumb leading-[1.5] text-muted-foreground",
        className,
      )}
    >
      <BillingGlyph
        name="info"
        className="mt-0.5 size-3.5 text-meta-foreground"
      />
      <span>{children}</span>
    </div>
  );
}

/** Renders `<b>…</b>` runs of a copy string as bold. */
export function Rich({ text }: { text: string }) {
  const parts = text.split(/(<b>[^<]{0,300}<\/b>)/g);
  return (
    <>
      {parts.map((part, index) =>
        part.startsWith("<b>") ? (
          <b key={index} className="font-semibold text-foreground">
            {part.slice(3, -4)}
          </b>
        ) : (
          <React.Fragment key={index}>{part}</React.Fragment>
        ),
      )}
    </>
  );
}

/** The back link of the sub pages (`.back`). */
export function BackLink({
  href,
  onNavigate,
  children,
}: {
  href: string;
  onNavigate: () => void;
  children: React.ReactNode;
}) {
  return (
    <a
      href={href}
      onClick={(event) => {
        if (!isPlainClick(event)) return;
        event.preventDefault();
        onNavigate();
      }}
      className={cn(
        "inline-flex items-center gap-1.5 self-start text-compact leading-[1.45] font-medium text-brand-ink",
        FOCUS,
      )}
    >
      <BillingGlyph name="back" />
      {children}
    </a>
  );
}

/** An anchor that navigates in the app on a plain click. */
export function NavAnchor({
  link,
  className,
  children,
  ...rest
}: {
  link: { href: string; onNavigate: () => void };
  className?: string;
  children: React.ReactNode;
} & Omit<React.AnchorHTMLAttributes<HTMLAnchorElement>, "href" | "onClick">) {
  return (
    <a
      {...rest}
      href={link.href}
      className={className}
      onClick={(event) => {
        if (!isPlainClick(event)) return;
        event.preventDefault();
        link.onNavigate();
      }}
    >
      {children}
    </a>
  );
}

/** The page head of a section (`.ph`): title, lede and an aside. */
export function PageHead({
  id,
  title,
  lede,
  aside,
}: {
  id: string;
  title: string;
  lede?: React.ReactNode;
  aside?: React.ReactNode;
}) {
  return (
    <div data-f="ph" className="flex items-start gap-4">
      <div>
        <h2
          id={id}
          className="m-0 text-section-heading leading-[1.45] font-[650] text-foreground"
        >
          {title}
        </h2>
        {lede ? (
          <p className="mt-[3px] mb-0 max-w-[72ch] text-compact leading-[1.5] text-meta-foreground">
            {lede}
          </p>
        ) : null}
      </div>
      <span className="grow" />
      {aside}
    </div>
  );
}

export const DIALOG_OVERLAY = "bg-modal-scrim backdrop-blur-none";
/** The dialog panel of the mockup (`.dlg`): 14 px radius, 560 px wide, 96 px from the top. */
export const DIALOG_PANEL =
  "top-24 box-border flex w-[560px] max-w-[calc(100%-2rem)] translate-y-0 flex-col gap-0 overflow-hidden rounded-[14px] border-0 bg-popover p-0 text-body leading-[1.45] font-normal shadow-dialog ring-0 sm:max-w-[calc(100%-2rem)] dark:bg-card";
export const FIELD_LABEL =
  "text-crumb leading-[1.45] font-semibold text-muted-foreground";

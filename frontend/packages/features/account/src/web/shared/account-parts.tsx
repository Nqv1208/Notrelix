import * as React from "react";
import { Check, Info as InfoIcon } from "lucide-react";
import { resolveApiAssetUrl } from "@notrelix/contracts";
import { cn } from "@notrelix/ui-web";

/**
 * Parts the account pages share (Final-Account): the page heading, tags, the
 * grey info note, the switch, the buttons and the toast. Presentation only.
 */

export const FOCUS =
  "outline-none focus-visible:ring-2 focus-visible:ring-focus-ring";

/** Final-Account `.btn`: 32 px, 1 px strong border, 13 px medium. */
export const BTN =
  "inline-flex h-8 items-center gap-1.5 rounded-[7px] border border-line-strong bg-popover px-3 text-compact leading-[1.45] font-medium whitespace-nowrap text-foreground hover:bg-state-hover-strong disabled:cursor-default disabled:opacity-60 dark:bg-card";
export const BTN_PRIMARY =
  "inline-flex h-8 items-center gap-1.5 rounded-[7px] border border-primary bg-primary px-3 text-compact leading-[1.45] font-semibold whitespace-nowrap text-primary-foreground hover:bg-primary/90 disabled:cursor-default disabled:opacity-60";
export const BTN_SM =
  "inline-flex h-7 items-center gap-1.5 rounded-[7px] border border-line-strong bg-popover px-2.5 text-crumb leading-[1.45] font-medium whitespace-nowrap text-foreground hover:bg-state-hover-strong disabled:cursor-default disabled:opacity-60 dark:bg-card";
export const BTN_DANGER =
  "inline-flex h-8 items-center gap-1.5 rounded-[7px] border border-danger bg-danger px-3 text-compact leading-[1.45] font-semibold whitespace-nowrap text-danger-foreground hover:bg-danger/90 disabled:cursor-default disabled:opacity-60";
/** Final-Account `.lnkb`: a text button, 12.5 px semibold. */
export const LINK_BUTTON =
  "border-0 bg-transparent p-0 text-crumb leading-[1.45] font-semibold text-link hover:underline disabled:cursor-not-allowed disabled:font-medium disabled:text-meta-foreground disabled:opacity-70 disabled:no-underline";
export const LINK_BUTTON_DANGER =
  "border-0 bg-transparent p-0 text-crumb leading-[1.45] font-semibold text-danger-text hover:underline disabled:cursor-not-allowed disabled:font-medium disabled:text-meta-foreground disabled:opacity-70 disabled:no-underline";
export const INPUT =
  "box-border h-9 w-full rounded-[8px] border border-line-strong bg-popover px-2.5 text-nav leading-[1.45] text-foreground focus:border-primary focus:ring-2 focus:ring-primary/20 focus:outline-none dark:bg-card";
export const SELECT_SM =
  "h-8 min-w-[150px] rounded-[7px] border border-line-strong bg-popover px-2 text-compact leading-[1.45] text-foreground focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-focus-ring dark:bg-card";

/** The heading of a page of the account: title, one line of help, an action. */
export function PageHead({
  id,
  title,
  description,
  action,
}: {
  id?: string;
  title: string;
  description: string;
  action?: React.ReactNode;
}) {
  return (
    <div data-f="ph" className="flex items-start gap-4">
      <div className="min-w-0">
        <h2
          id={id}
          className="m-0 text-lg leading-[1.45] font-[650] text-foreground"
        >
          {title}
        </h2>
        <p className="m-0 mt-[3px] max-w-[66ch] text-compact leading-normal text-meta-foreground">
          {description}
        </p>
      </div>
      {action ? <div className="ml-auto shrink-0">{action}</div> : null}
    </div>
  );
}

const TAG_TONE = {
  neutral: "bg-state-hover-strong text-muted-foreground",
  ok: "bg-success-subtle text-success-subtle-foreground",
  accent: "bg-state-selected text-link",
} as const;

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

/** Final-Account `.info`: a grey note with an info glyph. */
export function Note({
  id,
  className,
  children,
}: {
  id?: string;
  className?: string;
  children: React.ReactNode;
}) {
  return (
    <div
      id={id}
      data-f="info"
      className={cn(
        "flex items-start gap-[9px] rounded-[8px] border border-line bg-background px-3 py-2.5 text-crumb leading-normal text-muted-foreground",
        className,
      )}
    >
      <InfoIcon
        aria-hidden="true"
        className="mt-0.5 size-3.5 shrink-0 text-meta-foreground"
      />
      <span>{children}</span>
    </div>
  );
}

/** Final-Account `.sw`: a 34 x 20 switch. */
export function Toggle({
  checked,
  disabled,
  onChange,
  ...aria
}: {
  checked: boolean;
  disabled?: boolean;
  onChange: (next: boolean) => void;
  "aria-labelledby"?: string;
  "aria-describedby"?: string;
  "aria-label"?: string;
  id?: string;
}) {
  return (
    <button
      type="button"
      role="switch"
      aria-checked={checked}
      disabled={disabled}
      onClick={() => onChange(!checked)}
      className={cn(
        "relative h-5 w-[34px] shrink-0 rounded-full border-0 p-0 transition-colors disabled:cursor-not-allowed disabled:opacity-70",
        checked
          ? "bg-primary"
          : "bg-line-strong shadow-[inset_0_0_0_1px_var(--color-meta-foreground)]",
        FOCUS,
      )}
      {...aria}
    >
      <span
        aria-hidden="true"
        className={cn(
          "absolute top-0.5 size-4 rounded-full bg-white shadow-[0_1px_2px_rgb(0_0_0/0.2)] transition-[left]",
          checked ? "left-4" : "left-0.5",
        )}
      />
    </button>
  );
}

/** Final-Account `.toast`: the quiet confirmation at the foot of the page. */
export function Toast({ children }: { children: React.ReactNode }) {
  return (
    <div
      role="status"
      className="absolute bottom-[22px] left-1/2 z-toast flex h-[38px] -translate-x-1/2 items-center gap-2 rounded-[9px] bg-inverse-surface pr-3.5 pl-3 text-compact leading-[1.45] font-medium whitespace-nowrap text-inverse-foreground shadow-lg"
    >
      <Check
        aria-hidden="true"
        className="size-4 shrink-0 text-inverse-success"
      />
      {children}
    </div>
  );
}

/** The error panel of a page that could not load, with a retry. */
export function LoadError({
  message,
  retry,
  onRetry,
}: {
  message: string;
  retry: string;
  onRetry: () => void;
}) {
  return (
    <div
      role="alert"
      className="flex flex-wrap items-center justify-between gap-3 rounded-[12px] border border-line bg-popover px-4 py-3 dark:bg-card"
    >
      <p className="m-0 min-w-0 text-body text-muted-foreground">{message}</p>
      <button type="button" onClick={onRetry} className={cn(BTN_SM, FOCUS)}>
        {retry}
      </button>
    </div>
  );
}

/** The initials of a person's name: first and last letter group. */
export function initialsOf(name: string): string {
  const parts = name
    .trim()
    .split(/\s+/)
    .filter((part) => /^\p{L}/u.test(part));
  if (parts.length === 0) return "";
  const first = parts[0] ?? "";
  const last = parts.length > 1 ? (parts[parts.length - 1] ?? "") : "";
  return `${first.charAt(0)}${last.charAt(0)}`.toUpperCase();
}

/** A round avatar: the photo, or the initials on the sage identity colour. */
export function Avatar({
  name,
  url,
  className,
}: {
  name: string;
  url?: string | null;
  className?: string;
}) {
  const src = resolveApiAssetUrl(url);
  // A photo the producer will not serve (removed, not allowed) falls back to the initials.
  const [failedSrc, setFailedSrc] = React.useState<string | null>(null);
  return (
    <span
      aria-hidden="true"
      className={cn(
        "grid shrink-0 place-items-center overflow-hidden rounded-full bg-mark-sage font-bold text-mark-sage-foreground",
        className,
      )}
    >
      {src && src !== failedSrc ? (
        <img
          src={src}
          alt=""
          className="size-full object-cover"
          onError={() => setFailedSrc(src)}
        />
      ) : (
        initialsOf(name)
      )}
    </span>
  );
}

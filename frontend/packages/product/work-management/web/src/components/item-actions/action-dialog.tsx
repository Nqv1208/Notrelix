import type { ReactNode, Ref } from "react";
import { Info, TriangleAlert, X } from "lucide-react";
import {
  Button,
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  cn,
} from "@notrelix/ui-web";

/**
 * The shell of the item action dialogs (remind, move) as WM-Actions-Remind and
 * WM-Actions-Move draw it: a 14 px dialog with its title, a description (spoken
 * only, on states that replace it with a banner), a body and a footer with a
 * hint and 30 px buttons. On a phone it is a bottom sheet with 48 px buttons
 * stacked, the primary first. While `busy` the request is out and the dialog
 * cannot be dismissed.
 */
export function ActionDialog({
  open,
  busy = false,
  title,
  phoneTitle,
  description,
  phoneDescription,
  hideDescription = false,
  closeLabel,
  maxWidth = "sm:max-w-130",
  initialFocus,
  hint,
  footer,
  children,
  onCancel,
}: {
  open: boolean;
  busy?: boolean;
  title: string;
  /** The sheet's own title on a phone, when it differs. */
  phoneTitle?: string;
  description: string;
  /** What the sheet shows under its title on a phone instead of the description; `null` hides it there. */
  phoneDescription?: string | null;
  hideDescription?: boolean;
  closeLabel: string;
  maxWidth?: string;
  /** The button that takes focus on open, as drawn. */
  initialFocus?: { current: HTMLElement | null };
  hint?: string;
  footer: ReactNode;
  children?: ReactNode;
  onCancel: () => void;
}) {
  return (
    <Dialog
      open={open}
      onOpenChange={(next) => {
        if (!next && !busy) onCancel();
      }}
    >
      <DialogContent
        showCloseButton={false}
        overlayClassName="bg-modal-scrim backdrop-blur-none"
        className={cn(
          "box-border gap-0 overflow-hidden rounded-[14px] border-0 bg-popover p-0 leading-[1.45] shadow-[0_24px_60px_rgba(0,0,0,.28)] ring-0 dark:bg-card dark:shadow-[0_24px_60px_rgba(0,0,0,.6)]",
          maxWidth,
          "max-sm:top-auto max-sm:bottom-0 max-sm:max-w-full max-sm:translate-y-0 max-sm:rounded-b-none max-sm:rounded-t-[14px] max-sm:pb-6",
        )}
        onOpenAutoFocus={(event) => {
          const target = initialFocus?.current;
          if (!target) return;
          event.preventDefault();
          target.focus();
        }}
        onEscapeKeyDown={(event) => {
          if (busy) event.preventDefault();
        }}
        onInteractOutside={(event) => {
          if (busy) event.preventDefault();
        }}
      >
        <span
          aria-hidden="true"
          className="mx-auto mt-1.5 mb-0.5 hidden h-[5px] w-9 rounded-[3px] bg-line-strong max-sm:block"
        />
        <DialogHeader className="relative gap-0 pt-5 pr-[52px] pb-1.5 pl-[22px] text-left max-sm:flex max-sm:flex-row max-sm:flex-wrap max-sm:content-start max-sm:items-center max-sm:gap-x-2 max-sm:p-0 max-sm:pr-2 max-sm:pl-5">
          <DialogTitle className="text-[18px] leading-[1.45] font-[650] max-sm:my-[13.7px] max-sm:min-w-0 max-sm:flex-1 max-sm:text-[17px]">
            {phoneTitle ? (
              <>
                <span className="max-sm:hidden">{title}</span>
                <span className="hidden max-sm:inline">{phoneTitle}</span>
              </>
            ) : (
              title
            )}
          </DialogTitle>
          <DialogDescription
            className={cn(
              "mt-1.5 text-[13.5px] text-muted-foreground max-sm:order-last max-sm:mt-0 max-sm:basis-full max-sm:py-0.5 max-sm:pr-3 max-sm:pb-2 max-sm:text-sm",
              hideDescription && "sr-only",
              phoneDescription === null && "max-sm:sr-only",
            )}
          >
            {phoneDescription ? (
              <>
                <span className="max-sm:hidden">{description}</span>
                <span className="hidden max-sm:inline">{phoneDescription}</span>
              </>
            ) : (
              description
            )}
          </DialogDescription>
          <Button
            type="button"
            variant="ghost"
            size="icon-sm"
            aria-label={closeLabel}
            title={closeLabel}
            aria-disabled={busy || undefined}
            className={cn(
              "absolute top-3.5 right-3 size-7 rounded-[7px] text-meta-foreground max-sm:static max-sm:size-11 max-sm:text-foreground",
              busy && "opacity-40",
            )}
            onClick={busy ? undefined : onCancel}
          >
            <X aria-hidden="true" className="size-4" strokeWidth={1.8} />
          </Button>
        </DialogHeader>
        {children}
        <DialogFooter className="flex-row items-center gap-2 px-[22px] pt-4 pb-[18px] max-sm:flex-col-reverse max-sm:items-stretch max-sm:px-4 max-sm:pt-2 max-sm:pb-0">
          <span className="min-w-35 flex-1 text-xs leading-[1.45] text-meta-foreground max-sm:hidden">
            {hint}
          </span>
          {footer}
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}

// The WM-Actions `.btn`: 30 px (48 px on a phone), 7 px radius, 13 px type.
const buttonClass =
  "h-[30px] rounded-[7px] px-[11px] text-[13px] leading-[1.45] max-sm:h-12 max-sm:w-full max-sm:rounded-[12px] max-sm:text-[15px]";

/** A footer button: `primary` is the filled accent one, the rest are outlined. */
export function ActionButton({
  primary = false,
  disabled = false,
  buttonRef,
  className,
  onClick,
  children,
}: {
  primary?: boolean;
  /** Drawn disabled but still announced, like the canvas's `aria-disabled`. */
  disabled?: boolean;
  buttonRef?: Ref<HTMLButtonElement>;
  className?: string;
  onClick?: () => void;
  children: ReactNode;
}) {
  return (
    <Button
      ref={buttonRef}
      type="button"
      variant={primary ? "default" : "outline"}
      aria-disabled={disabled || undefined}
      className={cn(
        buttonClass,
        primary
          ? "font-semibold"
          : "border-line-strong bg-popover font-medium shadow-none dark:border-line-strong dark:bg-card",
        disabled && "opacity-50",
        className,
      )}
      onClick={disabled ? undefined : onClick}
    >
      {children}
    </Button>
  );
}

const tones = {
  err: "border-transparent bg-danger-subtle text-danger-text",
  warn: "border-warning-text/40 bg-warning-subtle text-warning-text",
  info: "border-line-strong bg-background text-muted-foreground",
} as const;

/** The WM-Actions banner: an icon and one message, `alert` for a failure, `status` otherwise. */
export function ActionBanner({
  tone,
  className,
  children,
}: {
  tone: keyof typeof tones;
  className?: string;
  children: ReactNode;
}) {
  const Icon = tone === "info" ? Info : TriangleAlert;
  return (
    <div
      role={tone === "err" ? "alert" : "status"}
      className={cn(
        "flex items-start gap-2 rounded-[8px] border px-2.5 py-2 text-[12.5px] leading-[1.45] max-sm:text-[13.5px]",
        tones[tone],
        className,
      )}
    >
      <Icon aria-hidden="true" className="mt-0.5 size-3.5 shrink-0" />
      <span className="min-w-0">{children}</span>
    </div>
  );
}

/** One line of the impact list: a 30 px icon tile, a bold lead and a note under it. */
export function ImpactLine({
  icon,
  lead,
  note,
  warn = false,
}: {
  icon: ReactNode;
  lead: string;
  note: string;
  warn?: boolean;
}) {
  return (
    <li className="flex gap-[11px] border-t border-line px-3.5 py-2.5 text-[13px] first:border-t-0">
      <span
        aria-hidden="true"
        className={cn(
          "box-border grid size-[30px] flex-none place-items-center rounded-[7px] border",
          warn
            ? "border-transparent bg-warning-subtle text-warning-text"
            : "border-line bg-background text-muted-foreground",
        )}
      >
        {icon}
      </span>
      <span className="min-w-0">
        <b className="block font-semibold">{lead}</b>
        <small className="block text-[12.5px] text-meta-foreground">
          {note}
        </small>
      </span>
    </li>
  );
}

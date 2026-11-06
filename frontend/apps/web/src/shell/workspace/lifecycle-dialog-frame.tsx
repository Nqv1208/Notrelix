import * as React from "react";
import { X } from "lucide-react";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogTitle,
  cn,
} from "@notrelix/ui-web";

/**
 * The frame of the board transfer dialogs (WM-Export, WM-Import): one 600 or 880 px box, a 390 px bottom sheet,
 * with the artboard's own header, body and footer rhythm (title 17 px / 650, padding 20, rows of 14 px). These
 * dialogs draw a denser type scale than the shared dialog (14 px title, padding 24), so they carry this variant
 * instead of changing every dialog.
 */
export function FrameDialog({
  open,
  onOpenChange,
  wide = false,
  children,
}: {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  wide?: boolean;
  children: React.ReactNode;
}) {
  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent
        showCloseButton={false}
        className={cn(
          "grid-cols-[minmax(0,1fr)] gap-0 overflow-hidden rounded-[14px] border border-line p-0 text-frame-body leading-[1.45] ring-0",
          wide ? "sm:max-w-[880px]" : "sm:max-w-[600px]",
          "max-sm:top-auto max-sm:bottom-0 max-sm:max-w-full max-sm:translate-y-0 max-sm:rounded-b-none max-sm:rounded-t-[16px] max-sm:border-x-0 max-sm:border-b-0 max-sm:text-frame-sheet",
        )}
      >
        {children}
      </DialogContent>
    </Dialog>
  );
}

export function FrameHeader({
  title,
  description,
  onClose,
  closeLabel,
  closeDisabled,
}: {
  title: string;
  description?: string;
  /** Absent: no close mark (the dialog is held open while it works). */
  onClose?: () => void;
  closeLabel: string;
  closeDisabled?: boolean;
}) {
  return (
    <div className="flex items-center gap-2.5 px-5 pb-2 pt-4 max-sm:pb-2 max-sm:pl-4 max-sm:pr-2 max-sm:pt-3.5">
      <div className="min-w-0 flex-1">
        <DialogTitle className="m-0 text-frame-title font-[650] leading-[1.45]">
          {title}
        </DialogTitle>
        {description ? (
          <DialogDescription className="mt-0.5 text-frame-small leading-[1.45] text-meta-foreground">
            {description}
          </DialogDescription>
        ) : (
          <DialogDescription className="sr-only">{title}</DialogDescription>
        )}
      </div>
      {onClose ? (
        <button
          type="button"
          aria-label={closeLabel}
          title={closeLabel}
          onClick={onClose}
          disabled={closeDisabled}
          className="inline-flex size-8 flex-none items-center justify-center rounded-[7px] text-meta-foreground outline-none hover:bg-state-hover-strong focus-visible:ring-2 focus-visible:ring-focus-ring max-sm:size-11"
        >
          <X aria-hidden="true" className="size-4" />
        </button>
      ) : null}
    </div>
  );
}

export function FrameBody({
  className,
  children,
}: {
  className?: string;
  children: React.ReactNode;
}) {
  return (
    <div
      className={cn(
        "flex flex-col gap-3.5 px-5 pb-4 pt-2 max-sm:px-4",
        className,
      )}
    >
      {children}
    </div>
  );
}

export function FrameFooter({
  children,
  stack,
}: {
  children: React.ReactNode;
  stack?: boolean;
}) {
  return (
    <div
      className={cn(
        "flex items-center justify-end gap-2 border-t border-line bg-popover px-5 py-3 max-sm:flex-wrap max-sm:px-4 max-sm:pb-4",
        stack && "max-sm:flex-col-reverse max-sm:items-stretch",
      )}
    >
      {children}
    </div>
  );
}

const BUTTON =
  "inline-flex h-8 flex-none items-center justify-center gap-1.5 whitespace-nowrap rounded-[7px] border px-3 text-frame-small outline-none focus-visible:ring-2 focus-visible:ring-focus-ring max-sm:h-11 max-sm:min-w-[120px] max-sm:flex-1";

export function FrameButton({
  primary,
  disabled,
  className,
  ...props
}: React.ComponentProps<"button"> & { primary?: boolean }) {
  return (
    <button
      type="button"
      {...props}
      disabled={disabled}
      aria-disabled={disabled || undefined}
      className={cn(
        BUTTON,
        disabled
          ? "cursor-not-allowed border-line-strong bg-state-hover-strong font-medium text-meta-foreground"
          : primary
            ? "border-primary bg-primary font-semibold text-primary-foreground hover:opacity-90"
            : "border-line-strong bg-popover font-medium text-foreground hover:bg-state-hover-strong",
        className,
      )}
    />
  );
}

export function FrameField({
  label,
  className,
  children,
}: {
  label: string;
  className?: string;
  children: React.ReactNode;
}) {
  return (
    <div className="flex min-w-0 flex-col gap-[5px]">
      <span className="text-frame-note font-semibold text-meta-foreground">
        {label}
      </span>
      <div
        role="radiogroup"
        aria-label={label}
        className={cn("flex flex-col gap-2", className)}
      >
        {children}
      </div>
    </div>
  );
}

/** A radio drawn as the artboard does: an 18 px ring, a title, a body and optional chips under them. */
export function FrameRadio({
  checked,
  name,
  onSelect,
  title,
  body,
  extra,
  disabled,
}: {
  checked: boolean;
  name: string;
  onSelect: () => void;
  title: string;
  body: string;
  extra?: React.ReactNode;
  disabled?: boolean;
}) {
  return (
    <label
      className={cn(
        "flex cursor-pointer items-start gap-3 rounded-[10px] border px-3.5 py-3 has-[:focus-visible]:ring-2 has-[:focus-visible]:ring-focus-ring max-sm:min-h-14",
        checked
          ? "border-primary bg-state-hover-strong ring-1 ring-primary"
          : "border-line-strong bg-popover",
        disabled && "opacity-60",
      )}
    >
      <input
        type="radio"
        name={name}
        checked={checked}
        disabled={disabled}
        onChange={onSelect}
        className="mt-px size-[18px] flex-none appearance-none rounded-full border-[1.5px] border-line-strong outline-none checked:border-2 checked:border-primary checked:bg-[radial-gradient(circle,var(--color-primary)_0_4px,transparent_4.5px)]"
      />
      <span className="min-w-0 flex-1">
        <b className="block text-frame-body font-semibold">{title}</b>
        <small className="mt-0.5 block text-frame-note leading-[1.45] text-meta-foreground">
          {body}
        </small>
        {extra}
      </span>
    </label>
  );
}

const TONES = {
  info: "bg-info-subtle text-info-subtle-foreground",
  warn: "bg-warning-subtle text-warning-text",
  err: "bg-danger-subtle text-danger-subtle-foreground",
  ok: "bg-success-subtle text-success-text",
} as const;

/** A note row: a 16 px mark, a bold line and a body (13 px, line 1.4). */
export function FrameBanner({
  tone,
  icon,
  title,
  body,
  role = "status",
}: {
  tone: keyof typeof TONES;
  icon: React.ReactNode;
  title: string;
  body?: string;
  role?: "status" | "alert";
}) {
  return (
    <div
      role={role}
      className={cn(
        "flex items-start gap-2.5 rounded-[10px] px-3.5 py-2.5 text-frame-small leading-[1.4]",
        TONES[tone],
      )}
    >
      <span className="mt-0.5 flex-none [&>svg]:size-4">{icon}</span>
      <span className="flex min-w-0 flex-1 flex-col gap-px">
        <b className="font-semibold">{title}</b>
        {body ? <span>{body}</span> : null}
      </span>
    </div>
  );
}

/** A bordered box of rows (the artboard's summary and progress boxes). */
export function FrameBox({ children }: { children: React.ReactNode }) {
  return (
    <div className="overflow-hidden rounded-[10px] border border-line bg-popover">
      {children}
    </div>
  );
}

export function FrameRow({
  children,
  top,
}: {
  children: React.ReactNode;
  top?: boolean;
}) {
  return (
    <div
      className={cn(
        "flex min-h-[52px] gap-3 border-t border-line px-3.5 py-2 first:border-t-0",
        top ? "items-start" : "items-center",
      )}
    >
      {children}
    </div>
  );
}

import type { ReactNode } from "react";
import { CircleAlert, RotateCw } from "lucide-react";
import { cn } from "@notrelix/ui-web";
import type { FormStatus } from "@notrelix/work-management-core";
import { BUTTON_CLASS } from "../board-settings/board-settings-parts";
import type { FormsCopy } from "./forms.copy";

const CHIP =
  "inline-flex h-5 shrink-0 items-center rounded-[6px] px-[7px] text-micro font-semibold whitespace-nowrap";

const STATUS_CHIP: Readonly<Record<FormStatus, string>> = {
  published: "bg-label-green text-label-green-foreground",
  draft: "bg-label-report text-label-report-foreground",
  closed: "bg-label-doc text-label-doc-foreground",
};

/** The status of a form as a chip. */
export function FormStatusChip({
  status,
  copy,
}: {
  status: FormStatus;
  copy: FormsCopy;
}) {
  return (
    <span className={cn(CHIP, STATUS_CHIP[status])}>
      {copy.common.status[status]}
    </span>
  );
}

export function Chip({
  tone,
  children,
}: {
  tone: "brand" | "danger" | "neutral";
  children: ReactNode;
}) {
  return (
    <span
      className={cn(
        CHIP,
        tone === "brand" && "bg-brand-subtle text-brand-subtle-foreground",
        tone === "danger" && "bg-danger-subtle text-danger-text",
        tone === "neutral" && "bg-state-hover-strong text-muted-foreground",
      )}
    >
      {children}
    </span>
  );
}

/** A failure with its retry: what did not happen and that nothing was lost (the drawn `.alert` and small button). */
export function FormAlert({
  message,
  onRetry,
  retryLabel,
  className,
}: {
  message: ReactNode;
  onRetry?: () => void;
  retryLabel?: string;
  className?: string;
}) {
  return (
    <div role="alert" className={cn("flex flex-col gap-2", className)}>
      <div className="flex items-start gap-2 text-crumb text-danger-text">
        <CircleAlert aria-hidden="true" className="size-4 shrink-0" />
        <span className="flex-1">{message}</span>
      </div>
      {onRetry ? (
        <button
          type="button"
          onClick={onRetry}
          className={cn(
            BUTTON_CLASS.outline,
            "h-7 self-start px-2.5 text-crumb max-md:h-11",
          )}
        >
          <RotateCw aria-hidden="true" />
          {retryLabel}
        </button>
      ) : null}
    </div>
  );
}

/** Rows that stand for what is loading. */
export function FormSkeleton({ label }: { label: string }) {
  return (
    <div role="status" aria-label={label} className="flex flex-col gap-2">
      {[0, 1, 2].map((n) => (
        <div
          key={n}
          className="h-11 animate-pulse rounded-[10px] bg-state-hover-strong"
        />
      ))}
    </div>
  );
}

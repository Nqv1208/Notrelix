import { forwardRef, type ButtonHTMLAttributes, type ReactNode } from "react";
import { AlertCircle, Loader2, Lock, RefreshCw } from "lucide-react";
import { cn } from "@notrelix/ui-web";
import type { FieldDefinition } from "@notrelix/work-management-core";
import { fillPopupCopy } from "../card-popup.copy";
import { useCardPopup } from "../card-popup-context";
import { PanelBtn } from "../popup-ui";

/** `.pill`: 30 px high (44 on a phone, for the finger), 10 px padding, 6 px gap, 8 px radius, 13 px text. */
const PILL =
  "relative box-border inline-flex h-[30px] max-w-full items-center gap-1.5 rounded-[8px] border border-line-strong bg-popover px-2.5 text-left text-[13px] whitespace-nowrap text-foreground max-md:h-11";
const FOCUS =
  "focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary";

/** `.btn.sm`: 26 px high on a desktop, the finger-sized 44 on a phone. */
const NOTE_BTN = "md:h-[26px] md:px-2 md:text-[12px]";

export type PillState = "rest" | "saving" | "failed" | "conflict";

/** `.pill`: the button of an editable property; `state` draws its border and dims it while it saves. */
export const PillBtn = forwardRef<
  HTMLButtonElement,
  ButtonHTMLAttributes<HTMLButtonElement> & {
    state?: PillState;
    dashed?: boolean;
  }
>(function PillBtn(
  { className, state = "rest", dashed, type = "button", ...props },
  ref,
) {
  return (
    <button
      ref={ref}
      type={type}
      className={cn(
        PILL,
        "cursor-pointer hover:bg-state-hover-strong",
        dashed && "border-dashed text-brand-ink",
        // Dimmed by the spinner and a lighter border, never by opacity: the text keeps its 4.5:1 contrast.
        state === "saving" && "border-line",
        state === "failed" && "border-danger",
        state === "conflict" && "border-warning-text",
        FOCUS,
        className,
      )}
      {...props}
    />
  );
});

/** `.pill.ro`: a property that cannot be edited here: the quiet fill, no pointer. */
export function PillStatic({
  className,
  children,
  ...props
}: {
  className?: string;
  children: ReactNode;
  title?: string;
  "data-computed-state"?: string;
  tabIndex?: number;
}) {
  return (
    <span
      className={cn(PILL, "cursor-default bg-state-hover", className)}
      {...props}
    >
      {children}
    </span>
  );
}

/** `.k`: the name of the property, before its value. */
export function PillKey({ children }: { children: ReactNode }) {
  return (
    <span className="shrink-0 text-[12px] text-meta-foreground">
      {children}
    </span>
  );
}

/** `.fxg`: the mark of a calculated property. */
export function FxMark() {
  return (
    <span
      aria-hidden="true"
      className="inline-flex h-[18px] flex-none items-center rounded-[5px] bg-state-hover-strong px-[5px] font-mono text-[11px] font-medium text-muted-foreground"
    >
      ƒx
    </span>
  );
}

export function PillSpinner() {
  return (
    <Loader2
      aria-hidden="true"
      className="size-3.5 shrink-0 animate-spin text-brand-ink motion-reduce:animate-none"
    />
  );
}

export function PillLock({ title }: { title: string }) {
  return (
    <span className="inline-flex text-meta-foreground" title={title}>
      <Lock aria-hidden="true" className="size-4" />
    </span>
  );
}

/**
 * The note under a pill whose write failed ("Lỗi lưu · Thử lại trong chỗ") or whose value someone else changed
 * ("Xung đột phiên bản"): a 300 or 320 px panel 6 px under the pill with a coloured border. Nothing is
 * undone by itself; the pill keeps the value that was written.
 */
export function PillNote({ field }: { field: FieldDefinition }) {
  const { copy, saves, fieldConflicts, discardSave } = useCardPopup();
  const t = copy.sections.chips;
  const key = `field:${field.id}`;
  const save = saves[key];
  const clash = fieldConflicts.find((c) => c.key === key);
  if (clash)
    return (
      <div
        role="alertdialog"
        aria-label={fillPopupCopy(t.pillConflict, { name: field.name })}
        className="absolute top-9 left-0 z-10 flex w-80 max-w-[calc(100vw-48px)] flex-col gap-1.5 rounded-[10px] border border-warning-text bg-popover p-2 text-[13px] shadow-popover"
      >
        <b className="text-[12.5px] leading-[1.45]">
          {fillPopupCopy(t.conflictTitle, {
            field: field.name,
            value: clash.theirs,
          })}
        </b>
        <span className="text-[12.5px] leading-[1.45] text-meta-foreground">
          {fillPopupCopy(t.conflictYours, { mine: clash.mine })}
        </span>
        <div className="flex flex-wrap justify-end gap-1.5">
          <PanelBtn small className={NOTE_BTN} onClick={clash.keepMine}>
            {t.conflictMine}
          </PanelBtn>
          <PanelBtn
            small
            variant="primary"
            className={NOTE_BTN}
            onClick={clash.takeTheirs}
          >
            {fillPopupCopy(t.conflictTheirs, { value: clash.theirs })}
          </PanelBtn>
        </div>
      </div>
    );
  if (save?.status !== "error") return null;
  const required = save.reason === "required";
  return (
    <div
      role="alert"
      className="absolute top-9 left-0 z-10 flex w-[300px] max-w-[calc(100vw-48px)] flex-col gap-1.5 rounded-[10px] border border-danger bg-popover p-2 text-[13px] shadow-popover"
    >
      <span className="inline-flex items-center gap-[5px] text-[12px] leading-[1.45] font-semibold text-danger-text">
        <AlertCircle aria-hidden="true" className="size-4" />
        {fillPopupCopy(required ? t.requiredRefused : t.saveFailed, {
          name: field.name,
        })}
      </span>
      <span className="text-[12.5px] leading-[1.45] text-meta-foreground">
        {required ? t.requiredRefusedBody : t.saveFailedBody}
      </span>
      <div className="flex justify-end gap-1.5">
        <PanelBtn
          small
          variant="ghost"
          className={NOTE_BTN}
          onClick={() => discardSave(key)}
        >
          {t.discard}
        </PanelBtn>
        {required ? null : (
          <PanelBtn
            small
            variant="primary"
            className={NOTE_BTN}
            onClick={save.retry}
          >
            <RefreshCw aria-hidden="true" className="size-3.5" />
            {t.retry}
          </PanelBtn>
        )}
      </div>
    </div>
  );
}

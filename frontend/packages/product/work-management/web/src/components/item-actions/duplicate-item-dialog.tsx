import { useEffect, useRef, useState } from "react";
import { Lock, TriangleAlert, X } from "lucide-react";
import {
  Button,
  Checkbox,
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  Input,
  cn,
} from "@notrelix/ui-web";
import { ITEM_TITLE_MAX_LENGTH } from "@notrelix/work-management-core";
import { fillLifecycleCopy } from "../item-lifecycle/item-lifecycle.copy";
import type { ItemActionsCopy } from "./item-actions.copy";

/** What the copy takes along; comments, history and files are never offered. */
export interface DuplicateChoices {
  readonly includeFields: boolean;
  readonly includeChecklists: boolean;
  readonly includeSubItems: boolean;
  readonly includeLabels: boolean;
  readonly includeAssignees: boolean;
}

const ALL: DuplicateChoices = {
  includeFields: true,
  includeChecklists: true,
  includeSubItems: true,
  includeLabels: true,
  includeAssignees: true,
};

/** The parts the person can choose, in the order the dialog lists them. */
const PARTS: readonly {
  readonly key: keyof DuplicateChoices;
  readonly label: keyof ItemActionsCopy;
}[] = [
  { key: "includeFields", label: "dupFields" },
  { key: "includeChecklists", label: "dupChecklists" },
  { key: "includeSubItems", label: "dupSubItems" },
  { key: "includeLabels", label: "dupLabels" },
  { key: "includeAssignees", label: "dupAssignees" },
];

/** What the copy would carry for each part, read from the item; a part without a count shows no note. */
export interface DuplicateSummary {
  readonly fields?: number;
  readonly checklists?: number;
  readonly subItems?: number;
  readonly labels?: number;
  readonly assignees?: readonly string[];
}

// The WM-Actions-Duplicate `.btn`: 30 px (48 px on a phone), 7 px radius, 13 px type.
const buttonClass =
  "h-[30px] rounded-[7px] px-[11px] text-[13px] leading-[1.45] max-sm:h-12 max-sm:w-full max-sm:rounded-[12px] max-sm:text-[15px]";

const rowClass =
  "flex h-11 items-center gap-2.5 rounded-[7px] px-2 text-[13.5px] leading-[1.45] max-sm:h-[52px] max-sm:gap-3 max-sm:rounded-none max-sm:px-5 max-sm:text-[15px]";

function LockedRow({
  label,
  reason,
  phone = true,
}: {
  label: string;
  reason: string;
  /** Whether the row also shows on a phone (only Files does; comments and history are left out). */
  phone?: boolean;
}) {
  return (
    <div
      role="checkbox"
      aria-checked="false"
      aria-disabled="true"
      aria-label={`${label}. ${reason}`}
      className={cn(
        rowClass,
        "text-meta-foreground",
        !phone && "max-sm:hidden",
      )}
    >
      <Lock
        aria-hidden="true"
        className="size-4 shrink-0 max-sm:size-6 max-sm:p-0.5"
      />
      <span className="min-w-0">
        <b className="block font-medium max-sm:font-medium">{label}</b>
        <small className="block text-[11.5px] leading-[1.45] max-sm:text-[13px]">
          {reason}
        </small>
      </span>
    </div>
  );
}

/**
 * Asks what a duplicate takes along: everything the server can copy is ticked
 * by default. Files are locked (they belong to Collaboration and are not
 * copied yet); comments and history are never copied. Once sent, the dialog
 * cannot be dismissed. A failure or a name over the limit replaces the parts
 * with its message; on a phone it is a bottom sheet.
 */
export function DuplicateItemDialog({
  copy,
  defaultName,
  summary,
  open,
  busy,
  failed,
  onCancel,
  onConfirm,
}: {
  copy: ItemActionsCopy;
  defaultName: string;
  summary?: DuplicateSummary;
  open: boolean;
  busy: boolean;
  failed: boolean;
  onCancel: () => void;
  onConfirm: (name: string, choices: DuplicateChoices) => void;
}) {
  const [name, setName] = useState(defaultName);
  const [choices, setChoices] = useState<DuplicateChoices>(ALL);
  const nameRef = useRef<HTMLInputElement>(null);
  useEffect(() => {
    if (open) {
      setName(defaultName);
      setChoices(ALL);
    }
  }, [open, defaultName]);
  const tooLong = name.length > ITEM_TITLE_MAX_LENGTH;
  const empty = name.trim().length === 0;
  const none = PARTS.every((part) => !choices[part.key]);
  const notes: Partial<Record<keyof DuplicateChoices, string>> = {
    includeFields:
      summary?.fields !== undefined
        ? fillLifecycleCopy(copy.dupFieldsNote, { count: summary.fields })
        : undefined,
    includeChecklists:
      summary?.checklists !== undefined
        ? fillLifecycleCopy(copy.dupChecklistsNote, {
            count: summary.checklists,
          })
        : undefined,
    includeSubItems:
      summary?.subItems !== undefined
        ? fillLifecycleCopy(copy.dupSubItemsNote, { count: summary.subItems })
        : undefined,
    includeLabels:
      summary?.labels !== undefined
        ? fillLifecycleCopy(copy.dupLabelsNote, { count: summary.labels })
        : undefined,
    includeAssignees: summary?.assignees?.length
      ? summary.assignees.join(", ")
      : undefined,
  };
  const showParts = busy || (!failed && !tooLong);
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
          "box-border gap-0 overflow-hidden rounded-[14px] border-0 bg-popover p-0 leading-[1.45] shadow-[0_24px_60px_rgba(0,0,0,.28)] ring-0 sm:max-w-130 dark:bg-card dark:shadow-[0_24px_60px_rgba(0,0,0,.6)]",
          "max-sm:top-auto max-sm:bottom-0 max-sm:max-w-full max-sm:translate-y-0 max-sm:rounded-b-none max-sm:rounded-t-[14px] max-sm:pb-6",
        )}
        // The name is selected on open (the close button comes first in the DOM).
        onOpenAutoFocus={(event) => {
          event.preventDefault();
          nameRef.current?.focus();
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
        <DialogHeader className="relative gap-0 pt-5 pr-[52px] pb-1.5 pl-[22px] text-left max-sm:flex max-sm:min-h-13 max-sm:flex-row max-sm:flex-wrap max-sm:items-center max-sm:gap-x-2 max-sm:p-0 max-sm:pr-2 max-sm:pl-5">
          <DialogTitle className="text-[18px] leading-[1.45] font-[650] max-sm:min-w-0 max-sm:flex-1 max-sm:text-[17px]">
            {copy.dupTitle}
          </DialogTitle>
          <DialogDescription className="mt-1.5 text-[13.5px] text-muted-foreground max-sm:sr-only">
            {busy ? copy.dupBusyBody : copy.dupBody}
          </DialogDescription>
          <Button
            type="button"
            variant="ghost"
            size="icon-sm"
            aria-label={copy.close}
            title={copy.close}
            aria-disabled={busy || undefined}
            className={cn(
              "absolute top-3.5 right-3 size-7 rounded-[7px] text-meta-foreground max-sm:static max-sm:size-11 max-sm:rounded-[12px] max-sm:text-foreground",
              busy && "opacity-40",
            )}
            onClick={busy ? undefined : onCancel}
          >
            <X aria-hidden="true" className="size-4" strokeWidth={1.8} />
          </Button>
        </DialogHeader>
        <div className="flex flex-col px-[22px] pt-2.5 max-sm:px-5 max-sm:pt-0">
          {busy ? null : (
            <>
              <label
                className="mb-2.5 text-xs leading-[1.45] font-semibold text-meta-foreground max-sm:mb-1.5"
                htmlFor="dup-name"
              >
                {copy.dupName}
              </label>
              <div className="relative">
                <Input
                  id="dup-name"
                  value={name}
                  ref={nameRef}
                  aria-invalid={tooLong}
                  onFocus={(event) => event.currentTarget.select()}
                  onChange={(event) => setName(event.target.value)}
                  onKeyDown={(event) => {
                    if (event.key === "Enter" && !tooLong && !empty) {
                      onConfirm(name.trim(), choices);
                    }
                  }}
                  className="h-[34px] rounded-[8px] pr-14 text-[13.5px] md:text-[13.5px] max-sm:h-11"
                />
                <span
                  className={cn(
                    "pointer-events-none absolute top-1/2 right-2.5 -translate-y-1/2 text-xs leading-[1.45]",
                    tooLong ? "text-danger-text" : "text-meta-foreground",
                  )}
                >
                  {fillLifecycleCopy(copy.dupNameCount, { count: name.length })}
                </span>
              </div>
            </>
          )}
          {showParts ? (
            <div
              role="group"
              aria-label={copy.dupParts}
              className={cn(
                "flex flex-col rounded-[10px] border border-line p-1 max-sm:-mx-5 max-sm:border-0 max-sm:p-0",
                busy ? "" : "mt-2.5 max-sm:mt-2",
              )}
            >
              {PARTS.map((part) => (
                <label
                  key={part.key}
                  className={cn(
                    rowClass,
                    "cursor-pointer hover:bg-state-hover-strong max-sm:hover:bg-transparent",
                  )}
                >
                  <Checkbox
                    checked={choices[part.key]}
                    disabled={busy}
                    onCheckedChange={(next) =>
                      setChoices((current) => ({
                        ...current,
                        [part.key]: next === true,
                      }))
                    }
                  />
                  <span className="min-w-0">
                    <b className="block font-normal max-sm:font-semibold">
                      {copy[part.label]}
                    </b>
                    {notes[part.key] ? (
                      <small className="block text-[11.5px] leading-[1.45] text-meta-foreground max-sm:hidden">
                        {notes[part.key]}
                      </small>
                    ) : null}
                  </span>
                </label>
              ))}
              <LockedRow label={copy.dupFiles} reason={copy.dupFilesLocked} />
              <LockedRow
                label={copy.dupComments}
                reason={copy.dupNeverCopied}
                phone={false}
              />
              <LockedRow
                label={copy.dupHistory}
                reason={copy.dupNeverCopied}
                phone={false}
              />
            </div>
          ) : null}
          {failed && !busy ? (
            <p
              role="alert"
              className="mt-2.5 flex items-start gap-2 rounded-[8px] bg-danger-subtle px-2.5 py-[9px] text-[12.5px] leading-[1.45] text-danger-text"
            >
              <TriangleAlert
                aria-hidden="true"
                className="mt-0.5 size-3.5 shrink-0"
              />
              <span>{copy.dupFailed}</span>
            </p>
          ) : null}
          {tooLong && !busy ? (
            <p
              role="alert"
              className="mt-2.5 flex items-start gap-2 rounded-[8px] bg-warning-subtle px-2.5 py-[9px] text-[12.5px] leading-[1.45] text-warning-text"
            >
              <TriangleAlert
                aria-hidden="true"
                className="mt-0.5 size-3.5 shrink-0"
              />
              <span>
                {fillLifecycleCopy(copy.dupNameTooLong, {
                  over: name.length - ITEM_TITLE_MAX_LENGTH,
                })}
              </span>
            </p>
          ) : null}
        </div>
        <DialogFooter className="flex-row items-center gap-2 px-[22px] pt-4 pb-[18px] max-sm:flex-col-reverse max-sm:items-stretch max-sm:px-4 max-sm:pt-2 max-sm:pb-0">
          <span className="min-w-35 flex-1 text-xs leading-[1.45] text-meta-foreground max-sm:hidden">
            {busy || failed || tooLong
              ? ""
              : none
                ? copy.dupHintNone
                : copy.dupHint}
          </span>
          {busy ? null : (
            <Button
              type="button"
              variant="outline"
              className={cn(
                buttonClass,
                "border-line-strong bg-popover font-medium shadow-none dark:border-line-strong dark:bg-card",
              )}
              onClick={onCancel}
            >
              {copy.cancel}
            </Button>
          )}
          <Button
            type="button"
            disabled={busy || tooLong || empty}
            className={cn(buttonClass, "font-semibold")}
            onClick={() => onConfirm(name.trim(), choices)}
          >
            {busy
              ? copy.dupBusy
              : failed
                ? copy.retry
                : none
                  ? copy.dupConfirmNone
                  : copy.dupConfirm}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}

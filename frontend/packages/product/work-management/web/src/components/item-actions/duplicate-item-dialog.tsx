import { useEffect, useState } from "react";
import { Lock, TriangleAlert } from "lucide-react";
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
import { fillLifecycleCopy } from "../item-lifecycle/item-lifecycle.copy";
import type { ItemActionsCopy } from "./item-actions.copy";

export const DUPLICATE_NAME_MAX = 200;

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

function LockedRow({ label, reason }: { label: string; reason: string }) {
  return (
    <div
      role="checkbox"
      aria-checked="false"
      aria-disabled="true"
      aria-label={`${label}. ${reason}`}
      className="flex items-center gap-2.5 px-2 py-1.5 text-compact text-muted-foreground"
    >
      <Lock aria-hidden="true" className="size-4 shrink-0" />
      <span>
        {label}
        <small className="block text-metadata">{reason}</small>
      </span>
    </div>
  );
}

/**
 * Asks what a duplicate takes along: everything the server can copy is ticked
 * by default. Files are locked (they belong to Collaboration and are not
 * copied yet); comments and history are never copied. Once sent, the dialog
 * cannot be dismissed.
 */
export function DuplicateItemDialog({
  copy,
  defaultName,
  open,
  busy,
  failed,
  onCancel,
  onConfirm,
}: {
  copy: ItemActionsCopy;
  defaultName: string;
  open: boolean;
  busy: boolean;
  failed: boolean;
  onCancel: () => void;
  onConfirm: (name: string, choices: DuplicateChoices) => void;
}) {
  const [name, setName] = useState(defaultName);
  const [choices, setChoices] = useState<DuplicateChoices>(ALL);
  useEffect(() => {
    if (open) {
      setName(defaultName);
      setChoices(ALL);
    }
  }, [open, defaultName]);
  const tooLong = name.length > DUPLICATE_NAME_MAX;
  const empty = name.trim().length === 0;
  return (
    <Dialog
      open={open}
      onOpenChange={(next) => {
        if (!next && !busy) onCancel();
      }}
    >
      <DialogContent
        className="sm:max-w-[520px]"
        showCloseButton={!busy}
        onEscapeKeyDown={(event) => {
          if (busy) event.preventDefault();
        }}
        onInteractOutside={(event) => {
          if (busy) event.preventDefault();
        }}
      >
        <DialogHeader>
          <DialogTitle>{copy.dupTitle}</DialogTitle>
          <DialogDescription>{copy.dupBody}</DialogDescription>
        </DialogHeader>
        <label
          className="text-metadata font-semibold text-muted-foreground"
          htmlFor="dup-name"
        >
          {copy.dupName}
        </label>
        <div className="relative">
          <Input
            id="dup-name"
            value={name}
            autoFocus
            aria-invalid={tooLong}
            disabled={busy}
            onFocus={(event) => event.currentTarget.select()}
            onChange={(event) => setName(event.target.value)}
            onKeyDown={(event) => {
              if (event.key === "Enter" && !tooLong && !empty && !busy) {
                onConfirm(name.trim(), choices);
              }
            }}
          />
          <span
            className={cn(
              "pointer-events-none absolute top-1/2 right-2.5 -translate-y-1/2 text-metadata",
              tooLong ? "text-destructive" : "text-muted-foreground",
            )}
          >
            {fillLifecycleCopy(copy.dupNameCount, { count: name.length })}
          </span>
        </div>
        {tooLong ? (
          <p role="alert" className="text-metadata text-destructive">
            {copy.dupNameTooLong}
          </p>
        ) : null}
        <div
          role="group"
          aria-label={copy.dupParts}
          className="rounded-[10px] border border-line p-1"
        >
          {PARTS.map((part) => (
            <label
              key={part.key}
              className="flex cursor-pointer items-center gap-2.5 rounded-md px-2 py-1.5 text-compact hover:bg-state-hover-strong"
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
              {copy[part.label]}
            </label>
          ))}
          <LockedRow label={copy.dupFiles} reason={copy.dupFilesLocked} />
          <LockedRow label={copy.dupComments} reason={copy.dupNeverCopied} />
          <LockedRow label={copy.dupHistory} reason={copy.dupNeverCopied} />
        </div>
        {failed ? (
          <p
            role="alert"
            className="flex items-start gap-2 rounded-lg bg-destructive/10 px-2.5 py-2 text-compact text-destructive"
          >
            <TriangleAlert
              aria-hidden="true"
              className="mt-0.5 size-3.5 shrink-0"
            />
            {copy.dupFailed}
          </p>
        ) : null}
        <DialogFooter>
          {!busy ? (
            <Button type="button" variant="ghost" onClick={onCancel}>
              {copy.cancel}
            </Button>
          ) : null}
          <Button
            type="button"
            disabled={busy || tooLong || empty}
            onClick={() => onConfirm(name.trim(), choices)}
          >
            {busy ? copy.dupBusy : failed ? copy.retry : copy.dupConfirm}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}

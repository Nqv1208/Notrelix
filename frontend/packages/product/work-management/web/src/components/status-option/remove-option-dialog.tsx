import { useId, useState } from "react";
import {
  AlertDialog,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
  Button,
  RadioGroup,
  RadioGroupItem,
  cn,
} from "@notrelix/ui-web";
import { fillCopy, pluralCopy } from "../views/kanban/kanban-card.copy";
import type { StatusOptionFormCopy } from "./status-option-form.copy";

/** Where the items of the deleted option can go. */
export interface RemoveOptionChoice {
  readonly id: string;
  readonly name: string;
  /** The option's stored colour, drawn as a dot. */
  readonly color?: string | null;
}

export interface RemoveOptionDialogProps {
  copy: StatusOptionFormCopy;
  /** A status of the Status field (items must move) or a value of another Select field (items may lose the value). */
  subject: "status" | "value";
  name: string;
  /** How many items hold the option. */
  itemCount: number;
  /** The other options of the field. */
  choices: readonly RemoveOptionChoice[];
  /** The delete is in flight: the dialog keeps its choice and cannot be sent twice. */
  pending?: boolean;
  onCancel: () => void;
  /** The chosen replacement; null when the items lose the value. */
  onConfirm: (replacementOptionId: string | null) => void;
}

const NONE = "";

/**
 * Asks before a status (or select value) is deleted. With items in it the
 * person chooses where they go: a status always needs one of the other
 * statuses (the first is preselected), another Select value may also leave
 * them without a value. Without items it is a plain confirmation. Pure: the
 * container sends the delete and closes the dialog.
 */
export function RemoveOptionDialog({
  copy,
  subject,
  name,
  itemCount,
  choices,
  pending = false,
  onCancel,
  onConfirm,
}: RemoveOptionDialogProps) {
  const forValue = subject === "value";
  const needsChoice = itemCount > 0;
  const [chosen, setChosen] = useState<string>(
    forValue ? NONE : (choices[0]?.id ?? NONE),
  );
  const ids = useId();
  const canConfirm = !needsChoice || forValue || chosen !== NONE;

  return (
    <AlertDialog
      open
      onOpenChange={(next) => {
        if (!next && !pending) onCancel();
      }}
    >
      <AlertDialogContent data-remove-option-dialog="">
        <AlertDialogHeader>
          <AlertDialogTitle>
            {fillCopy(forValue ? copy.removeValueTitle : copy.removeTitle, {
              name,
            })}
          </AlertDialogTitle>
          <AlertDialogDescription>
            {needsChoice
              ? pluralCopy(
                  {
                    one: fillCopy(copy.removeItemsBody_one, { name }),
                    other: fillCopy(copy.removeItemsBody_other, { name }),
                  },
                  itemCount,
                )
              : copy.removeEmptyBody}
          </AlertDialogDescription>
        </AlertDialogHeader>

        {needsChoice ? (
          <div className="flex flex-col gap-1.5">
            <span
              id={`${ids}-target`}
              className="text-metadata font-medium text-meta-foreground"
            >
              {copy.removeReplacementLabel}
            </span>
            <RadioGroup
              aria-labelledby={`${ids}-target`}
              value={chosen}
              onValueChange={setChosen}
              className="flex max-h-56 w-full flex-col gap-1 overflow-y-auto"
            >
              {forValue ? (
                <ChoiceRow
                  value={NONE}
                  label={copy.removeNoValue}
                  selected={chosen === NONE}
                />
              ) : null}
              {choices.map((choice) => (
                <ChoiceRow
                  key={choice.id}
                  value={choice.id}
                  label={choice.name}
                  color={choice.color}
                  selected={chosen === choice.id}
                />
              ))}
            </RadioGroup>
          </div>
        ) : null}

        <AlertDialogFooter>
          <Button
            type="button"
            variant="ghost"
            disabled={pending}
            onClick={onCancel}
          >
            {copy.cancel}
          </Button>
          <Button
            type="button"
            variant="destructive"
            disabled={pending || !canConfirm}
            onClick={() =>
              onConfirm(needsChoice && chosen !== NONE ? chosen : null)
            }
          >
            {pending
              ? copy.removing
              : needsChoice
                ? copy.removeMoveConfirm
                : forValue
                  ? copy.removeValueConfirm
                  : copy.removeConfirm}
          </Button>
        </AlertDialogFooter>
      </AlertDialogContent>
    </AlertDialog>
  );
}

function ChoiceRow({
  value,
  label,
  color,
  selected,
}: {
  value: string;
  label: string;
  color?: string | null;
  selected: boolean;
}) {
  return (
    <label
      className={cn(
        "flex h-8 cursor-pointer items-center gap-2 rounded-md px-2 text-label text-foreground hover:bg-state-hover",
        selected && "bg-state-hover",
      )}
    >
      <RadioGroupItem value={value} />
      {color ? (
        <span
          aria-hidden="true"
          className="size-2.5 shrink-0 rounded-full"
          style={{ backgroundColor: color }}
        />
      ) : null}
      <span className="min-w-0 truncate">{label}</span>
    </label>
  );
}

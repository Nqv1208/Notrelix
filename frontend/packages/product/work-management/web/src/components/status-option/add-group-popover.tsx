import { useState } from "react";
import { Plus } from "lucide-react";
import { Popover, PopoverContent, PopoverTrigger, cn } from "@notrelix/ui-web";
import { StatusOptionForm } from "./status-option-form";
import {
  groupFormCopy,
  type StatusOptionFormCopy,
} from "./status-option-form.copy";

/** What the form hands back for a new board group. */
export interface AddGroupDraft {
  readonly title: string;
  /** A palette value (the stored colour). */
  readonly color: string;
}

export interface AddGroupPopoverProps {
  copy: StatusOptionFormCopy;
  /** The trigger's label: "Add group". */
  label: string;
  /** Titles the board's groups already carry. */
  existingTitles: readonly string[];
  /** The create is in flight. */
  pending?: boolean;
  /** The trigger's look (a table row, a dashed lane slot). */
  triggerClassName?: string;
  /**
   * Creates the group. The form stays open while it runs and closes when it
   * resolves; a rejection keeps the form and its values for another try.
   */
  onSubmit: (draft: AddGroupDraft) => Promise<void> | void;
}

/**
 * "Add group": names a group of the board (it holds items) through the same
 * form as a status, worded for groups, so it is never mistaken for "Add
 * status" (which adds a column or table group to the status field). The
 * trigger is a plain button; the form opens in a popover under it.
 */
export function AddGroupPopover({
  copy,
  label,
  existingTitles,
  pending = false,
  triggerClassName,
  onSubmit,
}: AddGroupPopoverProps) {
  const [open, setOpen] = useState(false);
  const [submitting, setSubmitting] = useState(false);
  const form = groupFormCopy(copy);

  const submit = async (name: string, color: string) => {
    setSubmitting(true);
    try {
      await onSubmit({ title: name, color });
      setOpen(false);
    } catch {
      // The container reports the failure; the form keeps its values.
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <Popover open={open} onOpenChange={setOpen}>
      <PopoverTrigger asChild>
        <button
          type="button"
          data-add-board-group=""
          className={cn(
            "focus-visible:outline-2 focus-visible:outline-ring",
            triggerClassName,
          )}
        >
          <Plus className="size-3.5" aria-hidden="true" />
          {label}
        </button>
      </PopoverTrigger>
      <PopoverContent
        align="start"
        side="bottom"
        sideOffset={4}
        aria-label={form.titleCreate}
        className="w-72 gap-2 rounded-[10px] border border-line-strong bg-popover p-3 shadow-pop ring-0 dark:bg-card"
      >
        <StatusOptionForm
          copy={form}
          existingNames={existingTitles}
          showCategory={false}
          pending={pending || submitting}
          onSubmit={(draft) => void submit(draft.name, draft.color)}
          onCancel={() => setOpen(false)}
        />
      </PopoverContent>
    </Popover>
  );
}

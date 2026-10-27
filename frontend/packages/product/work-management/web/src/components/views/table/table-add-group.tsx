import { useState } from "react";
import { Plus } from "lucide-react";
import { Popover, PopoverContent, PopoverTrigger } from "@notrelix/ui-web";
import {
  AddGroupPopover,
  type AddGroupDraft,
} from "../../status-option/add-group-popover";
import {
  StatusOptionForm,
  type StatusOptionDraft,
} from "../../status-option/status-option-form";
import type { StatusOptionFormCopy } from "../../status-option/status-option-form.copy";
import type { MainTableCopy } from "./main-table.copy";

export interface TableAddGroupProps {
  copy: MainTableCopy;
  formCopy: StatusOptionFormCopy;
  /**
   * "status" when the table is grouped by the Status field (the form asks
   * for the status group too), "value" for any other Select field.
   */
  kind: "status" | "value";
  /** Names the grouped field already has (hidden and empty groups included). */
  existingNames: readonly string[];
  /** The option write is in flight. */
  pending?: boolean;
  /**
   * Creates the option. The form stays open while it runs and closes when it
   * resolves; a rejection keeps the form (and its values) for another try.
   */
  onSubmit: (draft: StatusOptionDraft) => Promise<void> | void;
  /**
   * "Add group": creates a group of the board (the container of items), beside
   * and distinct from the status add. Absent when the viewer may not add one.
   */
  boardGroup?: {
    /** Titles the board's groups already carry. */
    existingTitles: readonly string[];
    pending?: boolean;
    onSubmit: (draft: AddGroupDraft) => Promise<void> | void;
  };
}

const ROW_BUTTON_CLASS =
  "flex h-9 cursor-pointer items-center gap-1.5 border-0 bg-transparent px-3.5 text-left text-compact leading-[1.45] text-meta-foreground transition-colors hover:bg-state-hover hover:text-foreground focus-visible:outline-2 focus-visible:-outline-offset-2 focus-visible:outline-ring";

/**
 * Under the last group, two quiet entries: "Thêm trạng thái" (or "Thêm giá
 * trị" for another Select field) adds an option of the grouped field and
 * appears as an empty group at the end of the table; "Thêm nhóm" adds a group
 * of the board, which holds items and is chosen in an item's Group field. The
 * two never share a label.
 */
export function TableAddGroup({
  copy,
  formCopy,
  kind,
  existingNames,
  pending = false,
  onSubmit,
  boardGroup,
}: TableAddGroupProps) {
  const [open, setOpen] = useState(false);
  const [submitting, setSubmitting] = useState(false);
  const g = copy.group;
  const label = kind === "status" ? g.addStatus : g.addValue;

  const submit = async (draft: StatusOptionDraft) => {
    setSubmitting(true);
    try {
      await onSubmit(draft);
      setOpen(false);
    } catch {
      // The container reports the failure; the form keeps its values.
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <div role="row" className="flex min-w-full items-center">
      <div role="cell" className="flex min-w-0 items-center">
        <Popover open={open} onOpenChange={setOpen}>
          <PopoverTrigger asChild>
            <button
              type="button"
              data-add-group=""
              className={ROW_BUTTON_CLASS}
            >
              <Plus className="size-3.5" aria-hidden="true" />
              {label}
            </button>
          </PopoverTrigger>
          <PopoverContent
            align="start"
            side="bottom"
            sideOffset={4}
            aria-label={
              kind === "status"
                ? formCopy.titleCreate
                : formCopy.valueTitleCreate
            }
            className="w-72 gap-2 rounded-[10px] border border-line-strong bg-popover p-3 shadow-pop ring-0 dark:bg-card"
          >
            <StatusOptionForm
              copy={formCopy}
              subject={kind}
              existingNames={existingNames}
              pending={pending || submitting}
              onSubmit={(draft) => void submit(draft)}
              onCancel={() => setOpen(false)}
            />
          </PopoverContent>
        </Popover>
        {boardGroup ? (
          <AddGroupPopover
            copy={formCopy}
            label={g.addGroup}
            existingTitles={boardGroup.existingTitles}
            pending={boardGroup.pending}
            onSubmit={boardGroup.onSubmit}
            triggerClassName={ROW_BUTTON_CLASS}
          />
        ) : null}
      </div>
    </div>
  );
}

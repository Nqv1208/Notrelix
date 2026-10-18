import { useState } from "react";
import { Plus } from "lucide-react";
import { Popover, PopoverContent, PopoverTrigger } from "@notrelix/ui-web";
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
   * for the status group too), "group" for any other Select field.
   */
  kind: "status" | "group";
  /** Names the grouped field already has (hidden and empty groups included). */
  existingNames: readonly string[];
  /** The option write is in flight. */
  pending?: boolean;
  /**
   * Creates the option. The form stays open while it runs and closes when it
   * resolves; a rejection keeps the form (and its values) for another try.
   */
  onSubmit: (draft: StatusOptionDraft) => Promise<void> | void;
}

/**
 * "Thêm nhóm" / "Thêm trạng thái": a quiet full-width row under the last
 * group. It opens the status option form (name, colour, and for a Status
 * field the group); a new group is a new option of the grouped field, so it
 * appears as an empty group at the end of the table.
 */
export function TableAddGroup({
  copy,
  formCopy,
  kind,
  existingNames,
  pending = false,
  onSubmit,
}: TableAddGroupProps) {
  const [open, setOpen] = useState(false);
  const [submitting, setSubmitting] = useState(false);
  const g = copy.group;
  const label = kind === "status" ? g.addStatus : g.addGroup;
  const form: StatusOptionFormCopy =
    kind === "status"
      ? formCopy
      : {
          ...formCopy,
          titleCreate: g.groupForm.title,
          nameLabel: g.groupForm.nameLabel,
          namePlaceholder: g.groupForm.namePlaceholder,
          submitCreate: g.groupForm.submit,
          errorEmpty: g.groupForm.errorEmpty,
          errorDuplicate: g.groupForm.errorDuplicate,
        };

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
    <div role="row" className="grid min-w-full">
      <div role="cell" className="min-w-0">
        <Popover open={open} onOpenChange={setOpen}>
          <PopoverTrigger asChild>
            <button
              type="button"
              data-add-group=""
              className="flex h-9 w-full cursor-pointer items-center gap-1.5 border-0 bg-transparent pl-3.5 text-left text-compact leading-[1.45] text-meta-foreground transition-colors hover:bg-state-hover hover:text-foreground focus-visible:outline-2 focus-visible:-outline-offset-2 focus-visible:outline-ring"
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
              existingNames={existingNames}
              showCategory={kind === "status"}
              pending={pending || submitting}
              onSubmit={(draft) => void submit(draft)}
              onCancel={() => setOpen(false)}
            />
          </PopoverContent>
        </Popover>
      </div>
    </div>
  );
}

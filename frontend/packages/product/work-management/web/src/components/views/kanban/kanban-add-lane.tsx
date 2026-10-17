import { useState } from "react";
import { Plus } from "lucide-react";
import { Popover, PopoverContent, PopoverTrigger, cn } from "@notrelix/ui-web";
import {
  StatusOptionForm,
  type StatusOptionDraft,
} from "../../status-option/status-option-form";
import type { StatusOptionFormCopy } from "../../status-option/status-option-form.copy";
import { kanbanColumnWidthClass } from "./kanban-layout";
import type { KanbanLaneCopy } from "./kanban-lane.copy";

export interface KanbanAddLaneProps {
  copy: KanbanLaneCopy;
  formCopy: StatusOptionFormCopy;
  /** Status adds a status (with its group); another Select field adds a value. */
  subject: "status" | "value";
  /** The names the lanes already carry: a new one may not repeat them. */
  existingNames: readonly string[];
  /** A create is in flight. */
  pending?: boolean;
  onCreate: (draft: StatusOptionDraft) => void;
}

/**
 * The slot after the last lane: "+ Thêm trạng thái". It is as wide as a lane
 * and has the lane's radius, drawn as a dashed outline so it reads as room
 * for one more lane. Clicking opens the status form in a popover; Enter adds
 * the status as the last lane and closes it, Escape closes it without adding.
 */
export function KanbanAddLane({
  copy,
  formCopy,
  subject,
  existingNames,
  pending = false,
  onCreate,
}: KanbanAddLaneProps) {
  const [open, setOpen] = useState(false);
  return (
    <Popover open={open} onOpenChange={setOpen}>
      <PopoverTrigger asChild>
        <button
          type="button"
          data-kanban-add-lane=""
          className={cn(
            "flex h-11.5 shrink-0 items-center gap-1.5 rounded-xl border border-dashed border-line-strong px-4 text-compact font-medium text-muted-foreground transition-colors hover:border-transparent hover:bg-state-hover hover:text-foreground focus-visible:outline-hidden focus-visible:ring-2 focus-visible:ring-focus-ring aria-expanded:bg-state-hover aria-expanded:text-foreground",
            kanbanColumnWidthClass,
          )}
        >
          <Plus aria-hidden="true" className="size-4" />
          {subject === "status" ? copy.addStatus : copy.addValue}
        </button>
      </PopoverTrigger>
      <PopoverContent align="start" className="w-72 gap-0">
        <StatusOptionForm
          copy={formCopy}
          subject={subject}
          existingNames={existingNames}
          pending={pending}
          onCancel={() => setOpen(false)}
          onSubmit={(draft) => {
            onCreate(draft);
            setOpen(false);
          }}
        />
      </PopoverContent>
    </Popover>
  );
}

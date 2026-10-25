import { useState } from "react";
import { Table2 } from "lucide-react";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogTitle,
} from "@notrelix/ui-web";
import type { BoardSettingsFieldDto } from "@notrelix/work-management-core";
import { useSettings } from "./board-settings-context";
import { fillCopy, plural } from "./board-settings-format";
import { BUTTON_CLASS } from "./board-settings-parts";

/**
 * The confirmation before a board Manager deletes a field for good: the field
 * and the values items hold in it go, so the dialog says how many values that is.
 */
export function FieldDeleteDialog({
  field,
  onClose,
  onDeleted,
}: {
  field: BoardSettingsFieldDto;
  onClose: () => void;
  onDeleted: () => void;
}) {
  const { copy, actions, run } = useSettings();
  const t = copy.deleteDialog;
  const [working, setWorking] = useState(false);

  async function confirm() {
    setWorking(true);
    const result = await run(actions.deleteField(field.id));
    setWorking(false);
    if (result.ok) onDeleted();
  }

  return (
    <Dialog open onOpenChange={(open) => (open ? undefined : onClose())}>
      <DialogContent
        role="alertdialog"
        showCloseButton={false}
        overlayClassName="bg-modal-scrim backdrop-blur-none"
        className="top-[150px] box-border flex w-[480px] max-w-[calc(100%-2rem)] translate-y-0 flex-col gap-0 overflow-hidden rounded-[14px] border-0 bg-popover p-0 text-body leading-[1.45] font-normal shadow-dialog ring-0 sm:max-w-[calc(100%-2rem)] dark:bg-card"
      >
        <div className="px-[22px] pt-5 pb-1.5">
          <DialogTitle className="m-0 text-[18px] leading-[1.45] font-[650] text-foreground">
            {fillCopy(t.title, { name: field.name })}
          </DialogTitle>
          <DialogDescription className="mt-1.5 mb-0 text-item-ui text-muted-foreground">
            {t.body}
          </DialogDescription>
        </div>
        {field.valueCount > 0 ? (
          <div className="mx-[22px] mt-3 flex items-center gap-[11px] rounded-[10px] border border-line px-3.5 py-[11px] text-item-small">
            <span
              aria-hidden="true"
              className="grid size-7 shrink-0 place-items-center rounded-[7px] border border-transparent bg-warning-subtle text-warning-text"
            >
              <Table2 className="size-3.5" />
            </span>
            <b className="font-semibold">
              {plural(t.values_one, t.values_other, field.valueCount)}
            </b>
          </div>
        ) : null}
        <div className="flex items-center justify-end gap-2 px-[22px] pt-4 pb-[18px]">
          <button
            type="button"
            onClick={onClose}
            className={BUTTON_CLASS.outline}
          >
            {t.cancel}
          </button>
          <button
            type="button"
            disabled={working}
            onClick={() => void confirm()}
            className={BUTTON_CLASS.danger}
          >
            {working ? t.working : t.confirm}
          </button>
        </div>
      </DialogContent>
    </Dialog>
  );
}

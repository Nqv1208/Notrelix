import { useState } from "react";
import { Eye, Table2, Zap } from "lucide-react";
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
 * The impact of archiving a field (mockup `.dlg`): what keeps its value, what
 * loses a column, what turns "needs a fix" and what pauses. Nothing is deleted.
 */
export function FieldArchiveDialog({
  field,
  onClose,
  onArchived,
}: {
  field: BoardSettingsFieldDto;
  onClose: () => void;
  onArchived: () => void;
}) {
  const { copy, actions, run } = useSettings();
  const t = copy.archiveDialog;
  const [working, setWorking] = useState(false);

  const views = field.usedIn.filter((usage) => usage.kind !== "rule");
  const display = [
    ...new Set(
      views
        .filter((usage) => usage.kind === "view" || usage.kind === "card")
        .map((usage) => usage.name),
    ),
  ];
  const filters = [
    ...new Set(
      views
        .filter((usage) => usage.kind === "filter")
        .map((usage) => usage.name),
    ),
  ];
  const viewCount = new Set(views.map((usage) => usage.name)).size;
  const rules = [
    ...new Set(
      field.usedIn.filter((usage) => usage.kind === "rule").map((u) => u.name),
    ),
  ];

  async function confirm() {
    setWorking(true);
    const result = await run(() => actions.archiveField(field.id));
    setWorking(false);
    if (result.ok) onArchived();
  }

  return (
    <Dialog open onOpenChange={(open) => (open ? undefined : onClose())}>
      <DialogContent
        role="alertdialog"
        showCloseButton={false}
        overlayClassName="bg-modal-scrim backdrop-blur-none"
        className="top-[150px] box-border flex w-[540px] max-w-[calc(100%-2rem)] translate-y-0 flex-col gap-0 overflow-hidden rounded-[14px] border-0 bg-popover p-0 text-body leading-[1.45] font-normal shadow-dialog ring-0 sm:max-w-[calc(100%-2rem)] dark:bg-card"
      >
        <div className="px-[22px] pt-5 pb-1.5">
          <DialogTitle className="m-0 text-[18px] leading-[1.45] font-[650] text-foreground">
            {fillCopy(t.title, { name: field.name })}
          </DialogTitle>
          <DialogDescription className="mt-1.5 mb-0 text-item-ui text-muted-foreground">
            {t.body}
          </DialogDescription>
        </div>
        <div className="mx-[22px] mt-3 rounded-[10px] border border-line">
          <ImpactRow icon={Table2}>
            <b className="block font-semibold">
              {plural(t.values_one, t.values_other, field.valueCount)}
            </b>
            <small className="block text-[12.5px] leading-[1.45] text-meta-foreground">
              {t.valuesBody}
            </small>
          </ImpactRow>
          {viewCount > 0 ? (
            <ImpactRow icon={Eye} warn>
              <b className="block font-semibold">
                {plural(t.views_one, t.views_other, viewCount)}
              </b>
              <small className="block text-[12.5px] leading-[1.45] text-meta-foreground">
                {[
                  display.length > 0
                    ? fillCopy(t.viewsDisplay, { names: display.join(t.and) })
                    : "",
                  filters.length > 0
                    ? fillCopy(t.viewsFilter, { names: filters.join(", ") })
                    : "",
                ]
                  .filter(Boolean)
                  .join(" ")}
              </small>
            </ImpactRow>
          ) : null}
          {rules.length > 0 ? (
            <ImpactRow icon={Zap} warn>
              <b className="block font-semibold">
                {plural(t.rules_one, t.rules_other, rules.length)}
              </b>
              <small className="block text-[12.5px] leading-[1.45] text-meta-foreground">
                {fillCopy(t.rulesBody, { names: rules.join(", ") })}
              </small>
            </ImpactRow>
          ) : null}
        </div>
        <div className="flex items-center gap-2 px-[22px] pt-4 pb-[18px]">
          <span className="flex-1 text-metadata text-meta-foreground">
            {t.notify}
          </span>
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

function ImpactRow({
  icon: Icon,
  warn,
  children,
}: {
  icon: typeof Eye;
  warn?: boolean;
  children: React.ReactNode;
}) {
  return (
    <div className="flex gap-[11px] border-t border-line px-3.5 py-[11px] text-item-small first:border-t-0">
      <span
        aria-hidden="true"
        className={
          warn
            ? "grid size-7 shrink-0 place-items-center rounded-[7px] border border-transparent bg-warning-subtle text-warning-text"
            : "grid size-7 shrink-0 place-items-center rounded-[7px] border border-line bg-background text-muted-foreground"
        }
      >
        <Icon className="size-3.5" />
      </span>
      <span>{children}</span>
    </div>
  );
}

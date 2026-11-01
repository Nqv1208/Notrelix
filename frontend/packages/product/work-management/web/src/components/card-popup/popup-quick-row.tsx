import type { ReactNode } from "react";
import { Clock, ListChecks, Tag, UserPlus } from "lucide-react";
import { PopoverTrigger } from "@notrelix/ui-web";
import { useCardPopup, useCanEdit } from "./card-popup-context";
import { PanelPopover, QuickBtn } from "./popup-ui";
import { LabelsPanel } from "./pickers/labels-panel";
import { DatesPanel } from "./pickers/dates-panel";
import { PeoplePanel } from "./pickers/people-panel";
import { ChecklistPanel } from "./pickers/checklist-panel";
import { AddMenu } from "./add-menu";

/** One quick button that owns the picker anchored under it. */
function PickerButton({
  id,
  icon,
  label,
  width,
  children,
}: {
  id: "labels" | "dates" | "checklist" | "assignees";
  icon: ReactNode;
  label: string;
  width: number;
  children: (close: () => void) => ReactNode;
}) {
  const { picker, pickerAnchor, openPicker } = useCardPopup();
  const open = picker === id;
  return (
    <PanelPopover
      open={open}
      anchor={pickerAnchor}
      onOpenChange={(next) =>
        openPicker(next ? id : null, next ? null : undefined)
      }
      width={width}
      trigger={
        <PopoverTrigger asChild>
          <QuickBtn on={open} title={label} aria-haspopup="dialog">
            {icon}
            {label}
          </QuickBtn>
        </PopoverTrigger>
      }
    >
      {children(() => openPicker(null))}
    </PanelPopover>
  );
}

/** `.q-row`: the quick buttons under the title. Hidden when the card is read-only. */
export function PopupQuickRow() {
  const { copy, openPicker } = useCardPopup();
  const t = copy.shell;
  const canEdit = useCanEdit();
  if (!canEdit) return null;
  const close = () => openPicker(null);
  return (
    <div className="box-border flex flex-wrap gap-2 pt-2 pr-4 pl-4 md:h-11 md:pt-1 md:flex-nowrap md:pr-6 md:pl-[60px]">
      <AddMenu />
      <PickerButton
        id="labels"
        icon={<Tag aria-hidden="true" className="size-4" />}
        label={t.labels}
        width={328}
      >
        {() => <LabelsPanel onClose={close} />}
      </PickerButton>
      <PickerButton
        id="dates"
        icon={<Clock aria-hidden="true" className="size-4" />}
        label={t.dates}
        width={344}
      >
        {() => <DatesPanel onClose={close} />}
      </PickerButton>
      <PickerButton
        id="checklist"
        icon={<ListChecks aria-hidden="true" className="size-4" />}
        label={t.checklist}
        width={328}
      >
        {() => <ChecklistPanel onClose={close} />}
      </PickerButton>
      <PickerButton
        id="assignees"
        icon={<UserPlus aria-hidden="true" className="size-4" />}
        label={t.assignees}
        width={312}
      >
        {() => <PeoplePanel onClose={close} />}
      </PickerButton>
    </div>
  );
}

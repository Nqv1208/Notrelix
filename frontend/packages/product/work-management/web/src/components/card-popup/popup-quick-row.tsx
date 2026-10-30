import type { ReactNode } from "react";
import {
  Clock,
  ListChecks,
  Paperclip,
  Plus,
  SquareCheck,
  Tag,
  UserPlus,
  LayoutList,
} from "lucide-react";
import { PopoverTrigger } from "@notrelix/ui-web";
import { useCardPopup, useCanEdit } from "./card-popup-context";
import { PanelPopover, QuickBtn } from "./popup-ui";
import { LabelsPanel } from "./pickers/labels-panel";
import { DatesPanel } from "./pickers/dates-panel";
import { PeoplePanel } from "./pickers/people-panel";
import { ChecklistPanel } from "./pickers/checklist-panel";
import { AttachPanel } from "./pickers/attach-panel";

const ADD_ENTRIES: ReadonlyArray<{
  key: "labels" | "dates" | "checklist" | "assignees" | "attach" | "fields";
  icon: ReactNode;
}> = [
  { key: "labels", icon: <Tag aria-hidden="true" className="size-4" /> },
  { key: "dates", icon: <Clock aria-hidden="true" className="size-4" /> },
  {
    key: "checklist",
    icon: <SquareCheck aria-hidden="true" className="size-4" />,
  },
  {
    key: "assignees",
    icon: <UserPlus aria-hidden="true" className="size-4" />,
  },
  { key: "attach", icon: <Paperclip aria-hidden="true" className="size-4" /> },
  { key: "fields", icon: <LayoutList aria-hidden="true" className="size-4" /> },
];

/** The "+ Thêm" menu: six entries; the first four open their picker directly. */
function AddMenu() {
  const { copy, picker, openPicker } = useCardPopup();
  const t = copy.shell;
  const isOpen = picker === "add" || picker === "attach";
  return (
    <PanelPopover
      open={isOpen}
      onOpenChange={(next) => openPicker(next ? "add" : null)}
      width={344}
      trigger={
        <PopoverTrigger asChild>
          <QuickBtn on={isOpen} title={t.addTitle} aria-haspopup="menu">
            <Plus aria-hidden="true" className="size-4" />
            {t.add}
          </QuickBtn>
        </PopoverTrigger>
      }
    >
      {picker === "attach" ? (
        <AttachPanel
          onClose={() => openPicker(null)}
          onBack={() => openPicker("add")}
        />
      ) : (
        <>
          <div className="mb-2 flex h-8 items-center justify-center">
            <b className="text-[14px] font-[650]">{t.addTitle}</b>
          </div>
          <div role="menu" className="flex flex-col">
            {ADD_ENTRIES.map(({ key, icon }) => (
              <button
                key={key}
                type="button"
                role="menuitem"
                onClick={() => openPicker(key)}
                className="flex w-full items-center gap-3 rounded-[8px] border-0 bg-transparent p-2 text-left text-foreground hover:bg-state-hover-strong"
              >
                <span className="grid size-9 shrink-0 place-items-center rounded-[8px] border border-line-strong bg-popover text-muted-foreground">
                  {icon}
                </span>
                <span>
                  <b className="block text-[14px] leading-[1.3] font-semibold">
                    {t.addMenu[key].title}
                  </b>
                  <small className="block text-[12.5px] leading-[1.35] font-normal text-meta-foreground">
                    {t.addMenu[key].hint}
                  </small>
                </span>
              </button>
            ))}
          </div>
        </>
      )}
    </PanelPopover>
  );
}

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
  const { picker, openPicker } = useCardPopup();
  const open = picker === id;
  return (
    <PanelPopover
      open={open}
      onOpenChange={(next) => openPicker(next ? id : null)}
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
    <div className="box-border flex flex-wrap gap-2 pt-2 pr-4 pl-4 md:h-11 md:flex-nowrap md:pr-6 md:pl-[60px]">
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
        width={336}
      >
        {() => <PeoplePanel onClose={close} />}
      </PickerButton>
    </div>
  );
}

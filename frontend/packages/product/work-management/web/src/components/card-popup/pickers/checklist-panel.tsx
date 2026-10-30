import { useEffect, useRef, useState } from "react";
import { useCardPopup } from "../card-popup-context";
import { FieldLabel, PickerHead } from "./picker-bits";
import { PanelBtn, PanelInput } from "../popup-ui";

/** "Thêm checklist": one title, "Checklist" by default; the list appears in the card. */
export function ChecklistPanel({ onClose }: { onClose: () => void }) {
  const { copy, data, slots } = useCardPopup();
  const t = copy.pickers.checklist;
  const [title, setTitle] = useState(t.defaultTitle);
  const ref = useRef<HTMLInputElement>(null);
  useEffect(() => ref.current?.select(), []);
  const add = () => {
    data.checklists?.addList(title.trim() || t.defaultTitle);
    onClose();
  };
  return (
    <form
      onSubmit={(event) => {
        event.preventDefault();
        add();
      }}
    >
      <PickerHead title={t.title} onClose={onClose} />
      <FieldLabel htmlFor="popup-checklist-title">{t.titleLabel}</FieldLabel>
      <PanelInput
        ref={ref}
        id="popup-checklist-title"
        value={title}
        maxLength={200}
        onChange={(event) => setTitle(event.target.value)}
      />
      {slots.checklistExtras ? (
        <div className="mt-3">{slots.checklistExtras}</div>
      ) : null}
      <PanelBtn
        type="submit"
        variant="primary"
        className="mt-3.5 w-full"
        disabled={!data.checklists}
      >
        {t.add}
      </PanelBtn>
    </form>
  );
}

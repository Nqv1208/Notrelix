import { useEffect, useRef, useState } from "react";
import { useCopyChecklist } from "@notrelix/work-management-state";
import { CHECKLIST_COPY_NONE } from "../capabilities/checklist-links-controls";
import { useCardPopup } from "../card-popup-context";
import { noteChecklistRequested } from "../sections/checklist-focus";
import { FieldLabel, PickerHead } from "./picker-bits";
import { PanelBtn, PanelInput } from "../popup-ui";

/** "Thêm checklist": one title, "Checklist" by default; the list appears in the card. */
export function ChecklistPanel({ onClose }: { onClose: () => void }) {
  const { card, copy, data, slots } = useCardPopup();
  const t = copy.pickers.checklist;
  const [title, setTitle] = useState(t.defaultTitle);
  const ref = useRef<HTMLInputElement>(null);
  useEffect(() => ref.current?.select(), []);
  const [source, setSource] = useState(CHECKLIST_COPY_NONE);
  const [failed, setFailed] = useState(false);
  const copyList = useCopyChecklist({
    itemId: card.id,
    boardId: card.boardId,
    workspaceId: card.workspaceId,
  });
  const add = () => {
    const name = title.trim() || t.defaultTitle;
    if (source === CHECKLIST_COPY_NONE) {
      noteChecklistRequested();
      data.checklists?.addList(name);
      onClose();
      return;
    }
    setFailed(false);
    copyList.mutate(
      { sourceChecklistId: source, title: name },
      { onSuccess: onClose, onError: () => setFailed(true) },
    );
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
        data-autofocus=""
        id="popup-checklist-title"
        value={title}
        maxLength={200}
        onChange={(event) => setTitle(event.target.value)}
      />
      {slots.checklistExtras ? (
        <div className="mt-3">
          {slots.checklistExtras({
            source,
            onSourceChange: setSource,
            failed,
          })}
        </div>
      ) : null}
      <PanelBtn
        type="submit"
        variant="primary"
        className="mt-3.5 w-full"
        disabled={!data.checklists || copyList.isPending}
      >
        {t.add}
      </PanelBtn>
    </form>
  );
}

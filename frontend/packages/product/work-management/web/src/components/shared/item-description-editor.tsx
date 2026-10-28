import { useEffect, useRef, useState } from "react";
import { BUTTON_SM_VARIANT_CLASS } from "../item-page/item-page-parts";

const BUTTON_PRIMARY = BUTTON_SM_VARIANT_CLASS.primary;
const BUTTON_OUTLINE = BUTTON_SM_VARIANT_CLASS.outline;

export interface ItemDescriptionEditorLabels {
  readonly heading: string;
  readonly placeholder: string;
  readonly save: string;
  readonly cancel: string;
  readonly tooLong: string;
}

/** The longest description the backend keeps (`BoardItem.DescriptionMaxLength`). */
export const DESCRIPTION_MAX_LENGTH = 20000;

/**
 * The description's editor: a growing text box that saves with the button or
 * Ctrl/Cmd+Enter and closes without saving on Escape or Cancel. Saving an
 * empty box clears the description.
 */
export function ItemDescriptionEditor({
  initial,
  labels,
  onSave,
  onClose,
}: {
  initial: string;
  labels: ItemDescriptionEditorLabels;
  onSave: (text: string) => void;
  onClose: () => void;
}) {
  const [draft, setDraft] = useState(initial);
  const areaRef = useRef<HTMLTextAreaElement>(null);
  const t = labels;
  useEffect(() => {
    const node = areaRef.current;
    if (!node) return;
    node.focus();
    node.setSelectionRange(node.value.length, node.value.length);
  }, []);
  useEffect(() => {
    const node = areaRef.current;
    if (!node) return;
    node.style.height = "auto";
    node.style.height = `${node.scrollHeight}px`;
  }, [draft]);
  const tooLong = draft.length > DESCRIPTION_MAX_LENGTH;
  const unchanged = draft.trim() === initial.trim();
  const save = () => {
    if (tooLong) return;
    if (!unchanged) onSave(draft);
    onClose();
  };
  return (
    <div className="flex flex-col gap-2">
      <textarea
        ref={areaRef}
        value={draft}
        aria-label={t.heading}
        aria-invalid={tooLong || undefined}
        placeholder={t.placeholder}
        rows={6}
        onChange={(event) => setDraft(event.target.value)}
        onKeyDown={(event) => {
          if (event.key === "Escape") {
            event.stopPropagation();
            onClose();
          } else if (
            event.key === "Enter" &&
            (event.metaKey || event.ctrlKey)
          ) {
            event.preventDefault();
            save();
          }
        }}
        className="box-border max-h-[420px] min-h-[132px] w-full resize-none rounded-[8px] border border-line-strong bg-popover px-3 py-2 text-item-prose leading-[1.6] text-foreground outline-none focus-visible:border-primary focus-visible:ring-2 focus-visible:ring-focus-ring"
      />
      <div className="flex flex-wrap items-center gap-2">
        <button
          type="button"
          onClick={save}
          className={BUTTON_PRIMARY}
          disabled={tooLong}
        >
          {t.save}
        </button>
        <button type="button" onClick={onClose} className={BUTTON_OUTLINE}>
          {t.cancel}
        </button>
        {tooLong ? (
          <span className="text-metadata text-meta-foreground">
            {t.tooLong}
          </span>
        ) : null}
      </div>
    </div>
  );
}

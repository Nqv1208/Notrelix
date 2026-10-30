import { useEffect, useRef, useState } from "react";
import { Check, ArrowUpRight, Trash2 } from "lucide-react";
import { Button, cn } from "@notrelix/ui-web";
import {
  isPendingChecklistId,
  listProgress,
  type Checklist,
} from "@notrelix/work-management-core";
import type { ItemChecklistsModel } from "../../checklists/item-checklists";
import { useChecklistCopy } from "../../checklists/use-checklist-copy";
import { fillSheetCopy } from "../../card-detail/item-sheet.copy";
import { fillPopupCopy } from "../card-popup.copy";
import { useCardPopup } from "../card-popup-context";
import {
  IconBtn,
  PopSection,
  SECTION_ICONS,
  SectionHead,
  SmallBtn,
} from "../popup-ui";

/** Failure with Retry, version conflict and the undo bar of the checklist writes. */
function Notices({ model }: { model: ItemChecklistsModel }) {
  const copy = useChecklistCopy();
  const { failure, conflict, pendingDelete } = model;
  if (!failure && !conflict && !pendingDelete) return null;
  const box =
    "flex items-center gap-2.5 rounded-[10px] px-3 py-2.5 text-[13.5px]";
  return (
    <div className="flex flex-col gap-2 pt-4 pr-6 pl-[60px]">
      {failure ? (
        <div
          role="alert"
          className={cn(box, "bg-danger-subtle text-foreground")}
        >
          <span className="flex-1">
            {failure.op.text
              ? fillSheetCopy(copy.failed, { name: failure.op.text })
              : copy.failedList}
          </span>
          <Button
            type="button"
            size="sm"
            variant="outline"
            onClick={model.retry}
          >
            {copy.retry}
          </Button>
          <Button
            type="button"
            size="sm"
            variant="ghost"
            onClick={model.discard}
          >
            {copy.discard}
          </Button>
        </div>
      ) : null}
      {conflict ? (
        <div
          role="status"
          className={cn(box, "bg-brand-subtle text-foreground")}
        >
          <span className="flex-1">{copy.conflict}</span>
          <Button
            type="button"
            size="sm"
            variant="ghost"
            onClick={model.dismissConflict}
          >
            {copy.dismiss}
          </Button>
        </div>
      ) : null}
      {pendingDelete ? (
        <div role="status" className={cn(box, "bg-foreground text-background")}>
          <span className="flex-1">
            {fillSheetCopy(copy.deleted, { name: pendingDelete.text })}
          </span>
          <button
            type="button"
            onClick={model.undo}
            className="cursor-pointer border-0 bg-transparent p-0 font-semibold text-inherit underline focus-visible:outline-2 focus-visible:outline-primary"
          >
            {copy.undo}
          </button>
        </div>
      ) : null}
    </div>
  );
}

/** A text box that grows in place: Enter saves, Escape closes, leaving saves typed text. */
function InlineText({
  label,
  placeholder,
  initial = "",
  keepOpen,
  onSubmit,
  onClose,
}: {
  label: string;
  placeholder?: string;
  initial?: string;
  keepOpen?: boolean;
  onSubmit: (text: string) => void;
  onClose: () => void;
}) {
  const [draft, setDraft] = useState(initial);
  const ref = useRef<HTMLInputElement>(null);
  const done = useRef(false);
  useEffect(() => {
    ref.current?.focus();
  }, []);
  const submit = () => {
    const text = draft.trim();
    if (!text) return false;
    if (text !== initial.trim()) onSubmit(text);
    return true;
  };
  return (
    <input
      ref={ref}
      type="text"
      value={draft}
      maxLength={200}
      aria-label={label}
      placeholder={placeholder}
      onChange={(event) => setDraft(event.target.value)}
      onKeyDown={(event) => {
        if (event.key === "Escape") {
          event.preventDefault();
          event.stopPropagation();
          done.current = true;
          onClose();
        } else if (event.key === "Enter" && !event.nativeEvent.isComposing) {
          event.preventDefault();
          if (!submit()) return;
          if (keepOpen) setDraft("");
          else {
            done.current = true;
            onClose();
          }
        }
      }}
      onBlur={() => {
        if (done.current) return;
        done.current = true;
        submit();
        onClose();
      }}
      className="box-border h-8 min-w-0 flex-1 rounded-[8px] border border-primary bg-popover px-2.5 text-[14px] text-foreground ring-[3px] ring-brand-subtle outline-none placeholder:text-meta-foreground"
    />
  );
}

function ChecklistBlock({
  list,
  model,
  writable,
}: {
  list: Checklist;
  model: ItemChecklistsModel;
  writable: boolean;
}) {
  const { copy } = useCardPopup();
  const t = copy.related.checklists;
  const sheet = useChecklistCopy();
  const [hideDone, setHideDone] = useState(false);
  const [adding, setAdding] = useState(false);
  const [editing, setEditing] = useState<string | null>(null);
  const { done, total } = listProgress(list);
  const percent = total > 0 ? Math.round((done / total) * 100) : 0;
  const listPending = isPendingChecklistId(list.id);
  const shown = hideDone
    ? list.items.filter((item) => !item.isDone)
    : list.items;
  const canWrite = writable && !listPending;

  return (
    <PopSection
      label={`Checklist ${list.title}`}
      icon={SECTION_ICONS.checklist}
    >
      <SectionHead title={list.title}>
        {done > 0 ? (
          <SmallBtn
            aria-pressed={hideDone}
            onClick={() => setHideDone((v) => !v)}
          >
            {hideDone ? t.showDone : t.hideDone}
          </SmallBtn>
        ) : null}
        {canWrite ? (
          <SmallBtn
            aria-label={fillPopupCopy(t.deleteListLabel, { name: list.title })}
            onClick={() => model.deleteList(list.id)}
          >
            {t.deleteList}
          </SmallBtn>
        ) : null}
      </SectionHead>
      {total > 0 ? (
        <div className="mt-1.5 mb-1 flex items-center gap-2.5">
          <span className="w-9 text-[12px] text-meta-foreground tabular-nums">
            {percent}%
          </span>
          <span
            role="progressbar"
            aria-label={fillPopupCopy(t.progress, { name: list.title })}
            aria-valuemin={0}
            aria-valuemax={100}
            aria-valuenow={percent}
            className="block h-2 flex-1 overflow-hidden rounded-[4px] bg-state-hover-strong"
          >
            <span
              className={cn(
                "block h-full rounded-[4px]",
                percent === 100 ? "bg-work-status-done" : "bg-primary",
              )}
              style={{ width: `${percent}%` }}
            />
          </span>
        </div>
      ) : null}
      <div role="list">
        {shown.map((item) => {
          const pending = isPendingChecklistId(item.id) || listPending;
          const writableItem = writable && !pending;
          return (
            <div
              key={item.id}
              role="listitem"
              aria-busy={pending || undefined}
              className={cn(
                "group/cli relative box-border flex min-h-9 items-center gap-2.5 rounded-[8px] px-1.5 hover:bg-state-hover-strong focus-within:bg-state-hover-strong",
                pending && "opacity-70",
              )}
            >
              <button
                type="button"
                role="checkbox"
                aria-checked={item.isDone}
                aria-label={fillSheetCopy(
                  item.isDone ? sheet.untick : sheet.tick,
                  { name: item.title },
                )}
                disabled={!writableItem}
                onClick={() => model.toggleItem(list.id, item.id)}
                className={cn(
                  "box-border grid size-[18px] shrink-0 place-items-center rounded-[5px] border-[1.5px] border-line-strong bg-popover p-0 text-primary-foreground focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary disabled:cursor-default",
                  item.isDone && "border-primary bg-primary",
                )}
              >
                {item.isDone ? (
                  <Check
                    aria-hidden="true"
                    className="size-3"
                    strokeWidth={3}
                  />
                ) : null}
              </button>
              {editing === item.id ? (
                <InlineText
                  label={t.editItem}
                  initial={item.title}
                  onSubmit={(text) => model.renameItem(list.id, item.id, text)}
                  onClose={() => setEditing(null)}
                />
              ) : (
                <span
                  className={cn(
                    "min-w-0 flex-1 text-[14px]",
                    item.isDone && "text-meta-foreground line-through",
                  )}
                  onDoubleClick={
                    writableItem ? () => setEditing(item.id) : undefined
                  }
                >
                  {item.title}
                </span>
              )}
              {writableItem && editing !== item.id ? (
                <span className="hidden gap-0.5 group-focus-within/cli:flex group-hover/cli:flex">
                  {model.canConvert ? (
                    <IconBtn
                      small
                      aria-label={`${t.convert}: ${item.title}`}
                      title={t.convert}
                      onClick={() =>
                        void model
                          .convertItem(list.id, item.id)
                          .catch(() => undefined)
                      }
                    >
                      <ArrowUpRight aria-hidden="true" className="size-4" />
                    </IconBtn>
                  ) : null}
                  <IconBtn
                    small
                    aria-label={`${t.deleteItem}: ${item.title}`}
                    title={t.deleteItem}
                    onClick={() => model.deleteItem(list.id, item.id)}
                  >
                    <Trash2 aria-hidden="true" className="size-4" />
                  </IconBtn>
                </span>
              ) : null}
            </div>
          );
        })}
      </div>
      {canWrite ? (
        <div className="mt-2 flex">
          {adding ? (
            <InlineText
              keepOpen
              label={fillPopupCopy(t.itemLabel, { name: list.title })}
              placeholder={t.itemPlaceholder}
              onSubmit={(text) => model.addItem(list.id, text)}
              onClose={() => setAdding(false)}
            />
          ) : (
            <SmallBtn onClick={() => setAdding(true)}>{t.addItem}</SmallBtn>
          )}
        </div>
      ) : null}
    </PopSection>
  );
}

/** Every checklist of the card as its own section. */
export function ChecklistsSection() {
  const { data, readOnly, capabilities } = useCardPopup();
  const model = data.checklists;
  if (
    !model ||
    (model.checklists.length === 0 && !model.failure && !model.pendingDelete)
  )
    return null;
  const writable =
    model.canEdit && readOnly === null && capabilities.canEditFields;
  return (
    <>
      <Notices model={model} />
      {model.checklists.map((list) => (
        <ChecklistBlock
          key={list.id}
          list={list}
          model={model}
          writable={writable}
        />
      ))}
    </>
  );
}

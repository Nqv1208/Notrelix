import { useEffect, useRef, useState, type KeyboardEvent } from "react";
import { Check, MoreHorizontal, Plus } from "lucide-react";
import {
  Button,
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
  cn,
} from "@notrelix/ui-web";
import {
  isPendingChecklistId,
  listProgress,
  type Checklist,
  type ChecklistItem,
} from "@notrelix/work-management-core";
import {
  SortableList,
  SortableRow,
} from "../board-settings/board-settings-sortable";
import { fillSheetCopy, pluralSheetCopy } from "../card-detail/item-sheet.copy";
import type { ChecklistCopy } from "./item-checklists.copy";

/** What the section needs: the lists, what the viewer may do, and the writes. */
export interface ItemChecklistsModel {
  readonly checklists: readonly Checklist[];
  /** The viewer may add, tick, edit, delete and reorder. */
  readonly canEdit: boolean;
  /** A sub-item has no sub-items of its own, so it offers no conversion. */
  readonly canConvert: boolean;
  readonly addList: (title: string) => void;
  readonly renameList: (checklistId: string, title: string) => void;
  readonly deleteList: (checklistId: string) => void;
  readonly moveList: (checklistId: string, toIndex: number) => void;
  readonly addItem: (checklistId: string, title: string) => void;
  readonly renameItem: (
    checklistId: string,
    itemId: string,
    title: string,
  ) => void;
  readonly toggleItem: (checklistId: string, itemId: string) => void;
  readonly deleteItem: (checklistId: string, itemId: string) => void;
  readonly moveItem: (
    checklistId: string,
    itemId: string,
    toIndex: number,
  ) => void;
  /** Rejects when the sub-item could not be created (the item then stays as it is). */
  readonly convertItem: (checklistId: string, itemId: string) => Promise<void>;
  /** Takes the waiting delete back. */
  readonly undo: () => void;
  readonly retry: () => void;
  readonly discard: () => void;
  readonly dismissConflict: () => void;
  /** The write the server refused: its text stays on screen. */
  readonly failure: {
    readonly op: {
      readonly kind: string;
      readonly checklistId?: string;
      readonly itemId?: string;
      readonly text?: string;
    };
  } | null;
  /** Another writer changed the list first; the latest version is shown. */
  readonly conflict: boolean;
  /** The delete the user may still take back. */
  readonly pendingDelete: {
    readonly kind: "item" | "list";
    readonly text: string;
  } | null;
  /** Writes are on their way. */
  readonly busy: boolean;
}

const MAX_TITLE = 200;
const ICON_BUTTON =
  "size-6 text-muted-foreground opacity-0 group-hover/row:opacity-100 focus-visible:opacity-100 data-[state=open]:opacity-100 pointer-coarse:opacity-100";

/** The id the item moved to, given the order before and after a reorder. */
function movedId(before: readonly string[], after: readonly string[]) {
  const d = after.findIndex((id, i) => id !== before[i]);
  if (d < 0) return null;
  const forward = before[d]!;
  const without = (ids: readonly string[], id: string) =>
    ids.filter((entry) => entry !== id).join("|");
  return without(before, forward) === without(after, forward)
    ? forward
    : after[d]!;
}

/** Where `id` lands in the full order when the visible order became `visible`. */
function fullIndex(
  full: readonly string[],
  id: string,
  visible: readonly string[],
) {
  const previous = visible[visible.indexOf(id) - 1];
  const rest = full.filter((entry) => entry !== id);
  return previous === undefined ? 0 : rest.indexOf(previous) + 1;
}

export function ItemChecklists({
  model,
  copy,
}: {
  model: ItemChecklistsModel;
  copy: ChecklistCopy;
}) {
  const { checklists, canEdit } = model;
  const [addingList, setAddingList] = useState(false);
  const [hidden, setHidden] = useState<ReadonlySet<string>>(new Set());
  const [convertError, setConvertError] = useState<string | null>(null);
  const all = checklists.flatMap((list) => list.items);
  const total = all.length;
  const done = all.filter((item) => item.isDone).length;

  return (
    <section aria-labelledby="item-checklists-heading">
      <h3
        id="item-checklists-heading"
        className="m-0 mb-2 flex items-center gap-2 text-compact font-semibold text-muted-foreground"
      >
        {copy.heading}
        {total > 0 ? (
          <span className="font-normal text-meta-foreground tabular-nums">
            {fillSheetCopy(copy.progress, { done, total })}
          </span>
        ) : null}
      </h3>
      <Notices
        model={model}
        copy={copy}
        convertError={convertError}
        onDismissConvertError={() => setConvertError(null)}
      />
      {checklists.length === 0 && !canEdit ? (
        <p className="m-0 text-body text-meta-foreground">{copy.empty}</p>
      ) : null}
      {!canEdit && checklists.length > 0 ? (
        <p className="m-0 mb-2 text-crumb text-meta-foreground">
          {copy.readOnly}
        </p>
      ) : null}
      <SortableList
        ids={checklists.map((list) => list.id)}
        onReorder={(next) => {
          const before = checklists.map((list) => list.id);
          const id = movedId(before, next);
          if (id) model.moveList(id, next.indexOf(id));
        }}
      >
        {(order) => (
          <div className="flex flex-col gap-4">
            {order.map((id) => {
              const list = checklists.find((entry) => entry.id === id);
              if (!list) return null;
              return (
                <ChecklistBlock
                  key={list.id}
                  list={list}
                  model={model}
                  copy={copy}
                  hideDone={hidden.has(list.id)}
                  onToggleHideDone={() =>
                    setHidden((current) => {
                      const next = new Set(current);
                      if (!next.delete(list.id)) next.add(list.id);
                      return next;
                    })
                  }
                  onConvertError={() => setConvertError(copy.convertFailed)}
                />
              );
            })}
          </div>
        )}
      </SortableList>
      {canEdit ? (
        addingList ? (
          <InlineInput
            label={copy.listNameLabel}
            placeholder={copy.listNamePlaceholder}
            copy={copy}
            className={checklists.length > 0 ? "mt-3" : undefined}
            keepOpen={false}
            onSubmit={(title) => model.addList(title)}
            onClose={() => setAddingList(false)}
          />
        ) : (
          <button
            type="button"
            onClick={() => setAddingList(true)}
            className={cn(
              "flex h-8 cursor-pointer items-center gap-1.5 rounded-[7px] border-0 bg-transparent px-1.5 text-item-ui font-medium text-brand-subtle-foreground outline-none hover:bg-state-hover-strong focus-visible:ring-2 focus-visible:ring-focus-ring",
              checklists.length > 0 && "mt-2",
            )}
          >
            <Plus aria-hidden="true" className="size-4" />
            {copy.addList}
          </button>
        )
      ) : null}
      <span className="sr-only" role="status" aria-live="polite">
        {model.busy ? copy.saving : ""}
      </span>
    </section>
  );
}

/** Failure with Retry, version conflict, convert problems and the undo bar. */
function Notices({
  model,
  copy,
  convertError,
  onDismissConvertError,
}: {
  model: ItemChecklistsModel;
  copy: ChecklistCopy;
  convertError: string | null;
  onDismissConvertError: () => void;
}) {
  const { failure, conflict, pendingDelete } = model;
  const alert =
    "mb-2 flex items-center gap-2.5 rounded-[8px] px-2.5 py-2 text-crumb";
  return (
    <>
      {failure ? (
        <div
          role="alert"
          className={cn(alert, "bg-danger-subtle text-danger-text")}
        >
          <span className="flex-1">
            {failure.op.kind === "convert-cleanup"
              ? fillSheetCopy(copy.convertCleanup, {
                  name: failure.op.text ?? "",
                })
              : failure.op.text
                ? fillSheetCopy(copy.failed, { name: failure.op.text })
                : copy.failedList}
          </span>
          <Button
            type="button"
            size="sm"
            variant="outline"
            onClick={model.retry}
          >
            {failure.op.kind === "convert-cleanup"
              ? copy.convertCleanupAction
              : copy.retry}
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
          className={cn(alert, "bg-brand-subtle text-brand-subtle-foreground")}
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
      {convertError ? (
        <div
          role="alert"
          className={cn(alert, "bg-danger-subtle text-danger-text")}
        >
          <span className="flex-1">{convertError}</span>
          <Button
            type="button"
            size="sm"
            variant="ghost"
            onClick={onDismissConvertError}
          >
            {copy.dismiss}
          </Button>
        </div>
      ) : null}
      {pendingDelete ? (
        <div
          role="status"
          className={cn(alert, "bg-foreground text-background")}
        >
          <span className="flex-1">
            {fillSheetCopy(copy.deleted, { name: pendingDelete.text })}
          </span>
          <button
            type="button"
            onClick={model.undo}
            className="cursor-pointer border-0 bg-transparent p-0 font-semibold text-inherit underline outline-none focus-visible:ring-2 focus-visible:ring-focus-ring"
          >
            {copy.undo}
          </button>
        </div>
      ) : null}
    </>
  );
}

function ChecklistBlock({
  list,
  model,
  copy,
  hideDone,
  onToggleHideDone,
  onConvertError,
}: {
  list: Checklist;
  model: ItemChecklistsModel;
  copy: ChecklistCopy;
  hideDone: boolean;
  onToggleHideDone: () => void;
  onConvertError: () => void;
}) {
  const { canEdit } = model;
  const [renaming, setRenaming] = useState(false);
  const [adding, setAdding] = useState(false);
  const pending = isPendingChecklistId(list.id);
  const { done, total } = listProgress(list);
  const percent = total > 0 ? Math.round((done / total) * 100) : 0;
  const shown = hideDone
    ? list.items.filter((item) => !item.isDone)
    : list.items;
  const fullIds = list.items.map((item) => item.id);

  return (
    <SortableRow
      id={list.id}
      handleLabel={fillSheetCopy(copy.dragList, { name: list.title })}
      aria-busy={pending || undefined}
      className="group/list rounded-[10px]"
    >
      {(handle) => (
        <div
          role="group"
          aria-label={list.title}
          className="flex flex-col gap-1.5"
        >
          <div className="group/row flex items-center gap-1.5">
            {canEdit && !pending ? handle : <span className="w-7" />}
            {renaming ? (
              <InlineInput
                label={copy.listNameLabel}
                placeholder={copy.listNamePlaceholder}
                copy={copy}
                initial={list.title}
                keepOpen={false}
                className="flex-1"
                onSubmit={(title) => model.renameList(list.id, title)}
                onClose={() => setRenaming(false)}
              />
            ) : (
              <h4 className="m-0 min-w-0 flex-1 truncate text-body font-semibold text-foreground">
                {list.title}
              </h4>
            )}
            {canEdit && !pending && !renaming ? (
              <DropdownMenu>
                <DropdownMenuTrigger asChild>
                  <Button
                    type="button"
                    variant="ghost"
                    size="icon-sm"
                    aria-label={fillSheetCopy(copy.listActions, {
                      name: list.title,
                    })}
                    className={ICON_BUTTON}
                  >
                    <MoreHorizontal aria-hidden="true" className="size-3.5" />
                  </Button>
                </DropdownMenuTrigger>
                <DropdownMenuContent align="end">
                  <DropdownMenuItem onSelect={() => setRenaming(true)}>
                    {copy.renameList}
                  </DropdownMenuItem>
                  <DropdownMenuItem onSelect={() => model.deleteList(list.id)}>
                    {copy.deleteList}
                  </DropdownMenuItem>
                </DropdownMenuContent>
              </DropdownMenu>
            ) : null}
          </div>
          {total > 0 ? (
            <div className="flex items-center gap-2.5 pl-8 text-crumb text-meta-foreground tabular-nums">
              <span>{fillSheetCopy(copy.progress, { done, total })}</span>
              <span
                role="progressbar"
                aria-label={fillSheetCopy(copy.progressLabel, { done, total })}
                aria-valuemin={0}
                aria-valuemax={total}
                aria-valuenow={done}
                className="h-1.5 flex-1 overflow-hidden rounded-[3px] bg-state-hover-strong"
              >
                <span
                  className="block h-full rounded-[3px] bg-work-status-done transition-[width] duration-200"
                  style={{ width: `${percent}%` }}
                />
              </span>
              {done > 0 ? (
                <button
                  type="button"
                  aria-expanded={!hideDone}
                  onClick={onToggleHideDone}
                  className="cursor-pointer border-0 bg-transparent p-0 text-crumb font-medium text-brand-subtle-foreground outline-none focus-visible:ring-2 focus-visible:ring-focus-ring"
                >
                  {hideDone
                    ? pluralSheetCopy(
                        copy.showDone_one,
                        copy.showDone_other,
                        done,
                      )
                    : copy.hideDone}
                </button>
              ) : null}
            </div>
          ) : null}
          <SortableList
            ids={shown.map((item) => item.id)}
            onReorder={(next) => {
              const id = movedId(
                shown.map((item) => item.id),
                next,
              );
              if (id) model.moveItem(list.id, id, fullIndex(fullIds, id, next));
            }}
          >
            {(order) => (
              <div role="list" className="m-0 flex flex-col p-0">
                {order.map((itemId) => {
                  const item = list.items.find((entry) => entry.id === itemId);
                  if (!item) return null;
                  return (
                    <ItemRow
                      key={item.id}
                      list={list}
                      item={item}
                      model={model}
                      copy={copy}
                      onConvertError={onConvertError}
                    />
                  );
                })}
              </div>
            )}
          </SortableList>
          {hideDone && shown.length === 0 && total > 0 ? (
            <p className="m-0 pl-8 text-crumb text-meta-foreground">
              {copy.allDone}
            </p>
          ) : null}
          {canEdit && !pending ? (
            adding ? (
              <InlineInput
                label={copy.itemLabel}
                placeholder={copy.itemPlaceholder}
                copy={copy}
                keepOpen
                className="ml-8"
                onSubmit={(title) => model.addItem(list.id, title)}
                onClose={() => setAdding(false)}
              />
            ) : (
              <button
                type="button"
                onClick={() => setAdding(true)}
                className="ml-8 flex h-8 w-fit cursor-pointer items-center gap-1.5 rounded-[7px] border-0 bg-transparent px-1.5 text-item-ui font-medium text-brand-subtle-foreground outline-none hover:bg-state-hover-strong focus-visible:ring-2 focus-visible:ring-focus-ring"
              >
                <Plus aria-hidden="true" className="size-4" />
                {copy.addItem}
              </button>
            )
          ) : null}
        </div>
      )}
    </SortableRow>
  );
}

function ItemRow({
  list,
  item,
  model,
  copy,
  onConvertError,
}: {
  list: Checklist;
  item: ChecklistItem;
  model: ItemChecklistsModel;
  copy: ChecklistCopy;
  onConvertError: () => void;
}) {
  const { canEdit } = model;
  const [editing, setEditing] = useState(false);
  // An entry the server has not answered for has no id to write to yet.
  const pending =
    isPendingChecklistId(item.id) || isPendingChecklistId(list.id);
  const writable = canEdit && !pending;
  const label = fillSheetCopy(item.isDone ? copy.untick : copy.tick, {
    name: item.title,
  });

  return (
    <SortableRow
      id={item.id}
      handleLabel={fillSheetCopy(copy.dragItem, { name: item.title })}
      role="listitem"
      aria-busy={pending || undefined}
      className="group/row"
    >
      {(handle) => (
        <div className="-mx-1.5 flex min-h-8.5 items-center gap-1.5 rounded-[7px] px-1.5 hover:bg-state-hover-strong">
          {writable ? handle : <span className="w-7 flex-none" />}
          <button
            type="button"
            role="checkbox"
            aria-checked={item.isDone}
            aria-label={label}
            aria-disabled={!writable || undefined}
            onClick={
              writable ? () => model.toggleItem(list.id, item.id) : undefined
            }
            className={cn(
              "grid size-4 shrink-0 place-items-center rounded-[4px] border-[1.5px] p-0 outline-none focus-visible:ring-2 focus-visible:ring-focus-ring",
              writable ? "cursor-pointer" : "cursor-default opacity-70",
              item.isDone
                ? "border-work-status-done bg-work-status-done text-background"
                : "border-line-strong",
            )}
          >
            {item.isDone ? (
              <Check className="size-2.75" strokeWidth={3} />
            ) : null}
          </button>
          {editing ? (
            <InlineInput
              label={copy.itemLabel}
              placeholder={copy.itemPlaceholder}
              copy={copy}
              initial={item.title}
              keepOpen={false}
              className="flex-1"
              onSubmit={(title) => model.renameItem(list.id, item.id, title)}
              onClose={() => setEditing(false)}
            />
          ) : (
            <span
              className={cn(
                "min-w-0 flex-1 break-words text-body",
                item.isDone && "text-meta-foreground line-through",
              )}
            >
              {item.title}
            </span>
          )}
          {writable && !editing ? (
            <DropdownMenu>
              <DropdownMenuTrigger asChild>
                <Button
                  type="button"
                  variant="ghost"
                  size="icon-sm"
                  aria-label={fillSheetCopy(copy.itemActions, {
                    name: item.title,
                  })}
                  className={ICON_BUTTON}
                >
                  <MoreHorizontal aria-hidden="true" className="size-3.5" />
                </Button>
              </DropdownMenuTrigger>
              <DropdownMenuContent align="end">
                <DropdownMenuItem onSelect={() => setEditing(true)}>
                  {copy.editItem}
                </DropdownMenuItem>
                {model.canConvert ? (
                  <DropdownMenuItem
                    onSelect={() => {
                      model.convertItem(list.id, item.id).catch(onConvertError);
                    }}
                  >
                    {copy.convert}
                  </DropdownMenuItem>
                ) : null}
                <DropdownMenuItem
                  onSelect={() => model.deleteItem(list.id, item.id)}
                >
                  {copy.deleteItem}
                </DropdownMenuItem>
              </DropdownMenuContent>
            </DropdownMenu>
          ) : null}
        </div>
      )}
    </SortableRow>
  );
}

/**
 * A text box that grows in place of its button. Enter saves (and, with
 * `keepOpen`, stays for the next entry), Escape closes without saving, and
 * leaving the box with text saves it.
 */
function InlineInput({
  label,
  placeholder,
  copy,
  initial = "",
  keepOpen,
  className,
  onSubmit,
  onClose,
}: {
  label: string;
  placeholder: string;
  copy: ChecklistCopy;
  initial?: string;
  keepOpen: boolean;
  className?: string;
  onSubmit: (text: string) => void;
  onClose: () => void;
}) {
  const [draft, setDraft] = useState(initial);
  const ref = useRef<HTMLInputElement>(null);
  const done = useRef(false);
  useEffect(() => {
    const node = ref.current;
    node?.focus();
    node?.setSelectionRange(node.value.length, node.value.length);
  }, []);

  const submit = (): boolean => {
    const text = draft.trim();
    if (!text) return false;
    onSubmit(text);
    return true;
  };
  const onKeyDown = (event: KeyboardEvent<HTMLInputElement>) => {
    if (event.key === "Escape") {
      event.preventDefault();
      // The sheet closes on Escape too; this one only closes the box.
      event.stopPropagation();
      done.current = true;
      onClose();
    } else if (event.key === "Enter" && !event.nativeEvent.isComposing) {
      event.preventDefault();
      if (!submit()) return;
      if (keepOpen) {
        setDraft("");
      } else {
        done.current = true;
        onClose();
      }
    }
  };

  return (
    <div className={cn("flex flex-col gap-1", className)}>
      <input
        ref={ref}
        type="text"
        value={draft}
        maxLength={MAX_TITLE}
        aria-label={label}
        placeholder={placeholder}
        onChange={(event) => setDraft(event.target.value)}
        onKeyDown={onKeyDown}
        onBlur={() => {
          if (done.current) return;
          done.current = true;
          // Typed text is not thrown away by moving on.
          if (draft.trim() && draft.trim() !== initial.trim()) submit();
          onClose();
        }}
        className="h-8 w-full min-w-0 rounded-[7px] border border-primary bg-popover px-2.5 text-body text-foreground outline-none ring-2 ring-focus-ring/40 placeholder:text-meta-foreground"
      />
      {keepOpen ? (
        <span className="text-metadata text-meta-foreground">
          {copy.itemHint}
        </span>
      ) : null}
    </div>
  );
}

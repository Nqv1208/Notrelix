import { useId, useState } from "react";
import { Info, MoreHorizontal, Pencil, Plus } from "lucide-react";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
  cn,
} from "@notrelix/ui-web";
import type { BoardSettingsGroupDto } from "@notrelix/work-management-core";
import { DisabledControl } from "../item-page/item-page-parts";
import { useSettings } from "./board-settings-context";
import { fillCopy, plural } from "./board-settings-format";
import {
  BUTTON_CLASS,
  Callout,
  ColorSquare,
  HINT_CLASS,
  ICON_BUTTON_CLASS,
  INPUT_CLASS,
  SectionHeader,
  SectionLine,
} from "./board-settings-parts";
import { SortableList, SortableRow } from "./board-settings-sortable";

const ROW_GRID =
  "grid grid-cols-[28px_14px_minmax(0,1fr)_92px_66px] items-center gap-2.5 min-h-[47px] first:min-h-[46px] pr-2.5 pl-1.5 text-item-ui";

/** The "Nhóm việc" section: groups of the board, rename, reorder, delete with a move. */
export function GroupsSection() {
  const { copy, settings, actions, run } = useSettings();
  const t = copy.groups;
  const [adding, setAdding] = useState(false);
  const [newName, setNewName] = useState("");
  const [renameId, setRenameId] = useState<string | null>(null);
  const [deleteId, setDeleteId] = useState<string | null>(null);
  const groups = settings.groups;
  const total = groups.reduce((sum, group) => sum + group.itemCount, 0);

  async function add() {
    const name = newName.trim();
    if (!name) return;
    const result = await run(actions.createGroup(name));
    if (result.ok) {
      setAdding(false);
      setNewName("");
    }
  }

  return (
    <section
      aria-labelledby="bs-groups-h"
      className="min-w-0 flex-1 overflow-auto px-6 pt-5 pb-6"
    >
      <div className="max-w-[780px]">
        <SectionHeader
          id="bs-groups-h"
          title={t.title}
          intro={t.intro}
          actions={
            <button
              type="button"
              onClick={() => setAdding(true)}
              className={BUTTON_CLASS.primary}
            >
              <Plus aria-hidden="true" />
              {t.add}
            </button>
          }
        />
        <Callout icon={Info} className="mb-3.5">
          <span>{t.note}</span>
        </Callout>

        {adding ? (
          <form
            className="mb-3 flex items-center gap-2"
            onSubmit={(event) => {
              event.preventDefault();
              void add();
            }}
          >
            <input
              autoFocus
              value={newName}
              aria-label={t.addPlaceholder}
              placeholder={t.addPlaceholder}
              onChange={(event) => setNewName(event.target.value)}
              onKeyDown={(event) => {
                if (event.key === "Escape") {
                  event.stopPropagation();
                  setAdding(false);
                  setNewName("");
                }
              }}
              className={cn(INPUT_CLASS, "min-w-0 flex-1")}
            />
            <button
              type="button"
              onClick={() => {
                setAdding(false);
                setNewName("");
              }}
              className={BUTTON_CLASS.outline}
            >
              {copy.common.cancel}
            </button>
            <button
              type="submit"
              disabled={newName.trim() === ""}
              className={BUTTON_CLASS.primary}
            >
              {t.addConfirm}
            </button>
          </form>
        ) : null}

        <SectionLine
          title={t.list}
          count={fillCopy(t.summary, { groups: groups.length, items: total })}
          note={t.dragNote}
        />
        <div
          role="list"
          className="overflow-hidden rounded-[10px] border border-line bg-popover"
        >
          <div
            aria-hidden="true"
            className={cn(
              ROW_GRID,
              "min-h-[34px] first:min-h-[34px] bg-background text-metadata font-semibold text-meta-foreground",
            )}
          >
            <span />
            <span />
            <span>{t.columns.name}</span>
            <span>{t.columns.count}</span>
            <span />
          </div>
          <SortableList
            ids={groups.map((group) => group.id)}
            onReorder={(ids) => void run(actions.reorderGroups(ids))}
          >
            {(order) =>
              order.map((id) => {
                const group = groups.find((entry) => entry.id === id);
                if (!group) return null;
                return (
                  <GroupRow
                    key={group.id}
                    group={group}
                    last={groups.length <= 1}
                    renaming={renameId === group.id}
                    deleting={deleteId === group.id}
                    others={groups.filter((entry) => entry.id !== group.id)}
                    defaultTargetId={
                      (
                        groups[
                          groups.findIndex((entry) => entry.id === group.id) + 1
                        ] ??
                        groups[
                          groups.findIndex((entry) => entry.id === group.id) - 1
                        ]
                      )?.id ?? ""
                    }
                    onRename={() => setRenameId(group.id)}
                    onRenameDone={() => setRenameId(null)}
                    onDelete={() => setDeleteId(group.id)}
                    onDeleteDone={() => setDeleteId(null)}
                  />
                );
              })
            }
          </SortableList>
        </div>
        <p className={cn(HINT_CLASS, "mt-2.5 mb-0 px-0.5")}>{t.footnote}</p>
      </div>
    </section>
  );
}

function GroupRow({
  group,
  last,
  renaming,
  deleting,
  others,
  defaultTargetId,
  onRename,
  onRenameDone,
  onDelete,
  onDeleteDone,
}: {
  group: BoardSettingsGroupDto;
  last: boolean;
  renaming: boolean;
  deleting: boolean;
  others: readonly BoardSettingsGroupDto[];
  /** The group the items move to unless another is chosen: the next one, else the previous. */
  defaultTargetId: string;
  onRename: () => void;
  onRenameDone: () => void;
  onDelete: () => void;
  onDeleteDone: () => void;
}) {
  const { copy, settings, actions, run } = useSettings();
  const t = copy.groups;
  const keepsItems =
    settings.unavailable?.nonEmptyGroupDelete === true && group.itemCount > 0;
  const [name, setName] = useState(group.name);
  const [moveTo, setMoveTo] = useState(defaultTargetId);
  const confirmId = useId();
  const needsTarget = group.itemCount > 0;

  async function commitRename() {
    const next = name.trim();
    if (next && next !== group.name)
      await run(actions.updateGroup(group.id, { name: next }));
    else setName(group.name);
    onRenameDone();
  }

  async function confirmDelete() {
    const result = await run(
      actions.deleteGroup(group.id, needsTarget ? moveTo : null),
    );
    if (result.ok) onDeleteDone();
  }

  return (
    <>
      <SortableRow
        id={group.id}
        handleLabel={`${copy.common.dragHandle}: ${group.name}`}
        role="listitem"
        className={cn(
          ROW_GRID,
          "border-t border-line bg-popover first:border-t-0",
          deleting && "bg-row-selected",
        )}
      >
        {(handle) => (
          <>
            {handle}
            <ColorSquare color={group.color} />
            {renaming ? (
              <input
                autoFocus
                value={name}
                aria-label={t.renameField}
                onChange={(event) => setName(event.target.value)}
                onBlur={() => void commitRename()}
                onKeyDown={(event) => {
                  if (event.key === "Enter") event.currentTarget.blur();
                  if (event.key === "Escape") {
                    event.stopPropagation();
                    setName(group.name);
                    onRenameDone();
                  }
                }}
                className={cn(INPUT_CLASS, "h-8 min-w-0")}
              />
            ) : (
              <span className="truncate font-medium">{group.name}</span>
            )}
            <span className="text-crumb text-meta-foreground">
              {plural(
                copy.common.items_one,
                copy.common.items_other,
                group.itemCount,
              )}
            </span>
            <span className="flex justify-end gap-0.5">
              <button
                type="button"
                aria-label={fillCopy(t.rename, { name: group.name })}
                onClick={onRename}
                className={ICON_BUTTON_CLASS}
              >
                <Pencil aria-hidden="true" />
              </button>
              <DropdownMenu>
                <DropdownMenuTrigger asChild>
                  <button
                    type="button"
                    aria-haspopup="menu"
                    aria-controls={deleting ? confirmId : undefined}
                    aria-label={fillCopy(t.more, { name: group.name })}
                    className={ICON_BUTTON_CLASS}
                  >
                    <MoreHorizontal aria-hidden="true" />
                  </button>
                </DropdownMenuTrigger>
                <DropdownMenuContent align="end">
                  {last || keepsItems ? (
                    <DisabledControl reason={last ? t.lastGroup : t.notEmpty}>
                      <DropdownMenuItem
                        aria-disabled="true"
                        onSelect={(event) => event.preventDefault()}
                      >
                        {t.delete}
                      </DropdownMenuItem>
                    </DisabledControl>
                  ) : (
                    <DropdownMenuItem variant="destructive" onSelect={onDelete}>
                      {t.delete}
                    </DropdownMenuItem>
                  )}
                </DropdownMenuContent>
              </DropdownMenu>
            </span>
          </>
        )}
      </SortableRow>
      {deleting ? (
        <div
          id={confirmId}
          role="listitem"
          aria-label={fillCopy(t.deleteQuestion, { name: group.name })}
          className="flex flex-col gap-2.5 border-t border-line bg-background px-3.5 pt-3 pb-3.5 pl-[58px]"
        >
          <p className="m-0 text-item-small text-muted-foreground">
            <b className="font-semibold text-foreground">
              {fillCopy(t.deleteQuestion, { name: group.name })}
            </b>{" "}
            {needsTarget
              ? fillCopy(t.deleteBody, { count: group.itemCount })
              : t.deleteBodyEmpty}
          </p>
          <div className="flex flex-wrap items-center gap-2">
            {needsTarget ? (
              <>
                <label
                  htmlFor={`${confirmId}-to`}
                  className="text-item-small font-medium"
                >
                  {fillCopy(t.moveTo, { count: group.itemCount })}
                </label>
                <select
                  id={`${confirmId}-to`}
                  value={moveTo}
                  onChange={(event) => setMoveTo(event.target.value)}
                  className={cn(INPUT_CLASS, "w-[220px]")}
                >
                  <option value="">{t.choose}</option>
                  {others.map((other) => (
                    <option key={other.id} value={other.id}>
                      {other.name}
                    </option>
                  ))}
                </select>
              </>
            ) : null}
            <span className="flex-1" />
            <button
              type="button"
              onClick={onDeleteDone}
              className={BUTTON_CLASS.outline}
            >
              {copy.common.cancel}
            </button>
            <button
              type="button"
              disabled={needsTarget && moveTo === ""}
              onClick={() => void confirmDelete()}
              className={BUTTON_CLASS.danger}
            >
              {needsTarget ? t.deleteConfirm : t.deleteConfirmEmpty}
            </button>
          </div>
        </div>
      ) : null}
    </>
  );
}

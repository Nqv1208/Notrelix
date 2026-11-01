import { useId, useState } from "react";
import { Info, MoreHorizontal, Pencil, Plus } from "lucide-react";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
  cn,
} from "@notrelix/ui-web";
import type { BoardSettingsGroupDto } from "@notrelix/work-management-core";
import { DisabledControl } from "../item-page/item-page-parts";
import { useSettings } from "./board-settings-context";
import { WriteError, useWriteErrorProps } from "./board-settings-write-error";
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
  const addFailedProps = useWriteErrorProps("group-add", "bs-group-add-error");
  const groups = settings.groups;
  const total = groups.reduce((sum, group) => sum + group.itemCount, 0);

  async function add() {
    const name = newName.trim();
    if (!name) return;
    const result = await run(() => actions.createGroup(name), "group-add");
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
            className="mb-3 flex flex-wrap items-center gap-2"
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
              {...addFailedProps}
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
            <div className="basis-full empty:hidden">
              <WriteError scope="group-add" id="bs-group-add-error" />
            </div>
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
            onReorder={(ids) => run(() => actions.reorderGroups(ids))}
            announce={(id, position, total) =>
              fillCopy(copy.common.moved, {
                name: groups.find((entry) => entry.id === id)?.name ?? "",
                position,
                total,
              })
            }
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
        {settings.archivedGroups.length > 0 ? (
          <div className="mt-5">
            <SectionLine
              title={t.archivedList}
              count={fillCopy(t.archivedSummary, {
                count: settings.archivedGroups.length,
              })}
              note={t.archivedNote}
            />
            <div
              role="list"
              className="overflow-hidden rounded-[10px] border border-line bg-popover"
            >
              {settings.archivedGroups.map((group) => (
                <ArchivedGroupRow key={group.id} group={group} />
              ))}
            </div>
          </div>
        ) : null}
      </div>
    </section>
  );
}

/** An archived group: out of every view, its items inside; Unarchive brings the group and them back. */
function ArchivedGroupRow({ group }: { group: BoardSettingsGroupDto }) {
  const { copy, actions, run } = useSettings();
  const t = copy.groups;
  const errorId = useId();
  const scope = `group:${group.id}`;
  return (
    <>
      <div
        role="listitem"
        className="grid min-h-[47px] grid-cols-[14px_minmax(0,1fr)_92px_auto] items-center gap-2.5 border-t border-line pr-2.5 pl-3.5 text-item-ui text-meta-foreground first:border-t-0"
      >
        <ColorSquare color={group.color} />
        <span className="truncate font-medium">{group.name}</span>
        <span className="text-crumb">
          {plural(
            copy.common.items_one,
            copy.common.items_other,
            group.itemCount,
          )}
        </span>
        <span className="flex justify-end">
          <button
            type="button"
            aria-label={fillCopy(t.unarchiveLabel, { name: group.name })}
            onClick={() =>
              void run(() => actions.unarchiveGroup(group.id), scope)
            }
            className={BUTTON_CLASS.ghostBrand}
          >
            {t.unarchive}
          </button>
        </span>
      </div>
      <div role="listitem" className="empty:hidden pl-3.5">
        <WriteError scope={scope} id={errorId} />
      </div>
    </>
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
  const [archiving, setArchiving] = useState(false);
  const confirmId = useId();
  const archiveId = useId();
  const renameScope = `group:${group.id}`;
  const renameErrorId = `${confirmId}-rename-error`;
  const renameFailedProps = useWriteErrorProps(renameScope, renameErrorId);
  const needsTarget = group.itemCount > 0;

  async function commitRename() {
    const next = name.trim();
    if (next && next !== group.name) {
      // A failed rename stays open with the typed name; the error offers Retry.
      const result = await run(
        () => actions.updateGroup(group.id, { name: next }),
        renameScope,
      );
      if (result.ok) onRenameDone();
      return;
    }
    setName(group.name);
    onRenameDone();
  }

  async function confirmArchive() {
    const result = await run(() => actions.archiveGroup(group.id), renameScope);
    if (result.ok) setArchiving(false);
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
                {...renameFailedProps}
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
                  <DropdownMenuItem
                    onSelect={() =>
                      void run(
                        () =>
                          actions.duplicateGroup(
                            group.id,
                            fillCopy(t.copyName, { name: group.name }),
                          ),
                        renameScope,
                      )
                    }
                  >
                    {t.duplicate}
                  </DropdownMenuItem>
                  <DropdownMenuItem
                    onSelect={() => {
                      onDeleteDone();
                      setArchiving(true);
                    }}
                  >
                    {t.archive}
                  </DropdownMenuItem>
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
                    <DropdownMenuItem
                      variant="destructive"
                      onSelect={() => {
                        setArchiving(false);
                        onDelete();
                      }}
                    >
                      {t.delete}
                    </DropdownMenuItem>
                  )}
                </DropdownMenuContent>
              </DropdownMenu>
            </span>
          </>
        )}
      </SortableRow>
      <div role="listitem" className="empty:hidden pl-[58px]">
        <WriteError scope={renameScope} id={renameErrorId} />
      </div>
      {archiving ? (
        <div
          id={archiveId}
          role="listitem"
          aria-label={fillCopy(t.archiveQuestion, { name: group.name })}
          className="flex flex-col gap-2.5 border-t border-line bg-background px-3.5 pt-3 pb-3.5 pl-[58px]"
        >
          <p className="m-0 text-item-small text-muted-foreground">
            <b className="font-semibold text-foreground">
              {fillCopy(t.archiveQuestion, { name: group.name })}
            </b>{" "}
            {group.itemCount > 0
              ? fillCopy(t.archiveBody, { count: group.itemCount })
              : t.archiveBodyEmpty}
          </p>
          <div className="flex flex-wrap items-center gap-2">
            <span className="flex-1" />
            <button
              type="button"
              onClick={() => setArchiving(false)}
              className={BUTTON_CLASS.outline}
            >
              {copy.common.cancel}
            </button>
            <button
              type="button"
              onClick={() => void confirmArchive()}
              className={BUTTON_CLASS.primary}
            >
              {t.archiveConfirm}
            </button>
          </div>
        </div>
      ) : null}
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
                <Select value={moveTo} onValueChange={setMoveTo}>
                  <SelectTrigger id={`${confirmId}-to`} className="w-[220px]">
                    <SelectValue placeholder={t.choose} />
                  </SelectTrigger>
                  <SelectContent>
                    {others.map((other) => (
                      <SelectItem key={other.id} value={other.id}>
                        {other.name}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
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

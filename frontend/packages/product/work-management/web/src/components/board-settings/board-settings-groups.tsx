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

/** The inset panel under a row (the artboard's `.gconf`): the delete, archive and add forms and the gone notice. */
const PANEL_CLASS =
  "flex flex-col gap-2.5 border-t border-line bg-background px-3.5 pt-3 pb-3.5 pl-[58px] first:border-t-0";

/** A group whose rename, archive or delete panel is open; `leaving` while this person's own archive or delete runs. */
interface OpenPanel {
  readonly id: string;
  readonly name: string;
  readonly kind: "rename" | "delete" | "archive";
  readonly leaving: boolean;
}

const ROW_GRID =
  "grid grid-cols-[28px_14px_minmax(0,1fr)_92px_66px] items-center gap-2.5 min-h-[47px] first:min-h-[46px] pr-2.5 pl-1.5 text-item-ui";

/** The "Nhóm việc" section: groups of the board, rename, reorder, delete with a move. */
export function GroupsSection() {
  const { copy, settings, actions, run } = useSettings();
  const t = copy.groups;
  const [adding, setAdding] = useState(false);
  const [addPending, setAddPending] = useState(false);
  const [newName, setNewName] = useState("");
  const [open, setOpen] = useState<OpenPanel | null>(null);
  const addFailedProps = useWriteErrorProps("group-add", "bs-group-add-error");
  const groups = settings.groups;
  const total = groups.reduce((sum, group) => sum + group.itemCount, 0);
  // The group of an open panel left the settings without this person's own archive or delete:
  // someone else deleted or archived it. The panel gives way to a notice and nothing is sent.
  const gone =
    open && !open.leaving && !groups.some((group) => group.id === open.id)
      ? open
      : null;
  const openOf = (id: string) =>
    open && open.id === id && !gone ? open.kind : null;

  async function add() {
    const name = newName.trim();
    if (!name || addPending) return;
    setAddPending(true);
    const result = await run(() => actions.createGroup(name), "group-add");
    setAddPending(false);
    if (result.ok) {
      setAdding(false);
      setNewName("");
    }
  }

  function closeAdd() {
    if (addPending) return;
    setAdding(false);
    setNewName("");
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
          {gone ? (
            <div role="listitem" className={PANEL_CLASS}>
              <div role="status" className="flex flex-wrap items-center gap-2">
                <p className="m-0 min-w-0 flex-1 text-item-small text-muted-foreground">
                  <b className="font-semibold text-foreground">
                    {fillCopy(t.goneTitle, { name: gone.name })}
                  </b>{" "}
                  {t.goneBody}
                </p>
                <button
                  type="button"
                  onClick={() => setOpen(null)}
                  className={BUTTON_CLASS.outline}
                >
                  {copy.common.close}
                </button>
              </div>
            </div>
          ) : null}
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
                    panel={openOf(group.id)}
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
                    onOpen={(kind) =>
                      setOpen({
                        id: group.id,
                        name: group.name,
                        kind,
                        leaving: false,
                      })
                    }
                    onLeaving={(leaving) =>
                      setOpen((held) =>
                        held && held.id === group.id
                          ? { ...held, leaving }
                          : held,
                      )
                    }
                    onClose={() =>
                      setOpen((held) =>
                        held && held.id === group.id ? null : held,
                      )
                    }
                  />
                );
              })
            }
          </SortableList>
          {adding ? (
            <form
              role="listitem"
              aria-label={t.addTitle}
              aria-busy={addPending || undefined}
              className={PANEL_CLASS}
              onSubmit={(event) => {
                event.preventDefault();
                void add();
              }}
            >
              <p className="m-0 text-item-small text-muted-foreground">
                <b className="font-semibold text-foreground">{t.addTitle}</b>{" "}
                {t.addBody}
              </p>
              <div className="flex flex-wrap items-center gap-2">
                <input
                  autoFocus
                  value={newName}
                  readOnly={addPending}
                  aria-label={t.addPlaceholder}
                  placeholder={t.addPlaceholder}
                  {...addFailedProps}
                  onChange={(event) => setNewName(event.target.value)}
                  onKeyDown={(event) => {
                    if (event.key === "Escape") {
                      event.stopPropagation();
                      closeAdd();
                    }
                  }}
                  className={cn(INPUT_CLASS, "min-w-0 flex-1")}
                />
                <button
                  type="button"
                  disabled={addPending}
                  onClick={closeAdd}
                  className={BUTTON_CLASS.outline}
                >
                  {copy.common.cancel}
                </button>
                <button
                  type="submit"
                  disabled={addPending || newName.trim() === ""}
                  className={BUTTON_CLASS.primary}
                >
                  {addPending ? t.adding : t.addConfirm}
                </button>
              </div>
              <div className="empty:hidden">
                <WriteError scope="group-add" id="bs-group-add-error" />
              </div>
            </form>
          ) : null}
        </div>
        <p className={cn(HINT_CLASS, "mx-0.5 mt-2.5 mb-0")}>{t.footnote}</p>
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
  panel,
  others,
  defaultTargetId,
  onOpen,
  onLeaving,
  onClose,
}: {
  group: BoardSettingsGroupDto;
  last: boolean;
  /** The panel open under this row, if any. */
  panel: OpenPanel["kind"] | null;
  others: readonly BoardSettingsGroupDto[];
  /** The group the items move to unless another is chosen: the next one, else the previous. */
  defaultTargetId: string;
  onOpen: (kind: OpenPanel["kind"]) => void;
  /** True while this person's own archive or delete of the group runs, so its leaving is not news. */
  onLeaving: (leaving: boolean) => void;
  onClose: () => void;
}) {
  const { copy, settings, actions, run } = useSettings();
  const t = copy.groups;
  const keepsItems =
    settings.unavailable?.nonEmptyGroupDelete === true && group.itemCount > 0;
  const renaming = panel === "rename";
  const deleting = panel === "delete";
  const archiving = panel === "archive";
  const [name, setName] = useState(group.name);
  const [renamePending, setRenamePending] = useState(false);
  const [moveTo, setMoveTo] = useState(defaultTargetId);
  // A target someone else removed meanwhile is no target: the person picks again.
  const target = others.some((other) => other.id === moveTo) ? moveTo : "";
  const confirmId = useId();
  const archiveId = useId();
  const renameScope = `group:${group.id}`;
  const renameErrorId = `${confirmId}-rename-error`;
  const renameFailedProps = useWriteErrorProps(renameScope, renameErrorId);
  const needsTarget = group.itemCount > 0;

  async function commitRename() {
    if (renamePending) return;
    const next = name.trim();
    if (next && next !== group.name) {
      // A failed rename stays open with the typed name; the error offers Retry.
      setRenamePending(true);
      const result = await run(
        () => actions.updateGroup(group.id, { name: next }),
        renameScope,
      );
      setRenamePending(false);
      if (result.ok) onClose();
      return;
    }
    // The same name (or only spaces around it) is no change: nothing is sent.
    setName(group.name);
    onClose();
  }

  async function confirmArchive() {
    onLeaving(true);
    const result = await run(() => actions.archiveGroup(group.id), renameScope);
    if (result.ok) onClose();
    else onLeaving(false);
  }

  async function confirmDelete() {
    onLeaving(true);
    const result = await run(
      actions.deleteGroup(group.id, needsTarget ? target : null),
    );
    if (result.ok) onClose();
    else onLeaving(false);
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
                readOnly={renamePending}
                aria-busy={renamePending || undefined}
                aria-label={t.renameField}
                {...renameFailedProps}
                onChange={(event) => setName(event.target.value)}
                onBlur={() => void commitRename()}
                onKeyDown={(event) => {
                  if (event.key === "Enter") event.currentTarget.blur();
                  if (event.key === "Escape" && !renamePending) {
                    event.stopPropagation();
                    setName(group.name);
                    onClose();
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
                onClick={() => {
                  setName(group.name);
                  onOpen("rename");
                }}
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
                  <DropdownMenuItem onSelect={() => onOpen("archive")}>
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
                      onSelect={() => onOpen("delete")}
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
        {renamePending ? (
          <p
            role="status"
            className="m-0 mt-1 pb-1 text-crumb text-meta-foreground"
          >
            {t.renaming}
          </p>
        ) : (
          <WriteError scope={renameScope} id={renameErrorId} />
        )}
      </div>
      {archiving ? (
        <div
          id={archiveId}
          role="listitem"
          aria-label={fillCopy(t.archiveQuestion, { name: group.name })}
          className={PANEL_CLASS}
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
              onClick={onClose}
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
          className={PANEL_CLASS}
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
                <Select value={target} onValueChange={setMoveTo}>
                  <SelectTrigger
                    id={`${confirmId}-to`}
                    className="w-[220px] data-[size=default]:h-[34px]"
                  >
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
              onClick={onClose}
              className={BUTTON_CLASS.outline}
            >
              {copy.common.cancel}
            </button>
            <button
              type="button"
              disabled={needsTarget && target === ""}
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

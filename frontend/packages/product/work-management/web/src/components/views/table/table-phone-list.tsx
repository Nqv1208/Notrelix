import { useState, type ReactNode } from "react";
import {
  Calendar,
  Check,
  ChevronDown,
  ChevronRight,
  ExternalLink,
  Flag,
  MoreHorizontal,
  User,
  X,
} from "lucide-react";
import { cn, Sheet, SheetContent, SheetTitle } from "@notrelix/ui-web";
import { PriorityGlyph } from "../../shared/priority-glyph";
import { identityMarkClass } from "../../shared/item-display";
import {
  fillTableCopy,
  pluralTableCopy,
  type MainTableCopy,
} from "./main-table.copy";
import type { TableEditors } from "./table-cell-editors";
import { TableDuePicker } from "./table-due-picker";
import type {
  TableGroupModel,
  TableRowModel,
  TableStatusOption,
} from "./table-model";
import { StatusDot } from "./table-parts";
import { TableEmpty, TableNoResults } from "./table-states";
import type { TableRowActions } from "./table-row";

/** A group shows this many rows, then "Xem thêm N việc" (the mobile Board artboard, list view). */
const PHONE_WINDOW = 3;

const DUE_CLASS: Readonly<Record<TableRowModel["due"]["kind"], string>> = {
  overdue: "font-semibold text-danger-text",
  today: "font-semibold text-warning-subtle-foreground",
  date: "text-meta-foreground",
  none: "text-meta-foreground",
};
const PRIORITY_CLASS = {
  urgent: "text-danger-text",
  high: "text-priority-high-text",
  medium: "text-muted-foreground",
  low: "text-meta-foreground",
} as const;

type SheetView = "menu" | "assign" | "due" | "priority";

/** A 60 px row of a sheet: icon tile, label, and what it holds on the right. */
function SheetRow({
  icon,
  label,
  end,
  danger,
  onClick,
}: {
  icon?: ReactNode;
  label: string;
  end?: ReactNode;
  danger?: boolean;
  onClick: () => void;
}) {
  return (
    <button
      type="button"
      onClick={onClick}
      className={cn(
        "flex min-h-15 w-full cursor-pointer items-center gap-3 border-0 bg-transparent px-5 text-left text-[15px] font-semibold text-foreground hover:bg-state-hover focus-visible:outline-2 focus-visible:-outline-offset-2 focus-visible:outline-ring",
        danger && "text-danger-text",
      )}
    >
      {icon ? (
        <span
          aria-hidden="true"
          className="grid size-9 flex-none place-items-center rounded-[10px] border border-border bg-background text-muted-foreground"
        >
          {icon}
        </span>
      ) : null}
      <span className="min-w-0 flex-1 truncate">{label}</span>
      {end ? (
        <span className="inline-flex flex-none items-center gap-1.5 text-compact font-normal text-meta-foreground">
          {end}
        </span>
      ) : null}
    </button>
  );
}

/** A 52 px choice of a list sheet: a mark, the label and the tick on the current one. */
function ChoiceRow({
  mark,
  label,
  current,
  currentLabel,
  onClick,
}: {
  mark: ReactNode;
  label: string;
  current: boolean;
  currentLabel?: string;
  onClick: () => void;
}) {
  return (
    <button
      type="button"
      aria-pressed={current}
      onClick={onClick}
      className="flex min-h-13 w-full cursor-pointer items-center gap-3 border-0 bg-transparent px-5 text-left text-[15px] text-foreground hover:bg-state-hover focus-visible:outline-2 focus-visible:-outline-offset-2 focus-visible:outline-ring aria-pressed:font-semibold"
    >
      <span className="grid w-9 flex-none place-items-center">{mark}</span>
      <span className="min-w-0 flex-1 truncate">{label}</span>
      {current ? (
        <span className="inline-flex items-center gap-1.5 text-compact font-medium text-state-selected-foreground">
          {currentLabel}
          <Check className="size-4" aria-hidden="true" />
        </span>
      ) : null}
    </button>
  );
}

/**
 * The phone drawing of the board table (`Final-M-Board`, list view): one card per group, a row per item with a
 * check circle to mark it done, the title over "key · assignee", the due text and priority, and a "…" menu. The
 * menu opens a bottom sheet with the status list, "Giao cho…", "Đổi hạn…", the priority and the row's actions.
 * Every change goes through the same callbacks as the grid.
 */
export function TablePhoneList({
  copy,
  groups,
  statusOptions,
  collapsedGroupIds,
  canEdit,
  editors,
  rowActions,
  noResults,
  noResultsAssignee,
  hideDone,
  onClearFilter,
  onGroupToggle,
  onOpenItem,
  onStatusChange,
}: {
  copy: MainTableCopy;
  groups: readonly TableGroupModel[];
  statusOptions: readonly TableStatusOption[];
  collapsedGroupIds: ReadonlySet<string>;
  canEdit: boolean;
  editors?: TableEditors;
  rowActions?: {
    readonly onDuplicate?: (id: string) => void;
    readonly onCopyLink: (id: string) => void;
    readonly onArchive?: (id: string) => void;
    readonly onDelete?: (id: string) => void;
    readonly onMoveToBoard?: (id: string) => void;
  };
  noResults: boolean;
  noResultsAssignee?: string;
  hideDone: boolean;
  onClearFilter: () => void;
  onGroupToggle: (groupId: string) => void;
  onOpenItem: (id: string) => void;
  onStatusChange: (id: string, optionId: string) => void;
}) {
  const t = copy.phone;
  const [shown, setShown] = useState<Record<string, number>>({});
  const [sheet, setSheet] = useState<{ rowId: string; view: SheetView } | null>(
    null,
  );
  const rows = groups.flatMap((group) => group.rows);
  const target = sheet ? rows.find((row) => row.id === sheet.rowId) : undefined;
  const doneOption = statusOptions.find((option) => option.category === "done");
  const openOption =
    statusOptions.find((option) => option.category === "todo") ??
    statusOptions.find((option) => option.category !== "done");
  const close = () => setSheet(null);
  const keyOf = (row: TableRowModel) => row.key ?? row.title;
  const canTick = canEdit && Boolean(doneOption);

  const actions: TableRowActions | undefined = rowActions
    ? {
        onCopyLink: () => target && rowActions.onCopyLink(target.id),
        ...(rowActions.onDuplicate
          ? { onDuplicate: () => target && rowActions.onDuplicate?.(target.id) }
          : {}),
        ...(rowActions.onMoveToBoard
          ? {
              onMoveToBoard: () =>
                target && rowActions.onMoveToBoard?.(target.id),
            }
          : {}),
        ...(rowActions.onArchive
          ? { onArchive: () => target && rowActions.onArchive?.(target.id) }
          : {}),
        ...(rowActions.onDelete
          ? { onDelete: () => target && rowActions.onDelete?.(target.id) }
          : {}),
      }
    : undefined;

  const body = (row: TableRowModel, view: SheetView): ReactNode => {
    if (view === "assign" && editors)
      return (
        <div role="group" aria-label={t.assignSheet} className="flex flex-col">
          <ChoiceRow
            mark={
              <span
                aria-hidden="true"
                className="grid size-7 place-items-center rounded-full text-[10.5px] font-bold text-meta-foreground shadow-[inset_0_0_0_1.5px_var(--color-line-strong)]"
              >
                +
              </span>
            }
            label={copy.row.unassigned}
            current={row.assignee === null}
            currentLabel={t.current}
            onClick={() => {
              close();
              editors.onAssignee(row.id, null);
            }}
          />
          {editors.members.map((member) => (
            <ChoiceRow
              key={member.userId}
              mark={
                <span
                  aria-hidden="true"
                  className={cn(
                    "grid size-7 place-items-center rounded-full text-[10.5px] font-bold",
                    identityMarkClass(member.userId),
                  )}
                >
                  {member.initials}
                </span>
              }
              label={member.name}
              current={row.assignee?.userId === member.userId}
              currentLabel={t.current}
              onClick={() => {
                close();
                editors.onAssignee(row.id, member.userId);
              }}
            />
          ))}
        </div>
      );
    if (view === "due" && editors)
      return (
        <div className="px-4 pb-2">
          <TableDuePicker
            copy={copy}
            dueAt={row.dueAt}
            onPick={(day, time) => {
              close();
              editors.onDue(row.id, day, time);
            }}
          />
        </div>
      );
    if (view === "priority" && editors)
      return (
        <div
          role="group"
          aria-label={t.prioritySheet}
          className="flex flex-col"
        >
          {editors.priorities.map((option) => (
            <ChoiceRow
              key={option.id}
              mark={
                <Flag
                  className="size-4 text-muted-foreground"
                  aria-hidden="true"
                />
              }
              label={option.label}
              current={
                row.priority !== null &&
                copy.row.priority[row.priority] === option.label
              }
              currentLabel={t.current}
              onClick={() => {
                close();
                editors.onPriority(row.id, option.id);
              }}
            />
          ))}
        </div>
      );
    return (
      <div className="flex flex-col">
        {canEdit ? (
          <>
            <p className="m-0 px-5 pt-1.5 pb-1 text-compact font-semibold text-meta-foreground">
              {t.moveTo}
            </p>
            {statusOptions.map((option) => (
              <ChoiceRow
                key={option.id}
                mark={
                  <StatusDot category={option.category} className="size-3" />
                }
                label={option.name}
                current={row.status?.id === option.id}
                currentLabel={t.current}
                onClick={() => {
                  close();
                  if (option.id !== row.status?.id)
                    onStatusChange(row.id, option.id);
                }}
              />
            ))}
            {editors ? (
              <>
                <div className="mx-5 my-1.5 h-px bg-border" />
                <SheetRow
                  icon={<User className="size-4" />}
                  label={t.assign}
                  end={
                    row.assignee ? (
                      <>
                        <span
                          aria-hidden="true"
                          className={cn(
                            "grid size-7 place-items-center rounded-full text-[10.5px] font-bold",
                            identityMarkClass(row.assignee.userId),
                          )}
                        >
                          {row.assignee.initials}
                        </span>
                        {row.assignee.name}
                      </>
                    ) : (
                      copy.row.unassigned
                    )
                  }
                  onClick={() => setSheet({ rowId: row.id, view: "assign" })}
                />
                <SheetRow
                  icon={<Calendar className="size-4" />}
                  label={t.due}
                  end={
                    <span className={DUE_CLASS[row.due.kind]}>
                      {row.due.text}
                    </span>
                  }
                  onClick={() => setSheet({ rowId: row.id, view: "due" })}
                />
                {editors.priorities.length > 0 ? (
                  <SheetRow
                    icon={<Flag className="size-4" />}
                    label={t.priority}
                    end={
                      row.priority
                        ? copy.row.priority[row.priority]
                        : copy.row.noPriority
                    }
                    onClick={() =>
                      setSheet({ rowId: row.id, view: "priority" })
                    }
                  />
                ) : null}
              </>
            ) : null}
          </>
        ) : (
          <p className="m-0 mx-5 mb-2 rounded-[10px] bg-state-hover px-3 py-2.5 text-compact text-muted-foreground">
            {t.readOnly}
          </p>
        )}
        <div className="mx-5 my-1.5 h-px bg-border" />
        <SheetRow
          icon={<ExternalLink className="size-4" />}
          label={t.openFull}
          end={<ChevronRight className="size-4" aria-hidden="true" />}
          onClick={() => {
            close();
            onOpenItem(row.id);
          }}
        />
        {actions?.onDuplicate ? (
          <SheetRow
            label={copy.row.menu.duplicate}
            onClick={() => {
              close();
              actions.onDuplicate?.();
            }}
          />
        ) : null}
        {actions ? (
          <SheetRow
            label={copy.row.menu.copyLink}
            onClick={() => {
              close();
              actions.onCopyLink();
            }}
          />
        ) : null}
        {actions?.onMoveToBoard ? (
          <SheetRow
            label={copy.row.menu.moveToBoard}
            onClick={() => {
              close();
              actions.onMoveToBoard?.();
            }}
          />
        ) : null}
        {actions?.onArchive ? (
          <SheetRow
            label={copy.row.menu.archive}
            onClick={() => {
              close();
              actions.onArchive?.();
            }}
          />
        ) : null}
        {actions?.onDelete ? (
          <SheetRow
            danger
            label={copy.row.menu.delete}
            onClick={() => {
              close();
              actions.onDelete?.();
            }}
          />
        ) : null}
      </div>
    );
  };

  const sheetTitle = (row: TableRowModel, view: SheetView) =>
    view === "assign"
      ? t.assignSheet
      : view === "due"
        ? copy.duePicker.heading
        : view === "priority"
          ? t.prioritySheet
          : row.title;

  return (
    <div
      data-slot="table-phone-list"
      className="flex flex-col gap-3.5 px-4 pt-1 pb-6"
    >
      {noResults ? (
        <TableNoResults
          copy={copy}
          assigneeName={noResultsAssignee}
          hidesDone={hideDone}
          onClear={onClearFilter}
        />
      ) : groups.length === 0 ? (
        <TableEmpty copy={copy} />
      ) : null}
      {groups.map((group) => {
        const collapsed = collapsedGroupIds.has(group.id);
        const count = group.filteredTotal ?? group.rows.length;
        const visible = shown[group.id] ?? PHONE_WINDOW;
        const headingId = `phone-group-${group.id}`;
        return (
          <section
            key={group.id}
            aria-labelledby={headingId}
            className="flex flex-col"
          >
            <h2
              id={headingId}
              className="m-0 mb-1 flex min-h-10 items-center gap-2 text-[17px] font-semibold text-foreground"
            >
              <StatusDot category={group.category} className="size-2.5" />
              {group.name}
              <span className="text-[15px] font-medium text-meta-foreground">
                {count}
              </span>
              {group.overdueCount > 0 ? (
                <span className="ml-auto text-compact font-semibold text-danger-text">
                  {pluralTableCopy(
                    { one: t.overdueGroup_one, other: t.overdueGroup_other },
                    group.overdueCount,
                  )}
                </span>
              ) : null}
            </h2>
            {collapsed ? (
              <button
                type="button"
                aria-expanded="false"
                aria-label={fillTableCopy(t.openGroup, { name: group.name })}
                onClick={() => onGroupToggle(group.id)}
                className="flex min-h-13 w-full cursor-pointer items-center gap-2.5 rounded-[14px] border border-border bg-popover pr-3 pl-4 text-left text-[15px] text-foreground dark:bg-card"
              >
                <StatusDot category={group.category} className="size-2.5" />
                <b className="font-semibold">{group.name}</b>
                <span className="text-sm text-meta-foreground">
                  {fillTableCopy(t.closedGroup, { count })}
                </span>
                <ChevronRight
                  className="ml-auto size-4 text-meta-foreground"
                  aria-hidden="true"
                />
              </button>
            ) : (
              <ul className="m-0 list-none overflow-hidden rounded-[14px] border border-border bg-popover p-0 dark:bg-card">
                {group.rows.slice(0, visible).map((row) => {
                  const done = row.status?.category === "done";
                  return (
                    <li
                      key={row.id}
                      data-row-id={row.id}
                      className="flex items-start border-t border-border py-1.5 pl-0.5 first:border-t-0"
                    >
                      <button
                        type="button"
                        disabled={!canTick}
                        title={
                          canEdit && !doneOption ? t.noDoneStatus : undefined
                        }
                        aria-label={fillTableCopy(
                          done ? t.markOpen : t.markDone,
                          { key: keyOf(row) },
                        )}
                        onClick={() => {
                          const next = done ? openOption : doneOption;
                          if (next) onStatusChange(row.id, next.id);
                        }}
                        className="grid size-11 flex-none cursor-pointer place-items-center rounded-xl border-0 bg-transparent p-0 focus-visible:outline-2 focus-visible:outline-ring disabled:cursor-default disabled:opacity-60"
                      >
                        <span
                          aria-hidden="true"
                          className={cn(
                            "box-border grid size-5.5 place-items-center rounded-full border-[1.75px]",
                            done
                              ? "border-work-status-done bg-work-status-done text-primary-foreground"
                              : "border-line-strong",
                          )}
                        >
                          {done ? (
                            <Check className="size-3.5" strokeWidth={3} />
                          ) : null}
                        </span>
                      </button>
                      <button
                        type="button"
                        data-row-open=""
                        onClick={() => onOpenItem(row.id)}
                        className="flex min-h-11 min-w-0 flex-1 cursor-pointer items-start gap-2.5 rounded-lg border-0 bg-transparent py-[5px] text-left text-foreground focus-visible:outline-2 focus-visible:outline-ring"
                      >
                        <span className="min-w-0 flex-1">
                          <span className="line-clamp-2 text-[15px] leading-[1.36] font-medium">
                            {row.title}
                          </span>
                          <span className="mt-[3px] flex min-w-0 items-center gap-1.5 text-[13px] whitespace-nowrap text-meta-foreground">
                            {row.key ? (
                              <span className="flex-none font-mono text-xs">
                                {row.key}
                              </span>
                            ) : null}
                            {row.key && row.assignee ? (
                              <span aria-hidden="true">·</span>
                            ) : null}
                            {row.assignee ? (
                              <span className="min-w-0 truncate">
                                {row.assignee.name}
                              </span>
                            ) : null}
                          </span>
                        </span>
                        <span
                          className={cn(
                            "flex w-17.5 flex-none flex-col items-end gap-1 pt-px text-right text-[13px] leading-[1.3] tabular-nums",
                            DUE_CLASS[row.due.kind],
                          )}
                        >
                          {row.due.kind === "none" ? null : row.due.text}
                          {row.priority === "urgent" ||
                          row.priority === "high" ? (
                            <span
                              role="img"
                              aria-label={copy.row.priority[row.priority]}
                              className={cn(
                                "inline-flex items-center gap-[3px] text-xs font-semibold",
                                PRIORITY_CLASS[row.priority],
                              )}
                            >
                              <PriorityGlyph level={row.priority} />
                              {copy.row.priority[row.priority]}
                            </span>
                          ) : null}
                        </span>
                      </button>
                      <button
                        type="button"
                        aria-haspopup="dialog"
                        aria-label={fillTableCopy(t.more, { key: keyOf(row) })}
                        onClick={() =>
                          setSheet({ rowId: row.id, view: "menu" })
                        }
                        className="grid size-11 flex-none cursor-pointer place-items-center rounded-xl border-0 bg-transparent p-0 text-meta-foreground focus-visible:outline-2 focus-visible:outline-ring"
                      >
                        <MoreHorizontal className="size-5" aria-hidden="true" />
                      </button>
                    </li>
                  );
                })}
                {visible < group.rows.length ? (
                  <li className="border-t border-border">
                    <button
                      type="button"
                      onClick={() =>
                        setShown((current) => ({
                          ...current,
                          [group.id]: group.rows.length,
                        }))
                      }
                      className="flex min-h-12 w-full cursor-pointer items-center justify-center gap-0.5 border-0 bg-transparent text-sm font-medium text-state-selected-foreground focus-visible:outline-2 focus-visible:-outline-offset-2 focus-visible:outline-ring"
                    >
                      {pluralTableCopy(
                        { one: t.showMore_one, other: t.showMore_other },
                        group.rows.length - visible,
                      )}
                      <ChevronDown className="size-4" aria-hidden="true" />
                    </button>
                  </li>
                ) : null}
              </ul>
            )}
          </section>
        );
      })}
      <Sheet
        open={target !== undefined}
        onOpenChange={(open) => !open && close()}
      >
        <SheetContent
          side="bottom"
          showCloseButton={false}
          aria-describedby={undefined}
          className="max-h-[94%] gap-0 overflow-y-auto rounded-t-[14px] bg-popover px-0 pt-1.5 pb-8 text-popover-foreground data-[side=bottom]:border-t-0"
        >
          {target && sheet ? (
            <>
              <div
                aria-hidden="true"
                className="mx-auto mb-0.5 h-1.25 w-9 rounded-[3px] bg-line-strong"
              />
              <div className="flex min-h-13 items-start gap-2 pr-2 pl-5">
                <div className="min-w-0 flex-1 pt-1.5">
                  {sheet.view === "menu" ? (
                    <span className="block text-xs text-meta-foreground">
                      <span className="font-mono">{target.key}</span>
                      {target.status ? ` · ${target.status.name}` : null}
                    </span>
                  ) : null}
                  <SheetTitle className="m-0 text-[16px] leading-[1.35] font-semibold">
                    {sheetTitle(target, sheet.view)}
                  </SheetTitle>
                </div>
                <button
                  type="button"
                  aria-label={t.close}
                  onClick={close}
                  className="grid size-11 flex-none cursor-pointer place-items-center rounded-xl border-0 bg-transparent p-0 text-foreground focus-visible:outline-2 focus-visible:outline-ring"
                >
                  <X className="size-5" aria-hidden="true" />
                </button>
              </div>
              {body(target, sheet.view)}
            </>
          ) : null}
        </SheetContent>
      </Sheet>
    </div>
  );
}

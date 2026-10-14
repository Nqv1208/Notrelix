import {
  Bell,
  CalendarDays,
  ChartNoAxesGantt,
  ChevronDown,
  Flag,
  Lock,
  SquareKanban,
  Table2,
} from "lucide-react";
import type { ReactNode } from "react";
import { cn } from "@notrelix/ui-web";
import type {
  Board,
  CardDetail,
  UpdateFieldValueInput,
} from "@notrelix/work-management-core";
import {
  ITEM_LABEL_TONE_CLASS,
  formatItemDate,
  itemLabelTone,
} from "../shared/item-display";
import {
  describeSheetDue,
  type SheetDueTone,
} from "../card-detail/item-sheet-format";
import {
  fillSheetCopy,
  type ItemSheetCopy,
} from "../card-detail/item-sheet.copy";
import {
  ItemAssigneePopover,
  ItemDuePopover,
  ItemOptionMenu,
  PRIORITY_CLASS,
  STATUS_DOT_CLASS,
  STATUS_LANE_CLASS,
  describeItemStatus,
  findItemFields,
  isItemDone,
} from "../card-detail/item-field-editors";
import type {
  TaskDetailCallbacks,
  TaskDetailCapabilities,
} from "../card-detail/task-detail-types";
import type { ItemPageCopy } from "./item-page.copy";
import {
  formatDateTime,
  formatEditedAt,
  formatFullDate,
} from "./item-page-format";
import {
  BUTTON_SM_VARIANT_CLASS,
  DisabledControl,
  PersonMark,
} from "./item-page-parts";
import type {
  ItemPageConflict,
  ItemPageNavigation,
  ItemPageProperty,
  ItemPageViewKind,
} from "./item-page-types";

const DUE_TONE_CLASS: Readonly<Record<SheetDueTone, string>> = {
  overdue: "font-semibold text-danger-text",
  today: "text-warning-text",
  upcoming: "",
  done: "text-meta-foreground",
};

const DUE_TONE_WEIGHT_CLASS: Readonly<Record<SheetDueTone, string>> = {
  overdue: "font-semibold",
  today: "",
  upcoming: "",
  done: "",
};

/** A property value button (mockup `.pv`): the value box overhangs the column by 8 px. */
const VALUE_CLASS =
  "box-content flex min-h-8 w-full -ml-2 items-center gap-[7px] rounded-[7px] border-0 bg-transparent px-2 text-left text-item-ui font-medium text-foreground outline-none focus-visible:ring-2 focus-visible:ring-focus-ring";
/** The row whose write the server refused (mockup `.pv.conf`). */
const VALUE_CONFLICT_CLASS =
  "bg-warning-subtle font-semibold text-warning-text shadow-[inset_0_0_0_1px_var(--item-conflict-line)] hover:bg-warning-subtle";
const VALUE_ACTIVE_CLASS = "cursor-pointer hover:bg-state-hover-strong";
const VALUE_READONLY_CLASS = "cursor-default aria-disabled:cursor-default";

function PropertyValue({
  editable,
  className,
  children,
  ...rest
}: {
  editable: boolean;
  className?: string;
  children: ReactNode;
  "aria-label"?: string;
}) {
  return (
    <button
      type="button"
      {...rest}
      aria-disabled={editable ? undefined : "true"}
      className={cn(
        VALUE_CLASS,
        editable ? VALUE_ACTIVE_CLASS : VALUE_READONLY_CLASS,
        className,
      )}
    >
      {children}
    </button>
  );
}

function RailHeading({
  children,
  action,
}: {
  children: ReactNode;
  action?: ReactNode;
}) {
  return (
    <div className="mb-1 flex items-center justify-between text-item-note font-semibold text-meta-foreground">
      {children}
      {action}
    </div>
  );
}

/** Follow button and the faces of the people who follow (no producer yet: disabled). */
function FollowRow({ card, copy }: { card: CardDetail; copy: ItemPageCopy }) {
  const t = copy.rail;
  const watchers = card.watchers;
  const faces = watchers.slice(0, 3);
  const more = watchers.length - faces.length;
  return (
    <div className="flex items-center gap-2">
      <DisabledControl reason={t.followUnavailable}>
        <button
          type="button"
          aria-disabled="true"
          aria-pressed={card.isWatched}
          className={BUTTON_SM_VARIANT_CLASS.outline}
        >
          <Bell aria-hidden="true" className="size-3.5" />
          {card.isWatched ? t.following : t.follow}
        </button>
      </DisabledControl>
      {watchers.length > 0 ? (
        <div
          role="group"
          aria-label={fillSheetCopy(t.watchers, { count: watchers.length })}
          className="ml-auto flex"
        >
          {faces.map((person) => (
            <PersonMark
              key={person.id}
              person={person}
              title={person.name}
              className="-ml-1.5 size-6 text-mark-xs ring-2 ring-popover"
            />
          ))}
          {more > 0 ? (
            <span
              aria-hidden="true"
              className="-ml-1.5 grid size-6 place-items-center rounded-full bg-state-hover-strong text-mark-xs text-muted-foreground ring-2 ring-popover"
            >
              +{more}
            </span>
          ) : null}
        </div>
      ) : null}
    </div>
  );
}

export function ItemPageRail({
  board,
  card,
  capabilities,
  callbacks,
  navigation,
  copy,
  sheetCopy,
  now,
  timeZone,
  creatorName,
  conflict,
}: {
  board: Board;
  card: CardDetail;
  capabilities: TaskDetailCapabilities;
  callbacks: TaskDetailCallbacks;
  navigation: ItemPageNavigation;
  copy: ItemPageCopy;
  sheetCopy: ItemSheetCopy;
  now: Date;
  timeZone?: string;
  creatorName?: string | null;
  conflict?: ItemPageConflict | null;
}) {
  const t = copy.rail;
  const p = sheetCopy.properties;
  const editable = capabilities.canEditFields;
  const fields = findItemFields(board);
  const update = (
    field: { id: string } | undefined,
    value: UpdateFieldValueInput["value"],
  ) => {
    if (!field || !editable) return;
    callbacks.onUpdateFieldValue({
      cardId: card.id,
      fieldDefinitionId: field.id,
      value,
    });
  };

  const { option: statusOption, category } = describeItemStatus(
    card,
    fields.status,
  );
  const [firstMember, ...otherMembers] = card.members;
  const dueAt = card.dueAt ?? card.dueDate ?? null;
  const due = describeSheetDue(
    dueAt,
    now,
    isItemDone(card),
    sheetCopy,
    timeZone,
  );
  const conflictOn = (property: ItemPageProperty) =>
    conflict && conflict.property === property ? conflict : null;
  const dueEditable = editable && (!!fields.due || !!callbacks.onUpdateItem);
  const changeDue = (value: string | null) => {
    if (fields.due) update(fields.due, value);
    else callbacks.onUpdateItem?.(card.id, { dueDate: value });
  };
  const priority = card.priority;
  const label = card.labels[0];
  const progress = card.childProgress;
  const percent =
    progress && progress.total > 0
      ? Math.round((progress.done / progress.total) * 100)
      : null;
  const startDate = card.startDate ?? null;
  const conflictText = (entry: ItemPageConflict | null) =>
    entry
      ? fillSheetCopy(copy.conflict.changed, {
          mine: entry.mine,
          theirs: entry.theirs,
        })
      : null;

  return (
    <aside
      aria-label={t.label}
      className="flex w-full shrink-0 flex-col gap-5 border-t border-line bg-popover px-4.5 pt-4.5 pb-7 lg:w-80 lg:overflow-y-auto lg:border-t-0 lg:border-l"
    >
      <FollowRow card={card} copy={copy} />

      <div>
        <RailHeading
          action={
            editable ? (
              <DisabledControl reason={t.manageFieldsUnavailable}>
                <button
                  type="button"
                  aria-disabled="true"
                  className="border-0 bg-transparent p-0 text-item-note font-medium text-brand-subtle-foreground"
                >
                  {t.manageFields}
                </button>
              </DisabledControl>
            ) : null
          }
        >
          {t.properties}
        </RailHeading>
        <dl className="m-0 grid grid-cols-[118px_minmax(0,1fr)] items-center gap-y-0.5">
          <PropertyRow label={t.status}>
            <ItemOptionMenu
              field={fields.status}
              readOnly={!editable}
              onSelect={(optionId) => update(fields.status, optionId)}
            >
              <PropertyValue
                editable={editable && !!fields.status}
                aria-label={t.status}
                className={
                  conflictOn("status")
                    ? VALUE_CONFLICT_CLASS
                    : cn(
                        "w-auto font-semibold",
                        category && STATUS_LANE_CLASS[category],
                      )
                }
              >
                {conflictOn("status") ? (
                  conflictText(conflictOn("status"))
                ) : (
                  <>
                    <span
                      aria-hidden="true"
                      className={cn(
                        "size-2.25 shrink-0 rounded-full",
                        category
                          ? STATUS_DOT_CLASS[category]
                          : "bg-muted-foreground",
                      )}
                    />
                    {statusOption?.label ?? p.noStatus}
                    <ChevronDown
                      aria-hidden="true"
                      className="size-3.25 shrink-0"
                    />
                  </>
                )}
              </PropertyValue>
            </ItemOptionMenu>
          </PropertyRow>

          <PropertyRow label={t.assignee}>
            <ItemAssigneePopover
              board={board}
              card={card}
              label={t.assignee}
              readOnly={!editable || !fields.person}
              onChange={(userIds) => update(fields.person, userIds)}
            >
              <PropertyValue
                editable={editable && !!fields.person}
                aria-label={t.assignee}
                className={
                  conflictOn("assignee")
                    ? VALUE_CONFLICT_CLASS
                    : firstMember
                      ? undefined
                      : "font-normal text-meta-foreground"
                }
              >
                {conflictOn("assignee") ? (
                  conflictText(conflictOn("assignee"))
                ) : firstMember ? (
                  <>
                    <PersonMark
                      person={firstMember}
                      className="size-5 text-avatar-sm"
                    />
                    <span className="truncate">
                      {otherMembers.length > 0
                        ? fillSheetCopy(p.assigneeMore, {
                            name: firstMember.name,
                            count: otherMembers.length,
                          })
                        : firstMember.name}
                    </span>
                  </>
                ) : (
                  p.unassigned
                )}
              </PropertyValue>
            </ItemAssigneePopover>
          </PropertyRow>

          <PropertyRow label={t.startDate}>
            {editable && callbacks.onUpdateItem ? (
              <ItemDuePopover
                dueAt={startDate}
                clearLabel={p.clearDue}
                readOnly={false}
                onChange={(value) =>
                  callbacks.onUpdateItem?.(card.id, { startDate: value })
                }
              >
                <PropertyValue
                  editable
                  aria-label={t.startDate}
                  className={
                    conflictOn("startDate") ? VALUE_CONFLICT_CLASS : undefined
                  }
                >
                  {conflictOn("startDate")
                    ? conflictText(conflictOn("startDate"))
                    : (formatFullDate(startDate, timeZone) ?? t.noStartDate)}
                </PropertyValue>
              </ItemDuePopover>
            ) : editable ? (
              <DisabledControl reason={t.startDateUnavailable}>
                <PropertyValue editable={false} aria-label={t.startDate}>
                  {formatFullDate(startDate, timeZone) ?? t.noStartDate}
                </PropertyValue>
              </DisabledControl>
            ) : (
              <PropertyValue editable={false} aria-label={t.startDate}>
                {formatFullDate(startDate, timeZone) ?? t.noStartDate}
              </PropertyValue>
            )}
          </PropertyRow>

          <PropertyRow label={t.due}>
            <ItemDuePopover
              dueAt={dueAt}
              clearLabel={p.clearDue}
              readOnly={!dueEditable}
              onChange={changeDue}
            >
              <PropertyValue
                editable={dueEditable}
                aria-label={t.due}
                className={
                  conflictOn("due")
                    ? VALUE_CONFLICT_CLASS
                    : due
                      ? // A disabled value keeps the ink colour (mockup `.pv:disabled`).
                        dueEditable
                        ? DUE_TONE_CLASS[due.tone]
                        : DUE_TONE_WEIGHT_CLASS[due.tone]
                      : "font-normal text-meta-foreground"
                }
              >
                {conflictOn("due")
                  ? conflictText(conflictOn("due"))
                  : (due?.text ?? p.noDue)}
              </PropertyValue>
            </ItemDuePopover>
          </PropertyRow>

          <PropertyRow label={t.priority}>
            <ItemOptionMenu
              field={fields.priority}
              readOnly={!editable}
              onSelect={(optionId) => update(fields.priority, optionId)}
            >
              <PropertyValue
                editable={editable && !!fields.priority}
                aria-label={t.priority}
                className={
                  conflictOn("priority") ? VALUE_CONFLICT_CLASS : undefined
                }
              >
                {conflictOn("priority") ? (
                  conflictText(conflictOn("priority"))
                ) : priority ? (
                  <span
                    className={cn(
                      "inline-flex items-center gap-1 text-item-small font-semibold",
                      PRIORITY_CLASS[priority],
                    )}
                  >
                    <Flag aria-hidden="true" className="size-3.25" />
                    {p.priorities[priority]}
                  </span>
                ) : (
                  <span className="font-normal text-meta-foreground">
                    {p.noPriority}
                  </span>
                )}
              </PropertyValue>
            </ItemOptionMenu>
          </PropertyRow>

          {label ? (
            <PropertyRow label={t.labels}>
              <PropertyValue editable={false} aria-label={t.labels}>
                <span
                  className={cn(
                    "inline-flex h-5.5 items-center rounded-[6px] px-2 text-micro font-semibold",
                    ITEM_LABEL_TONE_CLASS[itemLabelTone(label)],
                  )}
                >
                  {label.name}
                </span>
              </PropertyValue>
            </PropertyRow>
          ) : null}

          {progress && percent !== null ? (
            <PropertyRow
              label={
                <>
                  {t.progress}
                  <Lock
                    aria-label={t.computed}
                    role="img"
                    className="size-3 shrink-0"
                  />
                </>
              }
            >
              <span
                className={cn(VALUE_CLASS, "cursor-default")}
                data-testid="item-page-progress"
              >
                <span className="h-1.25 w-15 flex-none overflow-hidden rounded-[3px] bg-state-hover-strong">
                  <span
                    className="block h-full bg-primary"
                    style={{ width: `${percent}%` }}
                  />
                </span>
                {percent}%
                <span className="ml-auto text-[11px] leading-[1.45] font-normal text-meta-foreground">
                  {fillSheetCopy(t.progressRollup, progress)}
                </span>
              </span>
            </PropertyRow>
          ) : null}
        </dl>
      </div>

      <div className="h-px bg-line" />

      <ViewsList
        card={card}
        statusLabel={statusOption?.label ?? ""}
        navigation={navigation}
        copy={copy}
        now={now}
        timeZone={timeZone}
      />

      <div className="h-px bg-line" />

      <div>
        <RailHeading>{copy.system.heading}</RailHeading>
        <dl className="m-0 grid grid-cols-[118px_minmax(0,1fr)] gap-y-2 text-crumb">
          {card.itemKey ? (
            <SystemRow label={copy.system.key} mono>
              {card.itemKey}
            </SystemRow>
          ) : null}
          {creatorName ? (
            <SystemRow label={copy.system.creator}>{creatorName}</SystemRow>
          ) : null}
          <SystemRow label={copy.system.createdAt}>
            {formatDateTime(card.createdAt, timeZone) ?? ""}
          </SystemRow>
          <SystemRow label={copy.system.editedAt}>
            {systemEditedText(card, now, copy, timeZone)}
          </SystemRow>
        </dl>
      </div>
    </aside>
  );
}

function systemEditedText(
  card: CardDetail,
  now: Date,
  copy: ItemPageCopy,
  timeZone?: string,
): string {
  const value = card.updatedAt ?? card.createdAt;
  const when = formatEditedAt(value, now, copy, timeZone);
  if (!when) return "";
  // "10:06 hôm nay" for the meta line, "Hôm nay, 10:06" in the system list.
  const match = /^(\d{2}:\d{2})/.exec(when);
  if (!match) return formatDateTime(value, timeZone) ?? when;
  const time = match[1] ?? "";
  const today = copy.meta.today.replace("{{time}}", time);
  return when === today
    ? fillSheetCopy(copy.system.today, { time })
    : fillSheetCopy(copy.system.yesterday, { time });
}

function PropertyRow({
  label,
  children,
}: {
  label: ReactNode;
  children: ReactNode;
}) {
  return (
    <>
      <dt className="flex min-h-8.5 items-center gap-[5px] text-crumb text-meta-foreground">
        {label}
      </dt>
      <dd className="m-0 min-w-0">{children}</dd>
    </>
  );
}

function SystemRow({
  label,
  mono,
  children,
}: {
  label: string;
  mono?: boolean;
  children: ReactNode;
}) {
  return (
    <>
      <dt className="text-meta-foreground">{label}</dt>
      <dd className={cn("m-0 text-muted-foreground", mono && "font-mono")}>
        {children}
      </dd>
    </>
  );
}

const VIEW_ICON: Readonly<Record<ItemPageViewKind, typeof Table2>> = {
  kanban: SquareKanban,
  table: Table2,
  calendar: CalendarDays,
  timeline: ChartNoAxesGantt,
};

function ViewsList({
  card,
  statusLabel,
  navigation,
  copy,
  now,
  timeZone,
}: {
  card: CardDetail;
  statusLabel: string;
  navigation: ItemPageNavigation;
  copy: ItemPageCopy;
  now: Date;
  timeZone?: string;
}) {
  const t = copy.views;
  const start = card.startDate
    ? formatItemDate(card.startDate, now, timeZone)
    : null;
  const end = card.dueAt ?? card.dueDate;
  const endText = end ? formatItemDate(end, now, timeZone) : null;
  const range =
    start || endText
      ? fillSheetCopy(t.range, {
          start: start ?? endText ?? "",
          end: endText ?? start ?? "",
        })
      : null;
  const rows: ReadonlyArray<{
    kind: ItemPageViewKind;
    label: string;
    detail: string | null;
  }> = [
    {
      kind: "kanban",
      label: t.kanban,
      detail: statusLabel
        ? fillSheetCopy(t.kanbanDetail, { status: statusLabel })
        : null,
    },
    {
      kind: "table",
      label: t.table,
      detail: statusLabel
        ? fillSheetCopy(t.tableDetail, { status: statusLabel })
        : null,
    },
    { kind: "calendar", label: t.calendar, detail: range },
    { kind: "timeline", label: t.timeline, detail: range },
  ];

  return (
    <div>
      <RailHeading>{t.heading}</RailHeading>
      {rows.map((row) => {
        const Icon = VIEW_ICON[row.kind];
        const href = navigation.viewHrefs?.[row.kind];
        const content = (
          <>
            <Icon aria-hidden="true" className="size-4 shrink-0" />
            {row.label}
            {row.detail ? (
              <span className="ml-auto text-item-note text-meta-foreground">
                {row.detail}
              </span>
            ) : null}
          </>
        );
        const className =
          "-mx-2 flex min-h-8.5 items-center gap-[9px] rounded-[7px] px-2 text-item-small text-muted-foreground hover:bg-state-hover-strong hover:text-foreground";
        return (
          <a
            key={row.kind}
            href={href}
            className={className}
            onClick={(event) => {
              if (
                !navigation.onOpenView ||
                event.button !== 0 ||
                event.metaKey ||
                event.ctrlKey ||
                event.shiftKey ||
                event.altKey
              )
                return;
              event.preventDefault();
              navigation.onOpenView(row.kind);
            }}
          >
            {content}
          </a>
        );
      })}
      <small className="mt-1.5 block text-item-note leading-normal text-meta-foreground">
        {t.hint}
      </small>
    </div>
  );
}

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
import { Popover, PopoverContent, PopoverTrigger, cn } from "@notrelix/ui-web";
import { useState } from "react";
import type {
  Board,
  CardDetail,
  FieldDefinition,
  UpdateFieldValueInput,
} from "@notrelix/work-management-core";
import {
  personInitials,
  priorityFromOptionName,
} from "@notrelix/work-management-core";
import {
  ITEM_LABEL_TONE_CLASS,
  calendarDaysFromToday,
  formatItemDate,
  itemLabelTone,
} from "../shared/item-display";
import { ItemApprovalSection } from "./item-page-approval";
import { PendingMark } from "./item-page-states";
import {
  ItemAssigneePicker,
  ItemDatePicker,
  ItemListPicker,
  PickerPopover,
  type PickerOption,
  type PickerPerson,
} from "./item-pickers";
import {
  describeSheetDue,
  type SheetDueTone,
} from "../card-detail/item-sheet-format";
import {
  fillSheetCopy,
  pluralSheetCopy,
  type ItemSheetCopy,
} from "../card-detail/item-sheet.copy";
import {
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
  TaskDetailData,
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
  ItemPagePeople,
  ItemPagePicker,
  ItemPageOffline,
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
const VALUE_ACTIVE_CLASS =
  "cursor-pointer hover:bg-state-hover-strong data-[state=open]:bg-popover data-[state=open]:shadow-[0_0_0_2px_var(--primary)]";
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

/** A property value that opens its picker; without edit access it is the value alone. */
function RailPicker({
  enabled,
  open,
  onOpenChange,
  panel,
  children,
}: {
  enabled: boolean;
  open?: boolean;
  onOpenChange?: (open: boolean) => void;
  panel: (close: () => void) => ReactNode;
  children: ReactNode;
}) {
  if (!enabled) return <>{children}</>;
  return (
    <PickerPopover trigger={children} open={open} onOpenChange={onOpenChange}>
      {panel}
    </PickerPopover>
  );
}

const PRIORITY_MARKER_CLASS: Readonly<Record<string, string>> = {
  urgent: "bg-danger",
  high: "bg-priority-high",
  medium: "bg-work-status-doing",
};

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

/** Follow button and the faces of the people who follow. */
function FollowRow({
  card,
  data,
  onToggle,
  copy,
}: {
  card: CardDetail;
  data: TaskDetailData | undefined;
  onToggle?: (watched: boolean) => void;
  copy: ItemPageCopy;
}) {
  const t = copy.rail;
  const known = data?.following !== undefined && data.following !== null;
  const following = known ? data?.following === true : card.isWatched;
  const people: readonly {
    id: string;
    userId: string;
    initials: string;
    name: string;
  }[] = data?.followers
    ? data.followers.map((person) => ({
        id: person.userId,
        userId: person.userId,
        initials: person.initials,
        name: person.name,
      }))
    : card.watchers;
  const faces = people.slice(0, 3);
  const more = people.length - faces.length;
  // Without a producer to follow through, the button is not offered.
  if (!(known && onToggle) && people.length === 0) return null;
  const button = (
    <button
      type="button"
      aria-pressed={following}
      aria-disabled={known && onToggle ? undefined : "true"}
      onClick={known && onToggle ? () => onToggle(!following) : undefined}
      className={BUTTON_SM_VARIANT_CLASS.outline}
    >
      <Bell aria-hidden="true" className="size-3.5" />
      {following ? t.following : t.follow}
    </button>
  );
  return (
    <div className="flex items-center gap-2">
      {known && onToggle ? (
        button
      ) : (
        <DisabledControl reason={t.followUnavailable}>{button}</DisabledControl>
      )}
      {people.length > 0 ? (
        <div
          role="group"
          aria-label={fillSheetCopy(t.watchers, { count: people.length })}
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

/** The fields of the board beyond the item's own properties: selects and numbers. */
function customFieldsOf(board: Board): FieldDefinition[] {
  const own = findItemFields(board);
  const ids = new Set(
    [own.status, own.priority, own.person, own.due].flatMap((field) =>
      field ? [field.id] : [],
    ),
  );
  return board.fieldDefinitions
    .filter(
      (field) =>
        !ids.has(field.id) &&
        !field.isHidden &&
        (field.fieldType === "select" || field.fieldType === "number"),
    )
    .sort((a, b) => a.position - b.position);
}

/** One custom field: a select as a menu of its options, a number as a small editor. */
function CustomFieldRow({
  field,
  card,
  editable,
  copy,
  onChange,
}: {
  field: FieldDefinition;
  card: CardDetail;
  editable: boolean;
  copy: ItemPageCopy;
  onChange: (value: UpdateFieldValueInput["value"]) => void;
}) {
  const t = copy.rail;
  const raw = card.fieldValues[field.id];
  if (field.fieldType === "number") {
    const value = typeof raw === "number" ? raw : null;
    return (
      <PropertyRow label={field.name}>
        <NumberEditor
          value={value}
          unit={field.unit}
          editable={editable}
          label={fillSheetCopy(t.editNumber, { name: field.name })}
          noValue={t.noValue}
          onChange={onChange}
        />
      </PropertyRow>
    );
  }
  const option = field.options.find((entry) => entry.id === raw);
  return (
    <PropertyRow label={field.name}>
      <ItemOptionMenu field={field} readOnly={!editable} onSelect={onChange}>
        <PropertyValue
          editable={editable}
          aria-label={field.name}
          className={option ? undefined : "font-normal text-meta-foreground"}
        >
          {option ? (
            <>
              {field.display === "mark" ? (
                <span
                  aria-hidden="true"
                  className="grid size-5 shrink-0 place-items-center rounded-[6px] text-[8.5px] font-bold text-white"
                  style={{ backgroundColor: option.color }}
                >
                  {personInitials(option.label)}
                </span>
              ) : null}
              {option.label}
            </>
          ) : (
            t.noValue
          )}
        </PropertyValue>
      </ItemOptionMenu>
    </PropertyRow>
  );
}

function NumberEditor({
  value,
  unit,
  editable,
  label,
  noValue,
  onChange,
}: {
  value: number | null;
  unit?: string;
  editable: boolean;
  label: string;
  noValue: string;
  onChange: (value: number) => void;
}) {
  const [open, setOpen] = useState(false);
  const [draft, setDraft] = useState("");
  const shown =
    value === null ? (
      <span className="font-normal text-meta-foreground">{noValue}</span>
    ) : (
      `${value}${unit ? ` ${unit}` : ""}`
    );
  if (!editable)
    return (
      <PropertyValue editable={false} aria-label={label}>
        {shown}
      </PropertyValue>
    );
  const commit = () => {
    const next = Number(draft.replace(",", "."));
    if (draft.trim() !== "" && Number.isFinite(next) && next !== value)
      onChange(next);
    setOpen(false);
  };
  return (
    <Popover
      open={open}
      onOpenChange={(next) => {
        if (next) setDraft(value === null ? "" : String(value));
        setOpen(next);
      }}
    >
      <PopoverTrigger asChild>
        <PropertyValue editable aria-label={label}>
          {shown}
        </PropertyValue>
      </PopoverTrigger>
      <PopoverContent align="start" aria-label={label} className="w-44 p-2">
        <input
          type="number"
          autoFocus
          value={draft}
          aria-label={label}
          onChange={(event) => setDraft(event.target.value)}
          onKeyDown={(event) => {
            if (event.key !== "Enter") return;
            event.preventDefault();
            commit();
          }}
          onBlur={commit}
          className="h-8 w-full rounded-[7px] border border-line-strong bg-background px-2 text-item-ui text-foreground"
        />
      </PopoverContent>
    </Popover>
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
  data,
  editedAt,
  offline,
  people,
  viewerUserId,
  openPicker,
  onOpenPickerChange,
}: {
  offline?: ItemPageOffline | null;
  /** The people the Assignee picker lists, with the invitation behind "Mời vào". */
  people?: ItemPagePeople;
  viewerUserId?: string;
  /** The picker opened from the keyboard (S, P), when the page owns it. */
  openPicker?: ItemPagePicker | null;
  onOpenPickerChange?: (picker: ItemPagePicker | null) => void;
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
  data?: TaskDetailData;
  /** When the item was last changed, history included. */
  editedAt?: string | null;
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
  // A done item says how early or late it finished instead of turning red.
  const doneTiming =
    due?.tone === "done" && card.completedAt && dueAt
      ? calendarDaysFromToday(dueAt, new Date(card.completedAt), timeZone)
      : null;
  const doneNote =
    doneTiming === null
      ? null
      : doneTiming === 0
        ? copy.states.completed.onTime
        : doneTiming > 0
          ? pluralSheetCopy(
              copy.states.completed.early_one,
              copy.states.completed.early_other,
              doneTiming,
            )
          : pluralSheetCopy(
              copy.states.completed.late_one,
              copy.states.completed.late_other,
              -doneTiming,
            );
  const pk = copy.pickers;
  const statusOptions: PickerOption[] = (fields.status?.options ?? []).map(
    (option) => ({
      id: option.id,
      label: option.label,
      markerClass: option.category
        ? STATUS_DOT_CLASS[option.category]
        : "bg-muted-foreground",
    }),
  );
  const priorityOptions: PickerOption[] = (fields.priority?.options ?? []).map(
    (option) => {
      const kind = priorityFromOptionName(option.label);
      return {
        id: option.id,
        label: option.label,
        markerClass: kind ? PRIORITY_MARKER_CLASS[kind] : undefined,
        hollow: kind === "low" || !kind,
      };
    },
  );
  const pickerPeople: PickerPerson[] =
    people && people.people.length > 0
      ? [...people.people]
      : board.members.map((member) => ({
          userId: member.userId,
          name: member.name,
          department: null,
          openCount: null,
          onBoard: true,
        }));
  const conflictOn = (property: ItemPageProperty) =>
    conflict && conflict.property === property ? conflict : null;
  const pendingOn = (property: ItemPageProperty) =>
    offline?.properties?.includes(property) ? (
      <PendingMark copy={copy} />
    ) : null;
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
  const customFields = customFieldsOf(board);
  // Unset optional fields fold away; "Hiện N trường trống" brings them back.
  const [showEmpty, setShowEmpty] = useState(false);
  const hiddenKeys = new Set<string>();
  if (!startDate && !conflictOn("startDate")) hiddenKeys.add("startDate");
  if (!dueAt && !conflictOn("due")) hiddenKeys.add("due");
  for (const field of customFields) {
    const raw = card.fieldValues[field.id];
    if (raw === undefined || raw === null || raw === "")
      hiddenKeys.add(field.id);
  }
  const isShown = (key: string) => showEmpty || !hiddenKeys.has(key);
  const customRow = (field: FieldDefinition | undefined) =>
    field && isShown(field.id) ? (
      <CustomFieldRow
        key={field.id}
        field={field}
        card={card}
        editable={editable}
        copy={copy}
        onChange={(value) => update(field, value)}
      />
    ) : null;
  const blockedBy = data?.links?.find((link) => link.relation === "blocked-by");
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
      <FollowRow
        card={card}
        data={data}
        onToggle={callbacks.onToggleWatch}
        copy={copy}
      />

      <ItemApprovalSection data={data} copy={copy} />

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
            <RailPicker
              enabled={editable && !!fields.status}
              open={openPicker === "status"}
              onOpenChange={(open) =>
                onOpenPickerChange?.(open ? "status" : null)
              }
              panel={(close) => (
                <ItemListPicker
                  idPrefix="item-status"
                  options={statusOptions}
                  currentId={statusOption?.id ?? null}
                  searchPlaceholder={pk.status.search}
                  searchLabel={pk.status.label}
                  noMatch={pk.noMatch}
                  shortcut="S"
                  onSelect={(optionId) => {
                    update(fields.status, optionId);
                    close();
                  }}
                  footer={
                    navigation.onEditStatuses ? (
                      <button
                        type="button"
                        onClick={() => {
                          close();
                          navigation.onEditStatuses?.();
                        }}
                        className="border-0 bg-transparent p-0 font-medium text-brand-subtle-foreground"
                      >
                        {pk.status.editList}
                      </button>
                    ) : (
                      <DisabledControl reason={pk.status.editListUnavailable}>
                        <button
                          type="button"
                          aria-disabled="true"
                          className="border-0 bg-transparent p-0 font-medium text-brand-subtle-foreground"
                        >
                          {pk.status.editList}
                        </button>
                      </DisabledControl>
                    )
                  }
                />
              )}
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
                    {pendingOn("status")}
                  </>
                )}
              </PropertyValue>
            </RailPicker>
          </PropertyRow>

          <PropertyRow label={t.assignee}>
            <RailPicker
              enabled={editable && !!fields.person}
              panel={(close) => (
                <ItemAssigneePicker
                  copy={pk}
                  people={pickerPeople}
                  loading={people?.loading}
                  selectedUserIds={card.members.map((member) => member.userId)}
                  viewerUserId={viewerUserId}
                  canInvite={people?.canInvite === true}
                  onInvite={people?.onInvite}
                  onChange={(userIds) => update(fields.person, userIds)}
                  onClose={close}
                />
              )}
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
                    {pendingOn("assignee")}
                  </>
                ) : (
                  p.unassigned
                )}
              </PropertyValue>
            </RailPicker>
          </PropertyRow>

          {isShown("startDate") ? (
            <PropertyRow label={t.startDate}>
              {editable && callbacks.onUpdateItem ? (
                <RailPicker
                  enabled
                  panel={(close) => (
                    <ItemDatePicker
                      copy={pk}
                      value={startDate}
                      now={now}
                      timeZone={timeZone}
                      onChange={(value) =>
                        callbacks.onUpdateItem?.(card.id, { startDate: value })
                      }
                      onClose={close}
                    />
                  )}
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
                </RailPicker>
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
          ) : null}

          {isShown("due") ? (
            <PropertyRow label={t.due}>
              <RailPicker
                enabled={dueEditable}
                panel={(close) => (
                  <ItemDatePicker
                    copy={pk}
                    value={dueAt}
                    rangeFrom={startDate}
                    now={now}
                    timeZone={timeZone}
                    onChange={changeDue}
                    onClose={close}
                  />
                )}
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
                  {conflictOn("due") ? (
                    conflictText(conflictOn("due"))
                  ) : (
                    <>
                      {due
                        ? doneNote
                          ? `${due.text} · ${doneNote}`
                          : due.text
                        : p.noDue}
                      {pendingOn("due")}
                    </>
                  )}
                </PropertyValue>
              </RailPicker>
            </PropertyRow>
          ) : null}

          <PropertyRow label={t.priority}>
            <RailPicker
              enabled={editable && !!fields.priority}
              open={openPicker === "priority"}
              onOpenChange={(open) =>
                onOpenPickerChange?.(open ? "priority" : null)
              }
              panel={(close) => (
                <ItemListPicker
                  idPrefix="item-priority"
                  options={priorityOptions}
                  currentId={card.priorityOptionId ?? null}
                  searchPlaceholder={pk.priority.search}
                  searchLabel={pk.priority.label}
                  noMatch={pk.noMatch}
                  shortcut="P"
                  none={pk.priority.none}
                  onSelect={(optionId) => {
                    update(fields.priority, optionId);
                    close();
                  }}
                  onClear={() => {
                    update(fields.priority, null);
                    close();
                  }}
                />
              )}
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
                    {pendingOn("priority")}
                  </span>
                ) : (
                  <span className="font-normal text-meta-foreground">
                    {p.noPriority}
                  </span>
                )}
              </PropertyValue>
            </RailPicker>
          </PropertyRow>

          {customRow(customFields[0])}

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

          {customRow(customFields[1])}

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

          {blockedBy ? (
            <PropertyRow label={t.dependency}>
              <span
                className={cn(
                  VALUE_CLASS,
                  "cursor-default gap-1.5 whitespace-nowrap",
                )}
              >
                <span className="text-crumb font-normal text-meta-foreground">
                  {t.waitingFor}
                </span>
                <span className="font-mono text-item-small">
                  {blockedBy.item.key}
                </span>
                <span className="text-crumb font-normal text-meta-foreground">
                  {blockedBy.item.statusName}
                </span>
              </span>
            </PropertyRow>
          ) : null}

          {customFields.slice(2).map(customRow)}
        </dl>
        {hiddenKeys.size > 0 ? (
          <button
            type="button"
            aria-expanded={showEmpty}
            onClick={() => setShowEmpty((open) => !open)}
            className="mt-2 border-0 bg-transparent p-0 text-item-note font-medium text-brand-subtle-foreground"
          >
            {showEmpty
              ? copy.states.empty.hideFields
              : pluralSheetCopy(
                  copy.states.empty.showFields_one,
                  copy.states.empty.showFields_other,
                  hiddenKeys.size,
                )}
          </button>
        ) : null}
      </div>

      <div className="h-px shrink-0 bg-line" />

      <ViewsList
        card={card}
        statusLabel={statusOption?.label ?? ""}
        navigation={navigation}
        copy={copy}
        now={now}
        timeZone={timeZone}
      />

      <div className="h-px shrink-0 bg-line" />

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
            {systemEditedText(
              editedAt ?? card.updatedAt ?? card.createdAt,
              now,
              copy,
              timeZone,
            )}
          </SystemRow>
        </dl>
      </div>
    </aside>
  );
}

function systemEditedText(
  value: string,
  now: Date,
  copy: ItemPageCopy,
  timeZone?: string,
): string {
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

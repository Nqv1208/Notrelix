import {
  useCallback,
  useEffect,
  useId,
  useRef,
  useState,
  type ComponentPropsWithRef,
  type KeyboardEvent,
  type ReactNode,
} from "react";
import {
  ChevronDown,
  ChevronUp,
  CalendarPlus,
  Check,
  Clock,
  Eye,
  EyeOff,
  Flag,
  Link2,
  Maximize2,
  MoreHorizontal,
  X,
} from "lucide-react";
import {
  Avatar,
  AvatarFallback,
  Button,
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSub,
  DropdownMenuSubContent,
  DropdownMenuSubTrigger,
  DropdownMenuTrigger,
  Tooltip,
  TooltipContent,
  TooltipTrigger,
  cn,
} from "@notrelix/ui-web";
import type {
  Board,
  CardDetail,
  FieldDefinition,
  CardLabel,
  UpdateCardInput,
  UpdateFieldValueInput,
} from "@notrelix/work-management-core";
import {
  ITEM_LABEL_TONE_CLASS,
  identityMarkClass,
  itemLabelTone,
  nextLabelColor,
} from "../shared/item-display";
import { boardSettingsHref } from "../shared/board-settings-href";
import { AddPropertyPopover } from "./item-add-property";
import {
  ItemMultiPopover,
  ItemTextPopover,
  isFieldValueEmpty,
  multiValueIds,
} from "./item-field-more-editors";
import { describeSheetDue, type SheetDueTone } from "./item-sheet-format";
import { fillSheetCopy, type ItemSheetCopy } from "./item-sheet.copy";
import {
  ItemAssigneePopover,
  ItemDuePopover,
  ItemLabelsPopover,
  labelCreate,
  ItemNumberPopover,
  ItemOptionMenu,
  PRIORITY_CLASS,
  STATUS_DOT_CLASS,
  STATUS_LANE_CLASS,
  customItemFields,
  describeItemStatus,
  findItemFields,
  isItemDone,
} from "./item-field-editors";
import type {
  ItemSheetGroup,
  ItemSheetMark,
  ItemSheetMarkTone,
  ItemSheetNewField,
  TaskDetailCapabilities,
} from "./task-detail-types";

/** Department mark tones (literal classes so Tailwind generates them). */
const MARK_TONE_CLASS: Readonly<Record<ItemSheetMarkTone, string>> = {
  blue: "bg-mark-blue text-mark-blue-foreground",
  gold: "bg-mark-gold text-mark-gold-foreground",
  coral: "bg-mark-coral text-mark-coral-foreground",
  sage: "bg-mark-sage text-mark-sage-foreground",
  crimson: "bg-mark-crimson text-mark-crimson-foreground",
  violet: "bg-mark-violet text-mark-violet-foreground",
};

const DUE_TONE_CLASS: Readonly<Record<SheetDueTone, string>> = {
  overdue: "border-transparent bg-danger-subtle font-semibold text-danger-text",
  today: "border-transparent bg-warning-subtle text-warning-text",
  upcoming: "",
  done: "text-muted-foreground",
};

import {
  describeComputed,
  ItemLinkPopover,
  ItemLongTextPopover,
} from "./item-field-extra-editors";

const PILL_CLASS =
  "inline-flex h-7.5 max-w-full items-center gap-[7px] rounded-md border border-line-strong bg-popover px-2.5 text-compact font-medium text-foreground transition-colors hover:bg-state-hover-strong focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-focus-ring aria-disabled:cursor-default aria-disabled:hover:bg-popover";

// ─── Head bar ───────────────────────────────────────────────────────────────

export function TaskDetailHeader({
  board,
  card,
  capabilities,
  copy,
  departmentMark,
  groups = [],
  onClose,
  onPrevious,
  onNext,
  onCopyLink,
  onOpenFullPage,
  onDuplicate,
  onArchive,
  onDelete,
  onMoveToBoard,
  onMoveToGroup,
  following,
  onToggleFollow,
}: {
  board: Board;
  card: CardDetail;
  capabilities: TaskDetailCapabilities;
  copy: ItemSheetCopy;
  departmentMark?: ItemSheetMark;
  groups?: readonly ItemSheetGroup[];
  onClose: () => void;
  onPrevious?: () => void;
  onNext?: () => void;
  onCopyLink?: (cardId: string) => void;
  onOpenFullPage?: (cardId: string) => void;
  onDuplicate: (cardId: string) => void;
  onArchive: (cardId: string) => void;
  onDelete?: (cardId: string) => void;
  onMoveToBoard?: (cardId: string) => void;
  onMoveToGroup?: (cardId: string, groupId: string) => void;
  /** Whether the member follows the item; `null`/absent draws no follow chip. */
  following?: boolean | null;
  onToggleFollow?: (following: boolean) => void;
}) {
  const fullPageReasonId = useId();
  const otherGroups = groups.filter((group) => group.id !== card.listId);
  const canMove =
    capabilities.canMoveCard && !!onMoveToGroup && otherGroups.length > 0;
  const hasMenu =
    capabilities.canCreateCard ||
    capabilities.canDelete ||
    canMove ||
    (capabilities.canMoveCard && Boolean(onMoveToBoard));

  return (
    <header className="flex h-12.25 shrink-0 items-center gap-1.5 border-b border-line pr-2.5 pl-4.5">
      <nav
        aria-label={copy.head.crumb}
        className="flex min-w-0 items-center gap-[7px] text-crumb text-meta-foreground"
      >
        {departmentMark ? (
          <span
            className={cn(
              "grid size-4.5 shrink-0 place-items-center rounded-sm text-mark-3xs",
              MARK_TONE_CLASS[departmentMark.tone],
            )}
          >
            {departmentMark.initials}
          </span>
        ) : null}
        <span className="truncate">{board.title}</span>
        {card.itemKey ? (
          <>
            <span aria-hidden="true">/</span>
            <span className="shrink-0 font-mono text-muted-foreground">
              {card.itemKey}
            </span>
          </>
        ) : null}
      </nav>
      {following === null || following === undefined ? null : (
        <button
          type="button"
          data-f="follow"
          aria-pressed={following}
          aria-label={following ? copy.head.following : copy.head.follow}
          disabled={!onToggleFollow}
          onClick={() => onToggleFollow?.(!following)}
          className={cn(
            "inline-flex h-6.5 shrink-0 items-center gap-1.5 rounded-full border-0 px-2.5 text-crumb font-medium outline-none focus-visible:ring-2 focus-visible:ring-focus-ring",
            following
              ? "bg-state-selected text-state-selected-foreground"
              : "bg-transparent text-muted-foreground hover:bg-state-hover-strong",
          )}
        >
          {following ? (
            <Eye aria-hidden="true" className="size-3.5" />
          ) : (
            <EyeOff aria-hidden="true" className="size-3.5" />
          )}
          {following ? copy.head.following : copy.head.follow}
        </button>
      )}
      <span className="flex-1" aria-hidden="true" />

      <HeadIconButton
        label={copy.head.previous}
        disabled={!onPrevious}
        onClick={onPrevious}
      >
        <ChevronUp />
      </HeadIconButton>
      <HeadIconButton
        label={copy.head.next}
        disabled={!onNext}
        onClick={onNext}
      >
        <ChevronDown />
      </HeadIconButton>
      <HeadIconButton
        label={copy.head.copyLink}
        disabled={!onCopyLink}
        onClick={() => onCopyLink?.(card.id)}
      >
        <Link2 />
      </HeadIconButton>
      {onOpenFullPage ? (
        <HeadIconButton
          label={copy.head.openFullPage}
          onClick={() => onOpenFullPage(card.id)}
        >
          <Maximize2 />
        </HeadIconButton>
      ) : (
        <>
          {/* A host without an item page: focusable, explained, inert. */}
          <Tooltip>
            <TooltipTrigger asChild>
              <Button
                type="button"
                variant="ghost"
                size="icon-sm"
                aria-label={copy.head.openFullPage}
                aria-disabled="true"
                aria-describedby={fullPageReasonId}
                className="rounded-[7px] text-meta-foreground aria-disabled:opacity-50"
              >
                <Maximize2 aria-hidden="true" />
              </Button>
            </TooltipTrigger>
            <TooltipContent>{copy.head.openFullPageUnavailable}</TooltipContent>
          </Tooltip>
          <span id={fullPageReasonId} className="sr-only">
            {copy.head.openFullPageUnavailable}
          </span>
        </>
      )}

      {hasMenu ? (
        <DropdownMenu>
          <DropdownMenuTrigger asChild>
            <Button
              type="button"
              variant="ghost"
              size="icon-sm"
              aria-label={copy.head.more}
              className="rounded-[7px] text-meta-foreground hover:bg-state-hover-strong"
            >
              <MoreHorizontal aria-hidden="true" />
            </Button>
          </DropdownMenuTrigger>
          <DropdownMenuContent align="end">
            {capabilities.canCreateCard ? (
              <DropdownMenuItem onSelect={() => onDuplicate(card.id)}>
                {copy.head.duplicate}
              </DropdownMenuItem>
            ) : null}
            {canMove ? (
              <DropdownMenuSub>
                <DropdownMenuSubTrigger>
                  {copy.head.moveToGroup}
                </DropdownMenuSubTrigger>
                <DropdownMenuSubContent>
                  {otherGroups.map((group) => (
                    <DropdownMenuItem
                      key={group.id}
                      onSelect={() => onMoveToGroup?.(card.id, group.id)}
                    >
                      {group.title}
                    </DropdownMenuItem>
                  ))}
                </DropdownMenuSubContent>
              </DropdownMenuSub>
            ) : null}
            {capabilities.canMoveCard && onMoveToBoard ? (
              <DropdownMenuItem onSelect={() => onMoveToBoard(card.id)}>
                {copy.head.moveToBoard}
              </DropdownMenuItem>
            ) : null}
            {capabilities.canDelete ? (
              <DropdownMenuItem onSelect={() => onArchive(card.id)}>
                {copy.head.archive}
              </DropdownMenuItem>
            ) : null}
            {capabilities.canDelete && onDelete ? (
              <DropdownMenuItem
                variant="destructive"
                onSelect={() => onDelete(card.id)}
              >
                {copy.head.delete}
              </DropdownMenuItem>
            ) : null}
          </DropdownMenuContent>
        </DropdownMenu>
      ) : null}

      <HeadIconButton label={copy.head.close} onClick={onClose}>
        <X />
      </HeadIconButton>
    </header>
  );
}

function HeadIconButton({
  label,
  disabled,
  onClick,
  children,
}: {
  label: string;
  disabled?: boolean;
  onClick?: () => void;
  children: ReactNode;
}) {
  return (
    <Button
      type="button"
      variant="ghost"
      size="icon-sm"
      aria-label={label}
      title={label}
      disabled={disabled}
      onClick={onClick}
      className="rounded-[7px] text-meta-foreground hover:bg-state-hover-strong hover:text-foreground [&_svg]:size-4"
    >
      {children}
    </Button>
  );
}

// ─── Title ──────────────────────────────────────────────────────────────────

export function TaskDetailTitle({
  card,
  titleId,
  editable,
  copy,
  onRenameTitle,
  as: Heading = "h2",
  className = "text-sheet-title",
}: {
  card: CardDetail;
  titleId: string;
  editable: boolean;
  copy: ItemSheetCopy;
  onRenameTitle: (cardId: string, patch: UpdateCardInput) => void;
  /** The heading element: the sheet uses h2, the item page h1. */
  as?: "h1" | "h2";
  /** The text role of the heading. */
  className?: string;
}) {
  const editorRef = useRef<HTMLSpanElement>(null);

  // Keep the editable text in step with a title changed elsewhere.
  useEffect(() => {
    const editor = editorRef.current;
    if (editor && editor.textContent !== card.title) {
      editor.textContent = card.title;
    }
  }, [card.title]);

  const commit = useCallback(() => {
    const editor = editorRef.current;
    const nextTitle = (editor?.textContent ?? "").trim();
    if (!nextTitle || nextTitle === card.title) {
      if (editor) editor.textContent = card.title;
      return;
    }
    onRenameTitle(card.id, { title: nextTitle });
  }, [card.id, card.title, onRenameTitle]);

  function handleKeyDown(event: KeyboardEvent<HTMLSpanElement>) {
    if (event.key === "Enter") {
      event.preventDefault();
      event.currentTarget.blur();
    }
    if (event.key === "Escape") {
      // Escape reverts the edit; a second Escape closes the sheet.
      event.stopPropagation();
      if (editorRef.current) editorRef.current.textContent = card.title;
      event.currentTarget.blur();
    }
  }

  return (
    <Heading
      id={titleId}
      className={cn("m-0 text-balance text-foreground", className)}
    >
      {editable ? (
        <span
          ref={editorRef}
          role="textbox"
          aria-label={copy.title.edit}
          contentEditable
          suppressContentEditableWarning
          spellCheck={false}
          onBlur={commit}
          onKeyDown={handleKeyDown}
          className="-mx-1 block rounded-md px-1 outline-none [word-break:break-word] hover:bg-state-hover focus:bg-state-hover focus:ring-1 focus:ring-border"
        >
          {card.title}
        </span>
      ) : (
        card.title
      )}
    </Heading>
  );
}

// ─── Property pills ─────────────────────────────────────────────────────────

type PillProps = ComponentPropsWithRef<"button"> & {
  readonly readOnly: boolean;
  readonly srLabel: string;
  readonly chevron?: boolean;
};

/**
 * One property pill. Read-only pills stay in the tab order with
 * `aria-disabled` (the commenter can still read them) and lose their chevron.
 */
function PropertyPill({
  readOnly,
  srLabel,
  chevron = false,
  className,
  children,
  ...rest
}: PillProps) {
  return (
    <button
      type="button"
      {...rest}
      aria-disabled={readOnly ? "true" : undefined}
      className={cn(PILL_CLASS, className)}
    >
      <span className="sr-only">{`${srLabel}: `}</span>
      {children}
      {chevron && !readOnly ? (
        <ChevronDown
          data-pill-chevron=""
          aria-hidden="true"
          className="size-3.25 shrink-0"
        />
      ) : null}
    </button>
  );
}

export function TaskDetailProperties({
  board,
  card,
  capabilities,
  copy,
  now,
  timeZone,
  onUpdateFieldValue,
  onUpdateItem,
  boardLabels,
  onToggleLabel,
  onCreateLabel,
  onCreateField,
}: {
  board: Board;
  card: CardDetail;
  capabilities: TaskDetailCapabilities;
  copy: ItemSheetCopy;
  now: Date;
  timeZone?: string;
  onUpdateFieldValue: (payload: UpdateFieldValueInput) => void;
  /** Patches the item's own dates; the Start pill is read-only without it. */
  onUpdateItem?: (cardId: string, patch: UpdateCardInput) => void;
  /** The labels the board offers; labels are read-only without them. */
  boardLabels?: readonly CardLabel[];
  onToggleLabel?: (labelId: string, on: boolean) => void;
  onCreateLabel?: (name: string, color?: string) => Promise<boolean>;
  /** Creates a custom property; absent: no "Thêm thuộc tính". */
  onCreateField?: (input: ItemSheetNewField) => Promise<string | null>;
}) {
  const readOnly = !capabilities.canEditFields;
  // Empty custom properties the member asked to fill, and the one just created.
  const [revealed, setRevealed] = useState<ReadonlySet<string>>(new Set());
  const [justCreated, setJustCreated] = useState<string | null>(null);
  const fields = findItemFields(board);
  const t = copy.properties;

  function update(field: FieldDefinition | undefined, value: unknown) {
    if (!field || readOnly) return;
    onUpdateFieldValue({ cardId: card.id, fieldDefinitionId: field.id, value });
  }

  // Status
  const { option: statusOption, category } = describeItemStatus(
    card,
    fields.status,
  );
  const statusPill = (props: Partial<PillProps> = {}) => (
    <PropertyPill
      {...props}
      readOnly={readOnly || !fields.status}
      srLabel={t.status}
      chevron
      className={cn(
        "font-semibold",
        category
          ? cn("border-transparent", STATUS_LANE_CLASS[category])
          : "text-muted-foreground",
      )}
    >
      <span
        aria-hidden="true"
        className={cn(
          "size-2.25 shrink-0 rounded-full",
          category ? STATUS_DOT_CLASS[category] : "bg-muted-foreground",
        )}
      />
      {statusOption?.label ?? t.noStatus}
    </PropertyPill>
  );

  // Assignee
  const [firstMember, ...otherMembers] = card.members;
  const assigneePill = (props: Partial<PillProps> = {}) => (
    <PropertyPill
      {...props}
      readOnly={readOnly || !fields.person}
      srLabel={t.assignee}
      className={firstMember ? undefined : "text-muted-foreground"}
    >
      {firstMember ? (
        <>
          <Avatar className="size-5">
            <AvatarFallback
              className={cn(
                "text-avatar-sm",
                identityMarkClass(firstMember.userId || firstMember.id),
              )}
            >
              {firstMember.initials}
            </AvatarFallback>
          </Avatar>
          <span className="truncate">
            {otherMembers.length > 0
              ? fillSheetCopy(t.assigneeMore, {
                  name: firstMember.name,
                  count: otherMembers.length,
                })
              : firstMember.name}
          </span>
        </>
      ) : (
        t.unassigned
      )}
    </PropertyPill>
  );

  // Due
  const dueAt = card.dueAt ?? card.dueDate ?? null;
  const due = describeSheetDue(dueAt, now, isItemDone(card), copy, timeZone);
  const duePill = (props: Partial<PillProps> = {}) => (
    <PropertyPill
      {...props}
      readOnly={readOnly || !fields.due}
      srLabel={t.due}
      className={due ? DUE_TONE_CLASS[due.tone] : "text-muted-foreground"}
    >
      <Clock aria-hidden="true" className="size-3.5 shrink-0" />
      {due?.text ?? t.noDue}
    </PropertyPill>
  );

  // Priority
  const priority = card.priority;
  const priorityPill = (props: Partial<PillProps> = {}) => (
    <PropertyPill
      {...props}
      readOnly={readOnly || !fields.priority}
      srLabel={t.priority}
      className={priority ? PRIORITY_CLASS[priority] : "text-muted-foreground"}
    >
      <Flag aria-hidden="true" className="size-3.25 shrink-0" />
      {priority ? t.priorities[priority] : t.noPriority}
    </PropertyPill>
  );

  // Start
  const startDate = card.startDate ?? null;
  const start = describeSheetDue(startDate, now, true, copy, timeZone);
  const startEditable = !readOnly && !!onUpdateItem;
  const startPill = (props: Partial<PillProps> = {}) => (
    <PropertyPill
      {...props}
      readOnly={!startEditable}
      srLabel={t.start}
      className="text-muted-foreground"
    >
      <CalendarPlus aria-hidden="true" className="size-3.5 shrink-0" />
      {start?.text ?? t.noStart}
    </PropertyPill>
  );

  const customFields = customItemFields(board);
  // A custom property with no value stays out of sight until "+" brings it back.
  const isShown = (field: FieldDefinition) =>
    !isFieldValueEmpty(card.fieldValues[field.id]) ||
    revealed.has(field.id) ||
    justCreated === field.id;
  const shownFields = customFields.filter(isShown);
  const emptyFields = customFields.filter((field) => !isShown(field));
  const labelsEditable = !readOnly && !!boardLabels && !!onToggleLabel;
  const selectedLabelIds = new Set(card.labels.map((entry) => entry.id));

  return (
    <div role="group" aria-label={t.group} className="flex flex-wrap gap-1.5">
      <ItemOptionMenu
        field={fields.status}
        readOnly={readOnly}
        onSelect={(optionId) => update(fields.status, optionId)}
      >
        {statusPill()}
      </ItemOptionMenu>

      <ItemAssigneePopover
        board={board}
        card={card}
        label={t.assignee}
        readOnly={readOnly || !fields.person}
        onChange={(userIds) => update(fields.person, userIds)}
      >
        {assigneePill()}
      </ItemAssigneePopover>

      <ItemDuePopover
        dueAt={dueAt}
        clearLabel={t.clearDue}
        readOnly={readOnly || !fields.due}
        onChange={(value) => update(fields.due, value)}
      >
        {duePill()}
      </ItemDuePopover>

      <ItemOptionMenu
        field={fields.priority}
        readOnly={readOnly}
        onSelect={(optionId) => update(fields.priority, optionId)}
      >
        {priorityPill()}
      </ItemOptionMenu>

      <ItemDuePopover
        dueAt={startDate}
        clearLabel={t.clearStart}
        readOnly={!startEditable}
        onChange={(value) => onUpdateItem?.(card.id, { startDate: value })}
      >
        {startPill()}
      </ItemDuePopover>

      {shownFields.map((field) => {
        const raw = card.fieldValues[field.id];
        const autoOpen = justCreated === field.id;
        const empty = isFieldValueEmpty(raw);
        if (field.fieldType === "number") {
          const value = typeof raw === "number" ? raw : null;
          return (
            <ItemNumberPopover
              key={field.id}
              value={value}
              label={fillSheetCopy(t.editNumber, { name: field.name })}
              readOnly={readOnly}
              autoOpen={autoOpen}
              onChange={(next) => update(field, next)}
            >
              <PropertyPill
                readOnly={readOnly}
                srLabel={field.name}
                className={value === null ? "text-muted-foreground" : undefined}
              >
                {value === null
                  ? field.name
                  : `${field.name}: ${value}${field.unit ? ` ${field.unit}` : ""}`}
              </PropertyPill>
            </ItemNumberPopover>
          );
        }
        if (field.fieldType === "text") {
          const value = typeof raw === "string" ? raw : "";
          return (
            <ItemTextPopover
              key={field.id}
              value={value}
              label={fillSheetCopy(t.editText, { name: field.name })}
              readOnly={readOnly}
              autoOpen={autoOpen}
              onChange={(next) => update(field, next)}
            >
              <PropertyPill
                readOnly={readOnly}
                srLabel={field.name}
                className={cn(
                  "max-w-60",
                  empty ? "text-muted-foreground" : undefined,
                )}
              >
                <span className="truncate">
                  {empty ? field.name : `${field.name}: ${value}`}
                </span>
              </PropertyPill>
            </ItemTextPopover>
          );
        }
        if (field.fieldType === "checkbox") {
          const on = raw === true;
          return (
            <PropertyPill
              key={field.id}
              readOnly={readOnly}
              srLabel={field.name}
              role="checkbox"
              aria-checked={on}
              onClick={readOnly ? undefined : () => update(field, !on)}
              className={on ? undefined : "text-muted-foreground"}
            >
              <span
                aria-hidden="true"
                className={cn(
                  "grid size-3.5 shrink-0 place-items-center rounded-[3px] border",
                  on
                    ? "border-primary bg-primary text-primary-foreground"
                    : "border-line-strong",
                )}
              >
                {on ? <Check className="size-2.5" strokeWidth={3} /> : null}
              </span>
              {field.name}
            </PropertyPill>
          );
        }
        if (field.fieldType === "multi_select") {
          const ids = multiValueIds(raw);
          const names = field.options
            .filter((entry) => ids.includes(entry.id))
            .map((entry) => entry.label);
          return (
            <ItemMultiPopover
              key={field.id}
              field={field}
              value={ids}
              readOnly={readOnly}
              autoOpen={autoOpen}
              onChange={(next) => update(field, next)}
            >
              <PropertyPill
                readOnly={readOnly}
                srLabel={field.name}
                chevron
                className={cn(
                  "max-w-60",
                  names.length === 0 ? "text-muted-foreground" : undefined,
                )}
              >
                <span className="truncate">
                  {names.length === 0
                    ? field.name
                    : `${field.name}: ${names.join(", ")}`}
                </span>
              </PropertyPill>
            </ItemMultiPopover>
          );
        }
        if (field.fieldType === "long_text" || field.fieldType === "link") {
          const value = typeof raw === "string" ? raw : "";
          const Popover =
            field.fieldType === "link" ? ItemLinkPopover : ItemLongTextPopover;
          return (
            <Popover
              key={field.id}
              value={value}
              label={fillSheetCopy(t.editLongText, { name: field.name })}
              readOnly={readOnly}
              autoOpen={autoOpen}
              copy={t}
              onChange={(next) => update(field, next)}
            >
              <PropertyPill
                readOnly={readOnly}
                srLabel={field.name}
                className={cn(
                  "max-w-60",
                  empty ? "text-muted-foreground" : undefined,
                )}
              >
                <span className="truncate">
                  {empty ? field.name : `${field.name}: ${value}`}
                </span>
              </PropertyPill>
            </Popover>
          );
        }
        if (field.fieldType === "date") {
          const value = typeof raw === "string" ? raw : null;
          return (
            <ItemDuePopover
              key={field.id}
              dueAt={value}
              clearLabel={t.clearDue}
              readOnly={readOnly}
              onChange={(next) => update(field, next)}
            >
              <PropertyPill
                readOnly={readOnly}
                srLabel={field.name}
                className={value ? undefined : "text-muted-foreground"}
              >
                {value ? `${field.name}: ${value.slice(0, 10)}` : field.name}
              </PropertyPill>
            </ItemDuePopover>
          );
        }
        if (field.fieldType === "formula" || field.fieldType === "rollup") {
          const shown = describeComputed(
            field,
            card.computedValues?.[field.id],
            t.computed,
          );
          return (
            <PropertyPill
              key={field.id}
              readOnly
              srLabel={field.name}
              title={shown.title}
              className={cn(
                shown.tone === "stale" && "text-muted-foreground",
                shown.tone === "empty" && "text-muted-foreground",
                shown.tone === "error" && "border-danger text-danger-text",
              )}
            >
              <span
                aria-hidden="true"
                className="rounded-[5px] bg-state-hover-strong px-1 font-mono text-[11px] font-medium text-muted-foreground"
              >
                ƒx
              </span>
              {field.name}: {shown.text}
            </PropertyPill>
          );
        }
        const option = field.options.find((entry) => entry.id === raw);
        return (
          <ItemOptionMenu
            key={field.id}
            field={field}
            readOnly={readOnly}
            autoOpen={autoOpen}
            onSelect={(optionId) => update(field, optionId)}
          >
            <PropertyPill
              readOnly={readOnly}
              srLabel={field.name}
              chevron
              className={option ? undefined : "text-muted-foreground"}
            >
              {option ? option.label : field.name}
            </PropertyPill>
          </ItemOptionMenu>
        );
      })}

      <ItemLabelsPopover
        labels={boardLabels ?? []}
        selectedIds={selectedLabelIds}
        label={t.labels}
        emptyText={t.boardHasNoLabels}
        readOnly={!labelsEditable}
        onToggle={(labelId, on) => onToggleLabel?.(labelId, on)}
        create={labelCreate(t, onCreateLabel, {
          href: boardSettingsHref(board.workspaceId, board.id, "labels"),
          nextColor: nextLabelColor(boardLabels?.length ?? 0),
        })}
      >
        <PropertyPill
          readOnly={!labelsEditable}
          srLabel={t.labels}
          chevron
          className={
            card.labels.length === 0 ? "text-muted-foreground" : undefined
          }
        >
          {card.labels.length === 0
            ? labelsEditable
              ? t.addLabel
              : t.noLabels
            : card.labels.map((entry) => (
                <span
                  key={entry.id}
                  className={cn(
                    "inline-flex h-5.5 items-center rounded-sm px-2 text-micro font-semibold",
                    ITEM_LABEL_TONE_CLASS[itemLabelTone(entry)],
                  )}
                >
                  {entry.name}
                </span>
              ))}
        </PropertyPill>
      </ItemLabelsPopover>

      {!readOnly && onCreateField ? (
        <AddPropertyPopover
          t={t}
          emptyFields={emptyFields}
          manageHref={boardSettingsHref(board.workspaceId, board.id, "fields")}
          onReveal={(id) => {
            setRevealed((set) => new Set(set).add(id));
            setJustCreated(id);
          }}
          onCreate={onCreateField}
          onCreated={setJustCreated}
          triggerClassName={cn(
            PILL_CLASS,
            "border-transparent text-muted-foreground",
          )}
        />
      ) : null}
    </div>
  );
}

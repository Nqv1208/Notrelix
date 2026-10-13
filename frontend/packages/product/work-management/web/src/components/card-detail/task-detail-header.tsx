import {
  useCallback,
  useEffect,
  useId,
  useRef,
  type ComponentPropsWithRef,
  type KeyboardEvent,
  type ReactNode,
} from "react";
import {
  ChevronDown,
  ChevronUp,
  Clock,
  Flag,
  Link2,
  Maximize2,
  MoreHorizontal,
  Plus,
  X,
} from "lucide-react";
import {
  Avatar,
  AvatarFallback,
  Button,
  Calendar,
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSub,
  DropdownMenuSubContent,
  DropdownMenuSubTrigger,
  DropdownMenuTrigger,
  Popover,
  PopoverContent,
  PopoverTrigger,
  Tooltip,
  TooltipContent,
  TooltipTrigger,
  cn,
} from "@notrelix/ui-web";
import {
  findPriorityField,
  findStatusField,
  type Board,
  type CardDetail,
  type FieldDefinition,
  type UpdateCardInput,
  type UpdateFieldValueInput,
} from "@notrelix/work-management-core";
import {
  ITEM_LABEL_TONE_CLASS,
  identityMarkClass,
  itemLabelTone,
} from "../shared/item-display";
import { describeSheetDue, type SheetDueTone } from "./item-sheet-format";
import { fillSheetCopy, type ItemSheetCopy } from "./item-sheet.copy";
import type {
  ItemSheetGroup,
  ItemSheetMark,
  ItemSheetMarkTone,
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

type StatusCategory = NonNullable<CardDetail["statusCategory"]>;

/** Status pill tint = the lane tint of the status category. */
const STATUS_LANE_CLASS: Readonly<Record<StatusCategory, string>> = {
  todo: "bg-work-status-todo-lane",
  in_progress: "bg-work-status-doing-lane",
  review: "bg-work-status-review-lane",
  done: "bg-work-status-done-lane",
};

const STATUS_DOT_CLASS: Readonly<Record<StatusCategory, string>> = {
  todo: "bg-work-status-todo",
  in_progress: "bg-work-status-doing",
  review: "bg-work-status-review",
  done: "bg-work-status-done",
};

const DUE_TONE_CLASS: Readonly<Record<SheetDueTone, string>> = {
  overdue: "border-transparent bg-danger-subtle font-semibold text-danger-text",
  today: "border-transparent bg-warning-subtle text-warning-text",
  upcoming: "",
  done: "text-muted-foreground",
};

type Priority = NonNullable<CardDetail["priority"]>;

const PRIORITY_CLASS: Readonly<Record<Priority, string>> = {
  urgent: "text-danger-text",
  high: "text-priority-high-text",
  medium: "text-muted-foreground",
  low: "text-muted-foreground",
};

const PILL_CLASS =
  "inline-flex h-7.5 max-w-full items-center gap-[7px] rounded-md border border-line-strong bg-popover px-2.5 text-compact font-medium text-foreground transition-colors hover:bg-state-hover-strong focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-focus-ring aria-disabled:cursor-default aria-disabled:hover:bg-popover";

/**
 * Board fields the sheet edits. Status and priority are resolved by their
 * role on the board read (`findStatusField`, `findPriorityField`), which
 * also accept the older fixture ids.
 */
function findFields(board: Board) {
  const fields = board.fieldDefinitions;
  const byId = (suffix: string) =>
    fields.find((field) => field.id.endsWith(suffix));
  return {
    status: findStatusField(fields),
    priority: findPriorityField(fields) ?? byId("field-priority"),
    person:
      fields.find((field) => field.fieldType === "person") ??
      byId("field-person"),
    due:
      fields.find((field) => field.fieldType === "date") ??
      byId("field-due-date"),
  };
}

function isDone(card: CardDetail): boolean {
  return card.statusCategory === "done" || Boolean(card.completedAt);
}

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
  onDuplicate,
  onArchive,
  onMoveToGroup,
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
  onDuplicate: (cardId: string) => void;
  onArchive: (cardId: string) => void;
  onMoveToGroup?: (cardId: string, groupId: string) => void;
}) {
  const fullPageReasonId = useId();
  const otherGroups = groups.filter((group) => group.id !== card.listId);
  const canMove =
    capabilities.canMoveCard && !!onMoveToGroup && otherGroups.length > 0;
  const hasMenu =
    capabilities.canCreateCard || capabilities.canDelete || canMove;

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
      {/* No full-page item route exists yet: focusable, explained, inert. */}
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
            {capabilities.canDelete ? (
              <DropdownMenuItem onSelect={() => onArchive(card.id)}>
                {copy.head.archive}
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
}: {
  card: CardDetail;
  titleId: string;
  editable: boolean;
  copy: ItemSheetCopy;
  onRenameTitle: (cardId: string, patch: UpdateCardInput) => void;
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
    <h2
      id={titleId}
      className="m-0 text-sheet-title text-balance text-foreground"
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
    </h2>
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
}: {
  board: Board;
  card: CardDetail;
  capabilities: TaskDetailCapabilities;
  copy: ItemSheetCopy;
  now: Date;
  timeZone?: string;
  onUpdateFieldValue: (payload: UpdateFieldValueInput) => void;
}) {
  const readOnly = !capabilities.canEditFields;
  const addPropertyReasonId = useId();
  const fields = findFields(board);
  const t = copy.properties;

  function update(field: FieldDefinition | undefined, value: unknown) {
    if (!field || readOnly) return;
    onUpdateFieldValue({ cardId: card.id, fieldDefinitionId: field.id, value });
  }

  // Status
  const statusId = card.statusOptionId ?? card.status;
  const statusOption = fields.status?.options.find(
    (option) => option.id === statusId,
  );
  const category = card.statusCategory ?? statusOption?.category ?? null;
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
  const selectedUserIds = new Set(card.members.map((member) => member.userId));
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
  const due = describeSheetDue(dueAt, now, isDone(card), copy, timeZone);
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

  const label = card.labels[0];

  return (
    <div role="group" aria-label={t.group} className="flex flex-wrap gap-1.5">
      {readOnly || !fields.status ? (
        statusPill()
      ) : (
        <DropdownMenu>
          <DropdownMenuTrigger asChild>{statusPill()}</DropdownMenuTrigger>
          <DropdownMenuContent align="start">
            {fields.status.options.map((option) => (
              <DropdownMenuItem
                key={option.id}
                onSelect={() => update(fields.status, option.id)}
              >
                {option.label}
              </DropdownMenuItem>
            ))}
          </DropdownMenuContent>
        </DropdownMenu>
      )}

      {readOnly || !fields.person ? (
        assigneePill()
      ) : (
        <Popover>
          <PopoverTrigger asChild>{assigneePill()}</PopoverTrigger>
          <PopoverContent align="start" className="w-64 p-1">
            <ul aria-label={t.assignee} className="flex flex-col gap-0.5">
              {board.members.map((member) => {
                const selected = selectedUserIds.has(member.userId);
                return (
                  <li key={member.id}>
                    <button
                      type="button"
                      aria-pressed={selected}
                      className="flex w-full items-center gap-2 rounded-md px-2 py-1.5 text-left text-sm hover:bg-state-hover"
                      onClick={() => {
                        const next = new Set(selectedUserIds);
                        if (selected) next.delete(member.userId);
                        else next.add(member.userId);
                        update(fields.person, Array.from(next));
                      }}
                    >
                      <Avatar className="size-6">
                        <AvatarFallback className="bg-foreground text-caption font-bold text-background">
                          {member.initials}
                        </AvatarFallback>
                      </Avatar>
                      <span className="min-w-0 flex-1 truncate">
                        {member.name}
                      </span>
                    </button>
                  </li>
                );
              })}
            </ul>
          </PopoverContent>
        </Popover>
      )}

      {readOnly || !fields.due ? (
        duePill()
      ) : (
        <Popover>
          <PopoverTrigger asChild>{duePill()}</PopoverTrigger>
          <PopoverContent align="start" className="w-auto p-0">
            <Calendar
              mode="single"
              selected={dueAt ? new Date(dueAt) : undefined}
              onSelect={(date: Date | undefined) =>
                update(fields.due, date ? date.toISOString() : null)
              }
            />
            {dueAt ? (
              <div className="flex justify-end border-t border-border p-2">
                <Button
                  type="button"
                  variant="ghost"
                  size="sm"
                  className="text-danger-text"
                  onClick={() => update(fields.due, null)}
                >
                  {t.clearDue}
                </Button>
              </div>
            ) : null}
          </PopoverContent>
        </Popover>
      )}

      {readOnly || !fields.priority ? (
        priorityPill()
      ) : (
        <DropdownMenu>
          <DropdownMenuTrigger asChild>{priorityPill()}</DropdownMenuTrigger>
          <DropdownMenuContent align="start">
            {fields.priority.options.map((option) => (
              <DropdownMenuItem
                key={option.id}
                onSelect={() => update(fields.priority, option.id)}
              >
                {option.label}
              </DropdownMenuItem>
            ))}
          </DropdownMenuContent>
        </DropdownMenu>
      )}

      {label ? (
        <span className={cn(PILL_CLASS, "hover:bg-popover")}>
          <span className="sr-only">{`${t.labels}: `}</span>
          <span
            className={cn(
              "inline-flex h-5.5 items-center rounded-sm px-2 text-micro font-semibold",
              ITEM_LABEL_TONE_CLASS[itemLabelTone(label)],
            )}
          >
            {label.name}
          </span>
        </span>
      ) : null}

      {/* Visible for everyone, inert until custom properties exist. */}
      <button
        type="button"
        aria-label={t.addProperty}
        aria-disabled="true"
        aria-describedby={addPropertyReasonId}
        title={t.addPropertyUnavailable}
        className={cn(PILL_CLASS, "border-transparent text-muted-foreground")}
      >
        <Plus aria-hidden="true" className="size-4" />
      </button>
      <span id={addPropertyReasonId} className="sr-only">
        {t.addPropertyUnavailable}
      </span>
    </div>
  );
}

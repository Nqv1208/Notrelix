import { useId, useState, type ReactNode } from "react";
import { ChevronDown, MoreHorizontal, Paperclip, Zap } from "lucide-react";
import {
  Avatar,
  AvatarFallback,
  Command,
  CommandEmpty,
  CommandGroup,
  CommandInput,
  CommandItem,
  CommandList,
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
  Popover,
  PopoverAnchor,
  PopoverContent,
  PopoverTrigger,
  Skeleton,
  Textarea,
  cn,
} from "@notrelix/ui-web";
import type {
  ActivityDetail,
  CardActivity,
  CardDetail,
  CardFile,
  CardMember,
  CardUpdate,
} from "@notrelix/work-management-core";
import { personInitials } from "@notrelix/work-management-core";
import {
  calendarDaysFromToday,
  identityMarkClass,
} from "../shared/item-display";
import { useCommentEdit } from "../card-detail/use-comment-edit";
import { useCommentDraft } from "../card-detail/update-composer";
import type {
  TaskDetailCallbacks,
  TaskDetailCapabilities,
  TaskDetailData,
} from "../card-detail/task-detail-types";
import { canWriteComment } from "../card-detail/task-detail-types";
import type { ItemPageCopy } from "./item-page.copy";
import { fillSheetCopy } from "../card-detail/item-sheet.copy";
import {
  buildActivityDays,
  countActivity,
  describeFile,
  formatClock,
  formatEntryDay,
  formatFileSize,
  type ActivityEntry,
} from "./item-page-format";
import {
  BUTTON_SM_VARIANT_CLASS,
  DisabledControl,
  ICON_BUTTON_CLASS,
  PersonMark,
} from "./item-page-parts";
import type { ItemPagePerson, ItemPageTab } from "./item-page-types";

const KEY_CLASS =
  "rounded-[4px] border border-line-strong bg-keycap px-1.25 font-mono text-key text-muted-foreground";

const TAB_ORDER: readonly ItemPageTab[] = ["all", "comments", "history"];

/** How many of the newest entries a tab lists before the rest folds (the mockup's). */
const VISIBLE_LIMIT: Readonly<Partial<Record<ItemPageTab, number>>> = {
  all: 5,
  history: 8,
};

const initialsOf = personInitials;

/**
 * "Hoạt động": the filter tabs (all, comments, history), the day-grouped
 * list and the composer. The history lines are the recorded action only: the
 * activity contract carries no before/after values, no field and no
 * attachments yet.
 */
export function ItemPageActivity({
  card,
  data,
  capabilities,
  callbacks,
  copy,
  now,
  timeZone,
  tab,
  onSelectTab,
  members,
  viewer,
}: {
  card: CardDetail;
  data: TaskDetailData;
  capabilities: TaskDetailCapabilities;
  callbacks: TaskDetailCallbacks;
  copy: ItemPageCopy;
  now: Date;
  timeZone?: string;
  tab: ItemPageTab;
  onSelectTab: (tab: ItemPageTab) => void;
  members: readonly CardMember[];
  /** The signed-in person, for the composer's mark. */
  viewer?: ItemPagePerson;
}) {
  const t = copy.activity;
  const baseId = useId();
  const [newestFirst, setNewestFirst] = useState(false);
  const [expanded, setExpanded] = useState(false);
  const counts = countActivity(data.updates, data.activity);
  const loading =
    (tab !== "history" && data.updatesLoading) ||
    (tab !== "comments" && data.activityLoading);
  // The history is listed newest first, as the mockup does; the other tabs
  // follow the sort. Older entries fold behind "Hiện N … cũ hơn".
  const listNewestFirst = tab === "history" ? true : newestFirst;
  const { days, hidden } = buildActivityDays({
    updates: data.updates,
    activity: data.activity,
    tab,
    newestFirst: listNewestFirst,
    now,
    copy,
    timeZone,
    limit: expanded ? undefined : (VISIBLE_LIMIT[tab] ?? undefined),
  });
  const olderRowShown = !listNewestFirst && hidden.length > 0;
  const olderRow =
    hidden.length > 0 ? (
      <OlderRow
        hidden={hidden}
        tab={tab}
        copy={copy}
        now={now}
        timeZone={timeZone}
        onShow={() => setExpanded(true)}
      />
    ) : null;
  const labels: Readonly<Record<ItemPageTab, string>> = {
    all: t.all,
    comments: t.comments,
    history: t.history,
  };
  const empty =
    tab === "all"
      ? t.emptyAll
      : tab === "comments"
        ? t.emptyComments
        : t.emptyHistory;

  function handleKeyDown(event: React.KeyboardEvent<HTMLElement>) {
    if (event.key !== "ArrowRight" && event.key !== "ArrowLeft") return;
    event.preventDefault();
    const step = event.key === "ArrowRight" ? 1 : -1;
    const next =
      TAB_ORDER[
        (TAB_ORDER.indexOf(tab) + step + TAB_ORDER.length) % TAB_ORDER.length
      ]!;
    onSelectTab(next);
    document.getElementById(`${baseId}-${next}`)?.focus();
  }

  return (
    <section aria-labelledby={`${baseId}-heading`}>
      <div className="mb-3.5 flex flex-wrap items-center gap-2 border-b border-line">
        <h2
          id={`${baseId}-heading`}
          className="m-0 mr-3 flex items-center gap-2 text-item-heading whitespace-nowrap text-foreground"
        >
          {t.heading}
        </h2>
        <nav
          aria-label={t.filter}
          onKeyDown={handleKeyDown}
          className="flex gap-0.5"
        >
          {TAB_ORDER.map((value) => {
            const selected = tab === value;
            return (
              <a
                key={value}
                id={`${baseId}-${value}`}
                href={`#${value}`}
                aria-current={selected ? "page" : undefined}
                tabIndex={selected ? 0 : -1}
                onClick={(event) => {
                  event.preventDefault();
                  setExpanded(false);
                  onSelectTab(value);
                }}
                className={cn(
                  "-mb-px inline-flex h-9.5 items-center gap-1.5 border-b-2 px-2.5 whitespace-nowrap text-item-ui font-medium outline-none focus-visible:ring-2 focus-visible:ring-focus-ring",
                  selected
                    ? "border-primary font-semibold text-foreground"
                    : "border-transparent text-muted-foreground hover:text-foreground",
                )}
              >
                {labels[value]}
                <span className="font-normal text-meta-foreground">
                  {counts[value]}
                </span>
              </a>
            );
          })}
        </nav>
        {tab === "history" ? (
          <DisabledControl reason={t.allFieldsUnavailable}>
            <button
              type="button"
              aria-disabled="true"
              className={SORT_PILL_CLASS}
            >
              {t.allFields}
              <ChevronDown aria-hidden="true" className="size-3.25" />
            </button>
          </DisabledControl>
        ) : (
          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <button type="button" className={SORT_PILL_CLASS}>
                {newestFirst ? t.sortNewest : t.sortOldest}
                <ChevronDown aria-hidden="true" className="size-3.25" />
              </button>
            </DropdownMenuTrigger>
            <DropdownMenuContent align="end">
              <DropdownMenuItem onSelect={() => setNewestFirst(false)}>
                {t.sortOldest}
              </DropdownMenuItem>
              <DropdownMenuItem onSelect={() => setNewestFirst(true)}>
                {t.sortNewest}
              </DropdownMenuItem>
            </DropdownMenuContent>
          </DropdownMenu>
        )}
      </div>

      {loading ? (
        <div
          className="flex flex-col gap-2"
          aria-busy="true"
          aria-label={t.loading}
        >
          <Skeleton className="h-12 rounded-lg" />
          <Skeleton className="h-12 rounded-lg" />
        </div>
      ) : days.length === 0 ? (
        <p className="m-0 text-item-text text-muted-foreground">{empty}</p>
      ) : (
        <>
          {!listNewestFirst ? olderRow : null}
          {days.map((day, dayIndex) => (
            <div key={day.key}>
              <div
                className={cn(
                  "mb-1.5 text-item-note font-semibold text-meta-foreground",
                  (dayIndex > 0 || olderRowShown) && "mt-4",
                )}
              >
                {day.heading}
              </div>
              {day.entries.map((entry) =>
                entry.kind === "comment" ? (
                  <CommentEntry
                    key={`c-${entry.update.id}`}
                    update={entry.update}
                    files={data.files}
                    now={now}
                    capabilities={capabilities}
                    callbacks={callbacks}
                    copy={copy}
                    timeZone={timeZone}
                  />
                ) : (
                  <EventEntry
                    key={`e-${entry.event.id}`}
                    event={entry.event}
                    tab={tab}
                    copy={copy}
                    timeZone={timeZone}
                  />
                ),
              )}
            </div>
          ))}
          {listNewestFirst ? olderRow : null}
        </>
      )}
      {tab === "history" && days.length > 0 ? (
        <p className="m-0 mt-1 py-1 pl-10.5 text-crumb text-meta-foreground">
          {t.historyNote}
        </p>
      ) : null}

      {tab !== "history" ? (
        <PageComposer
          cardId={card.id}
          members={members}
          capabilities={capabilities}
          callbacks={callbacks}
          copy={copy}
          viewer={viewer}
        />
      ) : null}
    </section>
  );
}

const SORT_PILL_CLASS =
  "ml-auto inline-flex h-7 items-center gap-[5px] rounded-[7px] border-0 bg-transparent px-2 text-crumb text-muted-foreground outline-none hover:bg-state-hover-strong focus-visible:ring-2 focus-visible:ring-focus-ring aria-disabled:cursor-default aria-disabled:opacity-60 aria-disabled:hover:bg-transparent";

function EntryTime({ value, timeZone }: { value: string; timeZone?: string }) {
  return (
    <time
      dateTime={value}
      className="ml-1.5 text-item-note whitespace-nowrap text-meta-foreground"
    >
      {formatClock(value, timeZone)}
    </time>
  );
}

/** The "Hiện N … cũ hơn" row: what is folded and from when to when. */
function OlderRow({
  hidden,
  tab,
  copy,
  now,
  timeZone,
  onShow,
}: {
  hidden: readonly ActivityEntry[];
  tab: ItemPageTab;
  copy: ItemPageCopy;
  now: Date;
  timeZone?: string;
  onShow: () => void;
}) {
  const t = copy.activity;
  const comments = hidden.filter((entry) => entry.kind === "comment").length;
  const changes = hidden.length - comments;
  const from = formatEntryDay(hidden[0]!.at, now, timeZone);
  const to = formatEntryDay(hidden[hidden.length - 1]!.at, now, timeZone);
  const created = hidden.some(
    (entry) => entry.kind === "event" && entry.event.type === "created",
  );
  const summary =
    tab === "history"
      ? fillSheetCopy(t.olderHistory, { from, to }) +
        (created ? t.olderHistoryCreated : "")
      : fillSheetCopy(t.olderAll, { changes, comments, from, to });
  return (
    <div className="flex items-center gap-2 py-1 pl-10.5">
      <button
        type="button"
        onClick={onShow}
        className="-ml-1.5 cursor-pointer rounded-[6px] border-0 bg-transparent px-1.5 py-[3px] text-item-small font-medium text-brand-subtle-foreground hover:bg-state-selected"
      >
        {fillSheetCopy(tab === "history" ? t.showOlderHistory : t.showOlder, {
          count: hidden.length,
        })}
      </button>
      <span className="text-item-note text-meta-foreground">{summary}</span>
    </div>
  );
}

const INSERT_CLASS =
  "rounded-[5px] bg-item-file-sheet px-1.5 font-medium text-item-file-sheet-foreground";

/** What a history line changed: a before and after, a link, a file or an edited description. */
function DetailRow({
  detail,
  copy,
}: {
  detail: ActivityDetail;
  copy: ItemPageCopy;
}) {
  return (
    <div className="mt-1 inline-flex flex-wrap items-center gap-1.5">
      {detail.kind === "diff" ? (
        <>
          {detail.label ? <span>{detail.label}</span> : null}
          <span className="text-meta-foreground line-through decoration-line-strong">
            {detail.from}
          </span>
          <span aria-hidden="true">→</span>
          <span className="font-medium text-foreground">{detail.to}</span>
          {detail.note ? (
            <span className="text-item-note text-meta-foreground">
              {detail.note}
            </span>
          ) : null}
        </>
      ) : detail.kind === "link" ? (
        <>
          <span>{detail.relation}</span>
          <span className={INSERT_CLASS}>
            {detail.key} {detail.title}
          </span>
        </>
      ) : detail.kind === "file" ? (
        <span className={INSERT_CLASS}>{detail.name}</span>
      ) : (
        <>
          <span>{detail.label}</span>
          <span className={INSERT_CLASS}>{detail.added}</span>
          <DisabledControl reason={copy.activity.compareUnavailable}>
            <button
              type="button"
              aria-disabled="true"
              className={cn(BUTTON_SM_VARIANT_CLASS.ghost, "h-6 px-1.5")}
            >
              {copy.activity.compare}
            </button>
          </DisabledControl>
        </>
      )}
    </div>
  );
}

function EventEntry({
  event,
  tab,
  copy,
  timeZone,
}: {
  event: CardActivity;
  tab: ItemPageTab;
  copy: ItemPageCopy;
  timeZone?: string;
}) {
  // The short verb and its accent target when the producer gives them.
  const compact = tab === "all" ? event.compact : undefined;
  const sentence = compact?.verb ?? event.verb ?? event.action;
  const target = compact ? compact.target : event.target;
  const detail = compact?.hideDetail ? undefined : event.detail;
  return (
    <div className="flex items-start gap-3 py-[5px] text-item-ui text-muted-foreground">
      <span className="grid w-7.5 flex-none place-items-center pt-0.5">
        {event.automationName ? (
          <span
            aria-hidden="true"
            className="grid size-5 place-items-center rounded-[6px] bg-state-hover-strong text-muted-foreground"
          >
            <Zap className="size-3.25" />
          </span>
        ) : (
          <span
            aria-hidden="true"
            className={cn(
              "grid size-5 place-items-center rounded-full text-mark-3xs",
              identityMarkClass(event.actor),
            )}
          >
            {initialsOf(event.actor)}
          </span>
        )}
      </span>
      <div>
        <b className="font-semibold text-foreground">{event.actor}</b>{" "}
        {sentence}
        {target ? (
          <>
            {" "}
            <span className="font-medium text-foreground">{target}</span>
          </>
        ) : null}
        {tab === "all" && event.notified ? ` ${event.notified}` : null}
        <EntryTime value={event.createdAt} timeZone={timeZone} />
        {detail ? <DetailRow detail={detail} copy={copy} /> : null}
      </div>
    </div>
  );
}

/** The comment text with its @mentions of people drawn in the accent. */
function CommentBody({ body }: { body: string }) {
  const parts = body.split(/(@[\p{L}][\p{L}\p{M}' -]*[\p{L}\p{M}])/u);
  return (
    <>
      {parts.map((part, index) =>
        part.startsWith("@") ? (
          <span
            key={index}
            className="font-medium text-brand-subtle-foreground"
          >
            {part}
          </span>
        ) : (
          part
        ),
      )}
    </>
  );
}

function CommentEntry({
  update,
  files,
  now,
  capabilities,
  callbacks,
  copy,
  timeZone,
}: {
  update: CardUpdate;
  files: readonly CardFile[];
  now: Date;
  capabilities: TaskDetailCapabilities;
  callbacks: TaskDetailCallbacks;
  copy: ItemPageCopy;
  timeZone?: string;
}) {
  const t = copy.activity;
  const edit = useCommentEdit(update.body, (body) =>
    callbacks.onUpdateUpdate(update.id, body),
  );
  const canEdit = capabilities.canEditFields;
  const canDelete = capabilities.canDelete;
  const attached = update.attachmentIds.flatMap((id) => {
    const file = files.find((candidate) => candidate.id === id);
    return file ? [file] : [];
  });
  const days = calendarDaysFromToday(update.createdAt, now, timeZone);
  // A comment older than yesterday names its day: "05/10 14:22".
  const stamp =
    days !== null && days < -1
      ? `${formatEntryDay(update.createdAt, now, timeZone)} ${formatClock(update.createdAt, timeZone)}`
      : formatClock(update.createdAt, timeZone);

  return (
    <div className="group/comment flex gap-3 py-2">
      <PersonMark person={update.author} className="size-7.5 text-avatar-lg" />
      <div className="relative min-w-0 flex-1 rounded-[10px] border border-line bg-popover px-3.5 py-2.5">
        <b className="font-semibold">{update.author.name}</b>
        {attached.length > 0 ? ` ${t.commentWithFile}` : null}
        <time
          dateTime={update.createdAt}
          className="ml-1.5 text-item-note whitespace-nowrap text-meta-foreground"
        >
          {stamp}
        </time>
        {canEdit || canDelete ? (
          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <button
                type="button"
                aria-label={t.actions}
                className={cn(
                  ICON_BUTTON_CLASS,
                  "absolute top-1.5 right-1.5 size-6 opacity-0 group-hover/comment:opacity-100 focus-visible:opacity-100 data-[state=open]:opacity-100 pointer-coarse:opacity-100",
                )}
              >
                <MoreHorizontal aria-hidden="true" className="size-3.5" />
              </button>
            </DropdownMenuTrigger>
            <DropdownMenuContent align="end">
              {canEdit ? (
                <DropdownMenuItem onSelect={edit.start}>
                  {t.edit}
                </DropdownMenuItem>
              ) : null}
              {canDelete ? (
                <DropdownMenuItem
                  onSelect={() => callbacks.onDeleteUpdate(update.id)}
                >
                  {t.delete}
                </DropdownMenuItem>
              ) : null}
            </DropdownMenuContent>
          </DropdownMenu>
        ) : null}
        {edit.editing ? (
          <div className="mt-2 flex flex-col gap-2">
            <Textarea
              value={edit.body}
              aria-label={t.editLabel}
              onChange={(event) => edit.setBody(event.target.value)}
              className="min-h-16 resize-none text-sm"
            />
            <div className="flex justify-end gap-1.5">
              <button
                type="button"
                className={BUTTON_SM_VARIANT_CLASS.ghost}
                onClick={edit.cancel}
              >
                {t.cancel}
              </button>
              <button
                type="button"
                className={BUTTON_SM_VARIANT_CLASS.primary}
                onClick={edit.save}
              >
                {t.save}
              </button>
            </div>
          </div>
        ) : (
          <p className="m-0 mt-1 text-item-comment whitespace-pre-wrap text-foreground">
            <CommentBody body={update.body} />
          </p>
        )}
        {attached.map((file) => {
          const kind = describeFile(file);
          const size = formatFileSize(file.size, "vi");
          return (
            <a
              key={file.id}
              href={file.url || undefined}
              download={file.name}
              aria-label={fillSheetCopy(t.downloadFile, {
                name: file.name,
                size,
              })}
              className="mt-2 inline-flex items-center gap-2 rounded-[8px] border border-line py-1.5 pr-2.5 pl-1.5 text-item-small font-medium text-foreground no-underline"
            >
              <span
                aria-hidden="true"
                className={cn(
                  "grid size-7 place-items-center rounded-[7px] text-[8.5px] font-bold tracking-[0.02em]",
                  kind.kind === "sheet"
                    ? "bg-item-file-sheet text-item-file-sheet-foreground"
                    : kind.kind === "pdf"
                      ? "bg-item-file-pdf text-item-file-pdf-foreground"
                      : "bg-state-hover-strong text-muted-foreground",
                )}
              >
                {kind.label}
              </span>
              <span>
                {file.name}{" "}
                <small className="font-normal text-meta-foreground">
                  {size}
                </small>
              </span>
            </a>
          );
        })}
        <div className="mt-2 flex gap-2 text-crumb text-meta-foreground">
          <ReplyAction reason={t.replyUnavailable}>{t.reply}</ReplyAction>
          <ReplyAction reason={t.reactUnavailable}>{t.react}</ReplyAction>
        </div>
      </div>
    </div>
  );
}

function ReplyAction({
  reason,
  children,
}: {
  reason: string;
  children: ReactNode;
}) {
  return (
    <DisabledControl reason={reason}>
      <button
        type="button"
        aria-disabled="true"
        className="-ml-1.5 rounded-[5px] border-0 bg-transparent px-1.5 py-0.5 text-crumb text-meta-foreground aria-disabled:cursor-default"
      >
        {children}
      </button>
    </DisabledControl>
  );
}

function PageComposer({
  cardId,
  members,
  capabilities,
  callbacks,
  copy,
  viewer,
}: {
  cardId: string;
  members: readonly CardMember[];
  capabilities: TaskDetailCapabilities;
  callbacks: TaskDetailCallbacks;
  copy: ItemPageCopy;
  viewer?: ItemPagePerson;
}) {
  const t = copy.composer;
  const disabled = !canWriteComment(capabilities);
  const draft = useCommentDraft({
    cardId,
    members,
    disabled,
    onCreateUpdate: callbacks.onCreateUpdate,
  });

  return (
    <div className="mt-3 flex gap-3">
      {viewer ? (
        <PersonMark person={viewer} className="size-7.5 text-avatar-lg" />
      ) : (
        <Avatar className="size-7.5">
          <AvatarFallback className="bg-state-hover-strong text-avatar-lg" />
        </Avatar>
      )}
      <Popover open={draft.mentionOpen} onOpenChange={draft.setMentionOpen}>
        <PopoverAnchor asChild>
          <div className="flex min-w-0 flex-1 flex-col gap-2.5 rounded-[10px] border border-line-strong bg-popover px-3 py-2.5 focus-within:ring-2 focus-within:ring-focus-ring">
            <Textarea
              value={draft.body}
              rows={2}
              disabled={disabled}
              aria-label={t.label}
              placeholder={t.placeholder}
              onChange={(event) => draft.changeBody(event.target.value)}
              onKeyDown={draft.handleKeyDown}
              className="h-12 min-h-11 resize-none rounded-none border-0 bg-transparent p-0.5 text-item-text shadow-none focus-visible:ring-0"
            />
            <span className="flex items-center gap-1.5 text-item-note text-meta-foreground">
              <DisabledControl reason={t.attachUnavailable}>
                <button
                  type="button"
                  aria-disabled="true"
                  aria-label={t.attach}
                  className={cn(ICON_BUTTON_CLASS, "size-7")}
                >
                  <Paperclip aria-hidden="true" />
                </button>
              </DisabledControl>
              <PopoverTrigger asChild>
                <button
                  type="button"
                  aria-label={t.mention}
                  title={t.mention}
                  disabled={disabled || members.length === 0}
                  className={cn(
                    ICON_BUTTON_CLASS,
                    "size-7 disabled:opacity-40",
                  )}
                >
                  <span aria-hidden="true" className="text-item-ui font-medium">
                    @
                  </span>
                </button>
              </PopoverTrigger>
              <span className="flex-1" />
              <kbd className={KEY_CLASS}>{t.shortcut}</kbd>
              <button
                type="button"
                disabled={!draft.canSubmit}
                onClick={draft.submit}
                className={cn(
                  BUTTON_SM_VARIANT_CLASS.primary,
                  "disabled:cursor-default disabled:opacity-50",
                )}
              >
                {t.submit}
              </button>
            </span>
          </div>
        </PopoverAnchor>
        <PopoverContent align="start" className="w-60 p-0">
          <Command>
            <CommandInput placeholder={t.searchPeople} />
            <CommandList>
              <CommandEmpty>{t.noPeople}</CommandEmpty>
              <CommandGroup heading={t.people}>
                {members.map((member) => (
                  <CommandItem
                    key={member.userId}
                    value={member.name}
                    onSelect={() => draft.mention(member)}
                  >
                    <PersonMark
                      person={member}
                      className="size-5 text-avatar-sm"
                    />
                    {member.name}
                  </CommandItem>
                ))}
              </CommandGroup>
            </CommandList>
          </Command>
        </PopoverContent>
      </Popover>
    </div>
  );
}

import { useState, type FormEvent, type ReactNode } from "react";
import {
  Calendar,
  CircleCheck,
  Clock,
  FileText,
  Plus,
  SquareKanban,
} from "lucide-react";
import { Skeleton, cn } from "@notrelix/ui-web";
import { describeMyWorkDue, type MyWorkGroup } from "../../core/model/my-work";
import {
  destinationForEntry,
  initialsFor,
  NAVIGATION_RESOURCE_KIND,
  type IdentityTone,
  type WorkspaceDestination,
} from "../../core/model/navigation";
import { ShellLink } from "../shell/shell-link";
import {
  formatCopy,
  IDENTITY_MARK_CLASS,
  type ShellNavigation,
} from "../shell/shell-models";
import type {
  ContinueEntry,
  MyWorkGroupView,
  MyWorkItemView,
  SectionState,
  SurfaceFormatters,
} from "./home-models";
import { pluralForm } from "./home-models";
import {
  WorkspaceOnboardingSurface,
  type WorkspaceOnboardingSurfaceProps,
} from "./workspace-onboarding-surface";
import {
  DepartmentMark,
  dueWords,
  PriorityFlag,
  STATUS_PILL_CLASS,
  StatusDot,
  TickButton,
} from "./work-row-parts";
import type { WorkspaceHomeCopy } from "./workspace-home.copy";

/** A person's reply request on Home: an approval or a mention (no producer yet). */
export interface HomeReplyView {
  readonly id: string;
  readonly kind: "approve" | "mention";
  readonly actorName: string;
  readonly actorTone: IdentityTone;
  /** Relative time label, already formatted. */
  readonly when: string;
  readonly text: string;
  /** Where "Xem" / "Mở tài liệu" lead. */
  readonly target?: WorkspaceDestination;
  /** The item key the approval is about ("ERP-138"), for accessible names. */
  readonly key?: string | null;
}

/** What the member can do with a reply request; every write is the host's. */
export interface HomeReplyActions {
  /** Requests in flight (their buttons wait). */
  readonly pendingIds: ReadonlySet<string>;
  readonly onApprove: (reply: HomeReplyView) => void;
  readonly onRequestChanges: (reply: HomeReplyView) => void;
  readonly onReply: (reply: HomeReplyView, text: string) => void;
}

/** One line of today's schedule (no producer yet). */
export interface HomeAgendaEntry {
  readonly id: string;
  readonly time: string;
  readonly title: string;
  readonly detail: string;
  readonly past: boolean;
}

/** Today's schedule with the time of the now line. */
export interface HomeAgenda {
  readonly entries: readonly HomeAgendaEntry[];
  /** `HH:mm` of the now line, inserted after the last past entry. */
  readonly nowTime: string;
}

export interface WorkspaceHomeSurfaceProps {
  readonly copy: WorkspaceHomeCopy;
  readonly navigation: ShellNavigation;
  readonly formatters: SurfaceFormatters;
  /** Reference time for due words and the greeting (epoch ms). */
  readonly now: number;
  /** `null` while the workspace record loads. */
  readonly workspaceName: string | null;
  /** The name to greet; `null` greets without one. */
  readonly greetingName: string | null;
  /** Recently opened Boards and Docs. */
  readonly continueState: SectionState<readonly ContinueEntry[]>;
  /** The member's open Items grouped by due window. */
  readonly myWork: SectionState<readonly MyWorkGroupView[]>;
  /** More assigned items exist than the groups hold. */
  readonly myWorkHasMore: boolean;
  readonly completingIds: ReadonlySet<string>;
  /** `null`: no inline completion (the tick is drawn inert). */
  readonly onComplete: ((item: MyWorkItemView) => void) | null;
  /**
   * Approvals and mentions waiting for the member, and the counts the quick
   * filters show. `undefined` = no producer: the card and the chips stay out.
   */
  readonly replies?: SectionState<readonly HomeReplyView[]>;
  readonly replyActions?: HomeReplyActions;
  /** What the member already completed today (the empty "Hôm nay" line). */
  readonly completedToday?: {
    readonly count: number;
    readonly time: string;
  } | null;
  readonly approvalCount?: number;
  readonly mentionCount?: number;
  /** Today's schedule; `undefined` = no producer. */
  readonly agenda?: SectionState<HomeAgenda>;
  /** How many meetings today (the summary line); `undefined` = unknown. */
  readonly meetingCount?: number;
  /** Where "Lịch của tôi" goes; `null` draws it inert. */
  readonly calendarHref?: string | null;
  /** `null`: "Thêm công việc" is drawn inert with the reason. */
  readonly onAddItem: (() => void) | null;
  /** A new workspace: onboarding replaces the work sections. */
  readonly onboarding: Pick<
    WorkspaceOnboardingSurfaceProps,
    "canCreate" | "canInvite"
  > | null;
  readonly onCreateBoard: () => void;
  readonly onCreateDoc: () => void;
  readonly onInvite: () => void;
  readonly onRetryContinue: () => void;
  readonly onRetryMyWork: () => void;
}

const BUTTON_CLASS =
  "inline-flex h-9 shrink-0 items-center gap-1.5 rounded-[8px] border border-line-strong bg-popover px-3 text-compact leading-[1.45] font-medium whitespace-nowrap text-foreground no-underline outline-none hover:bg-state-hover focus-visible:ring-2 focus-visible:ring-focus-ring aria-disabled:cursor-not-allowed aria-disabled:hover:bg-popover dark:bg-card";
const PRIMARY_BUTTON_CLASS =
  "inline-flex h-[34px] shrink-0 items-center gap-1.5 rounded-[8px] border border-primary bg-primary px-3 text-compact leading-[1.45] font-semibold whitespace-nowrap text-primary-foreground outline-none hover:bg-primary/90 focus-visible:ring-2 focus-visible:ring-focus-ring aria-disabled:cursor-not-allowed aria-disabled:hover:bg-primary";
const SMALL_BUTTON_CLASS =
  "inline-flex h-7 shrink-0 items-center gap-1.5 rounded-[8px] border border-line-strong bg-popover px-2.5 text-crumb leading-[1.45] font-medium whitespace-nowrap text-foreground no-underline outline-none hover:bg-state-hover focus-visible:ring-2 focus-visible:ring-focus-ring aria-disabled:cursor-not-allowed dark:bg-card";
const SMALL_PRIMARY_CLASS =
  "inline-flex h-7 shrink-0 items-center gap-1.5 rounded-[8px] border border-primary bg-primary px-2.5 text-crumb leading-[1.45] font-semibold whitespace-nowrap text-primary-foreground outline-none hover:bg-primary/90 focus-visible:ring-2 focus-visible:ring-focus-ring aria-disabled:cursor-not-allowed";
const PANEL_CLASS =
  "rounded-[12px] border border-border bg-popover dark:bg-card";
const HEADING_CLASS =
  "mb-2 flex items-center gap-2 text-sm leading-[1.45] font-[650]";
const COUNT_CLASS = "font-medium text-meta-foreground";

/** The greeting key for the local hour of `now`. */
export function greetingPeriod(
  now: number,
): "morning" | "noon" | "afternoon" | "evening" {
  const hour = new Date(now).getHours();
  if (hour < 11) return "morning";
  if (hour < 13) return "noon";
  if (hour < 18) return "afternoon";
  return "evening";
}

function Section({
  id,
  label,
  tone,
  count,
  action,
  busy,
  children,
}: {
  id: string;
  label: string;
  tone?: "over";
  count?: number;
  action?: ReactNode;
  busy?: boolean;
  children: ReactNode;
}) {
  return (
    <section aria-labelledby={id} aria-busy={busy ? true : undefined}>
      <h2
        id={id}
        data-f="h2"
        className={cn(HEADING_CLASS, tone === "over" && "text-danger-text")}
      >
        {label}
        {count !== undefined ? (
          <span className={COUNT_CLASS}>{count}</span>
        ) : null}
        {action}
      </h2>
      {children}
    </section>
  );
}

function TaskRow({
  item,
  copy,
  navigation,
  now,
  completing,
  onComplete,
}: {
  item: MyWorkItemView;
  copy: WorkspaceHomeCopy;
  navigation: ShellNavigation;
  now: number;
  completing: boolean;
  onComplete: WorkspaceHomeSurfaceProps["onComplete"];
}) {
  const due = dueWords(copy, describeMyWorkDue(item.dueAt, now), {
    timeOnToday: true,
  });
  const detail: string[] = [];
  if (item.boardName) detail.push(item.boardName);
  return (
    <div
      data-f="row"
      className="box-content flex min-h-[54px] items-center gap-3 border-t border-border px-3.5 py-2 first:border-t-0 hover:bg-state-hover"
    >
      <TickButton
        label={formatCopy(copy.myWork.complete, { name: item.name })}
        busy={completing}
        disabled={onComplete === null}
        onClick={onComplete ? () => onComplete(item) : undefined}
      />
      <div data-f="rm" className="min-w-0 flex-1">
        <p
          data-f="rt"
          className="m-0 text-label leading-[1.45] text-foreground"
        >
          <ShellLink
            destination={{
              section: "board",
              boardId: item.boardId,
              item: item.id,
            }}
            navigation={navigation}
            className="block truncate no-underline outline-none hover:underline focus-visible:underline"
          >
            {item.name}
          </ShellLink>
        </p>
        <div
          data-f="rc"
          className="mt-0.5 flex items-center gap-1.5 text-xs leading-[1.45] text-meta-foreground"
        >
          {item.department ? (
            <DepartmentMark
              small
              name={item.department.name}
              markTone={item.department.markTone}
            />
          ) : null}
          {detail.join(" · ")}
          {item.key ? (
            <>
              {detail.length > 0 ? " · " : null}
              <span className="font-mono">{item.key}</span>
            </>
          ) : null}
          {item.reason === "follow" ? <> · {copy.home.following}</> : null}
        </div>
      </div>
      {item.priority === "urgent" || item.priority === "high" ? (
        <PriorityFlag
          priority={item.priority}
          label={formatCopy(copy.work.priorityLabel, {
            priority: copy.work.priority[item.priority],
          })}
        />
      ) : item.priority ? (
        // Medium and low draw nothing, but keep the flag's column.
        <span aria-hidden="true" data-f="pr" className="w-3.5 shrink-0" />
      ) : null}
      {item.status ? (
        <span
          data-f="pill"
          className={cn(
            "inline-flex h-6 min-w-[92px] shrink-0 items-center gap-1.5 rounded-full px-[9px] text-xs leading-[1.45] font-semibold",
            STATUS_PILL_CLASS[item.status.bucket],
          )}
        >
          <StatusDot bucket={item.status.bucket} />
          {item.status.label}
        </span>
      ) : null}
      <span
        data-f="due"
        className={cn(
          "min-w-[100px] shrink-0 text-right text-crumb leading-[1.45] whitespace-nowrap tabular-nums",
          due.tone === "over" && "font-semibold text-danger-text",
          due.tone === "today" && "font-semibold text-warning-text",
          due.tone === "normal" && "text-meta-foreground",
        )}
      >
        {due.text}
      </span>
    </div>
  );
}

function RowSkeleton() {
  return (
    <div
      data-f="row"
      className="box-content flex min-h-[54px] items-center gap-3 border-t border-border px-3.5 py-2 first:border-t-0"
    >
      <Skeleton className="size-5 shrink-0 rounded-full" />
      <div data-f="rm" className="min-w-0 flex-1">
        <Skeleton className="h-3 w-[62%] rounded-md" />
        <Skeleton className="mt-2 h-2.5 w-[34%] rounded-md" />
      </div>
      <Skeleton className="h-6 w-[92px] shrink-0 rounded-full" />
      <Skeleton className="h-3 w-[72px] shrink-0 rounded-md" />
    </div>
  );
}

function SectionError({
  copy,
  onRetry,
}: {
  copy: WorkspaceHomeCopy;
  onRetry: () => void;
}) {
  return (
    <div
      role="alert"
      className={cn(
        PANEL_CLASS,
        "flex flex-wrap items-center justify-between gap-2 px-3.5 py-3",
      )}
    >
      <p className="text-compact text-muted-foreground">
        {copy.home.sectionError}
      </p>
      <button type="button" className={SMALL_BUTTON_CLASS} onClick={onRetry}>
        {copy.home.retry}
      </button>
    </div>
  );
}

function lowerFirst(text: string): string {
  return text.charAt(0).toLowerCase() + text.slice(1);
}

function windowItems(
  groups: readonly MyWorkGroup[] | readonly MyWorkGroupView[],
  range: MyWorkGroup["range"],
): readonly MyWorkItemView[] {
  const group = groups.find((entry) => entry.range === range);
  return (group?.items ?? []) as readonly MyWorkItemView[];
}

function Chip({
  tone,
  count,
  label,
  href,
  onNavigate,
  icon,
  boxed = false,
}: {
  /** The design draws the two filters as buttons (36 px) and the rest as links (38 px). */
  boxed?: boolean;
  tone?: "over" | "today";
  count: number;
  label: string;
  href: string;
  onNavigate: () => void;
  icon?: ReactNode;
}) {
  return (
    <a
      data-f="chip"
      href={href}
      className={cn(
        boxed && "box-content",
        "inline-flex h-9 shrink-0 cursor-pointer items-center gap-2 rounded-[10px] border border-line-strong bg-popover px-3 text-nav text-foreground no-underline outline-none focus-visible:ring-2 focus-visible:ring-focus-ring dark:bg-card",
        tone === "over" &&
          "border-transparent bg-danger-subtle text-danger-text dark:bg-danger-subtle",
        tone === "today" &&
          "border-transparent bg-warning-subtle text-warning-text dark:bg-warning-subtle",
      )}
      onClick={(event) => {
        if (
          event.defaultPrevented ||
          event.button !== 0 ||
          event.metaKey ||
          event.ctrlKey ||
          event.shiftKey ||
          event.altKey
        )
          return;
        event.preventDefault();
        onNavigate();
      }}
    >
      {icon}
      <b className="font-bold tabular-nums">{count}</b> {label}
    </a>
  );
}

function ContinueCard({
  copy,
  entry,
  formatters,
  now,
  navigation,
}: {
  copy: WorkspaceHomeCopy;
  entry: ContinueEntry;
  formatters: SurfaceFormatters;
  now: number;
  navigation: ShellNavigation;
}) {
  const isBoard = entry.kind === NAVIGATION_RESOURCE_KIND.board;
  const Icon = isBoard ? SquareKanban : FileText;
  const parts = [
    isBoard ? copy.home.continue.board : copy.home.continue.doc,
    ...(entry.location ? [entry.location] : []),
    ...(entry.at ? [formatters.relativeTime(entry.at, now)] : []),
  ];
  return (
    <ShellLink
      data-f="cont"
      destination={destinationForEntry(entry)}
      navigation={navigation}
      className="-mx-2 flex items-center gap-2.5 rounded-[8px] px-2 py-[7px] no-underline outline-none hover:bg-state-hover focus-visible:ring-2 focus-visible:ring-focus-ring"
    >
      <span
        aria-hidden="true"
        data-f="contic"
        className="box-content grid size-[30px] shrink-0 place-items-center rounded-[8px] border border-border bg-background text-muted-foreground"
      >
        <Icon className="size-4" strokeWidth={1.8} />
      </span>
      <span className="min-w-0">
        <b className="block truncate text-nav leading-[1.45] font-medium text-foreground">
          {entry.name}
        </b>
        <small className="block truncate text-xs leading-[1.45] text-meta-foreground">
          {parts.join(" · ")}
        </small>
      </span>
    </ShellLink>
  );
}

function ReplyBox({
  copy,
  reply,
  busy,
  onSend,
  onCancel,
}: {
  copy: WorkspaceHomeCopy;
  reply: HomeReplyView;
  busy: boolean;
  onSend: (text: string) => void;
  onCancel: () => void;
}) {
  const [text, setText] = useState("");
  const submit = (event: FormEvent) => {
    event.preventDefault();
    if (text.trim() && !busy) onSend(text.trim());
  };
  return (
    <form
      onSubmit={submit}
      data-f="replybox"
      className="mb-2 flex flex-col gap-1.5"
    >
      <textarea
        autoFocus
        value={text}
        rows={2}
        aria-label={formatCopy(copy.home.replies.replyLabel, {
          name: reply.actorName,
        })}
        placeholder={formatCopy(copy.home.replies.replyPlaceholder, {
          name: reply.actorName,
        })}
        onChange={(event) => setText(event.target.value)}
        onKeyDown={(event) => {
          if (event.key === "Escape") onCancel();
          if ((event.metaKey || event.ctrlKey) && event.key === "Enter")
            submit(event);
        }}
        className="box-border w-full resize-none rounded-[8px] border border-line-strong bg-popover px-2.5 py-1.5 text-compact leading-[1.45] text-foreground outline-none focus:border-primary dark:bg-card"
      />
      <div className="flex gap-1.5">
        <button
          type="submit"
          aria-disabled={!text.trim() || busy ? true : undefined}
          className={SMALL_PRIMARY_CLASS}
        >
          {copy.home.replies.replySend}
        </button>
        <button type="button" onClick={onCancel} className={SMALL_BUTTON_CLASS}>
          {copy.home.replies.replyCancel}
        </button>
      </div>
    </form>
  );
}

function ReplyCard({
  copy,
  navigation,
  replies,
  actions,
}: {
  copy: WorkspaceHomeCopy;
  navigation: ShellNavigation;
  replies: readonly HomeReplyView[];
  actions?: HomeReplyActions;
}) {
  const [replyingTo, setReplyingTo] = useState<string | null>(null);
  return (
    <section
      aria-labelledby="home-replies"
      data-f="box"
      className={cn(PANEL_CLASS, "px-3.5 py-3")}
    >
      <h2 id="home-replies" data-f="boxh2" className={HEADING_CLASS}>
        {copy.home.replies.title}
        <span className={COUNT_CLASS}>{replies.length}</span>
      </h2>
      {replies.map((reply, index) => (
        <div
          key={reply.id}
          data-f="ask"
          className={cn(
            "flex gap-2.5 border-t border-border py-2.5",
            index === 0 && "border-t-0 pt-0.5",
          )}
        >
          <span
            aria-hidden="true"
            data-f="askav"
            className={cn(
              "grid size-[26px] shrink-0 place-items-center rounded-full text-avatar-md font-bold",
              IDENTITY_MARK_CLASS[reply.actorTone],
            )}
          >
            {initialsFor(reply.actorName)}
          </span>
          <div>
            <div
              data-f="who"
              className="text-crumb leading-[1.45] text-meta-foreground"
            >
              <span
                data-f="kind"
                className={cn(
                  "rounded-full px-[7px] py-px text-micro font-semibold",
                  reply.kind === "approve"
                    ? "bg-status-chip-review text-warning-text"
                    : "bg-state-selected text-state-selected-foreground",
                )}
              >
                {reply.kind === "approve"
                  ? copy.home.replies.approve
                  : copy.home.replies.mention}
              </span>{" "}
              <b className="font-semibold text-foreground">{reply.actorName}</b>{" "}
              · {reply.when}
            </div>
            <p className="mt-0.5 mb-2 text-nav leading-[1.5] font-normal">
              {reply.text}
            </p>
            {replyingTo === reply.id && actions ? (
              <ReplyBox
                copy={copy}
                reply={reply}
                busy={actions.pendingIds.has(reply.id)}
                onCancel={() => setReplyingTo(null)}
                onSend={(text) => {
                  actions.onReply(reply, text);
                  setReplyingTo(null);
                }}
              />
            ) : null}
            <div data-f="acts" className="flex flex-wrap gap-1.5">
              {reply.kind === "approve" ? (
                <>
                  <button
                    type="button"
                    aria-disabled={
                      !actions || actions.pendingIds.has(reply.id)
                        ? true
                        : undefined
                    }
                    title={actions ? undefined : copy.home.addItemUnavailable}
                    aria-label={formatCopy(copy.home.replies.approveLabel, {
                      key: reply.key ?? reply.actorName,
                    })}
                    onClick={
                      actions && !actions.pendingIds.has(reply.id)
                        ? () => actions.onApprove(reply)
                        : undefined
                    }
                    className={SMALL_PRIMARY_CLASS}
                  >
                    {copy.home.replies.approveAction}
                  </button>
                  <button
                    type="button"
                    aria-disabled={
                      !actions || actions.pendingIds.has(reply.id)
                        ? true
                        : undefined
                    }
                    title={actions ? undefined : copy.home.addItemUnavailable}
                    aria-label={formatCopy(copy.home.replies.changesLabel, {
                      key: reply.key ?? reply.actorName,
                    })}
                    onClick={
                      actions && !actions.pendingIds.has(reply.id)
                        ? () => actions.onRequestChanges(reply)
                        : undefined
                    }
                    className={SMALL_BUTTON_CLASS}
                  >
                    {copy.home.replies.changesAction}
                  </button>
                </>
              ) : (
                <button
                  type="button"
                  aria-disabled={actions ? undefined : true}
                  title={actions ? undefined : copy.home.addItemUnavailable}
                  onClick={actions ? () => setReplyingTo(reply.id) : undefined}
                  className={SMALL_BUTTON_CLASS}
                >
                  {copy.home.replies.replyAction}
                </button>
              )}
              {reply.target ? (
                <ShellLink
                  destination={reply.target}
                  navigation={navigation}
                  className={cn(SMALL_BUTTON_CLASS, "box-content")}
                >
                  {reply.kind === "approve"
                    ? copy.home.replies.viewAction
                    : copy.home.replies.openDocAction}
                </ShellLink>
              ) : null}
            </div>
          </div>
        </div>
      ))}
    </section>
  );
}

function AgendaCard({
  copy,
  agenda,
}: {
  copy: WorkspaceHomeCopy;
  agenda: HomeAgenda;
}) {
  const lastPast = agenda.entries.reduce(
    (last, entry, index) => (entry.past ? index : last),
    -1,
  );
  return (
    <section
      aria-labelledby="home-agenda"
      data-f="box"
      className={cn(PANEL_CLASS, "px-3.5 py-3")}
    >
      <h2 id="home-agenda" data-f="boxh2" className={HEADING_CLASS}>
        {copy.home.agenda.title}
        <span className={COUNT_CLASS}>{agenda.entries.length}</span>
      </h2>
      {agenda.entries.map((entry, index) => (
        <div key={entry.id}>
          <div
            data-f="ev"
            className={cn(
              "flex gap-3 border-t border-border py-2 text-nav leading-[1.45] font-normal",
              index === 0 && "border-t-0 pt-0.5",
              entry.past && "text-meta-foreground",
            )}
          >
            <time
              className={cn(
                "w-11 shrink-0 tabular-nums",
                entry.past ? "font-medium" : "font-semibold",
              )}
            >
              {entry.time}
            </time>
            <div>
              {entry.title}
              <small className="block text-xs leading-[1.45] text-meta-foreground">
                {entry.detail}
              </small>
            </div>
          </div>
          {index === lastPast ? (
            <div
              data-f="now"
              className="my-0.5 flex items-center gap-2 text-micro leading-[1.45] font-semibold text-state-selected-foreground"
            >
              {formatCopy(copy.home.agenda.now, { time: agenda.nowTime })}
              <i
                aria-hidden="true"
                className="block h-0.5 flex-1 rounded-[1px] bg-primary"
              />
            </div>
          ) : null}
        </div>
      ))}
    </section>
  );
}

/** The loading shape of Home: three quick filters (Final-Home `dang-tai`). */
function HomeSkeleton() {
  return (
    <div data-f="chips" aria-hidden="true" className="flex flex-wrap gap-2">
      {[0, 1, 2].map((index) => (
        <Skeleton
          key={index}
          data-f="chip"
          className="block h-9 w-[120px] rounded-[10px]"
        />
      ))}
    </div>
  );
}

function ReplySkeleton({ copy }: { copy: WorkspaceHomeCopy }) {
  return (
    <section
      aria-busy="true"
      aria-labelledby="home-replies"
      data-f="box"
      className={cn(PANEL_CLASS, "px-3.5 py-3")}
    >
      <h2 id="home-replies" data-f="boxh2" className={HEADING_CLASS}>
        {copy.home.replies.title}
      </h2>
      <div aria-hidden="true">
        {[
          ["55%", "90%", "70%"],
          ["50%", "85%"],
        ].map((lines, index) => (
          <div
            key={index}
            data-f="ask"
            className={cn(
              "flex gap-2.5 border-t border-border py-2.5",
              index === 0 && "border-t-0 pt-0.5",
            )}
          >
            <Skeleton className="block size-[26px] shrink-0 rounded-full" />
            <div className="flex-1">
              {lines.map((width, line) => (
                <Skeleton
                  key={line}
                  className={cn(
                    "block h-[11px] rounded-md",
                    line === 1 && "mt-2.5",
                    line === 2 && "mt-1.5",
                  )}
                  style={{ width }}
                />
              ))}
            </div>
          </div>
        ))}
      </div>
    </section>
  );
}

function AgendaSkeleton({ copy }: { copy: WorkspaceHomeCopy }) {
  return (
    <section
      aria-busy="true"
      aria-labelledby="home-agenda"
      data-f="box"
      className={cn(PANEL_CLASS, "px-3.5 py-3")}
    >
      <h2 id="home-agenda" data-f="boxh2" className={HEADING_CLASS}>
        {copy.home.agenda.title}
      </h2>
      <div aria-hidden="true">
        {["60%", "52%", "44%"].map((width, index) => (
          <div
            key={width}
            data-f="ev"
            className={cn(
              "flex gap-3 border-t border-border py-2 text-nav leading-[1.45]",
              index === 0 && "border-t-0 pt-0.5",
            )}
          >
            <Skeleton className="block h-3 w-10 shrink-0 rounded-md" />
            <Skeleton className="block h-3 rounded-md" style={{ width }} />
          </div>
        ))}
      </div>
    </section>
  );
}

/**
 * Home (Final-Home): the greeting and the quick filters, then the member's
 * work by due window on the left, and what needs a reply, today's schedule
 * and where to continue on the right. A card or a chip whose producer does
 * not exist yet is simply not drawn, never filled with a guess.
 */
export function WorkspaceHomeSurface(props: WorkspaceHomeSurfaceProps) {
  const { copy, navigation, formatters, now, myWork, continueState } = props;
  const groups = myWork.status === "ready" ? myWork.data : [];
  const overdue = windowItems(groups, "overdue");
  const today = windowItems(groups, "today");
  const week = windowItems(groups, "this-week");
  const later = windowItems(groups, "later");
  const noDate = windowItems(groups, "no-date");
  const openCount =
    overdue.length + today.length + week.length + later.length + noDate.length;
  const loading = myWork.status === "loading";
  const nextDue = [...week, ...later].find((item) => item.dueAt);

  const dateText = formatters.date(now, {
    weekday: "long",
    day: "numeric",
    month: "long",
  });
  const summary = loading
    ? formatCopy(copy.home.summaryLoading, { date: dateText })
    : formatCopy(
        props.meetingCount ? copy.home.summaryMeetings : copy.home.summary,
        {
          date: dateText,
          meetings: props.meetingCount ?? 0,
          open: `${openCount}${props.myWorkHasMore ? "+" : ""}`,
        },
      );
  const greeting = props.greetingName
    ? formatCopy(copy.home.greeting[greetingPeriod(now)], {
        name: props.greetingName,
      })
    : copy.home.greeting.plain;

  const myWorkHref = navigation.getHref({ section: "my-work" });
  const goToMyWork = () => navigation.onNavigate({ section: "my-work" });
  const inboxHref = navigation.getHref({ section: "inbox" });
  const goToInbox = () => navigation.onNavigate({ section: "inbox" });

  const replies = props.replies;
  const agenda = props.agenda;
  const showReplies =
    replies !== undefined &&
    (replies.status !== "ready" || replies.data.length > 0);
  const showAgenda =
    agenda !== undefined &&
    (agenda.status !== "ready" || agenda.data.entries.length > 0);
  const showRight =
    showReplies ||
    showAgenda ||
    continueState.status !== "ready" ||
    continueState.data.length > 0;

  return (
    <div
      data-f="inner"
      className="mx-auto box-content flex max-w-[1120px] flex-col gap-[22px] px-8 pt-7 pb-10 text-sm leading-[1.45]"
    >
      <div data-f="hello" className="flex flex-wrap items-end gap-3">
        <div className="min-w-[280px] flex-1">
          <h1 data-f="h1" className="m-0 text-greeting text-foreground">
            {greeting}
          </h1>
          <p
            data-f="date"
            className="mt-1 mb-0 text-sm leading-[1.45] text-muted-foreground"
          >
            {summary}
          </p>
        </div>
        {props.calendarHref ? (
          <a href={props.calendarHref} className={BUTTON_CLASS}>
            <Calendar aria-hidden="true" className="size-4" strokeWidth={1.8} />
            {copy.home.calendar}
          </a>
        ) : (
          <button
            type="button"
            aria-disabled="true"
            title={copy.home.calendarUnavailable}
            className={BUTTON_CLASS}
          >
            <Calendar aria-hidden="true" className="size-4" strokeWidth={1.8} />
            {copy.home.calendar}
          </button>
        )}
        <button
          type="button"
          aria-disabled={props.onAddItem ? undefined : "true"}
          title={props.onAddItem ? undefined : copy.home.addItemUnavailable}
          onClick={props.onAddItem ?? undefined}
          className={PRIMARY_BUTTON_CLASS}
        >
          <Plus aria-hidden="true" className="size-4" strokeWidth={1.8} />
          {copy.home.addItem}
        </button>
      </div>

      {props.onboarding ? (
        <WorkspaceOnboardingSurface
          copy={copy}
          workspaceName={props.workspaceName ?? ""}
          canCreate={props.onboarding.canCreate}
          canInvite={props.onboarding.canInvite}
          onCreateBoard={props.onCreateBoard}
          onCreateDoc={props.onCreateDoc}
          onInvite={props.onInvite}
        />
      ) : (
        <>
          {loading ? (
            <HomeSkeleton />
          ) : myWork.status === "ready" ? (
            <div
              data-f="chips"
              role="group"
              aria-label={copy.home.chipsLabel}
              className="flex flex-wrap gap-2"
            >
              {overdue.length > 0 ? (
                <Chip
                  tone="over"
                  count={overdue.length}
                  label={copy.home.chips.overdue}
                  href={myWorkHref}
                  onNavigate={goToMyWork}
                  icon={
                    <Clock
                      aria-hidden="true"
                      className="size-3.5"
                      strokeWidth={1.8}
                    />
                  }
                />
              ) : null}
              {today.length > 0 ? (
                <Chip
                  tone="today"
                  count={today.length}
                  label={copy.home.chips.today}
                  href={myWorkHref}
                  onNavigate={goToMyWork}
                />
              ) : null}
              {props.approvalCount ? (
                <Chip
                  boxed
                  count={props.approvalCount}
                  label={copy.home.chips.approvals}
                  href={inboxHref}
                  onNavigate={goToInbox}
                />
              ) : null}
              {props.mentionCount ? (
                <Chip
                  boxed
                  count={props.mentionCount}
                  label={copy.home.chips.mentions}
                  href={inboxHref}
                  onNavigate={goToInbox}
                />
              ) : null}
            </div>
          ) : null}

          <div data-f="cols" className="flex flex-wrap items-start gap-7">
            <div
              data-f="colm"
              className="flex min-w-0 flex-[999_1_520px] flex-col gap-[18px]"
            >
              {loading ? (
                <>
                  <Section
                    id="home-overdue"
                    label={copy.home.sections.overdue}
                    tone="over"
                    busy
                  >
                    <div
                      data-f="rows"
                      className={cn(PANEL_CLASS, "overflow-hidden")}
                    >
                      <RowSkeleton />
                    </div>
                  </Section>
                  <Section
                    id="home-today"
                    label={copy.home.sections.today}
                    busy
                  >
                    <div
                      data-f="rows"
                      className={cn(PANEL_CLASS, "overflow-hidden")}
                    >
                      <RowSkeleton />
                      <RowSkeleton />
                    </div>
                  </Section>
                  <Section id="home-week" label={copy.home.sections.week} busy>
                    <div
                      data-f="rows"
                      className={cn(PANEL_CLASS, "overflow-hidden")}
                    >
                      <RowSkeleton />
                      <RowSkeleton />
                    </div>
                  </Section>
                </>
              ) : myWork.status === "error" ? (
                <SectionError copy={copy} onRetry={props.onRetryMyWork} />
              ) : (
                <>
                  {overdue.length > 0 ? (
                    <Section
                      id="home-overdue"
                      label={copy.home.sections.overdue}
                      tone="over"
                      count={overdue.length}
                    >
                      <div
                        data-f="rows"
                        className={cn(PANEL_CLASS, "overflow-hidden")}
                      >
                        {overdue.map((item) => (
                          <TaskRow
                            key={item.id}
                            item={item}
                            copy={copy}
                            navigation={navigation}
                            now={now}
                            completing={props.completingIds.has(item.id)}
                            onComplete={props.onComplete}
                          />
                        ))}
                      </div>
                    </Section>
                  ) : null}
                  <Section
                    id="home-today"
                    label={copy.home.sections.today}
                    count={today.length}
                  >
                    {today.length === 0 ? (
                      <div
                        data-f="empty"
                        className="flex items-center gap-3 rounded-[12px] border border-dashed border-line-strong bg-popover p-4 text-nav font-normal text-muted-foreground dark:bg-card"
                      >
                        <span
                          aria-hidden="true"
                          className="grid size-9 shrink-0 place-items-center rounded-[10px] bg-status-chip-done text-work-status-done"
                        >
                          <CircleCheck className="size-5" strokeWidth={1.8} />
                        </span>
                        <div className="leading-[1.45]">
                          <b className="block text-sm font-bold text-foreground">
                            {overdue.length > 0
                              ? formatCopy(copy.home.todayEmptyOverdue, {
                                  count: overdue.length,
                                })
                              : copy.home.todayEmpty}
                          </b>
                          {props.completedToday
                            ? `${formatCopy(copy.home.todayEmptyDone, {
                                count:
                                  (
                                    copy.home.numberWords as Record<
                                      string,
                                      string
                                    >
                                  )[String(props.completedToday.count)] ??
                                  props.completedToday.count,
                                time: props.completedToday.time,
                              })} `
                            : null}
                          {nextDue?.dueAt
                            ? formatCopy(copy.home.todayEmptyNext, {
                                when: lowerFirst(
                                  dueWords(
                                    copy,
                                    describeMyWorkDue(nextDue.dueAt, now),
                                    { timeOnToday: false },
                                  ).text,
                                ),
                              })
                            : null}
                        </div>
                      </div>
                    ) : (
                      <div
                        data-f="rows"
                        className={cn(PANEL_CLASS, "overflow-hidden")}
                      >
                        {today.map((item) => (
                          <TaskRow
                            key={item.id}
                            item={item}
                            copy={copy}
                            navigation={navigation}
                            now={now}
                            completing={props.completingIds.has(item.id)}
                            onComplete={props.onComplete}
                          />
                        ))}
                      </div>
                    )}
                  </Section>
                  {week.length > 0 ||
                  later.length > 0 ||
                  props.myWorkHasMore ? (
                    <Section
                      id="home-week"
                      label={copy.home.sections.week}
                      count={week.length}
                      action={
                        <ShellLink
                          destination={{ section: "my-work" }}
                          navigation={navigation}
                          className="ml-auto text-crumb leading-[1.45] font-medium text-state-selected-foreground no-underline outline-none hover:underline focus-visible:underline"
                        >
                          {copy.home.sections.viewAll}
                        </ShellLink>
                      }
                    >
                      <div
                        data-f="rows"
                        className={cn(PANEL_CLASS, "overflow-hidden")}
                      >
                        {week.map((item) => (
                          <TaskRow
                            key={item.id}
                            item={item}
                            copy={copy}
                            navigation={navigation}
                            now={now}
                            completing={props.completingIds.has(item.id)}
                            onComplete={props.onComplete}
                          />
                        ))}
                        {later.length > 0 ? (
                          <div
                            data-f="more"
                            className="box-content flex min-h-10 items-center gap-3 border-t border-border px-3.5 py-2 text-compact leading-[1.45] text-meta-foreground"
                          >
                            {pluralForm(
                              {
                                one: copy.home.later_one,
                                other: copy.home.later_other,
                              },
                              later.length,
                            )}
                            <ShellLink
                              destination={{ section: "my-work" }}
                              navigation={navigation}
                              className="ml-auto font-medium text-state-selected-foreground no-underline outline-none hover:underline focus-visible:underline"
                            >
                              {copy.home.laterAction}
                            </ShellLink>
                          </div>
                        ) : null}
                      </div>
                    </Section>
                  ) : null}
                  {noDate.length > 0 ? (
                    <Section
                      id="home-nodate"
                      label={copy.home.sections.noDate}
                      count={noDate.length}
                    >
                      <div
                        data-f="rows"
                        className={cn(PANEL_CLASS, "overflow-hidden")}
                      >
                        {noDate.map((item) => (
                          <TaskRow
                            key={item.id}
                            item={item}
                            copy={copy}
                            navigation={navigation}
                            now={now}
                            completing={props.completingIds.has(item.id)}
                            onComplete={props.onComplete}
                          />
                        ))}
                      </div>
                    </Section>
                  ) : null}
                </>
              )}
            </div>

            {showRight ? (
              <div
                data-f="colr"
                className="flex min-w-0 max-w-[360px] flex-[1_1_300px] flex-col gap-[22px]"
              >
                {replies?.status === "loading" ? (
                  <ReplySkeleton copy={copy} />
                ) : replies?.status === "error" ? (
                  <SectionError copy={copy} onRetry={props.onRetryContinue} />
                ) : replies && replies.data.length > 0 ? (
                  <ReplyCard
                    copy={copy}
                    navigation={navigation}
                    replies={replies.data}
                    actions={props.replyActions}
                  />
                ) : null}
                {agenda?.status === "loading" ? (
                  <AgendaSkeleton copy={copy} />
                ) : agenda?.status === "ready" &&
                  agenda.data.entries.length > 0 ? (
                  <AgendaCard copy={copy} agenda={agenda.data} />
                ) : null}
                {continueState.status === "ready" &&
                continueState.data.length > 0 ? (
                  <section aria-labelledby="home-continue" data-f="contbox">
                    <h2
                      id="home-continue"
                      data-f="conth2"
                      className={HEADING_CLASS}
                    >
                      {copy.home.continue.title}
                    </h2>
                    {continueState.data.map((entry) => (
                      <ContinueCard
                        key={`${entry.kind}:${entry.resourceId}`}
                        copy={copy}
                        entry={entry}
                        formatters={formatters}
                        now={now}
                        navigation={navigation}
                      />
                    ))}
                  </section>
                ) : continueState.status === "error" ? (
                  <SectionError copy={copy} onRetry={props.onRetryContinue} />
                ) : null}
              </div>
            ) : null}
          </div>
        </>
      )}
    </div>
  );
}

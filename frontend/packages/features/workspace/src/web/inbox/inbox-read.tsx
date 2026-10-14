import { Check, ChevronDown, ChevronUp, Clock } from "lucide-react";
import { cn } from "@notrelix/ui-web";
import { initialsFor } from "../../core/model/navigation";
import {
  formatCopy,
  IDENTITY_MARK_CLASS,
  pluralCopy,
} from "../shell/shell-models";
import {
  DepartmentMark,
  STATUS_PILL_CLASS,
  StatusDot,
} from "../home/work-row-parts";
import { InboxAvatar, InboxSegments } from "./inbox-list";
import type { InboxItemView } from "./inbox-models";
import type { WorkspaceInboxCopy } from "./workspace-inbox.copy";

/**
 * The reading pane of the Inbox (Final-Inbox `.read`): the crumb and the
 * previous / next / open bar, the event, the item card, the latest comment,
 * the reply field and the foot with "Đã xử lý". A part without data behind it
 * is not drawn; an action without a write is drawn inert with the reason.
 */

const OUTLINE_CLASS =
  "inline-flex h-8 shrink-0 cursor-pointer items-center gap-1.5 rounded-[7px] border border-line-strong bg-popover px-3 text-compact leading-[1.45] font-medium whitespace-nowrap text-foreground no-underline outline-none hover:bg-state-hover focus-visible:ring-2 focus-visible:ring-focus-ring aria-disabled:cursor-not-allowed aria-disabled:hover:bg-popover dark:bg-card";
const PRIMARY_CLASS =
  "inline-flex h-8 shrink-0 cursor-pointer items-center gap-1.5 rounded-[7px] border border-primary bg-primary px-3 text-compact leading-[1.45] font-semibold whitespace-nowrap text-primary-foreground outline-none hover:bg-primary/90 focus-visible:ring-2 focus-visible:ring-focus-ring aria-disabled:cursor-not-allowed aria-disabled:hover:bg-primary";
const QUIET_CLASS =
  "inline-flex h-8 shrink-0 cursor-pointer items-center gap-1.5 rounded-[7px] border border-transparent bg-transparent px-3 text-compact leading-[1.45] font-medium whitespace-nowrap text-muted-foreground outline-none hover:bg-state-hover focus-visible:ring-2 focus-visible:ring-focus-ring aria-disabled:cursor-not-allowed";

export function InboxRead({
  copy,
  item,
  position,
  total,
  onPrevious,
  onNext,
  onHandled,
}: {
  copy: WorkspaceInboxCopy;
  item: InboxItemView;
  /** 0-based position among the shown rows. */
  position: number;
  total: number;
  onPrevious: () => void;
  onNext: () => void;
  /** `null` draws "Đã xử lý" inert (no write). */
  onHandled: (() => void) | null;
}) {
  const detail = item.detail;
  const keyKept = detail?.crumb?.key ? (
    <span className="font-mono text-foreground">{detail.crumb.key}</span>
  ) : null;
  return (
    <main
      data-f="read"
      aria-label={copy.read.label}
      className="flex h-full min-w-0 flex-[999_1_400px] flex-col bg-background"
    >
      <div
        data-f="rbar"
        className="box-content flex h-[52px] flex-none items-center gap-1.5 border-b border-border bg-popover pr-4 pl-6 dark:bg-card"
      >
        {detail?.crumb ? (
          <nav
            data-f="crumb"
            aria-label={copy.read.crumb}
            className="flex items-center gap-1.5 text-compact leading-[1.45] text-meta-foreground"
          >
            {detail.crumb.department ? (
              <span className="inline-flex items-center">
                <DepartmentMark
                  small
                  name={detail.crumb.department.name}
                  markTone={detail.crumb.department.markTone}
                />
              </span>
            ) : null}
            <span>{detail.crumb.board}</span>
            {keyKept ? (
              <>
                <span aria-hidden="true">/</span>
                {keyKept}
              </>
            ) : null}
          </nav>
        ) : null}
        <span className="flex-1" />
        <button
          data-f="ib"
          type="button"
          aria-label={copy.read.previous}
          aria-disabled={position <= 0 ? true : undefined}
          onClick={position > 0 ? onPrevious : undefined}
          className="grid size-8 shrink-0 cursor-pointer place-items-center rounded-[7px] border-0 bg-transparent text-meta-foreground outline-none hover:bg-state-hover hover:text-foreground focus-visible:ring-2 focus-visible:ring-focus-ring aria-disabled:cursor-not-allowed aria-disabled:opacity-50"
        >
          <ChevronUp aria-hidden="true" className="size-4" strokeWidth={1.8} />
        </button>
        <button
          data-f="ib"
          type="button"
          aria-label={copy.read.next}
          aria-disabled={position >= total - 1 ? true : undefined}
          onClick={position < total - 1 ? onNext : undefined}
          className="grid size-8 shrink-0 cursor-pointer place-items-center rounded-[7px] border-0 bg-transparent text-meta-foreground outline-none hover:bg-state-hover hover:text-foreground focus-visible:ring-2 focus-visible:ring-focus-ring aria-disabled:cursor-not-allowed aria-disabled:opacity-50"
        >
          <ChevronDown
            aria-hidden="true"
            className="size-4"
            strokeWidth={1.8}
          />
        </button>
        {detail?.openHref ? (
          <a
            data-f="open"
            href={detail.openHref}
            className={cn(OUTLINE_CLASS, "box-content")}
          >
            {copy.read.openItem}
          </a>
        ) : (
          <button
            data-f="open"
            type="button"
            aria-disabled="true"
            title={copy.read.openUnavailable}
            className={cn(OUTLINE_CLASS, "box-content")}
          >
            {copy.read.openItem}
          </button>
        )}
      </div>

      <div
        data-f="rbody"
        className="box-content flex max-w-[820px] min-h-0 flex-1 flex-col gap-[18px] overflow-auto px-7 py-6"
      >
        {detail ? (
          <>
            <div data-f="event" className="flex items-start gap-3">
              <InboxAvatar
                item={{
                  person: detail.event.person,
                  kind: "system",
                  avatarText: item.avatarText,
                }}
              />
              <div>
                <p className="m-0 text-item-heading leading-[1.55] font-normal">
                  <InboxSegments segments={detail.event.segments} />
                </p>
                <small className="mt-0.5 block text-crumb leading-[1.45] text-meta-foreground">
                  {detail.event.when}
                </small>
              </div>
            </div>

            {detail.card ? (
              <div
                data-f="card"
                className="flex flex-col gap-3 rounded-[12px] border border-border bg-popover px-[18px] py-4 dark:bg-card"
              >
                <h2 className="m-0 text-dialog-title text-foreground">
                  {detail.card.title}
                </h2>
                <div
                  data-f="meta"
                  className="flex flex-wrap items-center gap-2 text-crumb leading-[1.45] text-meta-foreground"
                >
                  {detail.card.status ? (
                    <span
                      className={cn(
                        "inline-flex h-6 items-center gap-1.5 rounded-full px-[9px] text-xs leading-[1.45] font-semibold text-foreground",
                        STATUS_PILL_CLASS[detail.card.status.bucket],
                      )}
                    >
                      <StatusDot bucket={detail.card.status.bucket} />
                      {detail.card.status.label}
                    </span>
                  ) : null}
                  {detail.card.priority ? (
                    <span>
                      {formatCopy(copy.read.priority, {
                        value: detail.card.priority,
                      })}
                    </span>
                  ) : null}
                  {detail.card.due ? (
                    <span>
                      {formatCopy(copy.read.due, { date: detail.card.due })}
                    </span>
                  ) : null}
                  {detail.card.comments !== undefined ? (
                    <span>
                      {pluralCopy(
                        {
                          one: copy.read.comments_one,
                          other: copy.read.comments_other,
                        },
                        detail.card.comments,
                      )}
                    </span>
                  ) : null}
                </div>
                {detail.card.changes.length > 0 ? (
                  <dl
                    data-f="chg"
                    className="m-0 grid grid-cols-[170px_minmax(0,1fr)] gap-x-3.5 gap-y-2 text-nav leading-[1.45] font-normal"
                  >
                    {detail.card.changes.map((change) => (
                      <ChangeRow key={change.label} change={change} />
                    ))}
                  </dl>
                ) : null}
                {detail.card.approvable ? (
                  <div
                    data-f="acts"
                    className="flex flex-wrap items-center gap-2"
                  >
                    <button
                      type="button"
                      aria-disabled="true"
                      title={copy.read.actionUnavailable}
                      className={PRIMARY_CLASS}
                    >
                      <Check
                        aria-hidden="true"
                        className="size-4"
                        strokeWidth={1.8}
                      />
                      {copy.read.approve}
                    </button>
                    <button
                      type="button"
                      aria-disabled="true"
                      title={copy.read.actionUnavailable}
                      className={OUTLINE_CLASS}
                    >
                      {copy.read.requestChanges}
                    </button>
                    <span className="flex-1" />
                    <span className="text-crumb leading-[1.45] text-meta-foreground">
                      {copy.read.approveNote}
                    </span>
                  </div>
                ) : null}
              </div>
            ) : null}

            {detail.comment ? (
              <section aria-label={copy.read.commentsLatest}>
                <div data-f="cm" className="flex gap-2.5">
                  <span
                    aria-hidden="true"
                    className={cn(
                      "grid size-[30px] shrink-0 place-items-center rounded-full text-avatar-lg font-bold",
                      IDENTITY_MARK_CLASS[detail.comment.person.tone],
                    )}
                  >
                    {initialsFor(detail.comment.person.name)}
                  </span>
                  <div>
                    <b className="font-semibold">
                      {detail.comment.person.name}
                    </b>
                    <time className="ml-1.5 text-xs leading-[1.45] text-meta-foreground">
                      {detail.comment.time}
                    </time>
                    <p className="mt-[3px] mb-0 text-nav leading-[1.55] font-normal">
                      {detail.comment.text}
                    </p>
                  </div>
                </div>
              </section>
            ) : null}

            {detail.replyTo ? (
              <label
                data-f="reply"
                className="box-content flex h-11 items-center gap-2 rounded-[10px] border border-line-strong bg-popover pr-1.5 pl-3 dark:bg-card"
              >
                <input
                  type="text"
                  disabled
                  title={copy.read.actionUnavailable}
                  aria-label={copy.read.replyLabel}
                  placeholder={formatCopy(copy.read.replyPlaceholder, {
                    name: detail.replyTo,
                  })}
                  className="min-w-0 flex-1 border-0 bg-transparent px-0.5 py-px text-sm leading-[1.45] text-foreground outline-none placeholder:text-meta-foreground"
                />
                <span className="rounded-[4px] border border-line-strong bg-keycap px-[5px] font-mono text-key leading-[18px] text-muted-foreground">
                  ⌘↵
                </span>
              </label>
            ) : null}
          </>
        ) : null}
      </div>

      <div
        data-f="foot"
        className="flex flex-none flex-wrap items-center gap-2 border-t border-border bg-popover px-6 py-2.5 dark:bg-card"
      >
        <button
          data-f="handled"
          type="button"
          aria-disabled={onHandled ? undefined : true}
          title={onHandled ? undefined : copy.read.handledUnavailable}
          onClick={onHandled ?? undefined}
          className={OUTLINE_CLASS}
        >
          <Check aria-hidden="true" className="size-4" strokeWidth={1.8} />
          {copy.read.handled}
        </button>
        <button
          type="button"
          aria-disabled="true"
          title={copy.read.actionUnavailable}
          className={OUTLINE_CLASS}
        >
          <Clock aria-hidden="true" className="size-4" strokeWidth={1.8} />
          {copy.read.snooze}
          <ChevronDown
            aria-hidden="true"
            className="size-[13px]"
            strokeWidth={1.8}
          />
        </button>
        <button
          type="button"
          aria-disabled="true"
          title={copy.read.actionUnavailable}
          className={QUIET_CLASS}
        >
          {copy.read.mute}
        </button>
        <div
          data-f="hint"
          className="ml-auto flex flex-wrap gap-3 text-xs leading-[1.45] text-meta-foreground"
        >
          <span className="inline-flex items-center gap-1">
            <Key>J</Key>
            <Key>K</Key>
            {copy.read.shortcuts.move}
          </span>
          <span className="inline-flex items-center gap-1">
            <Key>E</Key>
            {copy.read.shortcuts.handled}
          </span>
          <span className="inline-flex items-center gap-1">
            <Key>H</Key>
            {copy.read.shortcuts.snooze}
          </span>
        </div>
      </div>
    </main>
  );
}

function Key({ children }: { children: string }) {
  return (
    <span className="rounded-[4px] border border-line-strong bg-keycap px-[5px] font-mono text-key leading-[18px] text-muted-foreground">
      {children}
    </span>
  );
}

function ChangeRow({
  change,
}: {
  change: { label: string; value: string; changed?: string };
}) {
  const index = change.changed ? change.value.indexOf(change.changed) : -1;
  return (
    <>
      <dt className="text-meta-foreground">{change.label}</dt>
      <dd className="m-0">
        {change.changed && index >= 0 ? (
          <>
            {change.value.slice(0, index)}
            <ins className="rounded-[4px] bg-status-chip-done px-1 no-underline">
              {change.changed}
            </ins>
            {change.value.slice(index + change.changed.length)}
          </>
        ) : (
          change.value
        )}
      </dd>
    </>
  );
}

/** The reading pane with nothing in it (no notification, or none left). */
export function InboxReadEmpty({
  copy,
  title,
  text,
}: {
  copy: WorkspaceInboxCopy;
  title: string;
  text: string;
}) {
  return (
    <main
      data-f="read"
      aria-label={copy.read.label}
      className="flex h-full min-w-0 flex-[999_1_400px] flex-col bg-background"
    >
      <div
        data-f="empty"
        className="flex flex-1 flex-col items-center justify-center gap-2.5 p-10 text-center text-muted-foreground"
      >
        <b className="text-base leading-[1.45] font-semibold text-foreground">
          {title}
        </b>
        <p className="m-0 max-w-[44ch] text-nav leading-[1.45] font-normal">
          {text}
        </p>
      </div>
    </main>
  );
}

import { useEffect, useRef, useState, type KeyboardEvent } from "react";
import {
  Check,
  CheckSquare,
  Eye,
  FileText,
  Lock,
  MessageSquare,
  X,
} from "lucide-react";
import { cn } from "@notrelix/ui-web";
import { fillDocCopy, type DocPageCopy } from "./doc-page.copy";
import {
  isRestrictedTask,
  type DocCommentView,
  type DocPageCallbacks,
  type DocPanelTab,
  type DocRelatedDoc,
  type DocTaskRef,
  type DocTaskResolution,
  type DocTocEntry,
} from "./doc-page-models";
import { DocAvatar, DocStatusDot } from "./doc-page-ui";

export interface DocSidePanelSurfaceProps {
  readonly copy: DocPageCopy;
  readonly tab: DocPanelTab;
  /** Tasks the page mentions, in reading order. */
  readonly tasks: readonly DocTaskResolution[];
  readonly relatedDocs: readonly DocRelatedDoc[];
  readonly toc: readonly DocTocEntry[];
  readonly comments: readonly DocCommentView[];
  /** The selected text a comment is being written on, when there is one. */
  readonly pendingQuote?: string | null;
  readonly canComment?: boolean;
  readonly callbacks?: DocPageCallbacks;
}

const TAB =
  "-mb-px box-border h-11 border-0 border-b-2 border-transparent bg-transparent px-2.5 text-compact leading-[1.45] font-medium text-muted-foreground outline-none hover:text-foreground focus-visible:ring-2 focus-visible:ring-focus-ring";

const ITEM =
  "-mx-2 flex items-start gap-[9px] rounded-[8px] p-2 no-underline hover:bg-state-hover";

const SECTION_TITLE =
  "mb-2 flex items-center gap-1.5 text-crumb leading-[1.45] font-semibold text-muted-foreground";

function dueDetail(task: DocTaskRef, copy: DocPageCopy) {
  const b = copy.blocks.due;
  const due = task.due;
  if (!due) return null;
  switch (due.kind) {
    case "overdue":
      return (
        <span className="font-semibold text-danger-text">
          {fillDocCopy(due.days === 1 ? b.overdue_one : b.overdue_other, {
            count: due.days,
          })}
        </span>
      );
    case "today":
      return <span className="font-semibold text-warning-text">{b.today}</span>;
    default:
      return <span>{due.label}</span>;
  }
}

function TaskItem({
  task,
  copy,
}: {
  task: DocTaskResolution;
  copy: DocPageCopy;
}) {
  if (isRestrictedTask(task)) {
    return (
      <div className={ITEM}>
        <Lock
          aria-hidden="true"
          className="mt-0.5 size-3.5 shrink-0 stroke-[1.8]"
        />
        <span>
          <b className="block text-compact leading-[1.4] font-normal text-meta-foreground italic">
            {copy.panel.restrictedTask}
          </b>
          <small className="mt-0.5 flex flex-wrap items-center gap-1.5 text-metadata leading-[1.45] text-meta-foreground tracking-normal">
            {copy.panel.restrictedHint}
          </small>
        </span>
      </div>
    );
  }
  const done = task.status === "done";
  return (
    <a href={task.href} className={ITEM}>
      <DocStatusDot status={task.status} className="mt-[5px]" />
      <span className="min-w-0">
        <b className="block text-compact leading-[1.4] font-medium text-foreground">
          {task.title}
        </b>
        <small className="mt-0.5 flex flex-wrap items-center gap-1.5 text-metadata leading-[1.45] text-meta-foreground tracking-normal">
          <span className="font-mono">{task.key}</span>
          {done && task.due?.kind === "done" ? (
            fillDocCopy(copy.panel.completedOn, { date: task.due.label })
          ) : (
            <>
              {task.assignee ? `${task.assignee.name} ·` : null}
              {dueDetail(task, copy)}
            </>
          )}
        </small>
      </span>
    </a>
  );
}

function LinksPanel({
  copy,
  tasks,
  relatedDocs,
}: Pick<DocSidePanelSurfaceProps, "copy" | "tasks" | "relatedDocs">) {
  const p = copy.panel;
  return (
    <>
      <section>
        <h3 className={SECTION_TITLE}>
          {p.mentionedTasks}
          <span className="font-normal text-meta-foreground">
            {tasks.length}
          </span>
        </h3>
        {tasks.length === 0 ? (
          <p className="text-compact leading-[1.45] text-meta-foreground">
            {p.linksEmpty}
          </p>
        ) : (
          tasks.map((task) => (
            <TaskItem key={task.id} task={task} copy={copy} />
          ))
        )}
      </section>
      {relatedDocs.length > 0 ? (
        <section>
          <h3 className={SECTION_TITLE}>{p.mentionedIn}</h3>
          {relatedDocs.map((item) => {
            const Icon = item.kind === "doc" ? FileText : CheckSquare;
            return (
              <a key={item.id} href={item.href} className={ITEM}>
                <Icon
                  aria-hidden="true"
                  className="mt-0.5 size-3.5 shrink-0 stroke-[1.8]"
                />
                <span className="min-w-0">
                  <b className="block text-compact leading-[1.4] font-medium text-foreground">
                    {item.title}
                  </b>
                  <small className="mt-0.5 flex flex-wrap items-center gap-1.5 text-metadata leading-[1.45] text-meta-foreground tracking-normal">
                    {item.detail}
                  </small>
                </span>
              </a>
            );
          })}
        </section>
      ) : null}
    </>
  );
}

function TocPanel({
  copy,
  toc,
  callbacks,
}: Pick<DocSidePanelSurfaceProps, "copy" | "toc" | "callbacks">) {
  const p = copy.panel;
  return (
    <nav aria-label={p.tocNav} className="flex flex-col gap-px">
      {toc.length === 0 ? (
        <small className="text-metadata text-meta-foreground leading-[1.45] tracking-normal">
          {p.tocEmpty}
        </small>
      ) : (
        toc.map((entry) => (
          <a
            key={entry.id}
            href={`#${entry.id}`}
            aria-current={entry.active ? "location" : undefined}
            onClick={(event) => {
              event.preventDefault();
              callbacks?.onTocSelect?.(entry.id);
            }}
            className={cn(
              "-mx-2 block rounded-[7px] px-2 py-1.5 text-compact leading-[1.4] text-muted-foreground no-underline",
              !entry.active && "hover:bg-state-hover hover:text-foreground",
              entry.level === 1 && "font-semibold text-foreground",
              entry.level === 2 && "pl-5",
              entry.level === 3 && "pl-8",
              entry.level === 4 && "pl-11",
              entry.active &&
                "bg-state-selected font-semibold text-state-selected-foreground",
            )}
          >
            {entry.text}
          </a>
        ))
      )}
      {toc.length > 0 ? (
        <small className="mt-1.5 block text-metadata text-meta-foreground leading-[1.45] tracking-normal">
          {p.tocNote}
        </small>
      ) : null}
    </nav>
  );
}

const ACTION =
  "inline-flex h-6.5 items-center gap-1 rounded-[6px] border-0 bg-transparent px-[7px] text-crumb leading-none font-medium text-meta-foreground outline-none hover:bg-state-hover hover:text-foreground focus-visible:ring-2 focus-visible:ring-focus-ring";
/** The longest comment the producer stores (Comment.MaxContentLength). */
export const COMMENT_MAX_LENGTH = 10_000;

function Quote({
  comment,
  copy,
  callbacks,
}: Pick<DocSidePanelSurfaceProps, "copy" | "callbacks"> & {
  comment: DocCommentView;
}) {
  const c = copy.comments;
  if (!comment.quote) return null;
  const base =
    "block w-full rounded-[5px] border-0 bg-doc-highlight px-[7px] py-1 text-left text-crumb leading-[1.45] text-doc-body";
  if (comment.anchored && !comment.detached)
    return (
      <button
        type="button"
        title={c.showInPage}
        onClick={() => callbacks?.onCommentSelect?.(comment.id)}
        className={cn(
          base,
          "mb-2 outline-none focus-visible:ring-2 focus-visible:ring-focus-ring",
        )}
      >
        {comment.quote}
      </button>
    );
  return (
    <>
      <div
        className={cn(
          base,
          comment.detached ? "mb-1.5 line-through opacity-80" : "mb-2",
        )}
      >
        {comment.quote}
      </div>
      {comment.detached ? (
        <span className="mb-2 inline-flex h-5.5 items-center rounded-full bg-warning-subtle px-2 text-metadata leading-none font-semibold text-foreground">
          {c.detached}
        </span>
      ) : null}
    </>
  );
}

function CommentRow({
  comment,
  copy,
  canComment,
  callbacks,
  confirmingId,
  setConfirmingId,
  onReply,
}: Pick<DocSidePanelSurfaceProps, "copy" | "canComment" | "callbacks"> & {
  comment: DocCommentView;
  confirmingId: string | null;
  setConfirmingId: (id: string | null) => void;
  /** Present on a thread starter only: replies join the thread. */
  onReply?: () => void;
}) {
  const c = copy.comments;
  return (
    <div
      id={`comment-${comment.id}`}
      aria-current={comment.active ? "true" : undefined}
      className={cn(
        "group flex gap-2.5 rounded-[8px]",
        comment.active && "-mx-1.5 bg-state-selected px-1.5 py-1",
        comment.resolved && "opacity-70",
      )}
    >
      <DocAvatar
        person={comment.author}
        className="size-7 text-mark-sm leading-none"
      />
      <div className="min-w-0 flex-1">
        <div className="flex items-center gap-1.5 text-compact leading-[1.45]">
          <b className="font-semibold">{comment.author.name}</b>
          <time className="text-metadata leading-[1.45] text-meta-foreground">
            {comment.when}
          </time>
        </div>
        <p className="mt-0.5 text-nav leading-[1.55] font-normal text-doc-body">
          {comment.body}
        </p>
        {comment.resolved && onReply ? (
          <span className="mt-1 inline-flex items-center gap-1 text-micro leading-[1.45] font-semibold text-meta-foreground">
            <Check aria-hidden="true" className="size-3" />
            {c.resolve}
          </span>
        ) : null}
        {confirmingId === comment.id ? (
          <div
            role="group"
            aria-label={c.deleteConfirm}
            className="mt-1.5 flex flex-wrap items-center gap-1.5"
          >
            <span className="text-crumb leading-[1.45] text-foreground">
              {c.deleteConfirm}
            </span>
            <button
              type="button"
              onClick={() => {
                setConfirmingId(null);
                callbacks?.onCommentDelete?.(comment.id);
              }}
              className="h-6 rounded-[6px] border-0 bg-danger-subtle px-2 text-crumb font-medium text-danger-text outline-none focus-visible:ring-2 focus-visible:ring-focus-ring"
            >
              {c.delete}
            </button>
            <button
              type="button"
              onClick={() => setConfirmingId(null)}
              className="h-6 rounded-[6px] border border-line-strong bg-transparent px-2 text-crumb text-foreground outline-none focus-visible:ring-2 focus-visible:ring-focus-ring"
            >
              {c.deleteKeep}
            </button>
          </div>
        ) : (
          <div className="mt-1 flex flex-wrap items-center gap-0.5">
            {onReply && canComment && !comment.resolved ? (
              <button type="button" onClick={onReply} className={ACTION}>
                {c.replyAction}
              </button>
            ) : null}
            {onReply &&
            !comment.resolved &&
            canComment &&
            comment.canResolve ? (
              <button
                type="button"
                onClick={() => callbacks?.onCommentResolve?.(comment.id)}
                className={ACTION}
              >
                {c.resolveAction}
              </button>
            ) : null}
            {onReply && comment.resolved && canComment && comment.canResolve ? (
              <button
                type="button"
                onClick={() => callbacks?.onCommentReopen?.(comment.id)}
                className={ACTION}
              >
                {c.reopenAction}
              </button>
            ) : null}
            {comment.mine ? (
              <button
                type="button"
                aria-label={c.delete}
                title={c.delete}
                onClick={() => setConfirmingId(comment.id)}
                className={ACTION}
              >
                {c.deleteAction}
              </button>
            ) : null}
          </div>
        )}
      </div>
    </div>
  );
}

function CommentsPanel({
  copy,
  comments,
  canComment,
  callbacks,
}: Pick<
  DocSidePanelSurfaceProps,
  "copy" | "comments" | "canComment" | "callbacks"
>) {
  const c = copy.comments;
  const [showResolved, setShowResolved] = useState(false);
  const [confirmingId, setConfirmingId] = useState<string | null>(null);
  const [replyingId, setReplyingId] = useState<string | null>(null);
  const resolvedCount = comments.filter((comment) => comment.resolved).length;
  const shown = showResolved
    ? comments
    : comments.filter((comment) => !comment.resolved);

  return (
    <div className="flex flex-col gap-3">
      {resolvedCount > 0 ? (
        <label className="flex items-center gap-1.5 text-crumb leading-[1.45] text-muted-foreground">
          <input
            type="checkbox"
            checked={showResolved}
            onChange={(event) => setShowResolved(event.target.checked)}
            className="size-3.5 accent-primary"
          />
          {c.showResolved} ({resolvedCount})
        </label>
      ) : null}
      {shown.length === 0 ? (
        <div className="flex flex-col items-center gap-2.5 px-2 py-10 text-center">
          <span className="grid size-14 place-items-center rounded-[16px] bg-state-hover text-muted-foreground">
            <MessageSquare aria-hidden="true" className="size-6" />
          </span>
          <b className="text-subheading font-semibold text-foreground">
            {c.emptyTitle}
          </b>
          <p className="m-0 max-w-[44ch] text-compact leading-[1.55] text-muted-foreground">
            {c.empty}
          </p>
        </div>
      ) : null}
      {shown.map((comment) => (
        <div key={comment.id}>
          <Quote comment={comment} copy={copy} callbacks={callbacks} />
          <CommentRow
            comment={comment}
            copy={copy}
            canComment={canComment}
            callbacks={callbacks}
            confirmingId={confirmingId}
            setConfirmingId={setConfirmingId}
            onReply={() => setReplyingId(comment.id)}
          />
          {comment.replies?.map((reply) => (
            <div key={reply.id} className="mt-2 pl-[38px]">
              <CommentRow
                comment={reply}
                copy={copy}
                canComment={canComment}
                callbacks={callbacks}
                confirmingId={confirmingId}
                setConfirmingId={setConfirmingId}
              />
            </div>
          ))}
          {replyingId === comment.id && canComment ? (
            <div className="mt-2 pl-[38px]">
              <CommentField
                copy={copy}
                label={c.reply}
                placeholder={c.replyPlaceholder}
                autoFocus
                onSend={(body) => {
                  callbacks?.onCommentCreate?.(body, comment.id);
                  setReplyingId(null);
                }}
                onCancel={() => setReplyingId(null)}
              />
            </div>
          ) : null}
        </div>
      ))}
    </div>
  );
}

/** A text field with its Send button and the length check (the page composer and a reply). */
function CommentField({
  copy,
  label,
  placeholder,
  autoFocus,
  focusKey,
  onSend,
  onCancel,
}: Pick<DocSidePanelSurfaceProps, "copy"> & {
  label: string;
  placeholder: string;
  autoFocus?: boolean;
  /** A change moves the focus into the field. */
  focusKey?: unknown;
  onSend: (body: string) => void;
  onCancel?: () => void;
}) {
  const c = copy.comments;
  const [draft, setDraft] = useState("");
  const field = useRef<HTMLTextAreaElement | null>(null);
  useEffect(() => {
    if (autoFocus || focusKey) field.current?.focus();
  }, [autoFocus, focusKey]);
  const over = draft.length - COMMENT_MAX_LENGTH;
  const send = () => {
    const body = draft.trim();
    if (!body || over > 0) return;
    onSend(body);
    setDraft("");
  };
  return (
    <div className="flex flex-col gap-1.5">
      <textarea
        ref={field}
        rows={2}
        value={draft}
        aria-label={label}
        aria-invalid={over > 0 ? true : undefined}
        placeholder={placeholder}
        onChange={(event) => setDraft(event.target.value)}
        onKeyDown={(event: KeyboardEvent<HTMLTextAreaElement>) => {
          if (event.key === "Enter" && !event.shiftKey) {
            event.preventDefault();
            send();
          }
        }}
        className={cn(
          "box-border w-full resize-none rounded-[8px] border bg-doc-panel px-2.5 py-2 text-nav leading-[1.5] font-normal text-foreground outline-none placeholder:text-meta-foreground focus-visible:ring-2 focus-visible:ring-focus-ring",
          over > 0 ? "border-danger" : "border-line-strong",
        )}
      />
      <div className="flex items-center gap-2.5">
        {over > 0 ? (
          <span role="alert" className="text-crumb text-danger-text">
            {fillDocCopy(c.tooLong, { max: COMMENT_MAX_LENGTH, over })}
          </span>
        ) : null}
        <span className="flex-1" />
        {onCancel ? (
          <button type="button" onClick={onCancel} className={ACTION}>
            {c.cancel}
          </button>
        ) : null}
        <button
          type="button"
          onClick={send}
          disabled={over > 0 || draft.trim() === ""}
          className="inline-flex h-8 items-center rounded-[7px] border border-primary bg-primary px-3 text-compact leading-none font-semibold text-primary-foreground outline-none focus-visible:ring-2 focus-visible:ring-focus-ring disabled:opacity-50"
        >
          {c.send}
        </button>
      </div>
    </div>
  );
}

/** The panel footer (Final-Docs `.rp` composer): the page comment field, or the read-only note. */
function CommentComposer({
  copy,
  pendingQuote,
  canComment,
  callbacks,
}: Pick<
  DocSidePanelSurfaceProps,
  "copy" | "pendingQuote" | "canComment" | "callbacks"
>) {
  const c = copy.comments;
  return (
    <div className="flex shrink-0 flex-col gap-1.5 border-t border-border px-4 py-3">
      {!canComment ? (
        <div className="flex items-start gap-[9px] rounded-[9px] bg-info-subtle px-3 py-2.5 text-crumb leading-normal text-doc-body">
          <Eye
            aria-hidden="true"
            className="mt-px size-[15px] shrink-0 text-info-text"
          />
          {c.denied}
        </div>
      ) : (
        <>
          {pendingQuote ? (
            <div className="flex items-start gap-1.5 rounded-[8px] border border-line-strong p-1.5">
              <div className="min-w-0 flex-1">
                <span className="block text-micro leading-[1.45] font-semibold text-meta-foreground">
                  {c.onSelection}
                </span>
                <div className="mt-0.5 rounded-[5px] bg-doc-highlight px-[7px] py-1 text-crumb leading-[1.45] text-doc-body">
                  {pendingQuote}
                </div>
              </div>
              <button
                type="button"
                aria-label={c.cancelSelection}
                title={c.cancelSelection}
                onClick={callbacks?.onCommentCancel}
                className="grid size-6 shrink-0 place-items-center rounded-[6px] border-0 bg-transparent text-meta-foreground outline-none hover:bg-state-hover focus-visible:ring-2 focus-visible:ring-focus-ring"
              >
                <X aria-hidden="true" className="size-3.5" />
              </button>
            </div>
          ) : null}
          <CommentField
            copy={copy}
            label={c.compose}
            placeholder={c.composePlaceholder}
            focusKey={pendingQuote}
            onSend={(body) => callbacks?.onCommentCreate?.(body)}
          />
        </>
      )}
    </div>
  );
}

/**
 * The document side panel (Final-Docs `.rp`): Links, Contents and Comments
 * tabs over a scrolling body, 308 px with its left border.
 */
export function DocSidePanelSurface({
  copy,
  tab,
  tasks,
  relatedDocs,
  toc,
  comments,
  pendingQuote,
  canComment = false,
  callbacks,
}: DocSidePanelSurfaceProps) {
  const p = copy.panel;
  const tabs: ReadonlyArray<{ id: DocPanelTab; label: string }> = [
    {
      id: "links",
      label: `${p.links} ${tasks.length}`,
    },
    { id: "toc", label: p.toc },
    {
      id: "comments",
      label: `${p.comments} ${comments.filter((comment) => !comment.resolved).length}`,
    },
  ];

  return (
    <aside
      aria-label={p.label}
      className="box-content flex w-[308px] shrink-0 flex-col border-l border-border bg-doc-panel max-sm:absolute max-sm:inset-x-0 max-sm:bottom-0 max-sm:z-dropdown max-sm:h-[72%] max-sm:w-auto max-sm:rounded-t-[16px] max-sm:border-t max-sm:border-l-0 max-sm:shadow-popover"
    >
      <div className="relative shrink-0">
        <div
          role="tablist"
          aria-label={p.tabs}
          className="flex gap-0.5 border-b border-border px-3"
        >
          {tabs.map((entry) => (
            <button
              key={entry.id}
              type="button"
              role="tab"
              aria-selected={tab === entry.id}
              onClick={() => callbacks?.onPanelTab?.(entry.id)}
              className={cn(
                TAB,
                tab === entry.id &&
                  "border-b-primary font-semibold text-foreground",
              )}
            >
              {entry.label}
            </button>
          ))}
        </div>
        {callbacks?.onPanelOpenChange ? (
          <button
            type="button"
            aria-label={p.close}
            title={p.close}
            onClick={() => callbacks.onPanelOpenChange?.(false)}
            className="absolute top-1/2 right-3 grid size-7 -translate-y-1/2 shrink-0 place-items-center rounded-[7px] text-meta-foreground outline-none hover:bg-state-hover hover:text-foreground focus-visible:ring-2 focus-visible:ring-focus-ring"
          >
            <X aria-hidden="true" className="size-4 stroke-[1.8]" />
          </button>
        ) : null}
      </div>
      <div
        role="tabpanel"
        aria-label={tabs.find((entry) => entry.id === tab)?.label}
        className="flex min-h-0 flex-1 flex-col gap-[18px] overflow-auto px-4 py-3.5"
      >
        {tab === "links" ? (
          <LinksPanel copy={copy} tasks={tasks} relatedDocs={relatedDocs} />
        ) : null}
        {tab === "toc" ? (
          <TocPanel copy={copy} toc={toc} callbacks={callbacks} />
        ) : null}
        {tab === "comments" ? (
          <CommentsPanel
            copy={copy}
            comments={comments}
            canComment={canComment}
            callbacks={callbacks}
          />
        ) : null}
      </div>
      {tab === "comments" ? (
        <CommentComposer
          copy={copy}
          pendingQuote={pendingQuote}
          canComment={canComment}
          callbacks={callbacks}
        />
      ) : null}
    </aside>
  );
}

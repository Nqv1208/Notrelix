import { useEffect, useRef, useState, type KeyboardEvent } from "react";
import {
  Check,
  CheckSquare,
  FileText,
  Lock,
  RotateCcw,
  Trash2,
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

const OUTLINE_BUTTON =
  "inline-flex h-7.5 items-center gap-1.5 rounded-[7px] border border-line-strong bg-doc-panel px-3 text-compact leading-[1.45] font-medium text-foreground outline-none aria-disabled:cursor-default focus-visible:ring-2 focus-visible:ring-focus-ring";

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

function CommentsPanel({
  copy,
  comments,
  pendingQuote,
  canComment,
  callbacks,
}: Pick<
  DocSidePanelSurfaceProps,
  "copy" | "comments" | "pendingQuote" | "canComment" | "callbacks"
>) {
  const c = copy.comments;
  const [draft, setDraft] = useState("");
  const field = useRef<HTMLInputElement | null>(null);
  const [showResolved, setShowResolved] = useState(false);
  const [confirmingId, setConfirmingId] = useState<string | null>(null);
  const resolvedCount = comments.filter((comment) => comment.resolved).length;
  const shown = showResolved
    ? comments
    : comments.filter((comment) => !comment.resolved);

  // Starting a comment on a selection moves the member into the field.
  useEffect(() => {
    if (pendingQuote) field.current?.focus();
  }, [pendingQuote]);

  const submit = () => {
    const body = draft.trim();
    if (!body) return;
    callbacks?.onCommentCreate?.(body);
    setDraft("");
  };
  const onKeyDown = (event: KeyboardEvent<HTMLInputElement>) => {
    if (event.key === "Enter") {
      event.preventDefault();
      submit();
    }
  };

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
        <p className="text-compact leading-[1.45] text-meta-foreground">
          {c.empty}
        </p>
      ) : null}
      {shown.map((comment) => (
        <div
          key={comment.id}
          id={`comment-${comment.id}`}
          aria-current={comment.active ? "true" : undefined}
          className={cn(
            "group flex gap-[9px] rounded-[8px]",
            comment.active && "-mx-1.5 bg-state-selected px-1.5 py-1",
            comment.resolved && "opacity-70",
          )}
        >
          <DocAvatar person={comment.author} className="size-6.5" />
          <div className="min-w-0 flex-1 text-body leading-[1.45]">
            <b className="text-crumb leading-[1.45] font-semibold">
              {comment.author.name}
            </b>
            <time className="ml-[5px] text-micro leading-[1.45] text-meta-foreground">
              {comment.when}
            </time>
            {comment.quote ? (
              comment.anchored && !comment.detached ? (
                <button
                  type="button"
                  title={c.showInPage}
                  onClick={() => callbacks?.onCommentSelect?.(comment.id)}
                  className="mt-[3px] block w-full rounded-[5px] border-0 bg-doc-highlight px-[7px] py-1 text-left text-crumb leading-[1.45] text-doc-body outline-none focus-visible:ring-2 focus-visible:ring-focus-ring"
                >
                  {comment.quote}
                </button>
              ) : (
                <div
                  className={cn(
                    "mt-[3px] rounded-[5px] bg-doc-highlight px-[7px] py-1 text-crumb leading-[1.45] text-doc-body",
                    comment.detached && "line-through opacity-70",
                  )}
                >
                  {comment.quote}
                </div>
              )
            ) : null}
            {comment.detached ? (
              <p className="mt-0.5 text-micro leading-[1.45] text-meta-foreground">
                {c.detached}
              </p>
            ) : null}
            <p className="mt-[3px] text-compact leading-[1.5] text-doc-body">
              {comment.body}
            </p>
            {comment.resolved ? (
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
              <div className="mt-0.5 flex gap-1 opacity-0 group-focus-within:opacity-100 group-hover:opacity-100">
                {!comment.resolved && canComment ? (
                  <button
                    type="button"
                    aria-label={c.resolveThread}
                    title={c.resolveThread}
                    onClick={() => callbacks?.onCommentResolve?.(comment.id)}
                    className="grid size-6 place-items-center rounded-[6px] border-0 bg-transparent text-meta-foreground outline-none hover:bg-state-hover focus-visible:ring-2 focus-visible:ring-focus-ring"
                  >
                    <Check aria-hidden="true" className="size-3.5" />
                  </button>
                ) : null}
                {comment.resolved && canComment ? (
                  <button
                    type="button"
                    aria-label={c.reopenThread}
                    title={c.reopenThread}
                    onClick={() => callbacks?.onCommentReopen?.(comment.id)}
                    className="grid size-6 place-items-center rounded-[6px] border-0 bg-transparent text-meta-foreground outline-none hover:bg-state-hover focus-visible:ring-2 focus-visible:ring-focus-ring"
                  >
                    <RotateCcw aria-hidden="true" className="size-3.5" />
                  </button>
                ) : null}
                {comment.mine ? (
                  <button
                    type="button"
                    aria-label={c.delete}
                    title={c.delete}
                    onClick={() => setConfirmingId(comment.id)}
                    className="grid size-6 place-items-center rounded-[6px] border-0 bg-transparent text-meta-foreground outline-none hover:bg-state-hover focus-visible:ring-2 focus-visible:ring-focus-ring"
                  >
                    <Trash2 aria-hidden="true" className="size-3.5" />
                  </button>
                ) : null}
              </div>
            )}
          </div>
        </div>
      ))}
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
      <label className="block">
        <span className="sr-only">{c.reply}</span>
        <input
          ref={field}
          type="text"
          value={draft}
          onChange={(event) => setDraft(event.target.value)}
          onKeyDown={onKeyDown}
          disabled={!canComment}
          placeholder={c.replyPlaceholder}
          aria-label={c.reply}
          className="box-border h-8.5 w-full rounded-[8px] border border-line-strong bg-background px-2.5 text-compact text-foreground outline-none placeholder:text-meta-foreground focus-visible:ring-2 focus-visible:ring-focus-ring disabled:cursor-default leading-[1.45]"
        />
      </label>
      <div className="flex flex-wrap gap-1.5">
        <button
          type="button"
          aria-disabled="true"
          title={c.unavailable}
          className={OUTLINE_BUTTON}
        >
          {c.createTask}
        </button>
      </div>
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
      className="box-content flex w-[308px] shrink-0 flex-col border-l border-border bg-doc-panel"
    >
      <div
        role="tablist"
        aria-label={p.tabs}
        className="flex shrink-0 gap-0.5 border-b border-border px-3"
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
        {callbacks?.onPanelOpenChange ? (
          <button
            type="button"
            aria-label={p.close}
            title={p.close}
            onClick={() => callbacks.onPanelOpenChange?.(false)}
            className="my-auto ml-auto grid size-7 shrink-0 place-items-center rounded-[7px] text-meta-foreground outline-none hover:bg-state-hover hover:text-foreground focus-visible:ring-2 focus-visible:ring-focus-ring"
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
            pendingQuote={pendingQuote}
            canComment={canComment}
            callbacks={callbacks}
          />
        ) : null}
      </div>
    </aside>
  );
}

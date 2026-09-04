import { useState } from "react";
import type {
  Block,
  BreadcrumbItem,
  PageActivity,
  PageComment,
  PageTreeNode,
} from "@notrelix/docs-core";
import { Avatar, AvatarFallback, Button, Skeleton } from "@notrelix/ui-web";
import {
  BookOpen,
  CheckSquare,
  ChevronRight,
  Clock,
  FileText,
  MessageSquare,
  MoreHorizontal,
  Plus,
  Search,
  Send,
  Share2,
  Sparkles,
  Square,
  Star,
  Trash2,
} from "lucide-react";

export interface DocPageSurfaceCallbacks {
  onAddPage?: (parentId: string | null) => void;
  onToggleFavorite?: () => void;
  onShare?: () => void;
  onAction?: (action: string) => void;
  onUpdateBlockText?: (blockId: string, text: string) => void;
  onToggleTodo?: (blockId: string, checked: boolean) => void;
  onDeleteBlock?: (blockId: string) => void;
  onCreateComment?: (body: string) => void;
  onDeleteComment?: (commentId: string) => void;
}

export interface DocPageScreenSurfaceProps {
  status?: "ready" | "loading" | "error";
  workspaceId: string;
  pageId: string;
  pageTitle: string;
  isFavorited: boolean;
  breadcrumbs: BreadcrumbItem[];
  pages: PageTreeNode[];
  blocks: Block[];
  comments: PageComment[];
  history: PageActivity[];
  callbacks?: DocPageSurfaceCallbacks;
}

function formatDate(value: string): string {
  return value.slice(0, 10);
}

function actionLabel(action: PageActivity["action"]): string {
  switch (action) {
    case "created":
      return "created this page";
    case "edited":
      return "edited this page";
    case "commented":
      return "commented on this page";
    case "shared":
      return "shared this page";
    case "moved":
      return "moved this page";
    case "published":
      return "published this page";
  }
}

function BlockSurface({
  block,
  callbacks,
}: {
  block: Block;
  callbacks?: DocPageSurfaceCallbacks;
}) {
  const text = block.properties.text ?? "";
  const baseInput =
    "w-full rounded-md border border-transparent bg-transparent px-2 py-1 text-foreground outline-none transition focus-visible:border-ring focus-visible:ring-1 focus-visible:ring-ring";

  if (block.type === "divider") {
    return <hr className="my-3 border-border" />;
  }

  if (block.type === "todo") {
    const checked = Boolean(block.properties.checked);
    return (
      <div className="group flex items-center gap-2 rounded-lg py-1">
        <button
          type="button"
          aria-label={
            checked ? `Mark ${text} incomplete` : `Mark ${text} complete`
          }
          className="text-muted-foreground hover:text-primary"
          onClick={() => callbacks?.onToggleTodo?.(block.id, !checked)}
        >
          {checked ? (
            <CheckSquare className="h-4 w-4 text-primary" />
          ) : (
            <Square className="h-4 w-4" />
          )}
        </button>
        <input
          aria-label={`Edit block ${block.id}`}
          className={`${baseInput} ${checked ? "text-muted-foreground line-through" : ""}`}
          defaultValue={text}
          onBlur={(event) =>
            callbacks?.onUpdateBlockText?.(block.id, event.currentTarget.value)
          }
        />
        <Button
          type="button"
          variant="ghost"
          size="icon"
          className="h-7 w-7 opacity-0 transition group-hover:opacity-100"
          aria-label={`Delete block ${block.id}`}
          onClick={() => callbacks?.onDeleteBlock?.(block.id)}
        >
          <Trash2 className="h-3.5 w-3.5" />
        </Button>
      </div>
    );
  }

  if (block.type === "callout") {
    return (
      <div className="rounded-xl border bg-muted/40 p-4">
        <div className="flex items-start gap-3">
          <span aria-hidden="true" className="text-lg">
            {block.properties.icon ?? "💡"}
          </span>
          <textarea
            aria-label={`Edit block ${block.id}`}
            className={`${baseInput} min-h-16 resize-none text-sm`}
            defaultValue={text}
            onBlur={(event) =>
              callbacks?.onUpdateBlockText?.(
                block.id,
                event.currentTarget.value,
              )
            }
          />
        </div>
      </div>
    );
  }

  if (block.type === "code") {
    return (
      <textarea
        aria-label={`Edit block ${block.id}`}
        className={`${baseInput} min-h-20 resize-none bg-muted font-mono text-sm`}
        defaultValue={text}
        onBlur={(event) =>
          callbacks?.onUpdateBlockText?.(block.id, event.currentTarget.value)
        }
      />
    );
  }

  const textClass =
    block.type === "heading_1"
      ? "text-3xl font-semibold"
      : block.type === "heading_2"
        ? "text-2xl font-semibold"
        : block.type === "heading_3"
          ? "text-xl font-medium"
          : block.type === "quote"
            ? "border-l-2 border-primary/50 pl-4 italic text-muted-foreground"
            : "";

  return (
    <div className="group flex items-start gap-2">
      <input
        aria-label={`Edit block ${block.id}`}
        className={`${baseInput} ${textClass}`}
        defaultValue={text}
        onBlur={(event) =>
          callbacks?.onUpdateBlockText?.(block.id, event.currentTarget.value)
        }
      />
      <Button
        type="button"
        variant="ghost"
        size="icon"
        className="mt-0.5 h-7 w-7 opacity-0 transition group-hover:opacity-100"
        aria-label={`Delete block ${block.id}`}
        onClick={() => callbacks?.onDeleteBlock?.(block.id)}
      >
        <Trash2 className="h-3.5 w-3.5" />
      </Button>
    </div>
  );
}

export function DocPageHeaderSurface({
  pageTitle,
  breadcrumbs,
  isFavorited,
  callbacks,
}: {
  pageTitle: string;
  breadcrumbs: BreadcrumbItem[];
  isFavorited: boolean;
  callbacks?: DocPageSurfaceCallbacks;
}) {
  const [showActions, setShowActions] = useState(false);

  return (
    <header className="border-b bg-background">
      <div className="flex items-center gap-1.5 px-6 py-2 text-xs text-muted-foreground">
        <BookOpen className="h-3.5 w-3.5" />
        {breadcrumbs.map((breadcrumb, index) => (
          <span key={breadcrumb.id} className="flex min-w-0 items-center gap-1">
            {index > 0 ? <ChevronRight className="h-3 w-3" /> : null}
            <span className="truncate">
              {breadcrumb.icon ? `${breadcrumb.icon} ` : ""}
              {breadcrumb.title}
            </span>
          </span>
        ))}
      </div>

      <div className="flex items-center justify-between gap-3 px-6 py-3">
        <h1 className="min-w-0 truncate text-lg font-semibold">{pageTitle}</h1>
        <div className="flex shrink-0 items-center gap-1">
          <Button
            type="button"
            variant="ghost"
            size="sm"
            className="gap-1.5"
            aria-pressed={isFavorited}
            onClick={() => callbacks?.onToggleFavorite?.()}
          >
            <Star
              className={`h-3.5 w-3.5 ${isFavorited ? "fill-yellow-500 text-yellow-500" : ""}`}
            />
            {isFavorited ? "Favorited" : "Favorite"}
          </Button>
          <Button
            type="button"
            variant="ghost"
            size="sm"
            className="gap-1.5"
            onClick={() => callbacks?.onShare?.()}
          >
            <Share2 className="h-3.5 w-3.5" />
            Share
          </Button>
          <div className="relative">
            <Button
              type="button"
              variant="ghost"
              size="icon"
              aria-label="Document actions"
              aria-expanded={showActions}
              onClick={() => setShowActions((value) => !value)}
            >
              <MoreHorizontal className="h-4 w-4" />
            </Button>
            {showActions ? (
              <div className="absolute right-0 top-10 z-20 w-48 overflow-hidden rounded-lg border bg-popover p-1 shadow-lg">
                {["Duplicate", "Move to...", "Export", "Delete"].map(
                  (action) => (
                    <button
                      key={action}
                      type="button"
                      className="w-full rounded-md px-3 py-2 text-left text-sm hover:bg-muted"
                      onClick={() => callbacks?.onAction?.(action)}
                    >
                      {action}
                    </button>
                  ),
                )}
              </div>
            ) : null}
          </div>
        </div>
      </div>
    </header>
  );
}

function PageTreeNodeSurface({
  node,
  workspaceId,
  currentPageId,
  callbacks,
}: {
  node: PageTreeNode;
  workspaceId: string;
  currentPageId?: string;
  callbacks?: DocPageSurfaceCallbacks;
}) {
  const [open, setOpen] = useState(node.children.length > 0);
  const active = currentPageId === node.id;

  return (
    <li>
      <div
        className={`flex items-center gap-1 rounded-lg px-2 py-1.5 text-sm ${
          active
            ? "bg-muted font-medium text-foreground"
            : "text-muted-foreground"
        }`}
        style={{ paddingLeft: 8 + node.depth * 16 }}
      >
        <button
          type="button"
          aria-label={open ? `Collapse ${node.title}` : `Expand ${node.title}`}
          className={`grid h-6 w-6 place-items-center rounded hover:bg-muted ${
            node.children.length === 0 ? "invisible" : ""
          }`}
          onClick={() => setOpen((value) => !value)}
        >
          <ChevronRight
            className={`h-3 w-3 transition ${open ? "rotate-90" : ""}`}
          />
        </button>
        <a
          href={`/workspaces/${workspaceId}/docs/${node.id}`}
          className="flex min-w-0 flex-1 items-center gap-2"
        >
          <span className="w-5 shrink-0 text-center text-xs">
            {node.icon ?? <FileText className="mx-auto h-3.5 w-3.5" />}
          </span>
          <span className="truncate">{node.title}</span>
        </a>
        <Button
          type="button"
          variant="ghost"
          size="icon"
          className="h-7 w-7"
          aria-label={`Add page under ${node.title}`}
          onClick={() => callbacks?.onAddPage?.(node.id)}
        >
          <Plus className="h-3.5 w-3.5" />
        </Button>
      </div>
      {open && node.children.length > 0 ? (
        <ul className="space-y-0.5">
          {node.children.map((child) => (
            <PageTreeNodeSurface
              key={child.id}
              node={child}
              workspaceId={workspaceId}
              currentPageId={currentPageId}
              callbacks={callbacks}
            />
          ))}
        </ul>
      ) : null}
    </li>
  );
}

export function DocPageTreeSurface({
  pages,
  workspaceId,
  currentPageId,
  callbacks,
}: {
  pages: PageTreeNode[];
  workspaceId: string;
  currentPageId?: string;
  callbacks?: DocPageSurfaceCallbacks;
}) {
  return (
    <aside
      className="flex h-full flex-col border-r bg-background"
      aria-label="Document pages"
    >
      <div className="flex items-center justify-between border-b px-3 py-2">
        <h2 className="text-sm font-semibold text-muted-foreground">Pages</h2>
        <Button
          type="button"
          variant="ghost"
          size="icon"
          className="h-7 w-7"
          aria-label="Create root page"
          onClick={() => callbacks?.onAddPage?.(null)}
        >
          <Plus className="h-3.5 w-3.5" />
        </Button>
      </div>
      <div className="px-3 py-2">
        <div className="flex items-center gap-2 rounded-lg bg-muted px-2 py-1.5 text-sm text-muted-foreground">
          <Search className="h-3.5 w-3.5" />
          <span>Search pages...</span>
        </div>
      </div>
      <div className="min-h-0 flex-1 overflow-y-auto px-1 py-1">
        {pages.length === 0 ? (
          <div className="px-4 py-8 text-center text-sm text-muted-foreground">
            <FileText className="mx-auto mb-2 h-8 w-8 opacity-50" />
            <p>No pages yet</p>
            <Button
              type="button"
              variant="ghost"
              size="sm"
              className="mt-2"
              onClick={() => callbacks?.onAddPage?.(null)}
            >
              Create first page
            </Button>
          </div>
        ) : (
          <ul className="space-y-0.5">
            {pages.map((node) => (
              <PageTreeNodeSurface
                key={node.id}
                node={node}
                workspaceId={workspaceId}
                currentPageId={currentPageId}
                callbacks={callbacks}
              />
            ))}
          </ul>
        )}
      </div>
    </aside>
  );
}

export function DocCommentsSurface({
  comments,
  callbacks,
}: {
  comments: PageComment[];
  callbacks?: DocPageSurfaceCallbacks;
}) {
  const [draft, setDraft] = useState("");

  function submit() {
    const body = draft.trim();
    if (!body) return;
    callbacks?.onCreateComment?.(body);
    setDraft("");
  }

  return (
    <section className="space-y-4" aria-label="Document comments">
      <div className="flex items-center gap-2 text-sm font-medium text-muted-foreground">
        <MessageSquare className="h-4 w-4" />
        Comments ({comments.length})
      </div>
      {comments.length === 0 ? (
        <p className="py-3 text-sm italic text-muted-foreground">
          No comments yet
        </p>
      ) : (
        <div className="divide-y divide-border">
          {comments.map((comment) => (
            <div key={comment.id} className="group flex gap-3 py-3">
              <Avatar className="h-8 w-8 shrink-0">
                <AvatarFallback className="bg-muted text-xs">
                  {comment.authorId.slice(0, 2).toUpperCase()}
                </AvatarFallback>
              </Avatar>
              <div className="min-w-0 flex-1">
                <div className="mb-1 flex items-center gap-2">
                  <span className="text-sm font-medium">
                    {comment.authorId}
                  </span>
                  <span className="text-xs text-muted-foreground">
                    {formatDate(comment.createdAt)}
                  </span>
                  {comment.resolved ? (
                    <span className="rounded border border-border bg-muted px-1.5 py-0.5 text-xs text-foreground">
                      Resolved
                    </span>
                  ) : null}
                </div>
                <p className="whitespace-pre-wrap text-sm text-foreground">
                  {comment.body}
                </p>
              </div>
              <Button
                type="button"
                variant="ghost"
                size="icon"
                className="h-7 w-7 text-muted-foreground"
                aria-label={`Delete comment ${comment.id}`}
                onClick={() => callbacks?.onDeleteComment?.(comment.id)}
              >
                <Trash2 className="h-3.5 w-3.5" />
              </Button>
            </div>
          ))}
        </div>
      )}
      <div className="flex gap-2">
        <input
          type="text"
          value={draft}
          onChange={(event) => setDraft(event.currentTarget.value)}
          onKeyDown={(event) => {
            if (event.key === "Enter") submit();
          }}
          placeholder="Add a comment..."
          aria-label="New comment"
          className="h-9 min-w-0 flex-1 rounded-md border border-input bg-transparent px-3 py-1 text-sm outline-none focus-visible:ring-1 focus-visible:ring-ring"
        />
        <Button
          type="button"
          size="icon"
          className="h-9 w-9 shrink-0"
          aria-label="Send comment"
          disabled={!draft.trim()}
          onClick={submit}
        >
          <Send className="h-4 w-4" />
        </Button>
      </div>
    </section>
  );
}

export function DocHistorySurface({ history }: { history: PageActivity[] }) {
  return (
    <section className="space-y-4" aria-label="Document history">
      <div className="flex items-center gap-2 text-sm font-medium text-muted-foreground">
        <Clock className="h-4 w-4" />
        History ({history.length})
      </div>
      {history.length === 0 ? (
        <p className="text-sm italic text-muted-foreground">No history yet</p>
      ) : (
        <div className="space-y-1">
          {history.map((activity) => (
            <div
              key={activity.id}
              className="flex items-start gap-3 rounded-lg px-2 py-2.5 hover:bg-muted/50"
            >
              <div className="mt-0.5 text-muted-foreground">
                {activity.action === "published" ? (
                  <Sparkles className="h-3.5 w-3.5" />
                ) : activity.action === "commented" ? (
                  <MessageSquare className="h-3.5 w-3.5" />
                ) : (
                  <FileText className="h-3.5 w-3.5" />
                )}
              </div>
              <div className="min-w-0 flex-1">
                <p className="text-sm">
                  <span className="font-medium">{activity.actorId}</span>{" "}
                  <span className="text-muted-foreground">
                    {actionLabel(activity.action)}
                  </span>
                </p>
                {activity.targetLabel ? (
                  <p className="mt-0.5 truncate text-xs text-muted-foreground">
                    {activity.targetLabel}
                  </p>
                ) : null}
              </div>
              <span className="whitespace-nowrap text-xs text-muted-foreground">
                {formatDate(activity.createdAt)}
              </span>
            </div>
          ))}
        </div>
      )}
    </section>
  );
}

export function DocPageScreenSurface({
  status = "ready",
  workspaceId,
  pageId,
  pageTitle,
  isFavorited,
  breadcrumbs,
  pages,
  blocks,
  comments,
  history,
  callbacks,
}: DocPageScreenSurfaceProps) {
  if (status === "loading") {
    return (
      <div className="grid h-[720px] grid-cols-[280px_1fr] overflow-hidden rounded-lg border bg-background">
        <div className="space-y-3 border-r p-4">
          {Array.from({ length: 6 }, (_, index) => (
            <Skeleton key={index} className="h-8 w-full" />
          ))}
        </div>
        <div className="space-y-4 p-8">
          <Skeleton className="h-10 w-2/3" />
          <Skeleton className="h-6 w-full" />
          <Skeleton className="h-6 w-5/6" />
          <Skeleton className="h-24 w-full" />
        </div>
      </div>
    );
  }

  if (status === "error") {
    return (
      <div className="grid h-[720px] place-items-center rounded-lg border bg-background p-8 text-center">
        <div>
          <FileText className="mx-auto mb-3 h-10 w-10 text-muted-foreground" />
          <h2 className="text-lg font-semibold">Document unavailable</h2>
          <p className="mt-1 text-sm text-muted-foreground">
            The document could not be prepared for presentation.
          </p>
        </div>
      </div>
    );
  }

  return (
    <div className="grid h-[720px] grid-cols-[280px_1fr] overflow-hidden rounded-lg border bg-background text-foreground">
      <DocPageTreeSurface
        pages={pages}
        workspaceId={workspaceId}
        currentPageId={pageId}
        callbacks={callbacks}
      />
      <main className="min-w-0 overflow-y-auto">
        <DocPageHeaderSurface
          pageTitle={pageTitle}
          breadcrumbs={breadcrumbs}
          isFavorited={isFavorited}
          callbacks={callbacks}
        />
        <article className="mx-auto max-w-3xl space-y-6 px-8 py-8">
          {blocks.length === 0 ? (
            <div className="rounded-xl border border-dashed p-8 text-center text-sm text-muted-foreground">
              Start writing with a heading, paragraph, checklist, or callout.
            </div>
          ) : (
            <div className="space-y-2">
              {blocks.map((block) => (
                <BlockSurface
                  key={block.id}
                  block={block}
                  callbacks={callbacks}
                />
              ))}
            </div>
          )}
          <div className="grid gap-6 border-t pt-6 md:grid-cols-2">
            <DocCommentsSurface comments={comments} callbacks={callbacks} />
            <DocHistorySurface history={history} />
          </div>
        </article>
      </main>
    </div>
  );
}

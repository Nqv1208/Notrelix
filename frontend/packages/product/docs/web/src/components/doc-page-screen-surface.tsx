import type {
  Block,
  BreadcrumbItem,
  PageActivity,
  PageComment,
  PageTreeNode,
} from "@notrelix/docs-core";
import { Button, Skeleton } from "@notrelix/ui-web";
import { CheckSquare, FileText, Square, Trash2 } from "lucide-react";
import type { DocPageSurfaceCallbacks } from "./doc-page-surface-models";
import { DocPageTreeSurface } from "./doc-page-tree-surface";
import { DocPageHeaderSurface } from "./doc-page-header-surface";
import { DocCommentsSurface } from "./doc-page-comments-surface";
import { DocHistorySurface } from "./doc-page-history-surface";

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

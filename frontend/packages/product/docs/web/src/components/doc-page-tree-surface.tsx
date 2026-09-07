import { useState } from "react";
import type { PageTreeNode } from "@notrelix/docs-core";
import { Button } from "@notrelix/ui-web";
import { ChevronRight, FileText, Plus, Search } from "lucide-react";
import type { DocPageSurfaceCallbacks } from "./doc-page-surface-models";

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
import React, { useState, memo } from 'react';
import {
  createUsePageList,
  createUseCreatePage,
  type DocsApiClient,
  type PageApiEndpoints,
  type PageTreeNode,
} from '@notrelix/docs-core';
import {
  Button,
  Collapsible,
  CollapsibleContent,
  CollapsibleTrigger,
  ContextMenu,
  ContextMenuContent,
  ContextMenuItem,
  ContextMenuTrigger,
} from '@notrelix/ui-web';
import {
  ChevronRight,
  FileText,
  GripVertical,
  MoreHorizontal,
  Plus,
  Search,
  Star,
} from 'lucide-react';

interface CreateDocPageTreeDeps {
  api: DocsApiClient;
  endpoints: PageApiEndpoints;
}

interface PageTreeItemProps {
  node: PageTreeNode;
  workspaceId: string;
  density: 'default' | 'compact';
  currentPageId?: string;
  onCreatePage: (parentId: string | null) => void;
}

const PageTreeItem = memo(function PageTreeItem({
  node,
  workspaceId,
  density,
  currentPageId,
  onCreatePage,
}: PageTreeItemProps) {
  const [open, setOpen] = useState(false);
  const hasChildren = node.children.length > 0;
  const isActive = currentPageId === node.id;

  return (
    <Collapsible open={open} onOpenChange={setOpen}>
      <ContextMenu>
        <ContextMenuTrigger asChild>
          <div
            className={`group flex items-center gap-1 rounded-lg text-sm transition hover:bg-muted hover:text-foreground ${
              density === 'compact' ? 'h-8 px-1.5' : 'h-10 px-2'
            } ${isActive ? 'bg-muted text-foreground font-medium' : 'text-muted-foreground'}`}
            style={{ paddingLeft: density === 'compact' ? 6 + (node as any).depth * 14 : 8 + (node as any).depth * 18 }}
          >
            <CollapsibleTrigger asChild disabled={!hasChildren}>
              <Button
                variant="ghost"
                size="icon"
                className={`size-5 shrink-0 ${!hasChildren ? 'opacity-0' : ''}`}
                aria-label={open ? 'Collapse page' : 'Expand page'}
              >
                <ChevronRight className={`size-3 transition-transform ${open ? 'rotate-90' : ''}`} />
              </Button>
            </CollapsibleTrigger>
            <GripVertical className="size-3.5 shrink-0 text-muted-foreground opacity-0 transition group-hover:opacity-100" />
            <a
              href={`/workspaces/${workspaceId}/docs/${node.id}`}
              className="flex min-w-0 flex-1 items-center gap-2"
            >
              <span className="w-5 shrink-0 text-center text-xs">
                {node.icon ?? <FileText className="size-3.5" />}
              </span>
              <span className="truncate">{node.title}</span>
            </a>
            <div className="flex shrink-0 opacity-0 transition group-hover:opacity-100">
              <Button
                variant="ghost"
                size="icon"
                className="size-5"
                aria-label="Add nested page"
                onClick={(e) => {
                  e.stopPropagation();
                  onCreatePage(node.id);
                }}
              >
                <Plus className="size-3" />
              </Button>
              <Button variant="ghost" size="icon" className="size-5" aria-label="Page actions">
                <MoreHorizontal className="size-3" />
              </Button>
            </div>
          </div>
        </ContextMenuTrigger>
        <ContextMenuContent>
          <ContextMenuItem>Open</ContextMenuItem>
          <ContextMenuItem onClick={() => onCreatePage(node.id)}>Add subpage</ContextMenuItem>
          <ContextMenuItem>Copy link</ContextMenuItem>
          <ContextMenuItem>Move</ContextMenuItem>
        </ContextMenuContent>
      </ContextMenu>
      {hasChildren ? (
        <CollapsibleContent>
          <PageTree
            tree={node.children}
            workspaceId={workspaceId}
            density={density}
            currentPageId={currentPageId}
            onCreatePage={onCreatePage}
          />
        </CollapsibleContent>
      ) : null}
    </Collapsible>
  );
});

interface PageTreeProps {
  tree: PageTreeNode[];
  workspaceId: string;
  density?: 'default' | 'compact';
  currentPageId?: string;
  onCreatePage: (parentId: string | null) => void;
}

const PageTree = memo(function PageTree({
  tree,
  workspaceId,
  density = 'default',
  currentPageId,
  onCreatePage,
}: PageTreeProps) {
  return (
    <nav aria-label="Page tree" className="space-y-0.5">
      {tree.map((node) => (
        <PageTreeItem
          key={node.id}
          node={node}
          workspaceId={workspaceId}
          density={density}
          currentPageId={currentPageId}
          onCreatePage={onCreatePage}
        />
      ))}
    </nav>
  );
});

export function createDocPageTree({ api, endpoints }: CreateDocPageTreeDeps) {
  const usePageList = createUsePageList(api, endpoints);
  const useCreatePage = createUseCreatePage(api, endpoints);

  return function DocPageTree({
    workspaceId,
    currentPageId,
  }: {
    workspaceId: string;
    currentPageId?: string;
  }) {
    const { data: pages = [], isLoading } = usePageList(workspaceId) as { data: PageTreeNode[]; isLoading: boolean };
    const createPageMutation = useCreatePage(workspaceId);

    const handleCreatePage = (parentId: string | null) => {
      createPageMutation.mutate({
        title: 'Untitled',
        workspaceId,
        parentId,
      });
    };

    if (isLoading) {
      return (
        <div className="space-y-2 p-2">
          {Array.from({ length: 5 }).map((_, i) => (
            <div key={i} className="h-8 bg-muted animate-pulse rounded" />
          ))}
        </div>
      );
    }

    return (
      <div className="flex flex-col h-full">
        {/* Header */}
        <div className="flex items-center justify-between px-3 py-2 border-b">
          <h3 className="text-sm font-semibold text-muted-foreground">Pages</h3>
          <Button
            variant="ghost"
            size="icon"
            className="size-6"
            onClick={() => handleCreatePage(null)}
          >
            <Plus className="size-3.5" />
          </Button>
        </div>

        {/* Search */}
        <div className="px-3 py-2">
          <div className="flex items-center gap-2 px-2 py-1.5 bg-muted rounded-lg text-sm text-muted-foreground">
            <Search className="size-3.5" />
            <span>Search pages...</span>
          </div>
        </div>

        {/* Tree */}
        <div className="flex-1 overflow-y-auto px-1">
          {pages.length === 0 ? (
            <div className="text-center py-8 text-sm text-muted-foreground">
              <FileText className="size-8 mx-auto mb-2 opacity-50" />
              <p>No pages yet</p>
              <Button
                variant="ghost"
                size="sm"
                className="mt-2"
                onClick={() => handleCreatePage(null)}
              >
                <Plus className="size-3.5 mr-1" />
                Create first page
              </Button>
            </div>
          ) : (
            <PageTree
              tree={pages}
              workspaceId={workspaceId}
              currentPageId={currentPageId}
              onCreatePage={handleCreatePage}
            />
          )}
        </div>

        {/* Favorites section */}
        <div className="border-t px-3 py-2">
          <div className="flex items-center gap-2 text-sm text-muted-foreground">
            <Star className="size-3.5" />
            <span>Favorites</span>
          </div>
        </div>
      </div>
    );
  };
}

import { useState, type ReactNode } from "react";
import {
  ChevronLeft,
  ChevronRight,
  FileText,
  Home,
  ListTodo,
  Plus,
  SquareKanban,
  Star,
  Workflow,
} from "lucide-react";
import {
  Button,
  cn,
  Collapsible,
  CollapsibleContent,
  CollapsibleTrigger,
  Dialog,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  Input,
  ScrollArea,
} from "@notrelix/ui-web";
import type { HomeSidebarData, HomeSidebarResource } from "../shell/home/types";

export type HomeSidebarNavigationTarget =
  | { readonly kind: "home" }
  | { readonly kind: "my-work" }
  | { readonly kind: "doc"; readonly resource: HomeSidebarResource }
  | { readonly kind: "board"; readonly resource: HomeSidebarResource }
  | { readonly kind: "workspace"; readonly resource: HomeSidebarResource };

export interface HomeSidebarNavigationProps {
  readonly className: string;
  readonly title?: string;
  readonly ariaLabel?: string;
  readonly ariaCurrent?: "page";
  readonly onClick?: () => void;
}

export type HomeSidebarNavigationRenderer = (
  target: HomeSidebarNavigationTarget,
  children: ReactNode,
  props: HomeSidebarNavigationProps,
) => ReactNode;

function NavigationLink({
  target,
  children,
  renderNavigation,
  props,
}: {
  target: HomeSidebarNavigationTarget;
  children: ReactNode;
  renderNavigation: HomeSidebarNavigationRenderer;
  props: HomeSidebarNavigationProps;
}) {
  return renderNavigation(target, children, props);
}

function PrimaryNav({
  renderNavigation,
}: {
  renderNavigation: HomeSidebarNavigationRenderer;
}) {
  const itemClass =
    "flex h-9 items-center gap-2 rounded-lg px-2 text-sm font-medium transition hover:bg-muted hover:text-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring";

  return (
    <nav className="space-y-1" aria-label="Primary navigation">
      <NavigationLink
        target={{ kind: "home" }}
        renderNavigation={renderNavigation}
        props={{
          className: `${itemClass} bg-accent text-accent-foreground`,
          ariaCurrent: "page",
        }}
      >
        <Home className="size-4" />
        <span>Home</span>
      </NavigationLink>
      <NavigationLink
        target={{ kind: "my-work" }}
        renderNavigation={renderNavigation}
        props={{ className: `${itemClass} text-muted-foreground` }}
      >
        <ListTodo className="size-4" />
        <span>My work</span>
      </NavigationLink>
    </nav>
  );
}

function ResourceLink({
  resource,
  type,
  favorite,
  renderNavigation,
}: {
  resource: HomeSidebarResource;
  type: "Doc" | "Board" | "Workspace";
  favorite?: boolean;
  renderNavigation: HomeSidebarNavigationRenderer;
}) {
  const Icon =
    type === "Doc" ? FileText : type === "Board" ? SquareKanban : Workflow;
  const target: HomeSidebarNavigationTarget = {
    kind: type === "Doc" ? "doc" : type === "Board" ? "board" : "workspace",
    resource,
  };
  const content = (
    <>
      <Icon className="size-4 shrink-0 text-muted-foreground" />
      <span className="min-w-0 flex-1">
        <span className="block truncate text-[13px] text-foreground">
          {resource.title}
        </span>
        <span className="block text-[11px] text-muted-foreground">{type}</span>
      </span>
      {favorite ? (
        <Star className="size-3.5 fill-amber-500 text-amber-500" />
      ) : null}
    </>
  );

  return (
    <NavigationLink
      target={target}
      renderNavigation={renderNavigation}
      props={{
        className:
          "flex min-h-9 items-center gap-2 rounded-lg px-2 py-1 text-sm transition hover:bg-muted focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring",
      }}
    >
      {content}
    </NavigationLink>
  );
}

function FavoritesSection({
  favorites,
  renderNavigation,
}: {
  favorites: readonly HomeSidebarResource[];
  renderNavigation: HomeSidebarNavigationRenderer;
}) {
  return (
    <Collapsible defaultOpen className="group/favorites mt-4">
      <CollapsibleTrigger className="mb-1 flex w-full items-center gap-1 px-2 py-1 text-[12px] font-semibold uppercase tracking-[0.06em] text-muted-foreground">
        Favorites
        <ChevronRight className="size-3.5 transition-transform group-data-[state=open]/favorites:rotate-90" />
      </CollapsibleTrigger>
      <CollapsibleContent className="space-y-1">
        {favorites.length === 0 ? (
          <p className="px-2 py-1 text-xs text-muted-foreground">
            No favorites yet
          </p>
        ) : (
          favorites
            .slice(0, 4)
            .map((resource) => (
              <ResourceLink
                key={resource.id}
                resource={resource}
                type="Doc"
                favorite
                renderNavigation={renderNavigation}
              />
            ))
        )}
      </CollapsibleContent>
    </Collapsible>
  );
}

function RecentSection({
  data,
  renderNavigation,
}: {
  data: HomeSidebarData;
  renderNavigation: HomeSidebarNavigationRenderer;
}) {
  const items = [
    ...data.recentDocs
      .slice(0, 2)
      .map((resource) => ({ resource, type: "Doc" as const })),
    ...data.recentBoards
      .slice(0, 2)
      .map((resource) => ({ resource, type: "Board" as const })),
  ];

  return (
    <section className="mt-4" aria-labelledby="recently-viewed-title">
      <h2
        id="recently-viewed-title"
        className="mb-1 px-2 py-1 text-[12px] font-semibold uppercase tracking-[0.06em] text-muted-foreground"
      >
        Recently viewed
      </h2>
      <div className="space-y-1">
        {items.map(({ resource, type }) => (
          <ResourceLink
            key={`${type}-${resource.workspaceId}-${resource.id}`}
            resource={resource}
            type={type}
            renderNavigation={renderNavigation}
          />
        ))}
      </div>
    </section>
  );
}

function WorkspaceSection({
  data,
  onCreateWorkspace,
  renderNavigation,
}: {
  data: HomeSidebarData;
  onCreateWorkspace?: (name: string) => void;
  renderNavigation: HomeSidebarNavigationRenderer;
}) {
  const [dialogOpen, setDialogOpen] = useState(false);
  const [workspaceName, setWorkspaceName] = useState("");

  const submit = () => {
    const name = workspaceName.trim();
    if (!name) return;
    onCreateWorkspace?.(name);
    setWorkspaceName("");
    setDialogOpen(false);
  };

  return (
    <section className="mt-4" aria-labelledby="home-workspaces-title">
      <div className="mb-1 flex items-center justify-between px-2 py-1">
        <h2
          id="home-workspaces-title"
          className="text-[12px] font-semibold uppercase tracking-[0.06em] text-muted-foreground"
        >
          Workspaces
        </h2>
        <Button
          type="button"
          variant="ghost"
          size="icon-sm"
          aria-label="Add workspace"
          onClick={() => setDialogOpen(true)}
        >
          <Plus className="size-4" />
        </Button>
      </div>
      <div className="space-y-1">
        {data.workspaces.slice(0, 5).map((workspace) => (
          <ResourceLink
            key={workspace.id}
            resource={{
              id: workspace.id,
              workspaceId: workspace.id,
              title: workspace.name,
            }}
            type="Workspace"
            renderNavigation={renderNavigation}
          />
        ))}
      </div>
      <Dialog open={dialogOpen} onOpenChange={setDialogOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Create workspace</DialogTitle>
          </DialogHeader>
          <div className="space-y-2">
            <Input
              value={workspaceName}
              onChange={(event) => setWorkspaceName(event.target.value)}
              onKeyDown={(event) => {
                if (event.key === "Enter") submit();
              }}
              autoFocus
              placeholder="Workspace name"
              aria-label="Workspace name"
            />
          </div>
          <DialogFooter>
            <Button variant="ghost" onClick={() => setDialogOpen(false)}>
              Cancel
            </Button>
            <Button onClick={submit} disabled={!workspaceName.trim()}>
              Create
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </section>
  );
}

function CompactRail({
  data,
  renderNavigation,
}: {
  data: HomeSidebarData;
  renderNavigation: HomeSidebarNavigationRenderer;
}) {
  const workspace = data.workspaces[0];
  const favorite = data.favoriteDocs[0];
  const recentBoard = data.recentBoards[0];
  const itemClass =
    "flex size-9 items-center justify-center rounded-lg text-muted-foreground transition hover:bg-muted hover:text-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring";

  return (
    <nav
      className="flex flex-col items-center gap-2 py-3"
      aria-label="Compact navigation"
    >
      <NavigationLink
        target={{ kind: "home" }}
        renderNavigation={renderNavigation}
        props={{
          className: `${itemClass} bg-accent text-accent-foreground`,
          title: "Home",
          ariaLabel: "Home",
          ariaCurrent: "page",
        }}
      >
        <Home className="size-4" />
      </NavigationLink>
      <NavigationLink
        target={{ kind: "my-work" }}
        renderNavigation={renderNavigation}
        props={{ className: itemClass, title: "My work", ariaLabel: "My work" }}
      >
        <ListTodo className="size-4" />
      </NavigationLink>
      {favorite ? (
        <NavigationLink
          target={{ kind: "doc", resource: favorite }}
          renderNavigation={renderNavigation}
          props={{
            className: itemClass,
            title: favorite.title,
            ariaLabel: `Favorite: ${favorite.title}`,
          }}
        >
          <Star className="size-4" />
        </NavigationLink>
      ) : null}
      {recentBoard ? (
        <NavigationLink
          target={{ kind: "board", resource: recentBoard }}
          renderNavigation={renderNavigation}
          props={{
            className: itemClass,
            title: recentBoard.title,
            ariaLabel: `Recent board: ${recentBoard.title}`,
          }}
        >
          <SquareKanban className="size-4" />
        </NavigationLink>
      ) : null}
      {workspace ? (
        <NavigationLink
          target={{
            kind: "workspace",
            resource: {
              id: workspace.id,
              workspaceId: workspace.id,
              title: workspace.name,
            },
          }}
          renderNavigation={renderNavigation}
          props={{
            className: `${itemClass} mt-1 bg-muted text-xs font-semibold text-foreground`,
            title: workspace.name,
            ariaLabel: `Workspace: ${workspace.name}`,
          }}
        >
          {workspace.icon || workspace.name.slice(0, 1).toUpperCase()}
        </NavigationLink>
      ) : null}
    </nav>
  );
}

export function HomeSidebarSurface({
  data,
  mode = "desktop",
  collapsed = false,
  previewOpen = false,
  onPreviewChange,
  onToggleCollapsed,
  onCreateWorkspace,
  renderNavigation,
}: {
  data: HomeSidebarData;
  mode?: "desktop" | "mobile";
  collapsed?: boolean;
  previewOpen?: boolean;
  onPreviewChange?: (open: boolean) => void;
  onToggleCollapsed?: () => void;
  onCreateWorkspace?: (name: string) => void;
  renderNavigation: HomeSidebarNavigationRenderer;
}) {
  const isMobile = mode === "mobile";
  const showFullContent = isMobile || !collapsed;
  const fullContent = (
    <ScrollArea className="h-full px-3 py-3">
      <PrimaryNav renderNavigation={renderNavigation} />
      <FavoritesSection
        favorites={data.favoriteDocs}
        renderNavigation={renderNavigation}
      />
      <RecentSection data={data} renderNavigation={renderNavigation} />
      <WorkspaceSection
        data={data}
        onCreateWorkspace={onCreateWorkspace}
        renderNavigation={renderNavigation}
      />
    </ScrollArea>
  );

  return (
    <aside
      data-home-sidebar
      onMouseEnter={() => {
        if (!isMobile && collapsed) onPreviewChange?.(true);
      }}
      onMouseLeave={() => {
        if (!isMobile && collapsed) onPreviewChange?.(false);
      }}
      onFocus={() => {
        if (!isMobile && collapsed) onPreviewChange?.(true);
      }}
      className={cn(
        "group/home-sidebar relative h-full shrink-0 border-border bg-sidebar text-sidebar-foreground",
        isMobile
          ? "w-full overflow-hidden"
          : [
              "overflow-visible rounded-l-xl border-r transition-[box-shadow] duration-200",
              collapsed ? "w-14" : "w-64",
            ],
      )}
    >
      <div className="h-full overflow-hidden rounded-l-xl">
        {showFullContent ? (
          fullContent
        ) : (
          <CompactRail data={data} renderNavigation={renderNavigation} />
        )}
      </div>
      {!isMobile && collapsed && previewOpen ? (
        <div
          className="absolute inset-y-0 left-full z-40 w-64 overflow-hidden rounded-r-xl border-y border-r border-border bg-sidebar text-sidebar-foreground shadow-2xl"
          onMouseEnter={() => onPreviewChange?.(true)}
          onMouseLeave={() => onPreviewChange?.(false)}
        >
          {fullContent}
        </div>
      ) : null}
      {!isMobile && onToggleCollapsed ? (
        <button
          type="button"
          aria-label={collapsed ? "Expand sidebar" : "Collapse sidebar"}
          onClick={onToggleCollapsed}
          className={cn(
            "absolute -right-3 top-5 z-50 flex size-7 items-center justify-center rounded-full border border-border bg-sidebar text-muted-foreground shadow-sm transition-[opacity,transform,box-shadow,color,background-color] duration-200 hover:bg-accent hover:text-accent-foreground",
            collapsed
              ? "opacity-75 group-hover/home-sidebar:opacity-100 group-hover/home-sidebar:shadow-md"
              : "-translate-x-1 opacity-0 group-hover/home-sidebar:translate-x-0 group-hover/home-sidebar:opacity-100",
          )}
        >
          {collapsed ? (
            <ChevronRight className="size-4" />
          ) : (
            <ChevronLeft className="size-4" />
          )}
        </button>
      ) : null}
    </aside>
  );
}

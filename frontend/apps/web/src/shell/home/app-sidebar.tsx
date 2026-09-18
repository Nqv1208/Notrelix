import { useState } from "react";
import { Link } from "@tanstack/react-router";
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
import type { HomeSidebarData, HomeSidebarResource } from "./types";

function PrimaryNav() {
  return (
    <nav className="space-y-1" aria-label="Primary navigation">
      <Link
        to="/home"
        className="flex h-9 items-center gap-2 rounded-lg bg-accent px-2 text-sm font-medium text-accent-foreground"
      >
        <Home className="size-4" />
        <span>Home</span>
      </Link>
      <a
        href="#my-work"
        className="flex h-9 items-center gap-2 rounded-lg px-2 text-sm text-muted-foreground transition hover:bg-muted hover:text-foreground"
      >
        <ListTodo className="size-4" />
        <span>My work</span>
      </a>
    </nav>
  );
}

function ResourceLink({
  resource,
  type,
  favorite,
}: {
  resource: HomeSidebarResource;
  type: "Doc" | "Board" | "Workspace";
  favorite?: boolean;
}) {
  const Icon =
    type === "Doc" ? FileText : type === "Board" ? SquareKanban : Workflow;
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

  if (type === "Doc") {
    return (
      <Link
        to="/workspaces/$workspaceId/docs/$docId"
        params={{ workspaceId: resource.workspaceId, docId: resource.id }}
        className="flex min-h-9 items-center gap-2 rounded-lg px-2 py-1 text-sm transition hover:bg-muted"
      >
        {content}
      </Link>
    );
  }

  if (type === "Board") {
    return (
      <Link
        to="/workspaces/$workspaceId/boards/$boardId"
        params={{ workspaceId: resource.workspaceId, boardId: resource.id }}
        className="flex min-h-9 items-center gap-2 rounded-lg px-2 py-1 text-sm transition hover:bg-muted"
      >
        {content}
      </Link>
    );
  }

  return (
    <Link
      to="/workspaces/$workspaceId"
      params={{ workspaceId: resource.workspaceId }}
      className="flex min-h-9 items-center gap-2 rounded-lg px-2 py-1 text-sm transition hover:bg-muted"
    >
      {content}
    </Link>
  );
}

function FavoritesSection({
  favorites,
}: {
  favorites: readonly HomeSidebarResource[];
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
              />
            ))
        )}
      </CollapsibleContent>
    </Collapsible>
  );
}

function RecentSection({ data }: { data: HomeSidebarData }) {
  const items = [
    ...data.recentDocs.slice(0, 2).map((resource) => ({
      resource,
      type: "Doc" as const,
    })),
    ...data.recentBoards.slice(0, 2).map((resource) => ({
      resource,
      type: "Board" as const,
    })),
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
          />
        ))}
      </div>
    </section>
  );
}

function WorkspaceSection({
  data,
  onCreateWorkspace,
}: {
  data: HomeSidebarData;
  onCreateWorkspace?: (name: string) => void;
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

export function AppSidebar({
  data,
  onCreateWorkspace,
  mode = "desktop",
}: {
  data: HomeSidebarData;
  onCreateWorkspace?: (name: string) => void;
  mode?: "desktop" | "mobile";
}) {
  const [collapsed, setCollapsed] = useState(false);
  const isMobile = mode === "mobile";

  return (
    <aside
      data-home-sidebar
      className={cn(
        "group/home-sidebar relative h-full shrink-0 border-border bg-card text-card-foreground",
        isMobile
          ? "w-full overflow-hidden"
          : [
              "overflow-visible rounded-l-xl border-r transition-[width,box-shadow] duration-500 ease-[cubic-bezier(0.22,1,0.36,1)]",
              collapsed ? "w-12 hover:w-14 hover:shadow-md" : "w-64",
            ],
      )}
    >
      <div
        className={cn(
          "h-full overflow-hidden rounded-l-xl transition-[opacity,transform] duration-300 ease-[cubic-bezier(0.22,1,0.36,1)]",
          !isMobile &&
            collapsed &&
            "pointer-events-none -translate-x-2 opacity-0",
        )}
      >
        <ScrollArea className="h-full px-3 py-3">
          <PrimaryNav />
          <FavoritesSection favorites={data.favoriteDocs} />
          <RecentSection data={data} />
          <WorkspaceSection data={data} onCreateWorkspace={onCreateWorkspace} />
        </ScrollArea>
      </div>

      {!isMobile ? (
        <button
          type="button"
          aria-label={collapsed ? "Expand sidebar" : "Collapse sidebar"}
          onClick={() => setCollapsed((value) => !value)}
          className={cn(
            "absolute -right-3 top-5 z-20 flex size-7 items-center justify-center rounded-full border border-border bg-card text-muted-foreground shadow-sm transition-[opacity,transform,box-shadow,color,background-color] duration-300 ease-[cubic-bezier(0.22,1,0.36,1)] hover:bg-accent hover:text-accent-foreground",
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

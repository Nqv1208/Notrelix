import { useState, type ComponentType } from "react";
import { Link, useNavigate } from "@tanstack/react-router";
import {
  Bot,
  CheckSquare,
  ChevronDown,
  ChevronLeft,
  ChevronRight,
  FileText,
  GitBranch,
  Heart,
  Home,
  Mic,
  MoreHorizontal,
  Plus,
  Search,
  Sparkles,
  SquareKanban,
  Star,
  Workflow,
} from "lucide-react";
import {
  createUseCreateWorkspace,
} from "@notrelix/features-workspace/web";
import { useAppRuntime } from "@notrelix/runtime-web";
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
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
  Input,
  ScrollArea,
} from "@notrelix/ui-web";
import type { HomeSidebarData, HomeSidebarResource } from "./types";

function DisabledNavItem({
  icon: Icon,
  label,
}: {
  icon: ComponentType<{ className?: string }>;
  label: string;
}) {
  return (
    <button
      type="button"
      disabled
      className="flex h-9 w-full items-center gap-2 rounded-lg px-2 text-sm text-muted-foreground opacity-60"
    >
      <Icon className="size-4" />
      <span>{label}</span>
    </button>
  );
}

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
      <DisabledNavItem icon={CheckSquare} label="My work" />
      <DisabledNavItem icon={MoreHorizontal} label="More" />
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
  const Icon = type === "Doc" ? FileText : type === "Board" ? SquareKanban : Workflow;
  const content = (
    <>
      <Icon className="size-4 shrink-0 text-muted-foreground" />
      <span className="min-w-0 flex-1">
        <span className="block truncate text-[13px] text-foreground">
          {resource.title}
        </span>
        <span className="block text-[11px] text-muted-foreground">{type}</span>
      </span>
      {favorite ? <Star className="size-3.5 fill-amber-500 text-amber-500" /> : null}
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

function FavoritesSection({ favorites }: { favorites: readonly HomeSidebarResource[] }) {
  return (
    <Collapsible defaultOpen className="group/favorites mt-4">
      <CollapsibleTrigger className="mb-1 flex w-full items-center gap-1 px-2 py-1 text-[12px] font-semibold uppercase tracking-[0.06em] text-muted-foreground">
        Favorites
        <ChevronRight className="size-3.5 transition-transform group-data-[state=open]/favorites:rotate-90" />
        <Search className="ml-auto size-3.5 opacity-0 transition group-hover/favorites:opacity-100" />
      </CollapsibleTrigger>
      <CollapsibleContent className="space-y-1">
        {favorites.length === 0 ? (
          <p className="px-2 py-1 text-xs text-muted-foreground">No favorites</p>
        ) : (
          favorites.map((resource) => (
            <ResourceLink key={resource.id} resource={resource} type="Doc" favorite />
          ))
        )}
      </CollapsibleContent>
    </Collapsible>
  );
}

function RecentSection({ data }: { data: HomeSidebarData }) {
  const workspaceResources: HomeSidebarResource[] = data.workspaces.map((workspace) => ({
    id: workspace.id,
    workspaceId: workspace.id,
    title: workspace.name,
  }));
  return (
    <section className="mt-4" aria-labelledby="recently-viewed-title">
      <h2 id="recently-viewed-title" className="mb-1 px-2 py-1 text-[12px] font-semibold uppercase tracking-[0.06em] text-muted-foreground">
        Recently viewed
      </h2>
      <div className="space-y-1">
        {workspaceResources.slice(0, 3).map((resource) => (
          <ResourceLink key={`workspace-${resource.id}`} resource={resource} type="Workspace" />
        ))}
        {data.recentDocs.slice(0, 2).map((resource) => (
          <ResourceLink key={`doc-${resource.id}`} resource={resource} type="Doc" />
        ))}
        {data.recentBoards.slice(0, 2).map((resource) => (
          <ResourceLink key={`board-${resource.id}`} resource={resource} type="Board" />
        ))}
      </div>
    </section>
  );
}

function WorkspaceSwitcher({ data }: { data: HomeSidebarData }) {
  const navigate = useNavigate();
  const { api: runtimeClient } = useAppRuntime();
  const activeWorkspace = data.workspaces[0];
  const [dialogOpen, setDialogOpen] = useState(false);
  const [workspaceName, setWorkspaceName] = useState("");
  const useCreateWorkspace = createUseCreateWorkspace({
    api: runtimeClient.api,
    endpoints: runtimeClient.endpoints,
  });
  const createWorkspace = useCreateWorkspace();

  const submit = () => {
    const name = workspaceName.trim();
    if (!name) return;
    const slug = name.toLowerCase().replace(/[^a-z0-9]+/g, "-").replace(/(^-|-$)/g, "");
    createWorkspace.mutate(
      { name, slug, isPersonal: false },
      {
        onSuccess: (workspace) => {
          setDialogOpen(false);
          setWorkspaceName("");
          navigate({ to: "/workspaces/$workspaceId", params: { workspaceId: workspace.id } });
        },
      },
    );
  };

  return (
    <section className="mt-4" aria-labelledby="home-workspace-switcher-title">
      <div className="mb-1 flex items-center justify-between px-2 py-1">
        <h2 id="home-workspace-switcher-title" className="text-[12px] font-semibold uppercase tracking-[0.06em] text-muted-foreground">
          Workspaces
        </h2>
        <div className="flex gap-1">
          <button type="button" disabled aria-label="Workspace tools" className="p-1 text-muted-foreground opacity-50"><MoreHorizontal className="size-4" /></button>
          <button type="button" disabled aria-label="Search workspaces" className="p-1 text-muted-foreground opacity-50"><Search className="size-4" /></button>
        </div>
      </div>
      <div className="flex items-center gap-2 px-2">
        <DropdownMenu>
          <DropdownMenuTrigger asChild>
            <button type="button" className="flex h-9 min-w-0 flex-1 items-center justify-between rounded-md border border-border px-2 hover:bg-muted">
              <span className="flex min-w-0 items-center gap-2">
                <span className="flex size-6 shrink-0 items-center justify-center rounded bg-primary/10 text-xs font-bold text-primary">
                  {activeWorkspace?.name.charAt(0).toUpperCase() || "W"}
                </span>
                <span className="truncate text-sm font-semibold">{activeWorkspace?.name || "No workspace"}</span>
              </span>
              <ChevronDown className="size-4 text-muted-foreground" />
            </button>
          </DropdownMenuTrigger>
          <DropdownMenuContent className="w-[280px]" align="start">
            <DropdownMenuLabel>Workspaces</DropdownMenuLabel>
            {data.workspaces.map((workspace) => (
              <DropdownMenuItem key={workspace.id} onSelect={() => navigate({ to: "/workspaces/$workspaceId", params: { workspaceId: workspace.id } })}>
                {workspace.name}
              </DropdownMenuItem>
            ))}
            <DropdownMenuSeparator />
            <DropdownMenuItem disabled>Manage workspace</DropdownMenuItem>
            <DropdownMenuItem disabled>Browse all workspaces</DropdownMenuItem>
            <DropdownMenuItem onSelect={() => setDialogOpen(true)}>Add new workspace</DropdownMenuItem>
          </DropdownMenuContent>
        </DropdownMenu>
        <Button type="button" size="icon" className="size-9 shrink-0" aria-label="Add new workspace" onClick={() => setDialogOpen(true)}>
          <Plus className="size-4" />
        </Button>
      </div>

      <Dialog open={dialogOpen} onOpenChange={setDialogOpen}>
        <DialogContent>
          <DialogHeader><DialogTitle>Create new workspace</DialogTitle></DialogHeader>
          <Input value={workspaceName} onChange={(event) => setWorkspaceName(event.target.value)} placeholder="Workspace name" />
          <DialogFooter>
            <Button variant="ghost" onClick={() => setDialogOpen(false)}>Cancel</Button>
            <Button onClick={submit} disabled={!workspaceName.trim() || createWorkspace.isPending}>
              {createWorkspace.isPending ? "Creating..." : "Create"}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </section>
  );
}

function AINav() {
  const items = [
    [Sparkles, "AI Sidekick"],
    [Heart, "Vibe"],
    [GitBranch, "AI Workflows"],
    [Bot, "AI Agents"],
    [Mic, "AI Notetaker"],
  ] as const;
  return (
    <Collapsible defaultOpen className="group/ai mt-4">
      <CollapsibleTrigger className="mb-1 flex w-full items-center gap-1 px-2 py-1 text-[13px] font-bold text-foreground">
        Notrelix AI
        <ChevronRight className="size-3.5 transition-transform group-data-[state=open]/ai:rotate-90" />
      </CollapsibleTrigger>
      <CollapsibleContent className="space-y-1">
        {items.map(([Icon, label]) => <DisabledNavItem key={label} icon={Icon} label={label} />)}
      </CollapsibleContent>
    </Collapsible>
  );
}

export function AppSidebar({ data }: { data: HomeSidebarData }) {
  const [collapsed, setCollapsed] = useState(false);
  return (
    <aside
      data-home-sidebar
      className={cn(
        "group/home-sidebar relative h-full shrink-0 overflow-hidden rounded-l-xl border-r border-border bg-card text-card-foreground transition-[width] duration-300",
        collapsed ? "w-12" : "w-64",
      )}
    >
      <div className={cn("h-full", collapsed && "pointer-events-none opacity-0")}>
        <ScrollArea className="h-full px-3 py-3">
          <PrimaryNav />
          <FavoritesSection favorites={data.favoriteDocs} />
          <RecentSection data={data} />
          <WorkspaceSwitcher data={data} />
          <AINav />
        </ScrollArea>
      </div>
      <button
        type="button"
        aria-label={collapsed ? "Expand sidebar" : "Collapse sidebar"}
        onClick={() => setCollapsed((value) => !value)}
        className={cn(
          "absolute right-0 top-5 z-20 flex size-7 items-center justify-center rounded-full border border-border bg-card text-muted-foreground shadow-sm transition",
          !collapsed && "opacity-0 group-hover/home-sidebar:opacity-100",
        )}
      >
        {collapsed ? <ChevronRight className="size-4" /> : <ChevronLeft className="size-4" />}
      </button>
    </aside>
  );
}

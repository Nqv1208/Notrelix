import { useEffect, useMemo, useRef, useState } from "react";
import {
  ArrowUpRight,
  CheckCircle2,
  Circle,
  Clock3,
  FileText,
  Search,
  SquareKanban,
} from "lucide-react";
import { WorkspaceDirectorySurface } from "@notrelix/features-workspace/web";
import { Badge, Input, Kbd, cn } from "@notrelix/ui-web";
import type {
  HomeActivityItem,
  HomeResourceItem,
  HomeViewModel,
  HomeTaskItem,
} from "./home-model";

export interface HomeSurfaceProps {
  data: HomeViewModel;
  onOpenResource?: (resource: HomeResourceItem) => void;
  onOpenWorkspace?: (workspaceId: string) => void;
  onToggleTask?: (taskId: string) => void;
}

function SectionHeading({
  id,
  title,
  description,
}: {
  id?: string;
  title: string;
  description?: string;
}) {
  return (
    <div>
      <h2
        id={id}
        className="text-base font-semibold tracking-tight text-foreground"
      >
        {title}
      </h2>
      {description ? (
        <p className="mt-1 text-xs text-muted-foreground">{description}</p>
      ) : null}
    </div>
  );
}

function ResourceCard({
  item,
  onOpen,
}: {
  item: HomeResourceItem;
  onOpen?: (item: HomeResourceItem) => void;
}) {
  const Icon = item.kind === "board" ? SquareKanban : FileText;

  return (
    <button
      type="button"
      onClick={() => onOpen?.(item)}
      className="group min-w-[240px] flex-1 rounded-2xl border border-border bg-card p-4 text-left transition hover:-translate-y-0.5 hover:border-primary/30 hover:shadow-sm focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
    >
      <div className="flex items-start justify-between gap-3">
        <span className="flex size-10 items-center justify-center rounded-xl bg-primary/10 text-primary">
          <Icon className="size-4" />
        </span>
        <ArrowUpRight className="size-4 text-muted-foreground opacity-0 transition group-hover:opacity-100 group-focus-visible:opacity-100" />
      </div>
      <h3 className="mt-5 line-clamp-1 text-sm font-semibold text-foreground">
        {item.title}
      </h3>
      <p className="mt-1 line-clamp-1 text-xs text-muted-foreground">
        {item.workspaceName} · {item.subtitle}
      </p>
      <p className="mt-3 flex items-center gap-1.5 text-[11px] text-muted-foreground">
        <Clock3 className="size-3" />
        {item.updatedLabel}
      </p>
    </button>
  );
}

function TaskRow({
  task,
  onToggle,
}: {
  task: HomeTaskItem;
  onToggle?: (taskId: string) => void;
}) {
  const priorityVariant =
    task.priority === "high"
      ? "outline"
      : task.priority === "medium"
        ? "secondary"
        : "outline";

  return (
    <div className="flex items-center gap-3 border-b border-border/70 px-4 py-3 last:border-b-0">
      <button
        type="button"
        aria-label={
          task.completed ? `Reopen ${task.title}` : `Complete ${task.title}`
        }
        onClick={() => onToggle?.(task.id)}
        className="shrink-0 rounded-full text-muted-foreground transition hover:text-primary focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
      >
        {task.completed ? (
          <CheckCircle2 className="size-5 text-primary" />
        ) : (
          <Circle className="size-5" />
        )}
      </button>
      <div className="min-w-0 flex-1">
        <p
          className={cn(
            "truncate text-sm font-medium text-foreground",
            task.completed && "text-muted-foreground line-through",
          )}
        >
          {task.title}
        </p>
        <p className="mt-0.5 text-xs text-muted-foreground">
          {task.workspaceName} · {task.dueLabel}
        </p>
      </div>
      <Badge
        variant={priorityVariant}
        className={cn(
          "capitalize",
          task.priority === "high" && "border-destructive/60",
        )}
      >
        {task.priority}
      </Badge>
    </div>
  );
}

function ActivityRow({ item }: { item: HomeActivityItem }) {
  return (
    <div className="flex gap-3 py-3 first:pt-0 last:pb-0">
      <span className="flex size-8 shrink-0 items-center justify-center rounded-full bg-muted text-[11px] font-semibold text-foreground">
        {item.initials}
      </span>
      <div className="min-w-0 flex-1">
        <p className="text-xs leading-5 text-muted-foreground">
          <span className="font-medium text-foreground">{item.actor}</span>{" "}
          {item.action}{" "}
          <span className="font-medium text-foreground">{item.target}</span>
        </p>
        <p className="mt-0.5 text-[11px] text-muted-foreground">
          {item.workspaceName} · {item.timeLabel}
        </p>
      </div>
    </div>
  );
}

export function HomeSurface({
  data,
  onOpenResource,
  onOpenWorkspace,
  onToggleTask,
}: HomeSurfaceProps) {
  const [searchQuery, setSearchQuery] = useState("");
  const searchRef = useRef<HTMLInputElement>(null);
  const normalizedQuery = searchQuery.trim().toLowerCase();

  useEffect(() => {
    const onKeyDown = (event: KeyboardEvent) => {
      if ((event.metaKey || event.ctrlKey) && event.key.toLowerCase() === "k") {
        event.preventDefault();
        searchRef.current?.focus();
      }
    };

    window.addEventListener("keydown", onKeyDown);
    return () => window.removeEventListener("keydown", onKeyDown);
  }, []);

  const visibleContinueItems = useMemo(() => {
    if (!normalizedQuery) return data.continueItems;
    return data.continueItems.filter((item) =>
      `${item.title} ${item.workspaceName} ${item.subtitle}`
        .toLowerCase()
        .includes(normalizedQuery),
    );
  }, [data.continueItems, normalizedQuery]);

  const visibleTasks = useMemo(() => {
    if (data.tasks.status !== "ready") return [];
    if (!normalizedQuery) return data.tasks.items;
    return data.tasks.items.filter((task) =>
      `${task.title} ${task.workspaceName} ${task.dueLabel}`
        .toLowerCase()
        .includes(normalizedQuery),
    );
  }, [data.tasks, normalizedQuery]);

  const visibleWorkspaces = useMemo(() => {
    if (!normalizedQuery) return data.workspaces;
    return data.workspaces.filter((workspace) =>
      `${workspace.name} ${workspace.description ?? ""}`
        .toLowerCase()
        .includes(normalizedQuery),
    );
  }, [data.workspaces, normalizedQuery]);

  return (
    <div className="home-surface-container mx-auto max-w-[1240px] space-y-8">
      <section className="flex flex-col gap-5 border-b border-border pb-6 lg:flex-row lg:items-end lg:justify-between">
        <div>
          <p className="text-sm font-medium text-primary">Welcome back</p>
          <h1 className="mt-1 text-3xl font-semibold tracking-[-0.02em] text-foreground">
            Good morning, {data.userName}
          </h1>
          <p className="mt-2 text-sm text-muted-foreground">
            Pick up where you left off and keep the important work moving.
          </p>
        </div>

        <div className="relative w-full lg:w-[380px]">
          <Search className="pointer-events-none absolute left-3 top-1/2 size-4 -translate-y-1/2 text-muted-foreground" />
          <Input
            ref={searchRef}
            value={searchQuery}
            onChange={(event) => setSearchQuery(event.target.value)}
            placeholder="Search work, tasks, and workspaces"
            aria-label="Search home content"
            className="h-10 bg-card pl-9 pr-14"
          />
          <Kbd className="pointer-events-none absolute right-2.5 top-1/2 -translate-y-1/2">
            ⌘K
          </Kbd>
        </div>
      </section>

      <section aria-labelledby="continue-working-title">
        <div className="mb-4">
          <SectionHeading
            id="continue-working-title"
            title="Continue working"
            description={
              data.continueItems[0]?.workspaceName
                ? `Recent work in ${data.continueItems[0].workspaceName}.`
                : "Recent work from your selected workspace."
            }
          />
        </div>
        {visibleContinueItems.length === 0 ? (
          <div className="rounded-2xl border border-dashed border-border p-6 text-sm text-muted-foreground">
            No recent work matches “{searchQuery}”.
          </div>
        ) : (
          <div className="flex gap-3 overflow-x-auto pb-1">
            {visibleContinueItems.map((item) => (
              <ResourceCard key={item.id} item={item} onOpen={onOpenResource} />
            ))}
          </div>
        )}
      </section>

      <div className="home-work-layout">
        <section
          id="my-work"
          aria-labelledby="my-work-title"
          className="overflow-hidden rounded-2xl border border-border bg-card"
        >
          <div className="flex items-center justify-between border-b border-border px-4 py-4">
            <SectionHeading
              id="my-work-title"
              title="My work"
              description="Tasks that need your attention next."
            />
            <span className="text-xs text-muted-foreground">
              {data.tasks.status === "ready"
                ? `${visibleTasks.filter((task) => !task.completed).length} open`
                : "Unavailable"}
            </span>
          </div>
          <div>
            {data.tasks.status === "unavailable" ? (
              <p className="p-5 text-sm text-muted-foreground">
                {data.tasks.message}
              </p>
            ) : visibleTasks.length === 0 ? (
              <p className="p-5 text-sm text-muted-foreground">
                {normalizedQuery
                  ? `No tasks match “${searchQuery}”.`
                  : "No tasks are available yet."}
              </p>
            ) : (
              visibleTasks.map((task) => (
                <TaskRow key={task.id} task={task} onToggle={onToggleTask} />
              ))
            )}
          </div>
        </section>

        <section
          aria-labelledby="home-activity-title"
          className="rounded-2xl border border-border bg-card p-4"
        >
          <div className="mb-4 border-b border-border pb-3">
            <SectionHeading
              id="home-activity-title"
              title="Activity"
              description="Recent updates from your teams."
            />
          </div>
          <div>
            {data.activity.status === "unavailable" ? (
              <p className="text-sm text-muted-foreground">
                {data.activity.message}
              </p>
            ) : data.activity.items.length === 0 ? (
              <p className="text-sm text-muted-foreground">
                No recent activity yet.
              </p>
            ) : (
              data.activity.items.map((item) => (
                <ActivityRow key={item.id} item={item} />
              ))
            )}
          </div>
        </section>
      </div>

      <section className="pb-4">
        <WorkspaceDirectorySurface
          workspaces={visibleWorkspaces}
          onOpenWorkspace={onOpenWorkspace}
        />
      </section>
    </div>
  );
}

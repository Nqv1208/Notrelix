import {
  ArrowDownUp,
  Bot,
  CalendarDays,
  ChevronDown,
  Filter,
  Group,
  ListPlus,
  Search,
  Settings2,
  UserRound,
} from "lucide-react";
import { Button, Input, ToggleGroup, ToggleGroupItem } from "@notrelix/ui-web";
import type {
  WorkspaceView,
  WorkspaceViewType,
} from "../../core/types/workspace";

export type WorkspaceToolbarAction =
  | "new-task"
  | "add-block"
  | "add-widget"
  | "today"
  | "person"
  | "filter"
  | "sort"
  | "hide"
  | "group"
  | "more"
  | "settings"
  | "sync"
  | "refresh"
  | "ai-summary";

interface WorkspaceContextualToolbarProps {
  activeType: WorkspaceViewType;
  activeView?: WorkspaceView;
  searchQuery?: string;
  onSearchChange?: (query: string) => void;
  onAction?: (action: WorkspaceToolbarAction) => void;
}

export function WorkspaceContextualToolbar({
  activeType,
  activeView,
  searchQuery,
  onSearchChange,
  onAction,
}: WorkspaceContextualToolbarProps) {
  if (activeType === "table") return null;
  if (activeType === "doc")
    return (
      <DocToolbar
        pageId={activeView?.target.pageId ?? ""}
        searchQuery={searchQuery}
        onSearchChange={onSearchChange}
        onAction={onAction}
      />
    );
  if (activeType === "kanban")
    return (
      <KanbanToolbar
        searchQuery={searchQuery}
        onSearchChange={onSearchChange}
        onAction={onAction}
      />
    );
  if (activeType === "calendar") return <CalendarToolbar onAction={onAction} />;
  if (activeType === "timeline") return <TimelineToolbar onAction={onAction} />;
  if (activeType === "dashboard")
    return <DashboardToolbar onAction={onAction} />;
  return null;
}

function SearchBox({
  placeholder = "Search",
  value,
  onChange,
}: {
  placeholder?: string;
  value?: string;
  onChange?: (query: string) => void;
}) {
  return (
    <div className="relative hidden min-w-[180px] sm:block">
      <Search className="pointer-events-none absolute left-2.5 top-1/2 size-4 -translate-y-1/2 text-muted-foreground" />
      <Input
        className="h-9 rounded-full bg-card pl-8"
        placeholder={placeholder}
        value={value}
        onChange={(e) => onChange?.(e.target.value)}
      />
    </div>
  );
}

function KanbanToolbar({
  searchQuery,
  onSearchChange,
  onAction,
}: Pick<
  WorkspaceContextualToolbarProps,
  "searchQuery" | "onSearchChange" | "onAction"
>) {
  return (
    <ToolbarShell>
      <SearchBox
        placeholder="Search cards"
        value={searchQuery}
        onChange={onSearchChange}
      />
      <ToolbarButton
        icon={UserRound}
        label="Person"
        action="person"
        onAction={onAction}
      />
      <ToolbarButton
        icon={Filter}
        label="Filter"
        action="filter"
        onAction={onAction}
      />
      <ToolbarButton
        icon={ArrowDownUp}
        label="Sort"
        action="sort"
        onAction={onAction}
      />
      <ToolbarButton
        icon={Group}
        label="Group by status"
        action="group"
        onAction={onAction}
      />
      <ToolbarButton
        icon={Settings2}
        label="Board settings"
        action="settings"
        onAction={onAction}
      />
    </ToolbarShell>
  );
}

function DocToolbar({
  pageId: _pageId,
  searchQuery,
  onSearchChange,
  onAction,
}: { pageId: string } & Pick<
  WorkspaceContextualToolbarProps,
  "searchQuery" | "onSearchChange" | "onAction"
>) {
  return (
    <ToolbarShell>
      <Button
        size="sm"
        className="rounded-full"
        onClick={() => onAction?.("add-block")}
      >
        <ListPlus className="size-4" />
        Add block
      </Button>
      <SearchBox
        placeholder="Search in doc"
        value={searchQuery}
        onChange={onSearchChange}
      />
    </ToolbarShell>
  );
}

function CalendarToolbar({
  onAction,
}: Pick<WorkspaceContextualToolbarProps, "onAction">) {
  return (
    <ToolbarShell>
      <Button
        size="sm"
        className="rounded-full"
        onClick={() => onAction?.("today")}
      >
        <CalendarDays className="size-4" />
        Today
      </Button>
      <ToggleGroup
        type="single"
        defaultValue="month"
        className="hidden sm:flex"
      >
        <ToggleGroupItem value="month" size="sm">
          Month
        </ToggleGroupItem>
        <ToggleGroupItem value="week" size="sm">
          Week
        </ToggleGroupItem>
        <ToggleGroupItem value="day" size="sm">
          Day
        </ToggleGroupItem>
      </ToggleGroup>
      <ToolbarButton
        icon={Filter}
        label="Filter"
        action="filter"
        onAction={onAction}
      />
      <ToolbarButton
        icon={ArrowDownUp}
        label="Sync"
        action="sync"
        onAction={onAction}
      />
      <ToolbarButton
        icon={Settings2}
        label="Settings"
        action="settings"
        onAction={onAction}
      />
    </ToolbarShell>
  );
}

function TimelineToolbar({
  onAction,
}: Pick<WorkspaceContextualToolbarProps, "onAction">) {
  return (
    <ToolbarShell>
      <Button
        size="sm"
        className="rounded-full"
        onClick={() => onAction?.("today")}
      >
        <CalendarDays className="size-4" />
        Today
      </Button>
      <ToolbarButton
        icon={UserRound}
        label="Person"
        action="person"
        onAction={onAction}
      />
      <ToolbarButton
        icon={Filter}
        label="Filter"
        action="filter"
        onAction={onAction}
      />
      <ToolbarButton
        icon={Group}
        label="Group by list"
        action="group"
        onAction={onAction}
      />
      <ToolbarButton
        icon={Settings2}
        label="Timeline settings"
        action="settings"
        onAction={onAction}
      />
    </ToolbarShell>
  );
}

function DashboardToolbar({
  onAction,
}: Pick<WorkspaceContextualToolbarProps, "onAction">) {
  return (
    <ToolbarShell>
      <Button
        size="sm"
        className="rounded-full"
        onClick={() => onAction?.("add-widget")}
      >
        <ListPlus className="size-4" />
        Add widget
      </Button>
      <ToolbarButton
        icon={Filter}
        label="Filter"
        action="filter"
        onAction={onAction}
      />
      <ToolbarButton
        icon={ArrowDownUp}
        label="Refresh"
        action="refresh"
        onAction={onAction}
      />
      <ToolbarButton
        icon={Bot}
        label="AI summary"
        action="ai-summary"
        onAction={onAction}
      />
    </ToolbarShell>
  );
}

function ToolbarShell({ children }: { children: React.ReactNode }) {
  return (
    <div className="flex min-h-14 flex-wrap items-center gap-2 bg-card px-4 py-2 sm:px-6">
      {children}
    </div>
  );
}

function ToolbarButton({
  icon: Icon,
  label,
  compact,
  action,
  onAction,
}: {
  icon: typeof Search;
  label: string;
  compact?: boolean;
  action: WorkspaceToolbarAction;
  onAction?: (action: WorkspaceToolbarAction) => void;
}) {
  return (
    <Button
      variant="ghost"
      size="sm"
      className="rounded-full"
      aria-label={label}
      onClick={() => onAction?.(action)}
    >
      <Icon className="size-4" />
      {!compact ? <span className="hidden sm:inline">{label}</span> : null}
      {!compact ? (
        <ChevronDown className="hidden size-3.5 text-muted-foreground sm:block" />
      ) : null}
      {compact ? <span className="sr-only">{label}</span> : null}
    </Button>
  );
}

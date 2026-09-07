import { MoreHorizontal, Plus } from "lucide-react";
import { Button, cn } from "@notrelix/ui-web";
import type { WorkspaceView } from "../../core/types/workspace";

export interface WorkspaceViewTabsSurfaceProps {
  workspaceId: string;
  views: WorkspaceView[];
  activeViewId?: string;
  onSelectView?: (view: WorkspaceView) => void;
  onAddView?: () => void;
}

export function WorkspaceViewTabsSurface({
  views,
  activeViewId,
  onSelectView,
  onAddView,
}: WorkspaceViewTabsSurfaceProps) {
  return (
    <div className="border-b border-border bg-card">
      <div className="flex min-w-0 items-center gap-2 px-4 sm:px-6">
        <div
          role="tablist"
          aria-label="Workspace views"
          className="flex h-12 min-w-0 flex-1 items-center gap-1.5 overflow-x-auto py-1 whitespace-nowrap scrollbar-none"
        >
          {views.map((view) => {
            const active = view.id === activeViewId;
            return (
              <button
                key={view.id}
                type="button"
                role="tab"
                aria-selected={active}
                onClick={() => onSelectView?.(view)}
                className={cn(
                  "relative inline-flex h-9 items-center gap-1.5 rounded-lg px-3 text-sm font-medium text-muted-foreground transition hover:bg-muted/80 hover:text-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring",
                  active && "bg-muted/40 font-semibold text-foreground",
                )}
              >
                {view.name}
                {active ? (
                  <span className="absolute inset-x-2 -bottom-1 h-0.5 rounded-full bg-primary" />
                ) : null}
              </button>
            );
          })}
        </div>
        <Button
          variant="ghost"
          size="sm"
          className="h-9 rounded-full px-2.5"
          onClick={onAddView}
        >
          <Plus className="size-4" />
          <span className="sr-only sm:not-sr-only">Add view</span>
        </Button>
        <Button variant="ghost" size="icon" aria-label="More view actions">
          <MoreHorizontal className="size-4" />
        </Button>
      </div>
    </div>
  );
}
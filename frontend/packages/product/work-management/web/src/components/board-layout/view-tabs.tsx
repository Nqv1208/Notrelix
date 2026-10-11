import { LayoutGrid, Table, Calendar, GanttChart, Plus } from "lucide-react";
import {
  Button,
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
  Tabs,
  TabsList,
  TabsTrigger,
} from "@notrelix/ui-web";
import type { BoardHeaderCopy } from "./board-header.copy";

const VIEW_TYPES = [
  { type: "kanban", icon: LayoutGrid },
  { type: "table", icon: Table },
  { type: "calendar", icon: Calendar },
  { type: "timeline", icon: GanttChart },
] as const;

interface ViewTabsProps {
  activeView: string;
  onViewChange: (viewType: string) => void;
  copy: BoardHeaderCopy;
}

/**
 * The board's view switcher. Arrow keys move focus between tabs and Enter or
 * Space picks one (manual activation), so browsing the tabs never navigates.
 */
export function ViewTabs({ activeView, onViewChange, copy }: ViewTabsProps) {
  return (
    <Tabs
      value={activeView}
      onValueChange={onViewChange}
      activationMode="manual"
      className="min-w-0 gap-0"
    >
      <TabsList
        variant="line"
        aria-label={copy.boardViews}
        className="h-auto w-max gap-1 p-0 group-data-horizontal/tabs:h-auto"
      >
        {VIEW_TYPES.map((view) => {
          const Icon = view.icon;
          return (
            <TabsTrigger
              key={view.type}
              value={view.type}
              // The strip scrolls sideways on narrow screens; bring the
              // focused tab into view (browsers do not do it for us here).
              onFocus={(event) =>
                event.currentTarget.scrollIntoView?.({
                  block: "nearest",
                  inline: "nearest",
                })
              }
              className="h-auto flex-none gap-1.5 px-2.5 py-1.5 text-label font-medium text-muted-foreground hover:bg-muted/50 hover:text-foreground data-[state=active]:bg-muted data-[state=active]:text-foreground"
            >
              <Icon className="size-3.5" aria-hidden="true" />
              {copy.views[view.type]}
            </TabsTrigger>
          );
        })}
      </TabsList>
    </Tabs>
  );
}

export function AddViewMenu({
  onAddView,
  copy,
}: {
  onAddView: (type: string) => void;
  copy: BoardHeaderCopy;
}) {
  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <Button
          variant="ghost"
          size="sm"
          className="h-7 gap-1 text-label text-muted-foreground hover:text-foreground"
        >
          <Plus className="size-3.5" aria-hidden="true" />
          {copy.addView}
        </Button>
      </DropdownMenuTrigger>
      <DropdownMenuContent align="end" className="w-44">
        {VIEW_TYPES.map((view) => {
          const Icon = view.icon;
          return (
            <DropdownMenuItem
              key={view.type}
              onSelect={() => onAddView(view.type)}
            >
              <Icon className="text-muted-foreground" aria-hidden="true" />
              {copy.views[view.type]}
            </DropdownMenuItem>
          );
        })}
      </DropdownMenuContent>
    </DropdownMenu>
  );
}

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
              // No panels exist (the board renders outside the tabs), so drop
              // the aria-controls Radix would point at a missing element.
              aria-controls={undefined}
              // The shared line-variant rules paint every trigger transparent
              // (also under `dark:`) with high specificity, so the state
              // tokens go through the same group variants to win.
              className="h-auto flex-none gap-1.5 px-2.5 py-1.5 text-label font-medium text-muted-foreground group-data-[variant=line]/tabs-list:hover:bg-state-hover hover:bg-state-hover hover:text-foreground group-data-[variant=line]/tabs-list:data-[state=active]:bg-state-selected data-[state=active]:bg-state-selected dark:group-data-[variant=line]/tabs-list:data-[state=active]:bg-state-selected group-data-[variant=line]/tabs-list:data-[state=active]:border-b-primary dark:group-data-[variant=line]/tabs-list:data-[state=active]:border-b-primary data-[state=active]:text-state-selected-foreground"
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

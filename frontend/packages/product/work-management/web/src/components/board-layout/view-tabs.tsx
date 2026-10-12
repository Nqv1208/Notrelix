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
  { type: "table", icon: Table },
  { type: "kanban", icon: LayoutGrid },
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
        className="h-auto w-max gap-0.5 p-0 group-data-horizontal/tabs:h-auto"
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
              // The selected tab is underlined in the primary colour and
              // set in semibold, with no fill. The shared line-variant rules
              // paint borders (also under `dark:`) through group variants, so
              // the underline goes through the same variants to win; the
              // primitive's pseudo-element underline is not used.
              className="h-9 flex-none gap-1.5 rounded-t-md border-0 border-b-2 border-transparent px-3 py-0 text-body font-medium text-muted-foreground after:hidden hover:bg-state-hover hover:text-foreground data-[state=active]:font-semibold data-[state=active]:text-foreground dark:data-[state=active]:text-foreground group-data-[variant=line]/tabs-list:data-[state=active]:border-b-primary dark:group-data-[variant=line]/tabs-list:data-[state=active]:border-b-primary"
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
          size="icon-sm"
          aria-label={copy.addView}
          className="mb-0.5 text-muted-foreground hover:text-foreground"
        >
          <Plus aria-hidden="true" />
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

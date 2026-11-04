import {
  LayoutGrid,
  Table,
  Calendar,
  GanttChart,
  Lock,
  Plus,
} from "lucide-react";
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

/** A saved view of the board, as a tab. */
export interface ViewTabSavedView {
  readonly id: string;
  readonly name: string;
  readonly viewMode: string;
  readonly visibility?: "board" | "me";
  readonly isDefault?: boolean;
}

interface ViewTabsProps {
  /** A saved view's id, or a layout name (table, kanban...) when none is saved for it. */
  activeView: string;
  onViewChange: (viewKey: string) => void;
  copy: BoardHeaderCopy;
  /** The board's saved views. A layout with none keeps its own tab. */
  savedViews?: readonly ViewTabSavedView[];
}

interface ViewTab {
  readonly key: string;
  readonly type: (typeof VIEW_TYPES)[number]["type"];
  readonly label: string;
  readonly icon: (typeof VIEW_TYPES)[number]["icon"];
  readonly isPrivate: boolean;
}

/** One tab per saved view, in layout order; a layout with no saved view keeps its own tab. */
export function buildViewTabs(
  savedViews: readonly ViewTabSavedView[],
  copy: BoardHeaderCopy,
): ViewTab[] {
  return VIEW_TYPES.flatMap((view) => {
    const ofType = savedViews.filter((saved) => saved.viewMode === view.type);
    if (ofType.length === 0)
      return [
        {
          key: view.type,
          type: view.type,
          label: copy.views[view.type],
          icon: view.icon,
          isPrivate: false,
        },
      ];
    return ofType.map((saved) => ({
      key: saved.id,
      type: view.type,
      label: saved.name || copy.views[view.type],
      icon: view.icon,
      isPrivate: saved.visibility === "me",
    }));
  });
}

/**
 * The board's view switcher. Arrow keys move focus between tabs and Enter or
 * Space picks one (manual activation), so browsing the tabs never navigates.
 */
export function ViewTabs({
  activeView,
  onViewChange,
  copy,
  savedViews = [],
}: ViewTabsProps) {
  const tabs = buildViewTabs(savedViews, copy);
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
        {tabs.map((view) => {
          const Icon = view.icon;
          return (
            <TabsTrigger
              key={view.key}
              value={view.key}
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
              className="h-9 flex-none gap-[7px] rounded-none border-0 border-b-2 border-transparent px-3 py-0 text-nav text-muted-foreground after:hidden hover:bg-state-hover hover:text-foreground data-[state=active]:font-semibold data-[state=active]:text-foreground dark:data-[state=active]:text-foreground group-data-[variant=line]/tabs-list:data-[state=active]:border-b-primary dark:group-data-[variant=line]/tabs-list:data-[state=active]:border-b-primary"
            >
              <Icon className="size-4" aria-hidden="true" />
              {view.label}
              {view.isPrivate ? (
                <Lock className="size-3" aria-label={copy.privateView} />
              ) : null}
            </TabsTrigger>
          );
        })}
      </TabsList>
    </Tabs>
  );
}

/** The tablet band's order of the four layouts (`Final-T-*`): the Kanban comes first there. */
const TABLET_ORDER = ["kanban", "table", "calendar", "timeline"] as const;

/**
 * The tablet header's view switcher: the same tabs as `ViewTabs`, drawn as the 44 px segmented control of the
 * `Final-T-*` boards (text only, the active segment lifted on a white pill).
 */
export function TabletViewTabs({
  activeView,
  onViewChange,
  copy,
  savedViews = [],
}: ViewTabsProps) {
  const tabs = buildViewTabs(savedViews, copy).sort(
    (a, b) => TABLET_ORDER.indexOf(a.type) - TABLET_ORDER.indexOf(b.type),
  );
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
        className="h-12 w-max gap-0 rounded-xl bg-state-hover-strong p-0.5 data-[variant=line]:rounded-[12px] group-data-horizontal/tabs:h-12"
      >
        {tabs.map((view) => (
          <TabsTrigger
            key={view.key}
            value={view.key}
            aria-controls={undefined}
            className="h-11 flex-none gap-1.5 rounded-[10px] border-0 px-3.5 py-0 text-[14px] font-medium text-muted-foreground after:hidden hover:text-foreground data-[state=active]:font-[650] data-[state=active]:text-foreground group-data-[variant=line]/tabs-list:data-[state=active]:bg-popover dark:group-data-[variant=line]/tabs-list:data-[state=active]:bg-card group-data-[variant=line]/tabs-list:data-[state=active]:shadow-[0_1px_3px_rgba(15,23,42,0.14)] dark:data-[state=active]:text-foreground"
          >
            {view.label}
            {view.isPrivate ? (
              <Lock className="size-3" aria-label={copy.privateView} />
            ) : null}
          </TabsTrigger>
        ))}
      </TabsList>
    </Tabs>
  );
}

export function AddViewMenu({
  onAddView,
  copy,
  tablet = false,
}: {
  onAddView: (type: string) => void;
  copy: BoardHeaderCopy;
  /** The 44 px button of the tablet toolbar instead of the tab strip's 40 px one. */
  tablet?: boolean;
}) {
  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <Button
          variant="ghost"
          size="icon-sm"
          aria-label={copy.addView}
          className={
            tablet
              ? "size-11 rounded-[10px] text-muted-foreground hover:text-foreground"
              : "-mb-px h-9 w-10 rounded-none text-muted-foreground hover:text-foreground"
          }
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

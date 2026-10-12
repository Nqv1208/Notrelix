import * as React from "react";
import {
  FileText,
  Home,
  Inbox,
  Keyboard,
  Library,
  Moon,
  Plus,
  Search,
  Settings,
  SquareKanban,
  Sun,
  UserRoundCheck,
  Users,
  UsersRound,
  type LucideIcon,
} from "lucide-react";
import {
  Command,
  CommandEmpty,
  CommandGroup,
  CommandInput,
  CommandItem,
  CommandList,
  CommandShortcut,
  Dialog,
  DialogContent,
  DialogDescription,
  DialogTitle,
} from "@notrelix/ui-web";
import {
  normalizeSearchText,
  type WorkspaceDestination,
} from "../../core/model/navigation";
import { IdentityMark } from "./shell-link";
import {
  formatCopy,
  type ShellAppearance,
  type ShellPaletteMember,
  type ShellPaletteResource,
  type ShellPaletteSelection,
} from "./shell-models";
import type { WorkspaceShellCopy } from "./workspace-shell.copy";

export interface WorkspaceCommandPaletteSurfaceProps {
  readonly copy: WorkspaceShellCopy;
  readonly open: boolean;
  readonly onOpenChange: (open: boolean) => void;
  /** Authorized Boards and Docs already loaded by the host. */
  readonly boards: readonly ShellPaletteResource[];
  readonly docs: readonly ShellPaletteResource[];
  readonly members: readonly ShellPaletteMember[];
  readonly appearance: ShellAppearance;
  /** Shown only when the member may create workspaces. */
  readonly canCreateWorkspace: boolean;
  readonly onSelect: (selection: ShellPaletteSelection) => void;
  /** Initial query (stories, tests). */
  readonly defaultQuery?: string;
}

interface NavigationCommand {
  readonly id: string;
  readonly label: string;
  readonly icon: LucideIcon;
  readonly destination: WorkspaceDestination;
  readonly shortcut?: string;
}

const ITEM = "gap-2 data-selected:bg-state-hover";

/**
 * Substring match on the item's labels (keywords), ignoring case and
 * diacritics. cmdk's default fuzzy match also matches internal ids and
 * scattered letters, which surfaces unrelated results.
 */
function matchLabels(
  _value: string,
  search: string,
  keywords?: string[],
): number {
  const query = normalizeSearchText(search.trim());
  if (!query) return 1;
  return normalizeSearchText((keywords ?? []).join(" ")).includes(query)
    ? 1
    : 0;
}

/**
 * Global command palette (WX-SHELL-003) on `Dialog` + cmdk. Radix provides
 * the dialog role, focus trap and Escape; cmdk provides the combobox/listbox
 * semantics and keyboard selection. Results navigate to the real resource.
 */
export function WorkspaceCommandPaletteSurface({
  copy,
  open,
  onOpenChange,
  boards,
  docs,
  members,
  appearance,
  canCreateWorkspace,
  onSelect,
  defaultQuery = "",
}: WorkspaceCommandPaletteSurfaceProps) {
  const [query, setQuery] = React.useState(defaultQuery);

  React.useEffect(() => {
    if (!open) setQuery(defaultQuery);
  }, [open, defaultQuery]);

  const select = (selection: ShellPaletteSelection) => {
    onOpenChange(false);
    onSelect(selection);
  };

  const navigationCommands: NavigationCommand[] = [
    {
      id: "home",
      label: copy.nav.home,
      icon: Home,
      destination: { section: "home" },
      shortcut: "G H",
    },
    {
      id: "my-work",
      label: copy.nav.myWork,
      icon: UserRoundCheck,
      destination: { section: "my-work" },
      shortcut: "G M",
    },
    {
      id: "inbox",
      label: copy.nav.inbox,
      icon: Inbox,
      destination: { section: "inbox" },
      shortcut: "G I",
    },
    {
      id: "search",
      label: copy.nav.search,
      icon: Search,
      destination: { section: "search" },
    },
    {
      id: "library",
      // The page name ("Boards & Docs"), not the sidebar's short "Docs".
      label: copy.sections.library,
      icon: Library,
      destination: { section: "library" },
    },
    {
      id: "members",
      label: copy.nav.members,
      icon: Users,
      destination: { section: "members" },
    },
    {
      id: "teams",
      label: copy.nav.teams,
      icon: UsersRound,
      destination: { section: "teams" },
    },
    {
      id: "settings",
      label: copy.nav.settings,
      icon: Settings,
      destination: { section: "settings" },
    },
  ];

  const trimmed = query.trim();

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent
        showCloseButton={false}
        className="top-[12vh] translate-y-0 gap-0 overflow-hidden p-0 sm:max-w-xl"
      >
        <DialogTitle className="sr-only">{copy.palette.title}</DialogTitle>
        <DialogDescription className="sr-only">
          {copy.palette.description}
        </DialogDescription>
        <Command
          label={copy.palette.title}
          filter={matchLabels}
          className="rounded-none! bg-popover"
        >
          <CommandInput
            value={query}
            onValueChange={setQuery}
            placeholder={copy.palette.placeholder}
            aria-label={copy.palette.placeholder}
          />
          <CommandList className="max-h-[min(60vh,26rem)] p-1">
            <CommandEmpty className="py-8 text-center text-body text-muted-foreground">
              {formatCopy(copy.palette.empty, { query: trimmed })}
            </CommandEmpty>

            {trimmed ? (
              // Always offered, but not counted as a match: the empty message
              // still tells the member nothing loaded matched.
              <CommandGroup forceMount heading={copy.nav.search}>
                <CommandItem
                  forceMount
                  value="__search-all__"
                  onSelect={() => select({ type: "search", query: trimmed })}
                  className={ITEM}
                >
                  <Search aria-hidden="true" className="text-icon" />
                  <span className="truncate">
                    {formatCopy(copy.palette.searchAll, { query: trimmed })}
                  </span>
                </CommandItem>
              </CommandGroup>
            ) : null}

            <CommandGroup heading={copy.palette.groups.navigation}>
              {navigationCommands.map((command) => (
                <CommandItem
                  key={command.id}
                  value={`nav:${command.id}`}
                  keywords={[command.label]}
                  onSelect={() =>
                    select({
                      type: "navigate",
                      destination: command.destination,
                    })
                  }
                  className={ITEM}
                >
                  <command.icon aria-hidden="true" className="text-icon" />
                  <span>{command.label}</span>
                  {command.shortcut ? (
                    <CommandShortcut>{command.shortcut}</CommandShortcut>
                  ) : null}
                </CommandItem>
              ))}
            </CommandGroup>

            {boards.length > 0 ? (
              <CommandGroup heading={copy.palette.groups.boards}>
                {boards.map((board) => (
                  <CommandItem
                    key={board.id}
                    value={`board:${board.id}`}
                    keywords={[board.name]}
                    onSelect={() =>
                      select({
                        type: "navigate",
                        destination: { section: "board", boardId: board.id },
                      })
                    }
                    className={ITEM}
                  >
                    <SquareKanban aria-hidden="true" className="text-icon" />
                    <span className="truncate">{board.name}</span>
                  </CommandItem>
                ))}
              </CommandGroup>
            ) : null}

            {docs.length > 0 ? (
              <CommandGroup heading={copy.palette.groups.docs}>
                {docs.map((doc) => (
                  <CommandItem
                    key={doc.id}
                    value={`doc:${doc.id}`}
                    keywords={[doc.name]}
                    onSelect={() =>
                      select({
                        type: "navigate",
                        destination: { section: "doc", docId: doc.id },
                      })
                    }
                    className={ITEM}
                  >
                    <FileText aria-hidden="true" className="text-icon" />
                    <span className="truncate">{doc.name}</span>
                  </CommandItem>
                ))}
              </CommandGroup>
            ) : null}

            {members.length > 0 ? (
              <CommandGroup heading={copy.palette.groups.members}>
                {members.map((member) => (
                  <CommandItem
                    key={member.id}
                    value={`member:${member.id}`}
                    keywords={[member.name]}
                    onSelect={() =>
                      select({
                        type: "navigate",
                        destination: { section: "members" },
                      })
                    }
                    className={ITEM}
                  >
                    <IdentityMark
                      name={member.name}
                      tone={member.identity}
                      size="sm"
                      shape="round"
                    />
                    <span className="truncate">{member.name}</span>
                    <span className="ml-auto text-metadata text-muted-foreground">
                      {member.roleLabel}
                    </span>
                  </CommandItem>
                ))}
              </CommandGroup>
            ) : null}

            <CommandGroup heading={copy.palette.groups.actions}>
              <CommandItem
                value="action:toggle-theme"
                keywords={[
                  appearance === "dark"
                    ? copy.palette.toggleThemeLight
                    : copy.palette.toggleThemeDark,
                ]}
                onSelect={() =>
                  select({ type: "action", action: "toggle-theme" })
                }
                className={ITEM}
              >
                {appearance === "dark" ? (
                  <Sun aria-hidden="true" className="text-icon" />
                ) : (
                  <Moon aria-hidden="true" className="text-icon" />
                )}
                <span>
                  {appearance === "dark"
                    ? copy.palette.toggleThemeLight
                    : copy.palette.toggleThemeDark}
                </span>
              </CommandItem>
              <CommandItem
                value="action:show-shortcuts"
                keywords={[copy.palette.shortcuts]}
                onSelect={() =>
                  select({ type: "action", action: "show-shortcuts" })
                }
                className={ITEM}
              >
                <Keyboard aria-hidden="true" className="text-icon" />
                <span>{copy.palette.shortcuts}</span>
                <CommandShortcut>?</CommandShortcut>
              </CommandItem>
              {canCreateWorkspace ? (
                <CommandItem
                  value="action:create-workspace"
                  keywords={[copy.palette.createWorkspace]}
                  onSelect={() =>
                    select({ type: "action", action: "create-workspace" })
                  }
                  className={ITEM}
                >
                  <Plus aria-hidden="true" className="text-icon" />
                  <span>{copy.palette.createWorkspace}</span>
                </CommandItem>
              ) : null}
            </CommandGroup>
          </CommandList>
        </Command>
      </DialogContent>
    </Dialog>
  );
}

import * as React from "react";
import { Moon, PanelLeft, Search, Sun } from "lucide-react";
import {
  Breadcrumb,
  BreadcrumbItem,
  BreadcrumbLink,
  BreadcrumbList,
  BreadcrumbPage,
  BreadcrumbSeparator,
  Button,
  Kbd,
  Skeleton,
  useSidebar,
} from "@notrelix/ui-web";
import type { WorkspaceBreadcrumbItem } from "../../core/model/breadcrumb";
import { ShellLink } from "./shell-link";
import type { ShellAppearance, ShellNavigation } from "./shell-models";
import type { WorkspaceShellCopy } from "./workspace-shell.copy";

export interface WorkspaceTopbarSurfaceProps {
  readonly copy: WorkspaceShellCopy;
  readonly navigation: ShellNavigation;
  /** Built from the product hierarchy (`buildWorkspaceBreadcrumb`). */
  readonly breadcrumb: readonly WorkspaceBreadcrumbItem[];
  readonly appearance: ShellAppearance;
  /** Platform label of the palette chord, e.g. "⌘K" or "Ctrl K". */
  readonly commandShortcut: string;
  readonly onOpenCommand: () => void;
  readonly onToggleTheme: () => void;
  /** Host-composed notifications bell (`@notrelix/features-notifications`). */
  readonly notifications?: React.ReactNode;
}

/** Workspace topbar (WX-SHELL-002): breadcrumb, command trigger, bell, theme. */
export function WorkspaceTopbarSurface({
  copy,
  navigation,
  breadcrumb,
  appearance,
  commandShortcut,
  onOpenCommand,
  onToggleTheme,
  notifications,
}: WorkspaceTopbarSurfaceProps) {
  const { toggleSidebar } = useSidebar();
  const themeLabel =
    appearance === "dark" ? copy.topbar.themeToLight : copy.topbar.themeToDark;

  return (
    <header className="flex h-12 shrink-0 items-center gap-2 border-b border-border bg-surface-canvas px-3 md:px-4">
      <Button
        variant="ghost"
        size="icon-sm"
        aria-label={copy.topbar.openNavigation}
        onClick={toggleSidebar}
        className="text-icon"
      >
        <PanelLeft aria-hidden="true" />
      </Button>

      <Breadcrumb aria-label={copy.breadcrumb.label} className="min-w-0 flex-1">
        <BreadcrumbList className="flex-nowrap gap-1.5 text-label font-normal sm:gap-1.5">
          {breadcrumb.map((item, index) => {
            const last = index === breadcrumb.length - 1;
            return (
              <React.Fragment key={item.id}>
                {index > 0 ? <BreadcrumbSeparator /> : null}
                <BreadcrumbItem
                  className={
                    last ? "min-w-0" : "hidden min-w-0 shrink-0 sm:inline-flex"
                  }
                >
                  {item.pending ? (
                    <>
                      <Skeleton className="h-3.5 w-24" aria-hidden="true" />
                      <span className="sr-only">{item.label}</span>
                    </>
                  ) : last || !item.destination ? (
                    <BreadcrumbPage className="truncate font-medium">
                      {item.label}
                    </BreadcrumbPage>
                  ) : (
                    <BreadcrumbLink asChild>
                      <ShellLink
                        destination={item.destination}
                        navigation={navigation}
                        className="max-w-48 truncate rounded-sm focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-focus-ring"
                      >
                        {item.label}
                      </ShellLink>
                    </BreadcrumbLink>
                  )}
                </BreadcrumbItem>
              </React.Fragment>
            );
          })}
        </BreadcrumbList>
      </Breadcrumb>

      <Button
        variant="outline"
        size="sm"
        onClick={onOpenCommand}
        aria-label={copy.topbar.commandLabel}
        aria-keyshortcuts="Meta+K Control+K"
        className="hidden w-56 justify-start gap-2 font-normal text-muted-foreground md:inline-flex"
      >
        <Search aria-hidden="true" />
        <span className="flex-1 truncate text-left">{copy.topbar.command}</span>
        <Kbd>{commandShortcut}</Kbd>
      </Button>
      <Button
        variant="ghost"
        size="icon-sm"
        onClick={onOpenCommand}
        aria-label={copy.topbar.commandLabel}
        className="text-icon md:hidden"
      >
        <Search aria-hidden="true" />
      </Button>

      {notifications}

      <Button
        variant="ghost"
        size="icon-sm"
        onClick={onToggleTheme}
        aria-label={themeLabel}
        title={themeLabel}
        className="text-icon"
      >
        {appearance === "dark" ? (
          <Sun aria-hidden="true" />
        ) : (
          <Moon aria-hidden="true" />
        )}
      </Button>
    </header>
  );
}

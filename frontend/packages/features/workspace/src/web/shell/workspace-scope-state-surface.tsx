import { CloudOff, ShieldX } from "lucide-react";
import {
  Button,
  Empty,
  EmptyContent,
  EmptyDescription,
  EmptyHeader,
  EmptyMedia,
  EmptyTitle,
  Skeleton,
} from "@notrelix/ui-web";
import { isPlainClick } from "./shell-models";
import type { WorkspaceShellCopy } from "./workspace-shell.copy";

export type WorkspaceScopeState = "loading" | "forbidden" | "error";

export interface WorkspaceScopeStateSurfaceProps {
  readonly copy: WorkspaceShellCopy;
  readonly state: WorkspaceScopeState;
  /** Re-runs the workspace load (error and forbidden states). */
  readonly onRetry: () => void;
  /** A way out of the scope: the account-level workspace list. */
  readonly exitHref: string;
  readonly onExit: () => void;
}

/**
 * Workspace scope states (WX-SHELL-004). Lost access or a failed load is an
 * explicit state with a retry and a way out — never a silent redirect.
 */
export function WorkspaceScopeStateSurface({
  copy,
  state,
  onRetry,
  exitHref,
  onExit,
}: WorkspaceScopeStateSurfaceProps) {
  if (state === "loading") {
    return (
      <div
        role="status"
        aria-label={copy.scope.loadingTitle}
        className="flex h-svh w-full bg-surface-canvas"
      >
        <div className="hidden w-64 shrink-0 flex-col gap-3 border-r border-sidebar-border bg-sidebar p-3 md:flex">
          <div className="flex items-center gap-2">
            <Skeleton className="size-8 rounded-md" />
            <Skeleton className="h-3.5 w-32" />
          </div>
          {["w-3/4", "w-2/3", "w-1/2", "w-3/5"].map((width) => (
            <Skeleton key={width} className={`h-3.5 ${width}`} />
          ))}
        </div>
        <div className="flex flex-1 flex-col">
          <div className="flex h-12 items-center border-b border-border px-4">
            <Skeleton className="h-3.5 w-40" />
          </div>
          <div className="flex flex-col gap-3 p-6">
            <Skeleton className="h-6 w-56" />
            <Skeleton className="h-4 w-full max-w-xl" />
            <Skeleton className="h-4 w-full max-w-md" />
          </div>
          <span className="sr-only">{copy.scope.loadingDescription}</span>
        </div>
      </div>
    );
  }

  const forbidden = state === "forbidden";
  const Icon = forbidden ? ShieldX : CloudOff;

  return (
    <main className="flex min-h-svh w-full items-center justify-center bg-surface-canvas p-4">
      <Empty className="max-w-md border-0">
        <EmptyHeader>
          <EmptyMedia variant="icon">
            <Icon aria-hidden="true" className="text-icon" />
          </EmptyMedia>
          <EmptyTitle>
            <h1 className="text-section-heading text-foreground">
              {forbidden ? copy.scope.forbiddenTitle : copy.scope.errorTitle}
            </h1>
          </EmptyTitle>
          <EmptyDescription className="text-body">
            {forbidden
              ? copy.scope.forbiddenDescription
              : copy.scope.errorDescription}
          </EmptyDescription>
        </EmptyHeader>
        <EmptyContent className="flex-row justify-center gap-2">
          <Button variant={forbidden ? "outline" : "default"} onClick={onRetry}>
            {copy.scope.retry}
          </Button>
          <Button variant={forbidden ? "default" : "outline"} asChild>
            <a
              href={exitHref}
              onClick={(event) => {
                if (!isPlainClick(event)) return;
                event.preventDefault();
                onExit();
              }}
            >
              {copy.scope.exit}
            </a>
          </Button>
        </EmptyContent>
      </Empty>
    </main>
  );
}

import { Archive, TriangleAlert } from "lucide-react";
import { Button } from "@notrelix/ui-web";
import type { WorkspaceShellCopy } from "./workspace-shell.copy";

/** What the Unarchive write last did; `idle` before it ran. */
export type ArchivedBannerState =
  "idle" | "pending" | "conflict" | "denied" | "failed";

export interface ArchivedWorkspaceBannerProps {
  readonly copy: WorkspaceShellCopy;
  /** Only the Owner holds the lifecycle action (GOV-013); everyone else reads the banner. */
  readonly canUnarchive: boolean;
  readonly state: ArchivedBannerState;
  readonly onUnarchive: () => void;
  /** Re-reads the workspace after a stale version. */
  readonly onReload: () => void;
}

/**
 * Every member sees the read-only banner of an archived workspace
 * (`Wsp-Var-Archived-Banner`); the Owner also gets Unarchive with its pending
 * and stale-version states. Write controls elsewhere stay inert because the
 * producer refuses the write with `workspace.archived`.
 */
export function ArchivedWorkspaceBanner({
  copy,
  canUnarchive,
  state,
  onUnarchive,
  onReload,
}: ArchivedWorkspaceBannerProps) {
  const text = copy.archived;
  if (state === "conflict") {
    return (
      <div
        role="alert"
        data-f="archived-banner"
        className="mx-4 mt-3 flex flex-wrap items-center gap-3 rounded-lg bg-danger-subtle px-3.5 py-2.5 text-compact text-danger-subtle-foreground"
      >
        <TriangleAlert aria-hidden="true" className="size-4 shrink-0" />
        <span className="min-w-0 flex-1">
          <b className="font-semibold">{text.conflictTitle}</b>{" "}
          {text.conflictText}
        </span>
        <Button size="sm" variant="outline" onClick={onReload}>
          {text.reload}
        </Button>
      </div>
    );
  }
  const pending = state === "pending";
  const problem =
    state === "denied"
      ? text.deniedText
      : state === "failed"
        ? text.failedText
        : null;
  return (
    <div
      role="status"
      data-f="archived-banner"
      className="mx-4 mt-3 flex flex-wrap items-center gap-3 rounded-lg bg-info-subtle px-3.5 py-2.5 text-compact text-info-subtle-foreground"
    >
      <Archive aria-hidden="true" className="size-4 shrink-0" />
      <span className="min-w-0 flex-1">
        <b className="font-semibold">{text.title}</b> {text.text}{" "}
        {canUnarchive ? text.ownerHint : text.memberHint}
        {problem ? (
          <span role="alert" className="mt-1 block font-semibold">
            {problem}
          </span>
        ) : null}
      </span>
      {canUnarchive ? (
        <Button
          size="sm"
          disabled={pending}
          aria-busy={pending || undefined}
          onClick={onUnarchive}
        >
          {pending ? text.unarchiving : text.unarchive}
        </Button>
      ) : null}
    </div>
  );
}

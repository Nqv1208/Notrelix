import { Loader2, UserPlus } from "lucide-react";
import {
  Button,
  Popover,
  PopoverContent,
  PopoverTrigger,
  cn,
} from "@notrelix/ui-web";
import type { PendingInvitation } from "../../core/types/workspace";
import {
  RAIL_ACTION_CLASS,
  RailLabel,
  UnreadBadge,
} from "../shell/notrelix-system-rail-surface";
import { formatCopy } from "../shell/shell-models";
import { en, type PendingInvitationsCopy } from "./pending-invitations.copy";

export interface PendingInvitationsMenuSurfaceProps {
  invitations: readonly PendingInvitation[];
  /** `loading`: the first read is on its way; `error`: it failed (the retry button answers). */
  status?: "idle" | "loading" | "error";
  /** The invitation being answered: its buttons wait. */
  busyId?: string | null;
  /** What the last answer said when it was refused, shown above the list. */
  feedback?: string | null;
  copy?: PendingInvitationsCopy;
  /** Renders an invitation's expiry; defaults to the browser's short date. */
  formatDate?: (iso: string) => string;
  onAccept?: (invitation: PendingInvitation) => void;
  onDecline?: (invitation: PendingInvitation) => void;
  onRetry?: () => void;
  onOpenChange?: (open: boolean) => void;
  /** Controls the flyout; omit it and the surface keeps its own open state. */
  open?: boolean;
}

function defaultDate(iso: string): string {
  const date = new Date(iso);
  return Number.isNaN(date.getTime()) ? iso : date.toLocaleDateString();
}

/**
 * The signed-in person's pending invitations: an entry in the system rail (next to "Add") that opens a
 * flyout of the invitations addressed to them, each with Accept and Decline. Loading, empty and error
 * have their own state; a refused answer is told above the list; the buttons of the invitation being
 * answered wait. The flyout is portaled and marked `data-rail-flyout`, so an overlay rail stays open
 * while focus is inside it, and Escape closes only the flyout.
 */
export function PendingInvitationsMenuSurface({
  invitations,
  status = "idle",
  busyId = null,
  feedback = null,
  copy = en,
  formatDate = defaultDate,
  onAccept,
  onDecline,
  onRetry,
  onOpenChange,
  open,
}: PendingInvitationsMenuSurfaceProps) {
  const count = invitations.length;
  const hasInvitations = count > 0;
  const waiting = formatCopy(
    count === 1 ? copy.waiting_one : copy.waiting_other,
    { count },
  );

  return (
    <Popover open={open} onOpenChange={onOpenChange}>
      <PopoverTrigger asChild>
        <button
          type="button"
          aria-label={copy.trigger}
          aria-describedby={
            hasInvitations ? "pending-invitations-count" : undefined
          }
          className={RAIL_ACTION_CLASS}
        >
          <span
            aria-hidden="true"
            className={cn(
              "flex size-10 items-center justify-center rounded-[11px] border-[1.5px] border-rail-outline text-rail-foreground",
              hasInvitations && "border-solid text-rail-active-foreground",
            )}
          >
            <UserPlus />
          </span>
          <RailLabel>{copy.label}</RailLabel>
          <UnreadBadge count={hasInvitations ? count : null} />
          {hasInvitations ? (
            <span id="pending-invitations-count" className="sr-only">
              {waiting}
            </span>
          ) : null}
        </button>
      </PopoverTrigger>

      <PopoverContent
        side="right"
        align="start"
        sideOffset={10}
        data-rail-flyout=""
        aria-label={copy.title}
        // Escape closes this flyout, not the overlay rail it opened from.
        onKeyDown={(event) => {
          if (event.key === "Escape") event.stopPropagation();
        }}
        className="w-80 gap-0 overflow-hidden rounded-[12px] border border-line p-0 shadow-modal ring-0"
      >
        <div className="flex items-center justify-between border-b border-line px-4 py-3">
          <h2 className="m-0 text-body font-semibold text-foreground">
            {copy.title}
            {hasInvitations ? (
              <span className="ml-1.5 font-normal text-muted-foreground">
                {count}
              </span>
            ) : null}
          </h2>
        </div>

        <div className="max-h-96 overflow-y-auto">
          {feedback ? (
            <p
              role="alert"
              className="m-0 border-b border-line bg-muted px-4 py-2.5 text-crumb text-danger-text"
            >
              {feedback}
            </p>
          ) : null}

          {status === "loading" && !hasInvitations ? (
            <div
              role="status"
              className="flex items-center justify-center gap-2 px-4 py-8 text-crumb text-muted-foreground"
            >
              <Loader2 aria-hidden="true" className="size-4 animate-spin" />
              {copy.loading}
            </div>
          ) : status === "error" && !hasInvitations ? (
            <div className="flex flex-col items-center gap-3 px-4 py-8 text-center">
              <p role="alert" className="m-0 text-crumb text-muted-foreground">
                {copy.loadError}
              </p>
              <Button size="sm" variant="outline" onClick={onRetry}>
                {copy.retry}
              </Button>
            </div>
          ) : !hasInvitations ? (
            <div className="flex flex-col items-center gap-1 px-4 py-8 text-center">
              <p className="m-0 text-nav font-semibold text-foreground">
                {copy.emptyTitle}
              </p>
              <p className="m-0 max-w-[220px] text-crumb text-muted-foreground">
                {copy.emptyText}
              </p>
            </div>
          ) : (
            <ul className="m-0 list-none divide-y divide-line p-0">
              {invitations.map((invitation) => {
                const busy = busyId === invitation.id;
                const role = copy.roles[invitation.role];
                return (
                  <li
                    key={invitation.id}
                    aria-busy={busy || undefined}
                    className="flex flex-col gap-2.5 px-4 py-3.5"
                  >
                    <div className="min-w-0">
                      <b className="block truncate text-nav font-semibold text-foreground">
                        {invitation.workspaceName}
                      </b>
                      <span className="block text-crumb text-muted-foreground">
                        {invitation.inviterName
                          ? formatCopy(copy.invitedBy, {
                              inviter: invitation.inviterName,
                              role,
                            })
                          : formatCopy(copy.invited, { role })}
                      </span>
                      <span className="block text-xs text-meta-foreground">
                        {formatCopy(copy.expires, {
                          date: formatDate(invitation.expiresAt),
                        })}
                      </span>
                    </div>
                    <div className="flex items-center gap-2">
                      <Button
                        size="sm"
                        disabled={busyId !== null}
                        aria-label={formatCopy(copy.acceptLabel, {
                          workspace: invitation.workspaceName,
                        })}
                        onClick={() => onAccept?.(invitation)}
                      >
                        {busy ? (
                          <Loader2
                            aria-hidden="true"
                            className="size-3.5 animate-spin"
                          />
                        ) : null}
                        {busy ? copy.working : copy.accept}
                      </Button>
                      <Button
                        size="sm"
                        variant="outline"
                        disabled={busyId !== null}
                        aria-label={formatCopy(copy.declineLabel, {
                          workspace: invitation.workspaceName,
                        })}
                        onClick={() => onDecline?.(invitation)}
                      >
                        {copy.decline}
                      </Button>
                    </div>
                  </li>
                );
              })}
            </ul>
          )}
        </div>
      </PopoverContent>
    </Popover>
  );
}

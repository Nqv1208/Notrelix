import { useState } from "react";
import {
  Briefcase,
  Calendar,
  Check,
  Loader2,
  User,
  UserPlus,
  X,
} from "lucide-react";
import {
  Button,
  Popover,
  PopoverContent,
  PopoverTrigger,
  cn,
} from "@notrelix/ui-web";

import type { WorkspaceInvitation } from "../../core/types/workspace";

export interface PendingInvitationsMenuSurfaceProps {
  invitations: readonly WorkspaceInvitation[];
  status?: "idle" | "loading";
  open?: boolean;
  onOpenChange?: (open: boolean) => void;
  acceptingInvitationId?: string | null;
  acceptDisabled?: boolean;
  onAccept?: (invitation: WorkspaceInvitation) => void;
  onDismiss?: () => void;
}

export function PendingInvitationsMenuSurface({
  invitations,
  status = "idle",
  open,
  onOpenChange,
  acceptingInvitationId,
  acceptDisabled = false,
  onAccept,
  onDismiss,
}: PendingInvitationsMenuSurfaceProps) {
  const [internalOpen, setInternalOpen] = useState(false);
  const resolvedOpen = open ?? internalOpen;
  const isLoading = status === "loading";
  const hasInvitations = invitations.length > 0;

  const setOpen = (nextOpen: boolean) => {
    if (open === undefined) {
      setInternalOpen(nextOpen);
    }
    onOpenChange?.(nextOpen);
  };

  const handleDismiss = () => {
    setOpen(false);
    onDismiss?.();
  };

  return (
    <Popover open={resolvedOpen} onOpenChange={setOpen}>
      <PopoverTrigger asChild>
        <button
          type="button"
          className={cn(
            "relative rounded-lg p-2 text-muted-foreground transition-all hover:bg-muted hover:text-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring",
            hasInvitations && "animate-pulse text-primary",
          )}
          aria-label="Pending workspace invitations"
        >
          <UserPlus className="size-[18px]" />
          {hasInvitations ? (
            <span className="absolute -right-0.5 -top-0.5 flex size-4 items-center justify-center rounded-full border border-card bg-emerald-500 text-[9px] font-bold text-white shadow-sm animate-bounce">
              {invitations.length}
            </span>
          ) : null}
        </button>
      </PopoverTrigger>

      <PopoverContent
        align="end"
        className="z-[100] w-80 overflow-hidden rounded-2xl border-border/40 bg-card/95 p-0 shadow-xl backdrop-blur-md"
      >
        <div className="flex items-center justify-between border-b border-border/40 bg-muted/30 px-4 py-3">
          <h4 className="flex items-center gap-2 text-sm font-semibold text-foreground">
            <UserPlus className="size-4 text-primary" />
            Invitations ({invitations.length})
          </h4>
          {isLoading ? (
            <Loader2 className="size-3.5 animate-spin text-muted-foreground" />
          ) : null}
        </div>

        <div className="max-h-80 divide-y divide-border/40 overflow-y-auto">
          {isLoading && invitations.length === 0 ? (
            <div className="flex flex-col items-center justify-center gap-2 py-8 text-center text-xs text-muted-foreground">
              <Loader2 className="size-6 animate-spin text-primary" />
              <span>Loading invitations...</span>
            </div>
          ) : !hasInvitations ? (
            <div className="flex flex-col items-center justify-center gap-2 px-4 py-8 text-center text-xs text-muted-foreground">
              <div className="rounded-full bg-muted p-2 text-muted-foreground/60">
                <UserPlus className="size-5" />
              </div>
              <p className="font-medium text-foreground/80">
                No pending invitations
              </p>
              <p className="max-w-[200px] text-[11px] leading-normal text-muted-foreground/80">
                When someone invites you to their workspace, it will appear
                here.
              </p>
            </div>
          ) : (
            invitations.map((invitation) => {
              const isAccepting = acceptingInvitationId === invitation.id;

              return (
                <div
                  key={invitation.id}
                  className="space-y-3 p-4 transition-colors hover:bg-muted/10"
                >
                  <div className="space-y-1.5">
                    <h5 className="text-sm font-bold leading-snug text-foreground">
                      Workspace: {invitation.workspaceName}
                    </h5>
                    <div className="space-y-1 text-xs text-muted-foreground">
                      <div className="flex items-center gap-1.5">
                        <User className="size-3.5 text-primary/75" />
                        <span>
                          Invited by:{" "}
                          <strong className="font-medium text-foreground/90">
                            {invitation.inviterName}
                          </strong>
                        </span>
                      </div>
                      <div className="flex items-center gap-1.5">
                        <Briefcase className="size-3.5 text-primary/75" />
                        <span>
                          Role:{" "}
                          <strong className="font-medium capitalize text-foreground/90">
                            {invitation.role}
                          </strong>
                        </span>
                      </div>
                      <div className="flex items-center gap-1.5">
                        <Calendar className="size-3.5 text-primary/75" />
                        <span>
                          Expires:{" "}
                          {new Date(invitation.expiresAt).toLocaleDateString()}
                        </span>
                      </div>
                    </div>
                  </div>

                  <div className="flex items-center gap-2 pt-1">
                    <Button
                      size="sm"
                      onClick={() => onAccept?.(invitation)}
                      disabled={
                        isAccepting || acceptDisabled || !invitation.token
                      }
                      className="h-8 flex-1 gap-1.5 rounded-lg bg-emerald-600 text-xs font-semibold text-white shadow-sm hover:bg-emerald-500"
                    >
                      {isAccepting ? (
                        <Loader2 className="size-3 animate-spin" />
                      ) : (
                        <Check className="size-3" />
                      )}
                      Accept
                    </Button>
                    <Button
                      size="sm"
                      variant="ghost"
                      onClick={handleDismiss}
                      disabled={isAccepting || acceptDisabled}
                      className="h-8 w-8 rounded-lg p-0 text-muted-foreground hover:bg-muted/80 hover:text-foreground"
                      aria-label="Dismiss invitations"
                    >
                      <X className="size-3.5" />
                    </Button>
                  </div>
                </div>
              );
            })
          )}
        </div>
      </PopoverContent>
    </Popover>
  );
}

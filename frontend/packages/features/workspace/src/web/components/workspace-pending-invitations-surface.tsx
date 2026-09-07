import { useState } from "react";
import { UserPlus, Users } from "lucide-react";
import { Button, cn } from "@notrelix/ui-web";
import type { WorkspaceInvitation } from "../../core/types/workspace";

export interface PendingInvitationsMenuSurfaceProps {
  invitations: readonly WorkspaceInvitation[];
  status?: "idle" | "loading";
  onAccept?: (invitation: WorkspaceInvitation) => void;
  onDismiss?: () => void;
}

export function PendingInvitationsMenuSurface({
  invitations,
  status = "idle",
  onAccept,
  onDismiss,
}: PendingInvitationsMenuSurfaceProps) {
  const [open, setOpen] = useState(false);
  const hasInvitations = invitations.length > 0;

  return (
    <div className="relative">
      <button
        type="button"
        aria-label="Pending workspace invitations"
        onClick={() => setOpen((value) => !value)}
        className={cn(
          "relative rounded-lg p-2 text-muted-foreground transition-all hover:bg-muted hover:text-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring",
          hasInvitations && "text-primary animate-pulse",
        )}
      >
        <UserPlus className="size-[18px]" />
        {hasInvitations ? (
          <span className="absolute -right-0.5 -top-0.5 flex size-4 items-center justify-center rounded-full border border-card bg-emerald-500 text-[9px] font-bold text-white shadow-sm animate-bounce">
            {invitations.length}
          </span>
        ) : null}
      </button>
      {open ? (
        <div className="absolute right-0 top-10 z-20 w-80 overflow-hidden rounded-2xl border border-border/40 bg-card/95 p-0 shadow-xl backdrop-blur-md">
          <div className="flex items-center justify-between border-b border-border/40 px-4 py-3 bg-muted/30">
            <h4 className="text-sm font-semibold text-foreground flex items-center gap-2">
              <UserPlus className="size-4 text-primary" />
              Invitations ({invitations.length})
            </h4>
          </div>
          <div className="max-h-80 overflow-y-auto divide-y divide-border/40">
            {status === "loading" ? (
              <div className="flex flex-col items-center justify-center py-8 text-center text-xs text-muted-foreground gap-2">
                <span>Loading invitations...</span>
              </div>
            ) : !hasInvitations ? (
              <div className="flex flex-col items-center justify-center py-8 px-4 text-center text-xs text-muted-foreground gap-2">
                <div className="rounded-full bg-muted p-2 text-muted-foreground/60">
                  <UserPlus className="size-5" />
                </div>
                <p className="font-medium text-foreground/80">
                  No pending invitations
                </p>
              </div>
            ) : (
              invitations.map((invite) => (
                <div
                  key={invite.id}
                  className="space-y-3 p-4 transition-colors hover:bg-muted/10"
                >
                  <div className="space-y-1.5">
                    <h5 className="text-sm font-bold text-foreground leading-snug">
                      Workspace: {invite.workspaceName}
                    </h5>
                    <div className="space-y-1 text-xs text-muted-foreground">
                      <div className="flex items-center gap-1.5">
                        <UserPlus className="size-3.5 text-primary/75" />
                        <span>
                          Invited by:{" "}
                          <strong className="text-foreground/90 font-medium">
                            {invite.inviterName}
                          </strong>
                        </span>
                      </div>
                      <div className="flex items-center gap-1.5">
                        <Users className="size-3.5 text-primary/75" />
                        <span>
                          Role:{" "}
                          <strong className="text-foreground/90 font-medium capitalize">
                            {invite.role}
                          </strong>
                        </span>
                      </div>
                    </div>
                  </div>
                  <div className="flex items-center gap-2 pt-1">
                    <Button
                      size="sm"
                      onClick={() => onAccept?.(invite)}
                      className="flex-1 h-8 rounded-lg text-xs font-semibold gap-1.5 bg-emerald-600 hover:bg-emerald-500 text-white shadow-sm"
                    >
                      Accept
                    </Button>
                    <Button
                      size="sm"
                      variant="ghost"
                      onClick={onDismiss}
                      className="h-8 w-8 p-0 rounded-lg text-muted-foreground hover:text-foreground hover:bg-muted/80"
                    >
                      ✕
                    </Button>
                  </div>
                </div>
              ))
            )}
          </div>
        </div>
      ) : null}
    </div>
  );
}
import { useState, useMemo } from "react";
import { useNavigate } from "@tanstack/react-router";
import {
  UserPlus,
  Loader2,
  Check,
  X,
  Calendar,
  User,
  Briefcase,
} from "lucide-react";
import {
  Button,
  Popover,
  PopoverContent,
  PopoverTrigger,
} from "@notrelix/ui-web";
import { createUsePendingInvitations } from "../query/hooks/use-pending-invitations";
import { createUseAcceptInvitation } from "../hooks/mutations/use-accept-invitation";
import type {
  PendingWorkspaceInvitation,
  WorkspaceApiClient,
  InvitationsEndpoints,
} from "../../core";
import { cn } from "@notrelix/ui-web";

interface PendingInvitationsMenuProps {
  api: WorkspaceApiClient;
  endpoints: InvitationsEndpoints;
}

export function PendingInvitationsMenu({
  api,
  endpoints,
}: PendingInvitationsMenuProps) {
  const navigate = useNavigate();
  const [open, setOpen] = useState(false);

  const usePendingInvitations = useMemo(
    () => createUsePendingInvitations({ api, endpoints }),
    [api, endpoints],
  );

  const useAcceptInvitation = useMemo(
    () => createUseAcceptInvitation({ api, endpoints }),
    [api, endpoints],
  );

  const { data: invitations, isLoading, refetch } = usePendingInvitations();
  const acceptMutation = useAcceptInvitation();
  const [acceptingId, setAcceptingId] = useState<string | null>(null);

  const hasInvitations = invitations && invitations.length > 0;

  const handleAccept = (invitationId: string) => {
    setAcceptingId(invitationId);
    acceptMutation.mutate(invitationId, {
      onSuccess: () => {
        setOpen(false);
        setAcceptingId(null);
        refetch();
        navigate({ to: "/home" });
      },
      onError: () => {
        setAcceptingId(null);
      },
    });
  };

  return (
    <Popover open={open} onOpenChange={setOpen}>
      <PopoverTrigger asChild>
        <button
          type="button"
          className={cn(
            "relative rounded-lg p-2 text-muted-foreground transition-all hover:bg-muted hover:text-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring",
            hasInvitations && "text-primary animate-pulse",
          )}
          aria-label="Pending workspace invitations"
        >
          <UserPlus className="size-[18px]" />
          {hasInvitations && (
            <span className="absolute -right-0.5 -top-0.5 flex size-4 items-center justify-center rounded-full border border-card bg-emerald-500 text-[9px] font-bold text-white shadow-sm animate-bounce">
              {invitations.length}
            </span>
          )}
        </button>
      </PopoverTrigger>

      <PopoverContent
        align="end"
        className="w-80 p-0 border-border/40 bg-card/95 shadow-xl backdrop-blur-md rounded-2xl overflow-hidden z-[100]"
      >
        <div className="flex items-center justify-between border-b border-border/40 px-4 py-3 bg-muted/30">
          <h4 className="text-sm font-semibold text-foreground flex items-center gap-2">
            <UserPlus className="size-4 text-primary" />
            Invitations ({invitations?.length || 0})
          </h4>
          {isLoading && (
            <Loader2 className="size-3.5 animate-spin text-muted-foreground" />
          )}
        </div>

        <div className="max-h-80 overflow-y-auto divide-y divide-border/40">
          {isLoading && !invitations ? (
            <div className="flex flex-col items-center justify-center py-8 text-center text-xs text-muted-foreground gap-2">
              <Loader2 className="size-6 animate-spin text-primary" />
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
              <p className="text-[11px] leading-normal text-muted-foreground/80 max-w-[200px]">
                When someone invites you to their workspace, it will appear
                here.
              </p>
            </div>
          ) : (
            invitations.map((invite: PendingWorkspaceInvitation) => {
              const isAccepting = acceptingId === invite.id;
              return (
                <div
                  key={invite.id}
                  className="p-4 hover:bg-muted/10 transition-colors space-y-3"
                >
                  <div className="space-y-1.5">
                    <h5 className="text-sm font-bold text-foreground leading-snug">
                      Workspace: {invite.workspaceName}
                    </h5>
                    <div className="space-y-1 text-xs text-muted-foreground">
                      <div className="flex items-center gap-1.5">
                        <User className="size-3.5 text-primary/75" />
                        <span>
                          Invited by:{" "}
                          <strong className="text-foreground/90 font-medium">
                            {invite.inviterName}
                          </strong>
                        </span>
                      </div>
                      <div className="flex items-center gap-1.5">
                        <Briefcase className="size-3.5 text-primary/75" />
                        <span>
                          Role:{" "}
                          <strong className="text-foreground/90 font-medium capitalize">
                            {invite.role}
                          </strong>
                        </span>
                      </div>
                      <div className="flex items-center gap-1.5">
                        <Calendar className="size-3.5 text-primary/75" />
                        <span>
                          Expires:{" "}
                          {new Date(invite.expiresAt).toLocaleDateString()}
                        </span>
                      </div>
                    </div>
                  </div>

                  <div className="flex items-center gap-2 pt-1">
                    <Button
                      size="sm"
                      onClick={() => handleAccept(invite.id)}
                      disabled={isAccepting || acceptMutation.isPending}
                      className="flex-1 h-8 rounded-lg text-xs font-semibold gap-1.5 bg-emerald-600 hover:bg-emerald-500 text-white shadow-sm"
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
                      onClick={() => setOpen(false)}
                      disabled={isAccepting || acceptMutation.isPending}
                      className="h-8 w-8 p-0 rounded-lg text-muted-foreground hover:text-foreground hover:bg-muted/80"
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

import { useCallback, useMemo, useState } from "react";
import { useLocale } from "@notrelix/i18n";
import { createUsePendingInvitations } from "../query/hooks/use-pending-invitations";
import { createUseAcceptInvitation } from "../hooks/mutations/use-accept-invitation";
import { createUseDeclineInvitation } from "../hooks/mutations/use-decline-invitation";
import type {
  AcceptedInvitation,
  InvitationsEndpoints,
  PendingInvitation,
  WorkspaceApiClient,
} from "../../core";
import { formatCopy } from "../shell/shell-models";
import { PendingInvitationsMenuSurface } from "./workspace-pending-invitations-surface";
import { usePendingInvitationsCopy } from "./use-pending-invitations-copy";

interface PendingInvitationsMenuProps {
  api: WorkspaceApiClient;
  endpoints: InvitationsEndpoints;
  /** The person accepted an invitation: the host leads them to the workspace. */
  onAccepted?: (accepted: AcceptedInvitation) => void;
}

/** What a refused answer carries: the HTTP status and the producer's reason. */
function refusalOf(error: unknown): { status?: number; message: string } {
  const e = error as { status?: number; message?: unknown } | null;
  return {
    status: e?.status,
    message: typeof e?.message === "string" ? e.message : "",
  };
}

/**
 * The signed-in person's own invitations, mounted in the system rail next to the workspaces: the list
 * reads `GET /invitations/pending`, Accept answers `POST /invitations/{id}/accept` (the rules of
 * accepting by link: seat, suspended or removed member) and Decline `POST /invitations/{id}/decline`.
 * An invitation that is gone answers 404: the list is read again and says so.
 */
export function PendingInvitationsMenu({
  api,
  endpoints,
  onAccepted,
}: PendingInvitationsMenuProps) {
  const copy = usePendingInvitationsCopy();
  const { locale } = useLocale();
  const [open, setOpen] = useState(false);
  const [busyId, setBusyId] = useState<string | null>(null);
  const [feedback, setFeedback] = useState<string | null>(null);

  const hooks = useMemo(
    () => ({
      usePending: createUsePendingInvitations({ api, endpoints }),
      useAccept: createUseAcceptInvitation({ api, endpoints }),
      useDecline: createUseDeclineInvitation({ api, endpoints }),
    }),
    [api, endpoints],
  );
  const pending = hooks.usePending();
  const accept = hooks.useAccept();
  const decline = hooks.useDecline();

  const formatDate = useMemo(() => {
    const format = new Intl.DateTimeFormat(locale, { dateStyle: "medium" });
    return (iso: string) => {
      const date = new Date(iso);
      return Number.isNaN(date.getTime()) ? iso : format.format(date);
    };
  }, [locale]);

  const refused = useCallback(
    (error: unknown, accepting: boolean) => {
      const { status, message } = refusalOf(error);
      if (status === 404) setFeedback(copy.errors.gone);
      else if (accepting && status === 400 && message)
        setFeedback(formatCopy(copy.errors.refused, { reason: message }));
      else setFeedback(copy.errors.failed);
    },
    [copy],
  );

  const onAccept = useCallback(
    (invitation: PendingInvitation) => {
      setFeedback(null);
      setBusyId(invitation.id);
      accept.mutate(invitation.id, {
        onSuccess: (accepted) => {
          setOpen(false);
          onAccepted?.(accepted);
        },
        onError: (error) => refused(error, true),
        onSettled: () => setBusyId(null),
      });
    },
    [accept, onAccepted, refused],
  );

  const onDecline = useCallback(
    (invitation: PendingInvitation) => {
      setFeedback(null);
      setBusyId(invitation.id);
      decline.mutate(invitation.id, {
        onError: (error) => refused(error, false),
        onSettled: () => setBusyId(null),
      });
    },
    [decline, refused],
  );

  return (
    <PendingInvitationsMenuSurface
      invitations={pending.data ?? []}
      status={
        pending.data
          ? "idle"
          : pending.isError
            ? "error"
            : pending.isPending
              ? "loading"
              : "idle"
      }
      busyId={busyId}
      feedback={feedback}
      copy={copy}
      formatDate={formatDate}
      open={open}
      onOpenChange={(next) => {
        setOpen(next);
        // Opening reads the list again: an invitation may have been answered elsewhere.
        if (next) {
          setFeedback(null);
          void pending.refetch();
        }
      }}
      onAccept={onAccept}
      onDecline={onDecline}
      onRetry={() => void pending.refetch()}
    />
  );
}

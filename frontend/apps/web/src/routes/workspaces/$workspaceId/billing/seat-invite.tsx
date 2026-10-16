import { useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { toast } from "sonner";
import {
  BillingSeatInviteDialog,
  useBillingCopy,
} from "@notrelix/features-workspace/web";
import type { WorkspaceRole } from "@notrelix/features-workspace/core";
import { useWorkspaceContext } from "@/providers/workspace-provider";
import { billingKeys, useBillingApi, useBillingLinks } from "./use-billing";

type InviteRole = Exclude<WorkspaceRole, "owner">;

/**
 * The invitation dialog that stops at the seats the account owns (Final-Billing
 * `het-cho`): opened on the Members page by `invite=seats`. Each address goes
 * through the Workspaces invitation producer; the seat count and the way to buy
 * more come from Billing.
 */
export function SeatInviteDialog({
  open,
  onClose,
  createInvitation,
}: {
  open: boolean;
  onClose: () => void;
  createInvitation: (input: {
    email: string;
    role: InviteRole;
  }) => Promise<unknown>;
}) {
  const { workspaceId, workspace } = useWorkspaceContext();
  const copy = useBillingCopy();
  const api = useBillingApi();
  const links = useBillingLinks();
  const [emails, setEmails] = useState<readonly string[]>([]);
  const [role, setRole] = useState<"member" | "admin">("member");
  const [sending, setSending] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const seats = useQuery({
    queryKey: billingKeys.seats(workspaceId),
    queryFn: () => api.seatSummary(workspaceId),
    enabled: open,
    retry: false,
  });

  const send = async () => {
    setSending(true);
    setError(null);
    const failed: string[] = [];
    for (const email of emails) {
      try {
        await createInvitation({
          email,
          role: role === "admin" ? "admin" : "member",
        });
      } catch {
        failed.push(email);
      }
    }
    setSending(false);
    if (failed.length > 0) {
      setEmails(failed);
      setError(copy.invite.sendFailed);
      return;
    }
    toast.success(copy.invite.sent);
    setEmails([]);
    onClose();
  };

  return (
    <BillingSeatInviteDialog
      copy={copy}
      open={open}
      workspaceName={workspace?.name ?? ""}
      seats={seats.data ?? null}
      emails={emails}
      role={role}
      sending={sending}
      error={error}
      buyLink={links.buy(10)}
      onEmails={setEmails}
      onRole={setRole}
      onSend={() => void send()}
      onClose={onClose}
    />
  );
}

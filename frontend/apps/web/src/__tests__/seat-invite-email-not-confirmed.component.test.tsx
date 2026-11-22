import { act } from "react";
import { createRoot, type Root } from "react-dom/client";
import { afterEach, describe, expect, it, vi } from "vitest";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";

(
  globalThis as typeof globalThis & { IS_REACT_ACT_ENVIRONMENT?: boolean }
).IS_REACT_ACT_ENVIRONMENT = true;

const h = vi.hoisted(() => ({
  errors: [] as {
    title: string;
    options?: {
      description?: string;
      action?: { label: string; onClick: () => void };
    };
  }[],
}));

vi.mock("sonner", () => ({
  toast: {
    success: vi.fn(),
    error: (title: string, options?: unknown) =>
      h.errors.push({ title, options: options as never }),
  },
}));
vi.mock("@/providers/workspace-provider", () => ({
  useWorkspaceContext: () => ({
    workspaceId: "ws-1",
    workspace: { name: "Acme" },
  }),
}));
vi.mock("../routes/workspaces/$workspaceId/billing/use-billing", () => ({
  billingKeys: { seats: (id: string) => ["seats", id] },
  useBillingApi: () => ({ seatSummary: async () => null }),
  useBillingLinks: () => ({ buy: () => "/buy" }),
}));
vi.mock("@notrelix/features-workspace/web", async (original) => ({
  ...(await original<object>()),
  useBillingCopy: () => ({
    invite: { sendFailed: "Some invitations were not sent.", sent: "Sent" },
  }),
  BillingSeatInviteDialog: (props: {
    error: string | null;
    onEmails: (emails: string[]) => void;
    onSend: () => void;
  }) => (
    <div>
      <button type="button" onClick={() => props.onEmails(["a@x.test"])}>
        add
      </button>
      <button type="button" onClick={props.onSend}>
        send
      </button>
      <p data-testid="error">{props.error}</p>
    </div>
  ),
}));

import { registerEmailConfirmationRecovery } from "../shell/workspace/email-confirmation-recovery";
import { SeatInviteDialog } from "../routes/workspaces/$workspaceId/billing/seat-invite";

let root: Root | null = null;
let host: HTMLDivElement | null = null;
afterEach(() => {
  act(() => root?.unmount());
  host?.remove();
  root = null;
  h.errors.length = 0;
});

async function sendRefusedWith(refusal: unknown) {
  host = document.createElement("div");
  document.body.append(host);
  root = createRoot(host);
  const client = new QueryClient();
  await act(async () => {
    root!.render(
      <QueryClientProvider client={client}>
        <SeatInviteDialog
          open
          onClose={() => undefined}
          createInvitation={async () => {
            throw refusal;
          }}
        />
      </QueryClientProvider>,
    );
  });
  const click = async (label: string) =>
    act(async () => {
      Array.from(host!.querySelectorAll("button"))
        .find((b) => b.textContent === label)
        ?.click();
    });
  await click("add");
  await click("send");
}

describe("seat invitation dialog when the inviter's email is not confirmed", () => {
  it("offers the confirmation message and a resend action instead of only 'not sent'", async () => {
    const resend = vi.fn();
    const unregister = registerEmailConfirmationRecovery({
      description: "Confirm your email first, then try again.",
      actionLabel: "Resend email",
      resend,
    });
    await sendRefusedWith({
      kind: "forbidden",
      status: 403,
      details: { errorCode: "auth.email_not_confirmed" },
    });
    expect(h.errors).toHaveLength(1);
    expect(h.errors[0]!.options?.description).toBe(
      "Confirm your email first, then try again.",
    );
    expect(h.errors[0]!.options?.action?.label).toBe("Resend email");
    h.errors[0]!.options!.action!.onClick();
    expect(resend).toHaveBeenCalledOnce();
    expect(host!.querySelector('[data-testid="error"]')?.textContent).toBe(
      "Some invitations were not sent.",
    );
    unregister();
  });

  it("keeps the plain 'not sent' message for any other refusal", async () => {
    await sendRefusedWith({ kind: "network" });
    expect(h.errors).toHaveLength(0);
    expect(host!.querySelector('[data-testid="error"]')?.textContent).toBe(
      "Some invitations were not sent.",
    );
  });
});

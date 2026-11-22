import type { ReactNode } from "react";
import { describe, expect, it, vi } from "vitest";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { I18nProvider } from "@notrelix/i18n";
import { fireEvent, renderPureUi, screen } from "@notrelix/testing";
import { authCatalog } from "../../copy/auth.catalog";
import { authCopy } from "../../copy/auth.copy";
import type { AuthFlowService } from "../../../core";
import { InviteFlow } from "../invite-flow";

const copy = authCopy("en");
const invitedEmail = "tuan@doitac.vn";
const preview = {
  state: "valid" as const,
  inviterName: "Nguyen Thu Ha",
  inviterInitials: "NH",
  orgName: "Minh An Group",
  boardTitle: null,
  level: "editor" as const,
  expiresOn: "2027-01-31",
  email: invitedEmail,
  name: null,
  hasAccount: true,
  workspaceRole: "member" as const,
};

function problem(status: number, details: Record<string, unknown> | null) {
  return { status, kind: status >= 500 ? "server" : "client", details };
}

function Providers({ children }: { children: ReactNode }) {
  const client = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  });
  return (
    <I18nProvider catalogs={[authCatalog]} initialLocale="en">
      <QueryClientProvider client={client}>{children}</QueryClientProvider>
    </I18nProvider>
  );
}

/** Opens the invitation as an account that already exists, presses Accept and answers it with `refusal`. */
async function acceptRefused(
  refusal: unknown,
  options: {
    signedInEmail?: string;
    previews?: (typeof preview)[];
    resend?: ReturnType<typeof vi.fn>;
  } = {},
) {
  const previews = options.previews ?? [preview];
  const getInvite = vi.fn();
  previews.forEach((p, i) =>
    i < previews.length - 1
      ? getInvite.mockResolvedValueOnce(p)
      : getInvite.mockResolvedValue(p),
  );
  const service = {
    getInvite,
    acceptInvite: vi.fn().mockRejectedValue(refusal),
    resendEmailVerification:
      options.resend ?? vi.fn().mockResolvedValue(undefined),
  };
  renderPureUi(
    <Providers>
      <InviteFlow
        service={service as unknown as AuthFlowService}
        navigation={{
          navigate: vi.fn(),
          startProviderSignIn: vi.fn(),
          startSsoSignIn: vi.fn(),
        }}
        token="tok"
        signedInEmail={options.signedInEmail ?? invitedEmail}
        onSwitchAccount={vi.fn()}
      />
    </Providers>,
  );
  fireEvent.click(
    await screen.findByRole("button", { name: copy.invite.acceptExisting }),
  );
  return service;
}

const alerts = () =>
  screen.queryAllByRole("alert").map((node) => node.textContent ?? "");

describe("invitation accept page: what each refusal says", () => {
  it("auth.email_not_confirmed asks to confirm the email and resends the link, never 'could not reach'", async () => {
    const resend = vi.fn().mockResolvedValue(undefined);
    await acceptRefused(
      problem(403, {
        errorCode: "auth.email_not_confirmed",
        detail:
          "Email must be confirmed before accepting workspace invitations.",
      }),
      { resend },
    );
    expect(await screen.findByText(copy.invite.emailNotConfirmed)).toBeTruthy();
    expect(alerts().join(" ")).not.toContain(copy.invite.unreachable);
    fireEvent.click(
      screen.getByRole("button", { name: copy.invite.resendEmail }),
    );
    expect(
      await screen.findByText(
        `We sent a new confirmation link to ${invitedEmail}.`,
      ),
    ).toBeTruthy();
    expect(resend).toHaveBeenCalledTimes(1);
    expect(resend).toHaveBeenCalledWith(invitedEmail);
    // One link per press: the button waits once the link is sent.
    expect(
      (
        screen.getByRole("button", {
          name: copy.invite.resendEmail,
        }) as HTMLButtonElement
      ).disabled,
    ).toBe(true);
  });

  it("says so when the resend itself fails", async () => {
    await acceptRefused(
      problem(403, { errorCode: "auth.email_not_confirmed" }),
      { resend: vi.fn().mockRejectedValue({ kind: "network" }) },
    );
    fireEvent.click(
      await screen.findByRole("button", { name: copy.invite.resendEmail }),
    );
    expect(await screen.findByText(copy.invite.resendFailed)).toBeTruthy();
  });

  it("a 400 for an invitation that expired in the meantime shows the expired page", async () => {
    await acceptRefused(
      problem(400, { detail: "This invitation is no longer valid." }),
      { previews: [preview, { ...preview, state: "expired" as never }] },
    );
    expect(await screen.findByText(copy.gone.expiredTitle)).toBeTruthy();
    expect(alerts().join(" ")).not.toContain(copy.invite.unreachable);
  });

  it("a 400 for an invitation already used shows the used page", async () => {
    await acceptRefused(
      problem(400, { detail: "This invitation is no longer valid." }),
      { previews: [preview, { ...preview, state: "used" as never }] },
    );
    expect(await screen.findByText(copy.gone.usedTitle)).toBeTruthy();
  });

  it("a 400 while signed in as another address names both addresses", async () => {
    await acceptRefused(
      problem(400, {
        detail: "This invitation belongs to a different email address.",
      }),
      { signedInEmail: "other@elsewhere.vn" },
    );
    const line = `You are signed in as other@elsewhere.vn; the invitation is for ${invitedEmail}.`;
    expect(await screen.findByText(line, { selector: "p" })).toBeTruthy();
    expect(alerts().join(" ")).not.toContain(copy.invite.unreachable);
  });

  it("a plan seat limit shows the producer's line", async () => {
    const detail =
      "The member limit of this account's plan has been reached. Ask an account owner to upgrade the plan.";
    await acceptRefused(problem(400, { detail }));
    expect(await screen.findByText(detail)).toBeTruthy();
    expect(alerts().join(" ")).not.toContain(copy.invite.unreachable);
  });

  it.each([
    [
      403,
      "workspace.guest_invites_off",
      "Guest invitations are turned off for this workspace.",
    ],
    [500, "server.error", "Something went wrong on our side."],
  ])(
    "a %i with a problem body shows its own line, not 'could not reach'",
    async (status, errorCode, detail) => {
      await acceptRefused(problem(status, { errorCode, detail }));
      expect(await screen.findByText(detail!)).toBeTruthy();
      expect(alerts().join(" ")).not.toContain(copy.invite.unreachable);
    },
  );

  it("a refusal with no body at all still says it could not be accepted, not 'could not reach'", async () => {
    await acceptRefused(problem(403, null));
    expect(await screen.findByText(copy.invite.refused)).toBeTruthy();
  });

  it("keeps 'could not reach' for a 503 with no problem body", async () => {
    await acceptRefused(problem(503, null));
    expect(await screen.findByText(copy.invite.unreachable)).toBeTruthy();
  });

  it("keeps 'could not reach' for a call that got no answer", async () => {
    await acceptRefused({ kind: "network" });
    expect(await screen.findByText(copy.invite.unreachable)).toBeTruthy();
  });
});

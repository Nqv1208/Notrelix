import type { ReactNode } from "react";
import { describe, expect, it, vi } from "vitest";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { I18nProvider } from "@notrelix/i18n";
import { fireEvent, renderPureUi, screen } from "@notrelix/testing";
import { authCatalog } from "../../copy/auth.catalog";
import { authCopy } from "../../copy/auth.copy";
import type { AuthFlowService } from "../../../core";
import { InviteFlow } from "../invite-flow";

const copy = authCopy("vi");
const preview = {
  state: "valid" as const,
  inviterName: "Nguyễn Thu Hà",
  inviterInitials: "NH",
  orgName: "Minh An Group",
  boardTitle: null,
  level: "editor" as const,
  expiresOn: "2027-01-31",
  email: "tuan@doitac.vn",
  name: null,
  hasAccount: false,
  workspaceRole: "member" as const,
};

function Providers({ children }: { children: ReactNode }) {
  const client = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  });
  return (
    <I18nProvider catalogs={[authCatalog]} initialLocale="vi">
      <QueryClientProvider client={client}>{children}</QueryClientProvider>
    </I18nProvider>
  );
}

function open(getInvite: ReturnType<typeof vi.fn>) {
  renderPureUi(
    <Providers>
      <InviteFlow
        service={{ getInvite } as unknown as AuthFlowService}
        navigation={{ navigate: vi.fn(), startProviderSignIn: vi.fn() }}
        token="tok"
      />
    </Providers>,
  );
}

describe("invite link: a failed preview read", () => {
  it.each([
    ["a server error", { status: 500, kind: "server" }],
    ["a network error", { kind: "network" }],
  ])(
    "after %s says it could not connect and Retry reads again",
    async (_name, failure) => {
      const getInvite = vi
        .fn()
        .mockRejectedValueOnce(failure)
        .mockResolvedValue(preview);
      open(getInvite);
      const alert = await screen.findByRole("alert");
      expect(alert.textContent).toContain(copy.invite.unreachable);
      expect(screen.queryByText(copy.gone.missingTitle)).toBeNull();
      fireEvent.click(screen.getByRole("button", { name: copy.invite.retry }));
      expect(await screen.findByLabelText(copy.invite.nameLabel)).toBeTruthy();
      expect(getInvite).toHaveBeenCalledTimes(2);
    },
  );

  it("keeps the not-found page for a 404", async () => {
    open(vi.fn().mockRejectedValue({ status: 404, kind: "not_found" }));
    expect(await screen.findByText(copy.gone.missingTitle)).toBeTruthy();
    expect(
      screen.queryByRole("button", { name: copy.invite.retry }),
    ).toBeNull();
  });
});

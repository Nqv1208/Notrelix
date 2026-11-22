import { describe, expect, it, vi } from "vitest";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { I18nProvider } from "@notrelix/i18n";
import { renderPureUi, screen, waitFor } from "@notrelix/testing";
import { authCatalog } from "../../copy/auth.catalog";
import { authCopy } from "../../copy/auth.copy";
import { authQueryKeys } from "../../../core/query/keys";
import type { AuthFlowService } from "../../../core";
import { VerifyEmailFlow } from "../verify-email-flow";

const copy = authCopy("en");

describe("confirming the email from the link", () => {
  it("reads the signed-in profile again so the confirm-your-email banner can go away", async () => {
    const client = new QueryClient({
      defaultOptions: { queries: { retry: false } },
    });
    const profile = vi.fn().mockResolvedValue({
      id: "u1",
      email: "owner@example.com",
      name: "Owner",
      avatarUrl: null,
      emailConfirmed: true,
    });
    client.setQueryDefaults(authQueryKeys.profile, { queryFn: profile });
    client.setQueryData(authQueryKeys.profile, {
      id: "u1",
      email: "owner@example.com",
      name: "Owner",
      avatarUrl: null,
      emailConfirmed: false,
    });
    const service = {
      confirmEmail: vi.fn().mockResolvedValue(undefined),
    } as unknown as AuthFlowService;

    renderPureUi(
      <I18nProvider catalogs={[authCatalog]} initialLocale="en">
        <QueryClientProvider client={client}>
          <VerifyEmailFlow
            service={service}
            navigation={{
              navigate: vi.fn(),
              startProviderSignIn: vi.fn(),
              startSsoSignIn: vi.fn(),
            }}
            token="refresh-token"
          />
        </QueryClientProvider>
      </I18nProvider>,
    );

    expect(await screen.findByText(copy.verifyEmail.doneTitle)).toBeTruthy();
    await waitFor(() => expect(profile).toHaveBeenCalledTimes(1));
    await waitFor(() =>
      expect(
        (
          client.getQueryData(authQueryKeys.profile) as {
            emailConfirmed?: boolean;
          }
        ).emailConfirmed,
      ).toBe(true),
    );
  });
});

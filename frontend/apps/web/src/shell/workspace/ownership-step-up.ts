import { createAccountService } from "@notrelix/features-account/core";
import type { OwnershipStepUp } from "@notrelix/features-workspace/web";

type AccountDeps = Parameters<typeof createAccountService>;

const PURPOSE = "TransferWorkspaceOwnership" as const;

/** What the provider round trip needs from the host runtime. */
export interface OwnershipStepUpEnv {
  readonly mockApi: boolean;
  readonly apiUrl: string;
}

/**
 * The workspace settings ask for a proof before the handover; this host answers with the account feature's
 * identity step-up (the same requirement, code and password calls the security page uses), so the workspace
 * feature never imports another feature. A person with an authenticator answers a code, one with only a
 * password answers the password, one who signs in only through a provider confirms with that provider: the
 * browser leaves for it and returns to the settings page with the chosen person in the address, the
 * single-use proof (bound to the person, the session and this purpose) waiting in an httpOnly cookie.
 */
export function createOwnershipStepUp(
  api: AccountDeps[0],
  endpoints: AccountDeps[1],
  env?: OwnershipStepUpEnv,
): OwnershipStepUp {
  const account = createAccountService(api, endpoints);
  return {
    async request() {
      const requirement = await account.getStepUpRequirement(PURPOSE);
      return {
        factor:
          requirement.factor === "mfa"
            ? "code"
            : requirement.factor === "oauth"
              ? "provider"
              : "password",
        challengeToken: requirement.challengeToken,
      };
    },
    withCode: (challengeToken, code) =>
      account.completeStepUpWithCode(PURPOSE, challengeToken, code),
    withPassword: (password) =>
      account.completeStepUpWithPassword(PURPOSE, password),
    provider: env
      ? {
          providers: async () =>
            (await account.getSecuritySummary()).linkedProviders,
          async start(provider, newOwnerId) {
            const back = `${window.location.pathname}?stepUp=${PURPOSE}&newOwner=${encodeURIComponent(newOwnerId)}`;
            const path = account.providerStepUpPath(provider, PURPOSE, back);
            if (!env.mockApi) {
              window.location.assign(`${env.apiUrl}${path}`);
              return;
            }
            const answer = await api.get<{ redirectTo?: string }>(path, {
              skipGlobalErrorToast: true,
            });
            window.location.assign(
              new URL(answer.redirectTo ?? "/", window.location.origin).href,
            );
          },
          resume() {
            const url = new URL(window.location.href);
            const owner = url.searchParams.get("newOwner");
            if (url.searchParams.get("stepUp") !== PURPOSE || !owner)
              return null;
            const failed = url.searchParams.get("stepUpResult") === "failed";
            // Read once: a reload must not claim (or ask for) another proof.
            for (const key of ["stepUp", "newOwner", "stepUpResult"])
              url.searchParams.delete(key);
            window.history.replaceState(
              window.history.state,
              "",
              `${url.pathname}${url.search}${url.hash}`,
            );
            return { newOwnerId: owner, failed };
          },
          claim: async () => (await account.claimProviderStepUp()).proof,
        }
      : undefined,
  };
}

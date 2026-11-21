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
 * single-use proof (bound to the person, the session, this purpose, this workspace and the chosen person)
 * waiting in an httpOnly cookie. The address only says who was chosen: the producer refuses a proof for
 * anyone else, and the settings page asks for one more click before anything is handed over.
 */
export function createOwnershipStepUp(
  api: AccountDeps[0],
  endpoints: AccountDeps[1],
  workspaceId: string,
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
    withCode: (challengeToken, code, newOwnerId) =>
      account.completeStepUpWithCode(PURPOSE, challengeToken, code, {
        workspaceId,
        newOwnerId,
      }),
    withPassword: (password, newOwnerId) =>
      account.completeStepUpWithPassword(PURPOSE, password, {
        workspaceId,
        newOwnerId,
      }),
    provider: env
      ? {
          providers: async () =>
            (await account.getSecuritySummary()).linkedProviders,
          async start(provider, newOwnerId) {
            const back = `${window.location.pathname}?stepUp=${PURPOSE}&newOwner=${encodeURIComponent(newOwnerId)}`;
            const url = await account.startProviderStepUp(
              provider,
              PURPOSE,
              back,
              { workspaceId, newOwnerId },
            );
            window.location.assign(
              env.mockApi ? new URL(url, window.location.origin).href : url,
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

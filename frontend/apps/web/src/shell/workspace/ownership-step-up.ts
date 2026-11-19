import { createAccountService } from "@notrelix/features-account/core";
import type { OwnershipStepUp } from "@notrelix/features-workspace/web";

type AccountDeps = Parameters<typeof createAccountService>;

const PURPOSE = "TransferWorkspaceOwnership" as const;

/**
 * The workspace settings ask for a proof before the handover; this host answers with the account feature's
 * identity step-up (the same requirement, code and password calls the security page uses), so the workspace
 * feature never imports another feature. A person with an authenticator answers a code, one with only a
 * password answers the password, one who signs in with a provider is told to add a way to confirm first.
 */
export function createOwnershipStepUp(
  api: AccountDeps[0],
  endpoints: AccountDeps[1],
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
  };
}

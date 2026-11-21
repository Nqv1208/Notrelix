import type { OperationResponse } from "@notrelix/contracts";
import type { WorkspaceApiClient } from "./workspace.service";

export interface WorkspaceAddressEndpoints {
  accounts: {
    /** `Workspaces.Workspaces.ResolveSlug` (GET, anonymous; the address goes in the `slug` query). */
    resolve: (accountId: string) => string;
  };
}

type ResolvedWorkspace = OperationResponse<
  "Workspaces.Workspaces.ResolveSlug",
  200
>;

/**
 * What an address opens. `refused` is one answer for a deleted address and a made-up one:
 * the caller can never tell them apart (enumeration resistance), so it carries nothing.
 */
export type WorkspaceAddressResolution =
  | { readonly kind: "found"; readonly workspaceId: string }
  | { readonly kind: "refused" };

/** The producer refuses an unknown or deleted address with a 400 (today) or a 404; both mean "refused". */
const REFUSAL_STATUSES: readonly number[] = [400, 404];

/**
 * Opens a workspace by its address. Any other failure (rate limit, network, server) is
 * thrown, so the page says "could not load, try again" and never "not found".
 */
export function createWorkspaceAddressService(
  api: WorkspaceApiClient,
  endpoints: WorkspaceAddressEndpoints,
) {
  return {
    async resolve(
      accountId: string,
      slug: string,
    ): Promise<WorkspaceAddressResolution> {
      try {
        const workspace = await api.get<ResolvedWorkspace>(
          `${endpoints.accounts.resolve(accountId)}?slug=${encodeURIComponent(slug)}`,
          { skipGlobalErrorToast: true },
        );
        return typeof workspace?.id === "string" && workspace.id.length > 0
          ? { kind: "found", workspaceId: workspace.id }
          : { kind: "refused" };
      } catch (error) {
        const status = (error as { status?: number } | null)?.status;
        if (status !== undefined && REFUSAL_STATUSES.includes(status))
          return { kind: "refused" };
        throw error;
      }
    },
  };
}

export type WorkspaceAddressService = ReturnType<
  typeof createWorkspaceAddressService
>;

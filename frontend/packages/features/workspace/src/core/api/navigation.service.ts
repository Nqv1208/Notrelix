import type { OperationResponse } from "@notrelix/contracts";
import {
  isNavigationResourceKind,
  type NavigationReference,
  type NavigationResourceKind,
} from "../model/navigation";

type RecentResourceDto =
  OperationResponse<
    "Workspaces.Navigation.ListRecents",
    200
  > extends readonly (infer Item)[]
    ? Item
    : never;
type PinnedResourceDto =
  OperationResponse<
    "Workspaces.Navigation.ListPins",
    200
  > extends readonly (infer Item)[]
    ? Item
    : never;

export interface NavigationRequestOptions {
  readonly idempotencyKey?: string;
  readonly skipGlobalErrorToast?: boolean;
}

export interface NavigationApiClient {
  get<T>(url: string): Promise<T>;
  post<T>(
    url: string,
    body: unknown,
    options?: NavigationRequestOptions,
  ): Promise<T>;
  put<T>(
    url: string,
    body?: unknown,
    options?: NavigationRequestOptions,
  ): Promise<T>;
  delete<T>(url: string, options?: NavigationRequestOptions): Promise<T>;
}

export interface NavigationEndpoints {
  workspaces: {
    recents: (workspaceId: string) => string;
    pins: (workspaceId: string) => string;
    pin: (workspaceId: string, kind: string, resourceId: string) => string;
  };
}

function toReferences(
  items: readonly { kind?: string | null; resourceId?: string; at?: string }[],
): NavigationReference[] {
  const references: NavigationReference[] = [];
  for (const item of items) {
    if (!isNavigationResourceKind(item.kind) || !item.resourceId) continue;
    references.push({
      kind: item.kind,
      resourceId: item.resourceId,
      at: item.at ?? "",
    });
  }
  return references;
}

/**
 * Adapter for the Workspaces navigation producer (WX-BE-001/002). Unknown
 * kinds are dropped: the producer may add kinds before this client knows them.
 */
export function createNavigationService(
  api: NavigationApiClient,
  endpoints: NavigationEndpoints,
) {
  return {
    async getRecents(workspaceId: string): Promise<NavigationReference[]> {
      const items = await api.get<RecentResourceDto[] | null>(
        endpoints.workspaces.recents(workspaceId),
      );
      return toReferences(
        (items ?? []).map((item) => ({ ...item, at: item.lastOpenedAt })),
      );
    },

    async getPins(workspaceId: string): Promise<NavigationReference[]> {
      const items = await api.get<PinnedResourceDto[] | null>(
        endpoints.workspaces.pins(workspaceId),
      );
      return toReferences(
        (items ?? []).map((item) => ({ ...item, at: item.pinnedAt })),
      );
    },

    /** Records an open. One idempotency key per open attempt. */
    async recordRecent(
      workspaceId: string,
      kind: NavigationResourceKind,
      resourceId: string,
      idempotencyKey: string,
    ): Promise<void> {
      await api.post<void>(
        endpoints.workspaces.recents(workspaceId),
        { kind, resourceId },
        { idempotencyKey, skipGlobalErrorToast: true },
      );
    },

    async pin(
      workspaceId: string,
      kind: NavigationResourceKind,
      resourceId: string,
    ): Promise<void> {
      await api.put<void>(
        endpoints.workspaces.pin(workspaceId, kind, resourceId),
      );
    },

    async unpin(
      workspaceId: string,
      kind: NavigationResourceKind,
      resourceId: string,
    ): Promise<void> {
      await api.delete<void>(
        endpoints.workspaces.pin(workspaceId, kind, resourceId),
      );
    },
  };
}

export type NavigationService = ReturnType<typeof createNavigationService>;

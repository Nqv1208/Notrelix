import type {
  IntegrationDetailDto,
  IntegrationsListDto,
} from "@notrelix/features-workspace/web";

/**
 * The calls behind the Integrations pages (gap `CTR-GAP-INTEGRATIONS`, see
 * docs/superpowers/be-blockers-2026-10.md, "BE requirement from FE:
 * Integrations"). Every call is quiet: the pages say what went wrong.
 */
export interface IntegrationsApiClient {
  get<T>(url: string, options?: { skipGlobalErrorToast?: boolean }): Promise<T>;
  post<T>(
    url: string,
    body?: unknown,
    options?: { skipGlobalErrorToast?: boolean; idempotencyKey?: string },
  ): Promise<T>;
  delete<T>(
    url: string,
    options?: { skipGlobalErrorToast?: boolean },
  ): Promise<T>;
}

const QUIET = { skipGlobalErrorToast: true } as const;
const root = (workspaceId: string) =>
  `/workspaces/${encodeURIComponent(workspaceId)}/integrations`;
const one = (workspaceId: string, key: string) =>
  `${root(workspaceId)}/${encodeURIComponent(key)}`;

export function createIntegrationsApi(api: IntegrationsApiClient) {
  const act = (workspaceId: string, key: string, verb: string) =>
    api.post<unknown>(
      `${one(workspaceId, key)}/${verb}`,
      {},
      { ...QUIET, idempotencyKey: crypto.randomUUID() },
    );
  return {
    list(workspaceId: string) {
      return api.get<IntegrationsListDto>(root(workspaceId), QUIET);
    },
    get(workspaceId: string, key: string) {
      return api.get<IntegrationDetailDto>(one(workspaceId, key), QUIET);
    },
    reauthorize: (workspaceId: string, key: string) =>
      act(workspaceId, key, "reauthorize"),
    pause: (workspaceId: string, key: string) => act(workspaceId, key, "pause"),
    resume: (workspaceId: string, key: string) =>
      act(workspaceId, key, "resume"),
    test: (workspaceId: string, key: string) =>
      api.post<{ result: string }>(`${one(workspaceId, key)}/test`, {}, QUIET),
    regenerateSecret: (workspaceId: string, key: string) =>
      api.post<{ secret: string }>(
        `${one(workspaceId, key)}/secret`,
        {},
        QUIET,
      ),
    disconnect(workspaceId: string, key: string) {
      return api.delete<void>(one(workspaceId, key), QUIET);
    },
    connect(
      workspaceId: string,
      provider: string,
      scope: "workspace" | "board",
    ) {
      return api.post<{ key: string }>(
        root(workspaceId),
        { provider, scope },
        { ...QUIET, idempotencyKey: crypto.randomUUID() },
      );
    },
    contactSales(workspaceId: string) {
      return api.post<{ requestedAt: string }>(
        `${root(workspaceId)}/contact-sales`,
        {},
        { ...QUIET, idempotencyKey: crypto.randomUUID() },
      );
    },
  };
}

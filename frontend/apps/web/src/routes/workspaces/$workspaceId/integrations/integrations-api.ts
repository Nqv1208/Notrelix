import type { OperationResponse } from "@notrelix/contracts";

/**
 * The calls behind the Integrations pages: the producer's `Integrations.*`
 * contract. Every call is quiet: the pages say what went wrong themselves.
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

export type IntegrationsOverview = NonNullable<
  OperationResponse<"Integrations.Connections.List">
>;
export type IntegrationConnection = NonNullable<
  IntegrationsOverview["connections"]
>[number];

/** A new webhook and its signing secret, known only in this answer. */
export interface CreatedWebhook {
  readonly connection: IntegrationConnection;
  readonly signingSecret: string;
}

const QUIET = { skipGlobalErrorToast: true } as const;
const root = (workspaceId: string) =>
  `/workspaces/${encodeURIComponent(workspaceId)}/integrations`;
const one = (workspaceId: string, connectionId: string) =>
  `${root(workspaceId)}/${encodeURIComponent(connectionId)}`;

export function createIntegrationsApi(api: IntegrationsApiClient) {
  const act = (workspaceId: string, connectionId: string, verb: string) =>
    api.post<IntegrationConnection>(
      `${one(workspaceId, connectionId)}/${verb}`,
      {},
      { ...QUIET, idempotencyKey: crypto.randomUUID() },
    );
  return {
    list(workspaceId: string) {
      return api.get<IntegrationsOverview>(root(workspaceId), QUIET);
    },
    /** The provider's authorization page the member's browser goes to. */
    authorize(workspaceId: string, provider: string) {
      return api.get<{ authorizeUrl: string }>(
        `${root(workspaceId)}/${encodeURIComponent(provider)}/authorize`,
        QUIET,
      );
    },
    connectWebhook(workspaceId: string, url: string) {
      return api.post<CreatedWebhook>(
        `${root(workspaceId)}/webhooks`,
        { url },
        { ...QUIET, idempotencyKey: crypto.randomUUID() },
      );
    },
    pause: (workspaceId: string, connectionId: string) =>
      act(workspaceId, connectionId, "pause"),
    resume: (workspaceId: string, connectionId: string) =>
      act(workspaceId, connectionId, "resume"),
    test(workspaceId: string, connectionId: string) {
      return api.post<{ deliveryId: string }>(
        `${one(workspaceId, connectionId)}/test`,
        {},
        { ...QUIET, idempotencyKey: crypto.randomUUID() },
      );
    },
    disconnect(workspaceId: string, connectionId: string) {
      return api.delete<void>(one(workspaceId, connectionId), QUIET);
    },
  };
}

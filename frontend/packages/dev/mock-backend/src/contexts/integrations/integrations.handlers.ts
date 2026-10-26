/**
 * Integrations — the producer's contract:
 *
 *   Integrations.Connections.List       — GET    /workspaces/:workspaceId/integrations
 *   Integrations.Connections.Authorize  — GET    /workspaces/:workspaceId/integrations/:provider/authorize
 *   Integrations.Webhooks.Connect       — POST   /workspaces/:workspaceId/integrations/webhooks
 *   Integrations.Connections.Pause      — POST   /workspaces/:workspaceId/integrations/:connectionId/pause
 *   Integrations.Connections.Resume     — POST   /workspaces/:workspaceId/integrations/:connectionId/resume
 *   Integrations.Connections.Test       — POST   /workspaces/:workspaceId/integrations/:connectionId/test
 *   Integrations.Connections.Disconnect — DELETE /workspaces/:workspaceId/integrations/:connectionId
 *
 * Who manages: workspace Owners and Admins (`ManageIntegrations`).
 */
import { defineMockOperation } from "../../operations/types";
import {
  created,
  forbidden,
  noContent,
  notFound,
  ok,
  validationError,
} from "../../transport/create-response";
import {
  INTEGRATION_CATALOG,
  type StoredConnection,
} from "../../state/mock-integrations";
import type { MockStore } from "../../state/mock-store";

const canManage = (store: MockStore, workspaceId: string): boolean => {
  const role = store.getCurrentMemberRole(workspaceId);
  return role === "owner" || role === "admin";
};

const dto = (connection: StoredConnection) => ({
  id: connection.id,
  provider: connection.provider,
  status: connection.status,
  connectedBy: connection.connectedBy,
  connectedAt: connection.connectedAt,
  lastSuccessAt: connection.lastSuccessAt,
  lastErrorAt: connection.lastErrorAt,
  lastError: connection.lastError,
  targetUrl: connection.targetUrl,
});

type Ws = { workspaceId: string };
type WithProvider = { workspaceId: string; provider: string };
type WithConnection = { workspaceId: string; connectionId: string };

export const integrationsOperations = [
  defineMockOperation<Ws, never, unknown>({
    id: "integrations.list",
    contract: {
      kind: "openapi",
      operationId: "Integrations.Connections.List",
    },
    method: "GET",
    route: "/workspaces/:workspaceId/integrations",
    async handle({ params, store }) {
      if (!canManage(store, params.workspaceId)) return forbidden();
      return ok({
        catalog: INTEGRATION_CATALOG,
        connections: store.integrations.list().map(dto),
      });
    },
  }),
  defineMockOperation<WithProvider, never, unknown>({
    id: "integrations.authorize",
    contract: {
      kind: "openapi",
      operationId: "Integrations.Connections.Authorize",
    },
    method: "GET",
    route: "/workspaces/:workspaceId/integrations/:provider/authorize",
    async handle({ params, store }) {
      if (!canManage(store, params.workspaceId)) return forbidden();
      const entry = INTEGRATION_CATALOG.find(
        (item) => item.provider === params.provider,
      );
      if (!entry || entry.provider === "webhook") return notFound();
      if (!entry.configured) {
        return {
          status: 422,
          body: {
            message: "The provider is not configured.",
            code: "integrations.provider.not-configured",
          },
        };
      }
      // The mock has no provider page: the member comes straight back connected.
      store.integrations.add(
        entry.provider,
        store.getCurrentUser().id,
        store.getClock().isoNow(),
        null,
      );
      return ok({
        authorizeUrl: `/workspaces/${params.workspaceId}/integrations?connected=${entry.provider}`,
      });
    },
  }),
  defineMockOperation<Ws, { url?: string }, unknown>({
    id: "integrations.connectWebhook",
    contract: {
      kind: "openapi",
      operationId: "Integrations.Webhooks.Connect",
    },
    method: "POST",
    route: "/workspaces/:workspaceId/integrations/webhooks",
    async handle({ body, params, store }) {
      if (!canManage(store, params.workspaceId)) return forbidden();
      const url = body?.url?.trim() ?? "";
      if (!/^https:\/\/[^/\s]+/.test(url)) {
        return validationError({ url: ["The address must use https."] });
      }
      const connection = store.integrations.add(
        "webhook",
        store.getCurrentUser().id,
        store.getClock().isoNow(),
        url,
      );
      return created({
        connection: dto(connection),
        signingSecret: "whsec_mock_0000000000000000000000000000",
      });
    },
  }),
  ...(["pause", "resume"] as const).map((verb) =>
    defineMockOperation<WithConnection, never, unknown>({
      id: `integrations.${verb}`,
      contract: {
        kind: "openapi",
        operationId:
          verb === "pause"
            ? "Integrations.Connections.Pause"
            : "Integrations.Connections.Resume",
      },
      method: "POST",
      route: `/workspaces/:workspaceId/integrations/:connectionId/${verb}`,
      async handle({ params, store }) {
        if (!canManage(store, params.workspaceId)) return forbidden();
        const connection = store.integrations.find(params.connectionId);
        if (!connection) return notFound();
        if (verb === "pause") connection.status = "paused";
        else if (connection.status === "paused") connection.status = "active";
        return ok(dto(connection));
      },
    }),
  ),
  defineMockOperation<WithConnection, never, unknown>({
    id: "integrations.test",
    contract: { kind: "openapi", operationId: "Integrations.Connections.Test" },
    method: "POST",
    route: "/workspaces/:workspaceId/integrations/:connectionId/test",
    async handle({ params, store }) {
      if (!canManage(store, params.workspaceId)) return forbidden();
      const connection = store.integrations.find(params.connectionId);
      if (!connection) return notFound();
      if (connection.provider !== "webhook") {
        return {
          status: 422,
          body: {
            message: "Only an outgoing webhook has a test delivery.",
            code: "integrations.test.unsupported",
          },
        };
      }
      connection.lastSuccessAt = store.getClock().isoNow();
      if (connection.status === "error") connection.status = "active";
      return ok({ deliveryId: "00000000-0000-7000-8000-00000000d001" });
    },
  }),
  defineMockOperation<WithConnection, never, unknown>({
    id: "integrations.disconnect",
    contract: {
      kind: "openapi",
      operationId: "Integrations.Connections.Disconnect",
    },
    method: "DELETE",
    route: "/workspaces/:workspaceId/integrations/:connectionId",
    async handle({ params, store }) {
      if (!canManage(store, params.workspaceId)) return forbidden();
      return store.integrations.remove(params.connectionId)
        ? noContent()
        : notFound();
    },
  }),
];

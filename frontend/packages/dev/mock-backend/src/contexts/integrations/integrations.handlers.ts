/**
 * Integrations — the Integrations pages of the 2026-10 Integrations mockup (gap
 * `CTR-GAP-INTEGRATIONS`, see docs/superpowers/be-blockers-2026-10.md, "BE
 * requirement from FE: Integrations"):
 *
 *   integrations.list         — GET    /workspaces/:workspaceId/integrations
 *   integrations.get          — GET    /workspaces/:workspaceId/integrations/:key
 *   integrations.reauthorize  — POST   /workspaces/:workspaceId/integrations/:key/reauthorize
 *   integrations.disconnect   — DELETE /workspaces/:workspaceId/integrations/:key
 *   integrations.pause        — POST   /workspaces/:workspaceId/integrations/:key/pause
 *   integrations.resume       — POST   /workspaces/:workspaceId/integrations/:key/resume
 *   integrations.test         — POST   /workspaces/:workspaceId/integrations/:key/test
 *   integrations.secret       — POST   /workspaces/:workspaceId/integrations/:key/secret
 *   integrations.connect      — POST   /workspaces/:workspaceId/integrations
 *   integrations.contactSales — POST   /workspaces/:workspaceId/integrations/contact-sales
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
import { INTEGRATION_CATALOG } from "../../state/mock-integrations";
import { mockIds } from "../../state/mock-ids";
import type { MockStore } from "../../state/mock-store";

const gap = { kind: "gap", gapId: "CTR-GAP-INTEGRATIONS" } as const;
const ADMINS = ["Nguyễn Thu Hà", "Trịnh Quốc Hưng"];

const canManage = (store: MockStore): boolean => {
  const id = store.getCurrentUser().id;
  return id === mockIds.users.owner || id === mockIds.users.admin;
};

type Ws = { workspaceId: string };
type WithKey = { workspaceId: string; key: string };

export const integrationsOperations = [
  defineMockOperation<Ws, never, unknown>({
    id: "integrations.list",
    contract: gap,
    method: "GET",
    route: "/workspaces/:workspaceId/integrations",
    async handle({ store }) {
      const rows = store.integrations.rows(store.getCurrentUser().name ?? "");
      return ok({
        serverTime: store.getClock().isoNow(),
        updatedAt: "10:40",
        canManage: canManage(store),
        workspaceName: "Minh An Group",
        admins: ADMINS,
        rows,
        catalog: INTEGRATION_CATALOG.filter(
          (item) => !rows.some((row) => row.key === item.key),
        ),
        plan: {
          name: "business",
          account: "Tập đoàn Minh An",
          workspaces: ["Minh An Group", "Minh An Đà Nẵng"],
        },
      });
    },
  }),
  defineMockOperation<WithKey, never, unknown>({
    id: "integrations.get",
    contract: gap,
    method: "GET",
    route: "/workspaces/:workspaceId/integrations/:key",
    async handle({ params, store }) {
      if (!canManage(store)) return forbidden();
      const detail = store.integrations.detail(params.key);
      return detail
        ? ok({
            ...detail,
            updatedAt: "10:40",
            serverTime: store.getClock().isoNow(),
          })
        : notFound();
    },
  }),
  defineMockOperation<WithKey, never, unknown>({
    id: "integrations.reauthorize",
    contract: gap,
    method: "POST",
    route: "/workspaces/:workspaceId/integrations/:key/reauthorize",
    async handle({ params, store }) {
      if (!canManage(store)) return forbidden();
      if (params.key !== "teams") return notFound();
      store.integrations.reauthorize();
      return ok({ ok: true });
    },
  }),
  defineMockOperation<WithKey, never, unknown>({
    id: "integrations.disconnect",
    contract: gap,
    method: "DELETE",
    route: "/workspaces/:workspaceId/integrations/:key",
    async handle({ params, store }) {
      if (!canManage(store)) return forbidden();
      return store.integrations.disconnect(params.key)
        ? noContent()
        : notFound();
    },
  }),
  ...(["pause", "resume"] as const).map((verb) =>
    defineMockOperation<WithKey, never, unknown>({
      id: `integrations.${verb}`,
      contract: gap,
      method: "POST",
      route: `/workspaces/:workspaceId/integrations/:key/${verb}`,
      async handle({ params, store }) {
        if (!canManage(store)) return forbidden();
        if (params.key !== "webhook") return notFound();
        store.integrations.setPaused(verb === "pause");
        return ok({ ok: true });
      },
    }),
  ),
  defineMockOperation<WithKey, never, unknown>({
    id: "integrations.test",
    contract: gap,
    method: "POST",
    route: "/workspaces/:workspaceId/integrations/:key/test",
    async handle({ params, store }) {
      if (!canManage(store)) return forbidden();
      return params.key === "webhook"
        ? ok({ result: "503", sentAt: store.getClock().isoNow() })
        : notFound();
    },
  }),
  defineMockOperation<WithKey, never, unknown>({
    id: "integrations.secret",
    contract: gap,
    method: "POST",
    route: "/workspaces/:workspaceId/integrations/:key/secret",
    async handle({ params, store }) {
      if (!canManage(store)) return forbidden();
      if (params.key !== "webhook") return notFound();
      store.integrations.rotateSecret();
      // The secret is shown once, here; the page never shows it again.
      return ok({ secret: "whsec_mock_0000000000000000" });
    },
  }),
  defineMockOperation<Ws, { provider?: string; scope?: string }, unknown>({
    id: "integrations.connect",
    contract: gap,
    method: "POST",
    route: "/workspaces/:workspaceId/integrations",
    async handle({ body, store }) {
      if (!canManage(store)) return forbidden();
      if (body?.provider !== "google-drive") {
        return validationError({ provider: ["unsupported"] });
      }
      store.integrations.connectGoogleDrive();
      return created({ key: "google-drive" });
    },
  }),
  defineMockOperation<Ws, unknown, unknown>({
    id: "integrations.contactSales",
    contract: gap,
    method: "POST",
    route: "/workspaces/:workspaceId/integrations/contact-sales",
    async handle({ store }) {
      if (!canManage(store)) return forbidden();
      store.integrations.contact();
      return created({ requestedAt: store.getClock().isoNow() });
    },
  }),
];

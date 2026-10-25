/**
 * Automation: the rules of a workspace and their run history, over the
 * producer's routes. Only a workspace owner or admin reads or changes them
 * (the producer asks for ManageWorkspaceSettings); anyone else gets 403.
 *
 * Operations:
 *   Automation.Rules.List         GET   /workspaces/:workspaceId/automations
 *   Automation.Rules.Create       POST  /workspaces/:workspaceId/automations
 *   Automation.Rules.SetEnabled   PATCH /workspaces/:workspaceId/automations/:automationId/enabled
 *   Automation.Executions.List    GET   /automations/:automationId/executions
 */
import { defineMockOperation } from "../../operations/types";
import {
  created,
  forbidden,
  noContent,
  notFound,
  ok,
  validationError,
  type MockHttpResult,
} from "../../transport/create-response";
import type { MockStore } from "../../state/mock-store";

function mayManage(store: MockStore, workspaceId: string): boolean {
  const role = store.getCurrentMemberRole(workspaceId);
  return role === "owner" || role === "admin";
}

function guard(
  store: MockStore,
  workspaceId: string,
): MockHttpResult<unknown> | null {
  if (!store.getWorkspace(workspaceId)) return notFound("Workspace not found");
  return mayManage(store, workspaceId) ? null : forbidden();
}

interface CreateBody {
  name?: string | null;
  triggerEvent?: string | null;
  actionType?: string | null;
  configuration?: string | null;
}

export const workspaceAutomationsOperations = [
  defineMockOperation<{ workspaceId: string }, never, unknown>({
    id: "automation.rules.list",
    contract: { kind: "openapi", operationId: "Automation.Rules.List" },
    method: "GET",
    route: "/workspaces/:workspaceId/automations",
    async handle({ params, store }) {
      const denied = guard(store, params.workspaceId);
      if (denied) return denied;
      return ok(store.workspaceAutomations.list(params.workspaceId));
    },
  }),
  defineMockOperation<{ workspaceId: string }, CreateBody, unknown>({
    id: "automation.rules.create",
    contract: { kind: "openapi", operationId: "Automation.Rules.Create" },
    method: "POST",
    route: "/workspaces/:workspaceId/automations",
    async handle({ params, body, store }) {
      const denied = guard(store, params.workspaceId);
      if (denied) return denied;
      const name = (body?.name ?? "").trim();
      if (!name || !body?.triggerEvent || !body.actionType) {
        return validationError({
          _errors: ["Invalid automation trigger and/or action configuration."],
        });
      }
      const rule = store.workspaceAutomations.create(
        params.workspaceId,
        {
          name,
          triggerEvent: body.triggerEvent,
          actionType: body.actionType,
          configuration: body.configuration ?? "{}",
        },
        store.getCurrentUser().id,
      );
      return created<string>(rule.id);
    },
  }),
  defineMockOperation<
    { workspaceId: string; automationId: string },
    { isEnabled?: boolean },
    void
  >({
    id: "automation.rules.setEnabled",
    contract: { kind: "openapi", operationId: "Automation.Rules.SetEnabled" },
    method: "PATCH",
    route: "/workspaces/:workspaceId/automations/:automationId/enabled",
    async handle({ params, body, store }) {
      const denied = guard(store, params.workspaceId);
      if (denied) return denied as MockHttpResult<void>;
      const rule = store.workspaceAutomations.find(params.automationId);
      if (!rule || rule.workspaceId !== params.workspaceId)
        return notFound("Automation rule not found");
      store.workspaceAutomations.setEnabled(rule, body?.isEnabled === true);
      return noContent();
    },
  }),
  defineMockOperation<{ automationId: string }, never, unknown>({
    id: "automation.executions.list",
    contract: { kind: "openapi", operationId: "Automation.Executions.List" },
    method: "GET",
    route: "/automations/:automationId/executions",
    async handle({ params, store }) {
      const rule = store.workspaceAutomations.find(params.automationId);
      if (!rule) return notFound("Automation rule not found");
      if (!mayManage(store, rule.workspaceId)) return forbidden();
      return ok([]);
    },
  }),
];

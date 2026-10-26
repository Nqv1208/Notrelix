/**
 * Automation: the rules of a workspace and their run history, over the
 * producer's routes. Only a workspace owner or admin reads or changes them
 * (the producer asks for ManageWorkspaceSettings); anyone else gets 403.
 *
 * Operations:
 *   Automation.Rules.List         GET   /workspaces/:workspaceId/automations
 *   Automation.Rules.Create       POST  /workspaces/:workspaceId/automations
 *   Automation.Rules.Update       PUT   /workspaces/:workspaceId/automations/:automationId
 *   Automation.Rules.Delete       DELETE /workspaces/:workspaceId/automations/:automationId
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

interface DefinitionBody {
  name?: string | null;
  triggerEvent?: string | null;
  triggerConfiguration?: string | null;
  actions?: { type?: string | null; configuration?: string | null }[] | null;
  conditions?: string | null;
  boardId?: string | null;
}

const MAX_ACTIONS = 5;
const RUNNABLE = new Set([
  "MoveItem",
  "ChangeStatus",
  "AssignPerson",
  "UpdateField",
  "NotifyMember",
  "SendEmail",
]);

/** The definition the body describes, or null when it is not a valid rule. */
function definitionOf(body: DefinitionBody | undefined) {
  const name = (body?.name ?? "").trim();
  const actions = (body?.actions ?? []).map((action) => ({
    type: action.type ?? "",
    configuration: action.configuration ?? null,
  }));
  if (
    !name ||
    !body?.triggerEvent ||
    actions.length === 0 ||
    actions.length > MAX_ACTIONS ||
    actions.some((action) => !RUNNABLE.has(action.type))
  ) {
    return null;
  }
  return {
    name,
    triggerEvent: body.triggerEvent,
    triggerConfiguration: body.triggerConfiguration ?? null,
    actions,
    conditions: body.conditions ?? null,
  };
}

/** A board Manager or a workspace Owner/Admin may manage the rules of a board. */
function mayManageBoard(
  store: MockStore,
  workspaceId: string,
  boardId: string | null | undefined,
): boolean {
  if (mayManage(store, workspaceId)) return true;
  // A board Manager is a member of the workspace who names the board they manage.
  const role = store.getCurrentMemberRole(workspaceId);
  return Boolean(boardId) && role === "member";
}

export const workspaceAutomationsOperations = [
  defineMockOperation<{ workspaceId: string }, never, unknown>({
    id: "automation.rules.list",
    contract: { kind: "openapi", operationId: "Automation.Rules.List" },
    method: "GET",
    route: "/workspaces/:workspaceId/automations",
    async handle({ params, query, store }) {
      if (!store.getWorkspace(params.workspaceId))
        return notFound("Workspace not found");
      const boardId = query.boardId ?? null;
      if (!mayManageBoard(store, params.workspaceId, boardId))
        return forbidden();
      return ok(store.workspaceAutomations.list(params.workspaceId, boardId));
    },
  }),
  defineMockOperation<{ workspaceId: string }, DefinitionBody, unknown>({
    id: "automation.rules.create",
    contract: { kind: "openapi", operationId: "Automation.Rules.Create" },
    method: "POST",
    route: "/workspaces/:workspaceId/automations",
    async handle({ params, body, store }) {
      if (!store.getWorkspace(params.workspaceId))
        return notFound("Workspace not found");
      if (!mayManageBoard(store, params.workspaceId, body?.boardId))
        return forbidden();
      const definition = definitionOf(body);
      if (!definition) {
        return validationError({
          _errors: ["Invalid automation trigger and/or action configuration."],
        });
      }
      const rule = store.workspaceAutomations.create(
        params.workspaceId,
        { ...definition, boardId: body?.boardId ?? null },
        store.getCurrentUser().id,
      );
      return created<string>(rule.id);
    },
  }),
  defineMockOperation<
    { workspaceId: string; automationId: string },
    DefinitionBody,
    void
  >({
    id: "automation.rules.update",
    contract: { kind: "openapi", operationId: "Automation.Rules.Update" },
    method: "PUT",
    route: "/workspaces/:workspaceId/automations/:automationId",
    async handle({ params, body, store }) {
      if (!mayManageBoard(store, params.workspaceId, body?.boardId))
        return forbidden() as MockHttpResult<void>;
      const rule = store.workspaceAutomations.find(params.automationId);
      if (
        !rule ||
        rule.removed ||
        rule.workspaceId !== params.workspaceId ||
        (body?.boardId && rule.boardId !== body.boardId)
      )
        return notFound("Automation rule not found");
      const definition = definitionOf(body);
      if (!definition) {
        return validationError({
          _errors: ["Invalid automation trigger and/or action configuration."],
        }) as MockHttpResult<void>;
      }
      store.workspaceAutomations.update(
        rule,
        definition,
        store.getCurrentUser().id,
      );
      return noContent();
    },
  }),
  defineMockOperation<
    { workspaceId: string; automationId: string },
    never,
    void
  >({
    id: "automation.rules.delete",
    contract: { kind: "openapi", operationId: "Automation.Rules.Delete" },
    method: "DELETE",
    route: "/workspaces/:workspaceId/automations/:automationId",
    async handle({ params, query, store }) {
      const boardId = query.boardId ?? null;
      if (!mayManageBoard(store, params.workspaceId, boardId))
        return forbidden() as MockHttpResult<void>;
      const rule = store.workspaceAutomations.find(params.automationId);
      if (
        !rule ||
        rule.removed ||
        rule.workspaceId !== params.workspaceId ||
        (boardId && rule.boardId !== boardId)
      )
        return notFound("Automation rule not found");
      store.workspaceAutomations.remove(rule);
      return noContent();
    },
  }),
  defineMockOperation<
    { workspaceId: string; automationId: string },
    { isEnabled?: boolean; boardId?: string | null },
    void
  >({
    id: "automation.rules.setEnabled",
    contract: { kind: "openapi", operationId: "Automation.Rules.SetEnabled" },
    method: "PATCH",
    route: "/workspaces/:workspaceId/automations/:automationId/enabled",
    async handle({ params, body, store }) {
      if (!mayManageBoard(store, params.workspaceId, body?.boardId))
        return forbidden() as MockHttpResult<void>;
      const rule = store.workspaceAutomations.find(params.automationId);
      if (
        !rule ||
        rule.removed ||
        rule.workspaceId !== params.workspaceId ||
        (body?.boardId && rule.boardId !== body.boardId)
      )
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
    async handle({ params, query, store }) {
      const rule = store.workspaceAutomations.find(params.automationId);
      if (!rule) return notFound("Automation rule not found");
      if (!mayManageBoard(store, rule.workspaceId, query.boardId ?? null))
        return forbidden();
      return ok(store.workspaceAutomations.executions(rule.id));
    },
  }),
];

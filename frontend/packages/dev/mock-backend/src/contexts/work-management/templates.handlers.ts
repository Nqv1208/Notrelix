/**
 * Board templates, over the producer's routes. Like the producer: a saved
 * template starts as a draft; only a workspace owner or admin publishes,
 * archives or deletes one; only published templates (and the built-in
 * starters) create boards; an archived template is not published again.
 *
 * Operations:
 *   WorkManagement.Templates.List         GET    /workspaces/:workspaceId/templates
 *   WorkManagement.Templates.Create       POST   /boards/:boardId/templates
 *   WorkManagement.Templates.Publish      PATCH  /templates/:templateId/publish
 *   WorkManagement.Templates.Archive      PATCH  /templates/:templateId/archive
 *   WorkManagement.Templates.Delete       DELETE /templates/:templateId
 *   WorkManagement.Templates.CreateBoard  POST   /templates/:templateId/create-board
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
import type { MockStore } from "../../state/mock-store";
import { MOCK_BUILT_IN_BOARD_TEMPLATES } from "../../state/mock-board-templates";

function mayManage(store: MockStore, workspaceId: string): boolean {
  const role = store.getCurrentMemberRole(workspaceId);
  return role === "owner" || role === "admin";
}

/** The workspace a template belongs to, or null when no template has this id. */
function workspaceOf(store: MockStore, templateId: string): string | null {
  return store.boardTemplates.get(templateId)?.workspaceId ?? null;
}

export const templatesOperations = [
  defineMockOperation<{ workspaceId: string }, never, unknown>({
    id: "templates.list",
    contract: {
      kind: "openapi",
      operationId: "WorkManagement.Templates.List",
    },
    method: "GET",
    route: "/workspaces/:workspaceId/templates",
    async handle({ params, store }) {
      if (!store.isCurrentMemberOf(params.workspaceId))
        return notFound("Workspace not found");
      return ok([
        ...MOCK_BUILT_IN_BOARD_TEMPLATES.map((template) => ({
          id: template.id,
          name: template.name,
          description: template.description,
          status: "Published",
          builtInKey: template.key,
        })),
        ...store.boardTemplates.list(params.workspaceId).map((template) => ({
          id: template.id,
          name: template.name,
          description: template.description,
          status: template.status,
          builtInKey: null,
        })),
      ]);
    },
  }),

  defineMockOperation<
    { boardId: string },
    { name?: string; description?: string | null; includeItems?: boolean },
    string
  >({
    id: "templates.create",
    contract: {
      kind: "openapi",
      operationId: "WorkManagement.Templates.Create",
    },
    method: "POST",
    route: "/boards/:boardId/templates",
    async handle({ params, body, store }) {
      const board = store.getBoard(params.boardId);
      if (!board) return notFound("Board not found");
      // Saving needs the right to manage the board; the mock holds that for owners and admins.
      if (!mayManage(store, board.workspaceId)) return forbidden();
      const name = (body?.name ?? "").trim();
      if (!name || name.length > 255) {
        return validationError({ name: ["Name is required."] });
      }
      const description = (body?.description ?? "").trim();
      const record = store.boardTemplates.save(board.workspaceId, {
        name,
        description: description === "" ? null : description,
      });
      return created<string>(record.id);
    },
  }),

  defineMockOperation<{ templateId: string }, never, void>({
    id: "templates.publish",
    contract: {
      kind: "openapi",
      operationId: "WorkManagement.Templates.Publish",
    },
    method: "PATCH",
    route: "/templates/:templateId/publish",
    async handle({ params, store }) {
      const workspaceId = workspaceOf(store, params.templateId);
      if (!workspaceId) return notFound("Template not found");
      if (!mayManage(store, workspaceId)) return forbidden();
      const template = store.boardTemplates.get(params.templateId)!;
      if (template.status === "Archived") {
        return validationError({
          _errors: ["Cannot publish an archived template."],
        });
      }
      template.status = "Published";
      return noContent();
    },
  }),

  defineMockOperation<{ templateId: string }, never, void>({
    id: "templates.archive",
    contract: {
      kind: "openapi",
      operationId: "WorkManagement.Templates.Archive",
    },
    method: "PATCH",
    route: "/templates/:templateId/archive",
    async handle({ params, store }) {
      const workspaceId = workspaceOf(store, params.templateId);
      if (!workspaceId) return notFound("Template not found");
      if (!mayManage(store, workspaceId)) return forbidden();
      store.boardTemplates.get(params.templateId)!.status = "Archived";
      return noContent();
    },
  }),

  defineMockOperation<{ templateId: string }, never, void>({
    id: "templates.delete",
    contract: {
      kind: "openapi",
      operationId: "WorkManagement.Templates.Delete",
    },
    method: "DELETE",
    route: "/templates/:templateId",
    async handle({ params, store }) {
      const workspaceId = workspaceOf(store, params.templateId);
      if (!workspaceId) return notFound("Template not found");
      if (!mayManage(store, workspaceId)) return forbidden();
      store.boardTemplates.remove(params.templateId);
      return noContent();
    },
  }),

  defineMockOperation<
    { templateId: string },
    { workspaceId?: string; name?: string; spaceId?: string | null },
    string
  >({
    id: "templates.createBoard",
    contract: {
      kind: "openapi",
      operationId: "WorkManagement.Templates.CreateBoard",
    },
    method: "POST",
    route: "/templates/:templateId/create-board",
    async handle({ params, body, store }) {
      const workspaceId = body?.workspaceId ?? "";
      if (!store.isCurrentMemberOf(workspaceId))
        return notFound("Workspace not found");
      const builtIn = MOCK_BUILT_IN_BOARD_TEMPLATES.some(
        (template) => template.id === params.templateId,
      );
      const template = store.boardTemplates.get(params.templateId);
      // A template of another workspace, or one that is not published, is not found.
      if (
        !builtIn &&
        (!template ||
          template.workspaceId !== workspaceId ||
          template.status !== "Published")
      ) {
        return notFound("Template not found");
      }
      const name = (body?.name ?? "").trim();
      if (!name) return validationError({ name: ["Name is required."] });
      const board = store.createBoard(workspaceId, {
        title: name,
        description: template?.description ?? undefined,
        spaceId: body?.spaceId ?? null,
      });
      return created<string>(board.id);
    },
  }),
];

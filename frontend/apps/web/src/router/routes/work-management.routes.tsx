import { createRoute, redirect } from "@tanstack/react-router";
import { workspaceRoute } from "./base.routes";
import { parseBoardSearch } from "../schemas/board-search-schema";
import { BoardRouteError } from "../../routes/workspaces/$workspaceId/boards/board-route-error";
import { BoardPage } from "../../routes/workspaces/$workspaceId/boards/$boardId";
import { BoardSettingsRoutePage } from "../../routes/workspaces/$workspaceId/boards/$boardId.settings";
import { BoardFormFillPage } from "../../routes/workspaces/$workspaceId/boards/$boardId.forms.$formId";
import { BoardFormInboxRoutePage } from "../../routes/workspaces/$workspaceId/boards/$boardId.forms.$formId.responses";
import { boardSettingsSearchSchema } from "../schemas/board-settings-search-schema";

export const boardRoute = createRoute({
  getParentRoute: () => workspaceRoute,
  path: "/boards/$boardId",
  validateSearch: (search) => parseBoardSearch(search),
  component: BoardPage,
  errorComponent: ({ error, reset }) => (
    <BoardRouteError error={error} reset={reset} />
  ),
});

/**
 * An item has no page of its own: its link opens the board with the card
 * popup on that item, so a shared link and a bookmark keep working.
 */
export const boardItemRoute = createRoute({
  getParentRoute: () => workspaceRoute,
  path: "/boards/$boardId/items/$itemId",
  beforeLoad: ({ params }) => {
    throw redirect({
      to: "/workspaces/$workspaceId/boards/$boardId",
      params: { workspaceId: params.workspaceId, boardId: params.boardId },
      search: { view: "kanban", item: params.itemId },
      replace: true,
    });
  },
});

export const boardSettingsRoute = createRoute({
  getParentRoute: () => workspaceRoute,
  path: "/boards/$boardId/settings",
  validateSearch: (search) => boardSettingsSearchSchema.parse(search),
  component: BoardSettingsRoutePage,
});

/** The member's page of a form (the link a manager shares): in the app, signed in. */
export const boardFormRoute = createRoute({
  getParentRoute: () => workspaceRoute,
  path: "/boards/$boardId/forms/$formId",
  component: BoardFormFillPage,
});

/** The builder lives in the board settings; the old address still opens it, so shared links keep working. */
export const boardFormBuilderRoute = createRoute({
  getParentRoute: () => workspaceRoute,
  path: "/boards/$boardId/forms/$formId/edit",
  beforeLoad: ({ params }) => {
    throw redirect({
      to: "/workspaces/$workspaceId/boards/$boardId/settings",
      params: { workspaceId: params.workspaceId, boardId: params.boardId },
      search: { section: "forms", form: params.formId },
      replace: true,
    });
  },
});

export const boardFormInboxRoute = createRoute({
  getParentRoute: () => workspaceRoute,
  path: "/boards/$boardId/forms/$formId/responses",
  component: BoardFormInboxRoutePage,
});

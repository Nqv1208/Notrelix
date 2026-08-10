import { createRoute } from "@tanstack/react-router";
import { workspaceRoute } from "./base.routes";
import { boardSearchSchema } from "../schemas/board-search-schema";
import { BoardPage } from "../../routes/workspaces/$workspaceId/boards/$boardId";

export const boardRoute = createRoute({
  getParentRoute: () => workspaceRoute,
  path: "/boards/$boardId",
  validateSearch: (search) => boardSearchSchema.parse(search),
  component: BoardPage,
});

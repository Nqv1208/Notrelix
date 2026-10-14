import { createRoute } from "@tanstack/react-router";
import { workspaceRoute } from "./base.routes";
import { boardSearchSchema } from "../schemas/board-search-schema";
import { BoardPage } from "../../routes/workspaces/$workspaceId/boards/$boardId";
import { BoardItemPage } from "../../routes/workspaces/$workspaceId/boards/$boardId.items.$itemId";
import { itemSearchSchema } from "../schemas/item-search-schema";

export const boardRoute = createRoute({
  getParentRoute: () => workspaceRoute,
  path: "/boards/$boardId",
  validateSearch: (search) => boardSearchSchema.parse(search),
  component: BoardPage,
});

export const boardItemRoute = createRoute({
  getParentRoute: () => workspaceRoute,
  path: "/boards/$boardId/items/$itemId",
  validateSearch: (search) => itemSearchSchema.parse(search),
  component: BoardItemPage,
});

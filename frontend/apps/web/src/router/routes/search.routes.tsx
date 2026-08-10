import { createRoute } from "@tanstack/react-router";
import { workspaceRoute } from "./base.routes";
import { searchSearchSchema } from "../schemas/search-search-schema";
import { SearchResultsPage } from "../../routes/workspaces/$workspaceId/search";

export const searchRoute = createRoute({
  getParentRoute: () => workspaceRoute,
  path: "/search",
  validateSearch: (search) => searchSearchSchema.parse(search),
  component: SearchResultsPage,
});

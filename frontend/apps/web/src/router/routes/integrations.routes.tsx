import { createRoute } from "@tanstack/react-router";
import { workspaceRoute } from "./base.routes";
import { integrationsSearchSchema } from "../schemas/integrations-search-schema";
import { IntegrationsPage } from "../../routes/workspaces/$workspaceId/integrations";

export const integrationsRoute = createRoute({
  getParentRoute: () => workspaceRoute,
  path: "/integrations",
  validateSearch: (search) => integrationsSearchSchema.parse(search),
  component: IntegrationsPage,
});

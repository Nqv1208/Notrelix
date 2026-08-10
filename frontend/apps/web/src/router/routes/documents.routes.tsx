import { createRoute } from "@tanstack/react-router";
import { workspaceRoute } from "./base.routes";
import { DocPage } from "../../routes/workspaces/$workspaceId/docs/$docId";

export const docRoute = createRoute({
  getParentRoute: () => workspaceRoute,
  path: "/docs/$docId",
  component: DocPage,
});

import { createRoute } from "@tanstack/react-router";
import { workspaceRoute } from "./base.routes";
import { BillingPage } from "../../routes/workspaces/$workspaceId/billing";

export const billingRoute = createRoute({
  getParentRoute: () => workspaceRoute,
  path: "/billing",
  component: BillingPage,
});

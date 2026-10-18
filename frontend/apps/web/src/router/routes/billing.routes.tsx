import { createRoute } from "@tanstack/react-router";
import { workspaceRoute } from "./base.routes";
import { billingSearchSchema } from "../schemas/billing-search-schema";
import { BillingPage } from "../../routes/workspaces/$workspaceId/billing";

export const billingRoute = createRoute({
  getParentRoute: () => workspaceRoute,
  path: "/billing",
  validateSearch: (search) => billingSearchSchema.parse(search),
  component: BillingPage,
});

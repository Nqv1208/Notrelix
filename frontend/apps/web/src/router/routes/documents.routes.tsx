import { createRoute, redirect } from "@tanstack/react-router";
import { workspaceRoute } from "./base.routes";
import { DocPage } from "../../routes/workspaces/$workspaceId/docs/$docId";

/** Docs live in the Boards and Docs directory; `/docs` opens it. */
export const docsIndexRoute = createRoute({
  getParentRoute: () => workspaceRoute,
  path: "/docs",
  beforeLoad: ({ params }) => {
    throw redirect({
      to: "/workspaces/$workspaceId/library",
      params: { workspaceId: params.workspaceId },
      replace: true,
    });
  },
});

export const docRoute = createRoute({
  getParentRoute: () => workspaceRoute,
  path: "/docs/$docId",
  component: DocPage,
});

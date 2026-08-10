import { createRouter as createTanStackRouter } from "@tanstack/react-router";
import { ErrorState, LoadingState, NotFoundState } from "@notrelix/ui-web";
import type { AppRouterContext } from "./context";
import { routeTree } from "./route-tree";
import { boardSearchSchema } from "./schemas/board-search-schema";
import { searchSearchSchema } from "./schemas/search-search-schema";

export function createRouter() {
  const router = createTanStackRouter({
    routeTree,
    context: undefined as unknown as AppRouterContext,
    defaultPreload: "intent",
    defaultPendingComponent: () => (
      <LoadingState title="Loading" description="Preparing workspace..." />
    ),
    defaultErrorComponent: ({ error }) => (
      <ErrorState error={error} title="Route error" />
    ),
    defaultNotFoundComponent: () => (
      <NotFoundState
        title="Page not found"
        description="The requested route does not exist."
      />
    ),
    scrollRestoration: true,
  });

  return router;
}

export const router = createRouter();
export { boardSearchSchema, searchSearchSchema };

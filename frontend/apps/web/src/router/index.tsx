import { createRouter as createTanStackRouter } from "@tanstack/react-router";
import type { AppRouterContext } from "./context";
import { RouterError, RouterNotFound, RouterPending } from "./router-states";
import { routeTree } from "./route-tree";
import { boardSearchSchema } from "./schemas/board-search-schema";
import { searchSearchSchema } from "./schemas/search-search-schema";

export function createRouter() {
  const router = createTanStackRouter({
    routeTree,
    context: undefined as unknown as AppRouterContext,
    defaultPreload: "intent",
    defaultPendingComponent: RouterPending,
    defaultErrorComponent: ({ reset }) => <RouterError reset={reset} />,
    defaultNotFoundComponent: RouterNotFound,
    scrollRestoration: true,
  });

  return router;
}

export const router = createRouter();
export { boardSearchSchema, searchSearchSchema };

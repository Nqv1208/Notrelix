import { describe, expect, it } from "vitest";
import { createMemoryHistory, createRouter } from "@tanstack/react-router";
import type { AppRouterContext } from "../router/context";
import { routeTree } from "../router/route-tree";

const ACCOUNT = "3f2a9c1e-7b64-4d0a-9e51-0c8f6a2b7d10";

// Top-level pages the web app already serves; `invite` and `onboarding` are also reserved addresses (Domain `WorkspaceSlugRules.Reserved`).
const PAGES = [
  "/home",
  "/sign-in",
  "/sign-up",
  "/forgot-password",
  "/reset-password",
  "/verify-email",
  "/invite",
  "/onboarding",
];

function match(path: string) {
  const router = createRouter({
    routeTree,
    context: undefined as unknown as AppRouterContext,
    history: createMemoryHistory({ initialEntries: [path] }),
  });
  return router.matchRoutes(router.state.location);
}

const leaf = (path: string) => match(path).at(-1)?.routeId;

describe("workspace address route", () => {
  it("opens an address with its Account id on the address route", () => {
    expect(leaf(`/acme-team?account=${ACCOUNT}`)).toBe("/$slug");
  });

  it("never shadows a product page, with or without an Account id", () => {
    for (const page of PAGES) {
      expect(leaf(page)).not.toBe("/$slug");
      expect(leaf(`${page}?account=${ACCOUNT}`)).not.toBe("/$slug");
    }
    expect(leaf("/invite/some-token")).toBe("/invite/$token");
    expect(leaf("/welcome/ws-1")).toBe("/welcome/$workspaceId");
    expect(leaf("/workspaces/ws-1")).toBe("/workspaces/$workspaceId/");
  });

  it("is not an address without a well-formed Account id: the router's own not-found page stays", async () => {
    for (const path of ["/acme-team", "/acme-team?account=nope"]) {
      const router = createRouter({
        routeTree,
        context: undefined as unknown as AppRouterContext,
        history: createMemoryHistory({ initialEntries: [path] }),
      });
      await router.load();
      expect(router.state.statusCode).toBe(404);
    }
  });
});

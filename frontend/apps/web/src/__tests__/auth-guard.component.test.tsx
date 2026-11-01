import { act } from "react";
import { createRoot, type Root } from "react-dom/client";
import { afterEach, describe, expect, it, vi } from "vitest";
import {
  RouterProvider,
  createMemoryHistory,
  createRootRoute,
  createRoute,
  createRouter,
} from "@tanstack/react-router";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { authQueryKeys, createAuthProvider } from "@notrelix/features-auth";
import { endpoints } from "@notrelix/contracts";
import { I18nProvider, type Locale } from "@notrelix/i18n";
import { AppError } from "@notrelix/kernel";
import { ThemeProvider } from "@notrelix/ui-web";
import { webCatalogs } from "../i18n/catalogs";
import { AuthGuard } from "../shell/guards/auth-guard";

(
  globalThis as { IS_REACT_ACT_ENVIRONMENT?: boolean }
).IS_REACT_ACT_ENVIRONMENT = true;

let root: Root | undefined;
let host: HTMLElement | undefined;

afterEach(() => {
  act(() => root?.unmount());
  host?.remove();
  root = undefined;
  host = undefined;
});

const person = { id: "u1", email: "a@b.vn", name: "An", avatarUrl: null };

const authRejected = () =>
  new AppError({
    kind: "auth",
    status: 401,
    message: "Session expired. Please sign in again.",
  });
const networkDown = () =>
  new AppError({ kind: "network", message: "Network error." });

async function mountAt(
  path: string,
  profile: () => Promise<unknown>,
  locale: Locale = "en",
) {
  const get = vi.fn(profile);
  const AuthProvider = createAuthProvider({
    api: { get: get as never, post: vi.fn() },
    endpoints,
  });
  const rootRoute = createRootRoute();
  const guarded = createRoute({
    getParentRoute: () => rootRoute,
    path: "/workspaces/$id",
    component: () => (
      <AuthGuard>
        <p>protected page</p>
      </AuthGuard>
    ),
  });
  const signIn = createRoute({
    getParentRoute: () => rootRoute,
    path: "/sign-in",
    component: () => <p>sign-in page</p>,
  });
  const queryClient = new QueryClient();
  const router = createRouter({
    routeTree: rootRoute.addChildren([guarded, signIn]),
    history: createMemoryHistory({ initialEntries: [path] }),
  });
  host = document.createElement("div");
  document.body.appendChild(host);
  root = createRoot(host);
  await act(async () => {
    root?.render(
      <QueryClientProvider client={queryClient}>
        <AuthProvider>
          <I18nProvider catalogs={webCatalogs} initialLocale={locale}>
            <ThemeProvider target={null}>
              <RouterProvider router={router} />
            </ThemeProvider>
          </I18nProvider>
        </AuthProvider>
      </QueryClientProvider>,
    );
  });
  await settle();
  return { router, get, queryClient };
}

const settle = () =>
  act(async () => {
    await new Promise((resolve) => setTimeout(resolve, 50));
  });

const text = () => host?.textContent ?? "";

describe("auth guard: reading the signed-in person", () => {
  it("announces the session check while it is pending", async () => {
    await mountAt("/workspaces/w1", () => new Promise(() => {}));
    const status = host?.querySelector('[role="status"]');
    expect(status?.textContent).toContain("Verifying session...");
    expect(text()).not.toContain("protected page");
  });

  it("opens the page once the person is read", async () => {
    await mountAt("/workspaces/w1", async () => person);
    expect(text()).toContain("protected page");
  });

  it("sends a rejected session to sign-in and remembers the page", async () => {
    const { router } = await mountAt("/workspaces/w1?tab=a", async () => {
      throw authRejected();
    });
    expect(router.state.location.pathname).toBe("/sign-in");
    expect(
      new URLSearchParams(router.state.location.searchStr).get("redirect"),
    ).toBe("/workspaces/w1?tab=a");
    expect(text()).not.toContain("protected page");
  });

  it("keeps the person on the page and offers a retry when the server cannot be reached", async () => {
    const reads = [
      () => Promise.reject(networkDown()),
      () => Promise.resolve(person),
    ];
    const { router, get } = await mountAt("/workspaces/w1", () => {
      const next = reads.shift();
      return next ? next() : Promise.resolve(person);
    });

    expect(router.state.location.pathname).toBe("/workspaces/w1");
    const alert = host?.querySelector('[role="alert"]');
    expect(alert?.textContent).toContain("Could not check your session");
    expect(alert?.textContent).toContain("still signed in");
    expect(text()).not.toContain("protected page");

    const retry = Array.from(host?.querySelectorAll("button") ?? []).find(
      (button) => button.textContent === "Try again",
    );
    expect(retry).toBeDefined();
    await act(async () => retry?.click());
    await settle();

    expect(get).toHaveBeenCalledTimes(2);
    expect(text()).toContain("protected page");
    expect(host?.querySelector('[role="alert"]')).toBeNull();
  });

  it("keeps a person who is already read on the page when a later read of the session fails", async () => {
    const reads = [
      () => Promise.resolve(person),
      () => Promise.reject(networkDown()),
    ];
    const { queryClient } = await mountAt("/workspaces/w1", () => {
      const next = reads.shift();
      return next ? next() : Promise.reject(networkDown());
    });
    expect(text()).toContain("protected page");

    await act(async () => {
      await queryClient.invalidateQueries({ queryKey: authQueryKeys.profile });
    });
    await settle();

    expect(text()).toContain("protected page");
    expect(host?.querySelector('[role="alert"]')).toBeNull();
  });

  it("says the retry screen in Vietnamese with no English fallback", async () => {
    await mountAt("/workspaces/w1", () => Promise.reject(networkDown()), "vi");
    expect(text()).toContain("Chưa kiểm tra được phiên đăng nhập");
    expect(text()).toContain("Thử lại");
    expect(text()).not.toMatch(/Could not check|Try again/);
  });
});

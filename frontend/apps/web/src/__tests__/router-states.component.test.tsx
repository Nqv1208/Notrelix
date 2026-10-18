import { act } from "react";
import { createRoot, type Root } from "react-dom/client";
import { afterEach, describe, expect, it } from "vitest";
import {
  RouterProvider,
  createMemoryHistory,
  createRootRoute,
  createRoute,
  createRouter,
} from "@tanstack/react-router";
import { I18nProvider, type Locale } from "@notrelix/i18n";
import { ThemeProvider } from "@notrelix/ui-web";
import { webCatalogs } from "../i18n/catalogs";
import { RouterError, RouterNotFound } from "../router/router-states";

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

function Boom(): never {
  throw new Error(
    "useWorkspaceContext must be used within a WorkspaceProvider",
  );
}

async function mountAt(path: string, locale: Locale) {
  const rootRoute = createRootRoute();
  const boom = createRoute({
    getParentRoute: () => rootRoute,
    path: "/boom",
    component: Boom,
  });
  const router = createRouter({
    routeTree: rootRoute.addChildren([boom]),
    history: createMemoryHistory({ initialEntries: [path] }),
    defaultErrorComponent: ({ reset }) => <RouterError reset={reset} />,
    defaultNotFoundComponent: RouterNotFound,
  });
  host = document.createElement("div");
  document.body.appendChild(host);
  root = createRoot(host);
  await act(async () => {
    root?.render(
      <I18nProvider catalogs={webCatalogs} initialLocale={locale}>
        <ThemeProvider target={null}>
          <RouterProvider router={router} />
        </ThemeProvider>
      </I18nProvider>,
    );
  });
  await act(async () => {
    await new Promise((resolve) => setTimeout(resolve, 50));
  });
}

const text = () => host?.textContent ?? "";

describe("router default screens", () => {
  it("says a missing page in Vietnamese, with no English fallback", async () => {
    await mountAt("/nope", "vi");
    expect(text()).toContain("Không tìm thấy trang");
    expect(text()).toContain("Về Trang chủ");
    expect(text()).not.toMatch(/Page not found|requested route/);
  });

  it("says a route error in Vietnamese and never shows the exception text", async () => {
    await mountAt("/boom", "vi");
    expect(text()).toContain("Không mở được trang này");
    expect(text()).toContain("Thử lại");
    expect(text()).not.toMatch(/Route error|useWorkspaceContext/);
  });

  it("follows the English locale", async () => {
    await mountAt("/nope", "en");
    expect(text()).toContain("Page not found");
    expect(text()).not.toContain("Không tìm thấy");
  });
});

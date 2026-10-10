import { expect, test } from "@playwright/test";

const scenario = process.env.VITE_MOCK_STATE ?? "default";

test("loads auth and workspace routes without backend network", async ({
  page,
}) => {
  const backendRequests: string[] = [];
  const backendSockets: string[] = [];
  page.on("request", (request) => {
    if (request.url().includes("127.0.0.1:59999"))
      backendRequests.push(request.url());
  });
  page.on("websocket", (socket) => {
    if (socket.url().includes("127.0.0.1:59998"))
      backendSockets.push(socket.url());
  });

  await page.goto("/home");

  // Network isolation holds regardless of scenario — zero backend escapes is always required
  expect(backendRequests).toEqual([]);
  expect(backendSockets).toEqual([]);

  if (scenario !== "default") {
    // Remaining assertions require default world data
    return;
  }

  await expect(page).toHaveURL(/\/home$/);
  const primaryNavigation = page.getByRole("navigation", {
    name: "Primary navigation",
  });
  await expect(primaryNavigation).toBeVisible();
  await expect(
    primaryNavigation.getByRole("link", { name: "Home" }),
  ).toBeVisible();
  await expect(
    primaryNavigation.getByRole("link", { name: "My work" }),
  ).toBeVisible();
  await expect(page.getByRole("button", { name: "Favorites" })).toBeVisible();
  await expect(
    page.getByRole("heading", { name: "Recently viewed" }),
  ).toBeVisible();
  await expect(
    page.getByRole("heading", { name: "Workspaces", exact: true }),
  ).toBeVisible();
  await expect(
    page.getByRole("textbox", { name: "Search home content" }),
  ).toBeVisible();
  await expect(
    page.getByRole("heading", { name: "Continue working" }),
  ).toBeVisible();
  await expect(page.getByRole("heading", { name: "My work" })).toBeVisible();
  await expect(page.getByRole("heading", { name: "Activity" })).toBeVisible();
  await expect(
    page.getByRole("heading", { name: "Your workspaces" }),
  ).toBeVisible();
  await expect(
    page.getByRole("button", { name: /Notrelix Product Lab/ }).first(),
  ).toBeVisible();
  await page.reload();
  await expect(
    page.getByRole("heading", { name: "Your workspaces" }),
  ).toBeVisible();
  await expect(page).not.toHaveURL(/sign-in/);

  await page.getByRole("button", { name: "User settings" }).click();
  const userMenu = page.getByRole("menu");
  await expect(userMenu).toBeVisible();
  await expect(
    userMenu.getByRole("menuitem", { name: "My profile" }),
  ).toBeEnabled();
  await expect(
    userMenu.getByRole("menuitem", { name: "Notifications" }),
  ).toBeEnabled();
  await expect(
    userMenu.getByRole("menuitem", { name: "Appearance" }),
  ).toBeEnabled();
  await expect(
    userMenu.getByRole("button", { name: "Light theme" }),
  ).toBeVisible();
  await expect(
    userMenu.getByRole("button", { name: "Dark theme" }),
  ).toBeVisible();
  await expect(
    userMenu.getByRole("button", { name: "System theme" }),
  ).toBeVisible();
  await expect(
    userMenu.getByRole("menuitem", { name: "Log out" }),
  ).toBeEnabled();
  await userMenu.getByRole("menuitem", { name: "My profile" }).click();
  await expect(page).toHaveURL(
    /\/workspaces\/mock-workspace-primary\/account\/profile$/,
  );

  await page.goto("/home");
  await page.getByRole("button", { name: "User settings" }).click();
  await page
    .getByRole("menu")
    .getByRole("button", { name: "Dark theme" })
    .click();
  await expect(page.locator("html")).toHaveClass(/dark/);

  await page.goto("/workspaces/mock-workspace-primary/dashboard");
  await expect(page.locator("[data-home-sidebar]")).toHaveCount(0);
  await expect(
    page.getByRole("complementary", { name: "Workspace navigation" }),
  ).toBeVisible();
  await expect(
    page.getByRole("button", { name: "Search workspace" }),
  ).toBeVisible();
  await expect(page.getByRole("button", { name: "Team online" })).toBeVisible();
  await expect(
    page.getByRole("heading", { name: "Active Boards" }),
  ).toBeVisible();

  await page.goto("/home");
  await page.getByRole("button", { name: "User settings" }).click();
  await page
    .getByRole("menu")
    .getByRole("menuitem", { name: "Log out" })
    .click();
  await expect(page).toHaveURL(/\/sign-in/);

  // Verify no backend escapes after full flow
  expect(backendRequests).toEqual([]);
  expect(backendSockets).toEqual([]);
});

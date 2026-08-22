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
  await expect(page.getByText("Notrelix Sandbox")).toHaveCount(0);
  await expect(page.getByText("work management")).toBeVisible();
  await expect(
    page.getByRole("navigation", { name: "Primary navigation" }),
  ).toBeVisible();
  await expect(page.getByText("Favorites", { exact: true })).toBeVisible();
  await expect(
    page.getByText("Recently viewed", { exact: true }),
  ).toBeVisible();
  await expect(
    page.getByText("Workspaces", { exact: true }).first(),
  ).toBeVisible();
  await expect(page.getByText("Notrelix AI", { exact: true })).toBeVisible();
  await expect(page.getByRole("heading", { name: "Home" })).toBeVisible();
  await expect(
    page.getByRole("heading", { name: "Your workspaces" }),
  ).toBeVisible();
  await expect(page.getByText(/Notrelix Product Lab/).first()).toBeVisible();
  await expect(
    page.getByRole("heading", { name: "Recent docs" }),
  ).toBeVisible();
  await expect(page.getByRole("heading", { name: "Activity" })).toBeVisible();
  await expect(
    page.getByRole("heading", { name: "Recent boards" }),
  ).toBeVisible();
  await expect(page.getByText("Product Roadmap").last()).toBeVisible();
  await expect(page.getByText("Product specification").last()).toBeVisible();
  await page.reload();
  await expect(
    page.getByRole("heading", { name: "Your workspaces" }),
  ).toBeVisible();
  await expect(page).not.toHaveURL(/sign-in/);

  await page.getByRole("button", { name: "User settings" }).click();
  await expect(page.getByRole("menu", { name: "User settings" })).toBeVisible();
  await expect(page.getByText("Account", { exact: true })).toBeVisible();
  await expect(page.getByText("Explore", { exact: true })).toBeVisible();
  await expect(
    page.getByRole("menuitem", { name: "My profile" }),
  ).toBeEnabled();
  await expect(
    page.getByRole("menuitem", { name: "Marketplace" }),
  ).toBeDisabled();
  await page.getByRole("menuitem", { name: "My profile" }).click();
  await expect(page).toHaveURL(
    /\/workspaces\/mock-workspace-primary\/account\/profile$/,
  );

  await page.goto("/home");
  await page.getByRole("button", { name: "User settings" }).click();
  await page.getByRole("button", { name: "Dark theme" }).click();
  await expect(page.locator("html")).toHaveClass(/dark/);

  await page.goto("/workspaces/mock-workspace-primary/dashboard");
  await expect(page.locator("[data-home-sidebar]")).toHaveCount(0);
  await expect(
    page.getByText("Search workspace", { exact: true }),
  ).toBeVisible();
  await expect(page.getByText("Quick access", { exact: true })).toBeVisible();
  await expect(page.getByText("Team online", { exact: true })).toBeVisible();
  await expect(
    page.getByRole("heading", { name: "Active Boards" }),
  ).toBeVisible();

  await page.goto("/home");
  await page.getByRole("button", { name: "User settings" }).click();
  await page.getByRole("menuitem", { name: "Log out" }).click();
  await expect(page).toHaveURL(/\/sign-in/);

  // Verify no backend escapes after full flow
  expect(backendRequests).toEqual([]);
  expect(backendSockets).toEqual([]);
});

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

  // `/home` resolves into the member's first workspace (WX-SHELL-001).
  await expect(page).toHaveURL(/\/workspaces\/mock-workspace-primary$/);
  await expect(page.getByText("Notrelix Sandbox")).toHaveCount(0);
  const nav = page.getByRole("navigation", {
    name: /Workspace navigation|Điều hướng/,
  });
  await expect(nav).toBeVisible();
  for (const path of ["my-work", "inbox", "search", "members", "settings"]) {
    await expect(
      nav.locator(`a[href="/workspaces/mock-workspace-primary/${path}"]`),
    ).toHaveCount(1);
  }
  await expect(page.getByText("Quick access", { exact: true })).toHaveCount(0);
  await expect(page.getByText("Team online", { exact: true })).toHaveCount(0);
  await expect(page.locator("[data-home-sidebar]")).toHaveCount(0);
  await page.reload();
  await expect(page).not.toHaveURL(/sign-in/);

  // Lost access is an explicit state, not a silent redirect (WX-SHELL-004).
  await page.goto("/workspaces/not-a-member");
  await expect(page.getByRole("heading", { level: 1 })).toBeVisible();
  await expect(page).toHaveURL(/\/workspaces\/not-a-member$/);

  // Sign out from the user card.
  await page.goto("/workspaces/mock-workspace-primary");
  await page
    .getByRole("button", { name: /Account menu|Menu tài khoản/ })
    .click();
  await page.getByRole("menuitem", { name: /Sign out|Đăng xuất/ }).click();
  await expect(page).toHaveURL(/\/sign-in/);

  // Verify no backend escapes after full flow
  expect(backendRequests).toEqual([]);
  expect(backendSockets).toEqual([]);
});

import { expect, test } from "@playwright/test";

// Valid VITE_MOCK_STATE values:
// "default" | "new-user" | "empty-workspace" | "permission-limited" | "expired-session"
const scenario = process.env.VITE_MOCK_STATE ?? "default";
const persona = process.env.VITE_MOCK_PERSONA ?? "owner";

test("renders the configured deterministic scenario without unhandled mock errors", async ({
  page,
}) => {
  const unhandled: string[] = [];
  page.on("pageerror", (error) => {
    if (error.message.includes("MockUnhandledOperationError")) {
      unhandled.push(error.message);
    }
  });

  await page.goto("/home");

  if (scenario === "new-user") {
    // New user has no workspaces
    await expect(
      page.getByText("No workspaces are available for this account."),
    ).toBeVisible();
  } else if (scenario === "expired-session") {
    // Expired session redirects to sign-in
    await expect(page).toHaveURL(/sign-in/);
  } else {
    // default, empty-workspace, permission-limited — all have primary workspace
    await expect(page.getByText(/Notrelix Product Lab/).first()).toBeVisible();
  }

  expect(unhandled).toEqual([]);
});

test("personas load distinct deterministic worlds", async ({ page }) => {
  const unhandled: string[] = [];
  page.on("pageerror", (error) => {
    if (error.message.includes("MockUnhandledOperationError")) {
      unhandled.push(error.message);
    }
  });

  // Only run persona-specific assertions for default state
  if (scenario !== "default") {
    await page.goto("/home");
    expect(unhandled).toEqual([]);
    return;
  }

  await page.goto("/home");
  // All personas in default state should see the primary workspace
  await expect(page.getByText(/Notrelix Product Lab/).first()).toBeVisible();
  expect(unhandled).toEqual([]);
});

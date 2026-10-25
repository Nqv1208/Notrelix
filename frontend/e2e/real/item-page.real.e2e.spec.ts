import { expect, test } from "@playwright/test";
import { openBoard, realUsers, signIn } from "./support/real-session";

test.use({ viewport: { width: 1440, height: 900 } });

test("an item page loads from the backend without calling a route the backend does not have", async ({
  page,
}) => {
  await signIn(page, realUsers.workspaceAdmin);
  const { workspace, board } = await openBoard(
    page,
    "Workspace 3",
    "Engineering Tasks",
  );
  await page
    .getByRole("button", { name: /Task \d+/ })
    .first()
    .click();
  await expect(page).toHaveURL(/[?&]item=/);
  const itemId = new URL(page.url()).searchParams.get("item")!;

  const missing: string[] = [];
  page.on("response", (response) => {
    if (response.status() === 404)
      missing.push(`${response.request().method()} ${response.url()}`);
  });
  await page.goto(
    `/workspaces/${workspace.id}/boards/${board.id}/items/${itemId}`,
  );
  await expect(page.getByRole("heading", { level: 1 })).toBeVisible();
  await page.waitForLoadState("networkidle");
  expect(missing).toEqual([]);
});

import { expect, test, type Page } from "@playwright/test";
import { openBoard, realUsers, signIn } from "./support/real-session";

test.use({ viewport: { width: 1440, height: 900 } });

function watchErrors(page: Page): string[] {
  const errors: string[] = [];
  page.on("console", (message) => {
    if (message.type() === "error") errors.push(message.text());
  });
  page.on("pageerror", (error) => errors.push(error.message));
  return errors;
}

test("a workspace admin reads the work report of all boards, then of one board", async ({
  page,
}) => {
  await signIn(page, realUsers.workspaceAdmin);
  const { workspace } = await openBoard(
    page,
    "Workspace 3",
    "Engineering Tasks",
  );
  const errors = watchErrors(page);

  await page.goto(`/workspaces/${workspace.id}/reports`);
  await expect(
    page.getByRole("heading", { level: 2, name: "Reports" }),
  ).toBeVisible();
  await expect(
    page.getByRole("region", { name: "Items by status" }),
  ).toBeVisible();
  await expect(
    page.getByRole("region", { name: "Finished per week, last 8 weeks" }),
  ).toBeVisible();

  await page.getByLabel("Board").selectOption({ label: "Engineering Tasks" });
  await expect(
    page.getByRole("region", { name: "Items by assignee" }),
  ).toBeVisible();

  expect(errors).toEqual([]);
});

test("a guest has no department filter on the report", async ({ page }) => {
  await signIn(page, realUsers.workspaceGuest);
  const workspaceId = /\/workspaces\/([^/]+)/.exec(page.url())?.[1];
  await page.goto(`/workspaces/${workspaceId}/reports`);

  await expect(
    page.getByRole("heading", { level: 2, name: "Reports" }),
  ).toBeVisible();
  await expect(page.getByLabel("Department")).toHaveCount(0);
});

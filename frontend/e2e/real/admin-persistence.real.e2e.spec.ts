import { expect, test } from "@playwright/test";
import {
  addItemToLane,
  openBoard,
  realUsers,
  showAllInLane,
  signIn,
} from "./support/real-session";

test("admin creates and renames an item that remains after a real backend reload", async ({
  page,
}) => {
  await signIn(page, realUsers.admin);
  await openBoard(page, "Workspace 2", "Bug Tracker");

  const title = `E2E persisted item ${Date.now()}`;
  expect(await addItemToLane(page, "Backlog", title)).toBeLessThan(300);
  await showAllInLane(page, "Backlog");
  await expect(page.getByRole("button", { name: title })).toBeVisible();

  await page.getByRole("button", { name: title }).click();
  const renamed = `${title} renamed`;
  const edit = page.getByRole("textbox", { name: "Edit title" });
  const saved = page.waitForResponse(
    (response) =>
      response.request().method() === "PATCH" &&
      /\/board-items\/[^/]+$/.test(response.url()),
  );
  await edit.fill(renamed);
  await edit.press("Enter");
  expect((await saved).status()).toBeLessThan(300);

  await page.reload();
  await showAllInLane(page, "Backlog");
  await expect(page.getByRole("button", { name: renamed })).toBeVisible();
});

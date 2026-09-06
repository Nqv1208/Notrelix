import { expect, test } from "@playwright/test";
import {
  openBoard,
  openWorkspace,
  realUsers,
  signIn,
} from "./support/real-session";

test("workspace guest cannot persist a new board item", async ({ page }) => {
  await signIn(page, realUsers.guest);
  await openWorkspace(page, "Workspace 1");
  await openBoard(page, /^Product Roadmap \(/);

  await page.getByRole("button", { name: "Add card to Backlog" }).click();
  await page
    .getByLabel("New card title for Backlog")
    .fill("E2E forbidden guest card");
  const deniedResponse = page.waitForResponse(
    (response) =>
      response.request().method() === "POST" &&
      /\/boards\/[^/]+\/items$/.test(response.url()),
  );
  await page.getByRole("button", { name: "Add card", exact: true }).click();
  await expect((await deniedResponse).status()).toBe(403);

  await page.reload();
  await expect(
    page.getByRole("button", { name: "E2E forbidden guest card" }),
  ).toHaveCount(0);
});

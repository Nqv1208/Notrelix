import { expect, test } from "@playwright/test";
import {
  openBoard,
  openWorkspace,
  realUsers,
  signIn,
} from "./support/real-session";

test("admin creates a card that remains after a real backend reload", async ({
  page,
}) => {
  await signIn(page, realUsers.admin);
  await openWorkspace(page, "Workspace 1");
  await openBoard(page, /^Product Roadmap \(/);

  const backlogButton = page.getByRole("button", {
    name: "Add card to Backlog",
  });
  await backlogButton.click();
  await page
    .getByLabel("New card title for Backlog")
    .fill("E2E persisted card");
  const createResponse = page.waitForResponse(
    (response) =>
      response.request().method() === "POST" &&
      /\/boards\/[^/]+\/items$/.test(response.url()),
  );
  await page.getByRole("button", { name: "Add card", exact: true }).click();
  await expect((await createResponse).status()).toBeLessThan(300);
  await expect(
    page.getByRole("button", { name: "E2E persisted card" }),
  ).toBeVisible();

  await page.reload();
  await expect(
    page.getByRole("button", { name: "E2E persisted card" }),
  ).toBeVisible();
});

import { expect, test } from "@playwright/test";
import {
  findWorkspace,
  openBoard,
  realUsers,
  signIn,
} from "./support/real-session";

test.use({ viewport: { width: 1440, height: 900 } });
test.setTimeout(120_000);

test("a member mentioned in a comment finds it in the inbox and marks it read", async ({
  browser,
  page,
}) => {
  // A manager of Workspace 3 mentions the admin in a comment on an item.
  await signIn(page, realUsers.workspaceAdmin);
  await openBoard(page, "Workspace 3", "Engineering Tasks");
  await page
    .getByRole("button", { name: /Task \d+/ })
    .first()
    .click();
  // The item has no page of its own: the card popup that opened is where the comment is written.
  await expect(page).toHaveURL(/[?&]item=/);
  const card = page.getByRole("dialog");
  // The composer opens from its one-line prompt.
  await card.getByRole("button", { name: "Write a comment…" }).click();
  const composer = card.getByRole("textbox", { name: "Write a comment" });
  await composer.fill("Please look at this ");
  await card.getByRole("button", { name: "Mention someone" }).click();
  await expect(
    page.getByRole("option", { name: "admin", exact: true }),
  ).toBeVisible();
  await page.keyboard.press("Enter");
  const commented = page.waitForResponse(
    (response) =>
      response.request().method() === "POST" &&
      /\/board-items\/[^/]+\/comments$/.test(response.url()),
  );
  await card.getByRole("button", { name: "Comment", exact: true }).click();
  expect((await commented).status()).toBeLessThan(300);

  // The admin sees the mention in the inbox once the notification is delivered.
  const context = await browser.newContext();
  const reader = await context.newPage();
  await signIn(reader, realUsers.admin);
  const readerWorkspace = await findWorkspace(reader, "Workspace 3");
  const listed = (page: typeof reader) =>
    page.waitForResponse(
      (response) =>
        response.request().method() === "GET" &&
        /\/workspaces\/[^/]+\/inbox\?/.test(response.url()),
    );
  await expect(async () => {
    const answered = listed(reader);
    await reader.goto(`/workspaces/${readerWorkspace.id}/inbox`);
    expect((await answered).status()).toBe(200);
    await expect(
      reader.getByRole("heading", { level: 1, name: "Inbox" }),
    ).toBeVisible();
    await expect(reader.getByText("User 4").first()).toBeVisible({
      timeout: 2_000,
    });
  }).toPass({ timeout: 60_000 });

  const read = reader.waitForResponse(
    (response) =>
      response.request().method() === "POST" &&
      /\/inbox\/[^/]+\/read$/.test(response.url()),
  );
  await reader
    .getByRole("button", { name: /^Mark as read/ })
    .first()
    .click();
  expect((await read).status()).toBeLessThan(300);
  await context.close();
});

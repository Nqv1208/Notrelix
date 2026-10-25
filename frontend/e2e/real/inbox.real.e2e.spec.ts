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
  await page.goto(
    `/workspaces/${workspace.id}/boards/${board.id}/items/${itemId}`,
  );
  const composer = page.getByRole("textbox", { name: "Write a comment" });
  await composer.fill("Please look at this ");
  await page.getByRole("button", { name: "Mention someone" }).click();
  await page.getByRole("option", { name: "admin", exact: true }).click();
  const commented = page.waitForResponse(
    (response) =>
      response.request().method() === "POST" &&
      /\/board-items\/[^/]+\/comments$/.test(response.url()),
  );
  await page.getByRole("button", { name: "Send", exact: true }).click();
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
    .getByRole("button", { name: /^Handled/ })
    .first()
    .click();
  expect((await read).status()).toBeLessThan(300);
  await context.close();
});

import { expect, test, type Page } from "@playwright/test";
import {
  findWorkspace,
  openBoard,
  realUsers,
  signIn,
} from "./support/real-session";

test.use({ viewport: { width: 1440, height: 900 } });
test.setTimeout(180_000);

const postedTo = (page: Page, suffix: RegExp) =>
  page.waitForResponse(
    (response) =>
      response.request().method() === "POST" && suffix.test(response.url()),
  );

test("a mention can be marked done, found under Done, moved back, snoozed and muted", async ({
  browser,
  page,
}) => {
  // A manager mentions the admin in a comment on an item.
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
  await composer.fill("States check ");
  await page.getByRole("button", { name: "Mention someone" }).click();
  await page.getByRole("option", { name: "admin", exact: true }).click();
  const commented = postedTo(page, /\/board-items\/[^/]+\/comments$/);
  await page.getByRole("button", { name: "Send", exact: true }).click();
  expect((await commented).status()).toBeLessThan(300);

  const context = await browser.newContext();
  const reader = await context.newPage();
  await signIn(reader, realUsers.admin);
  const readerWorkspace = await findWorkspace(reader, "Workspace 3");
  await expect(async () => {
    await reader.goto(`/workspaces/${readerWorkspace.id}/inbox`);
    await expect(
      reader.getByRole("heading", { level: 1, name: "Inbox" }),
    ).toBeVisible();
    await expect(reader.getByText("States check").first()).toBeVisible({
      timeout: 2_000,
    });
  }).toPass({ timeout: 60_000 });

  // The mention carries a plain-text excerpt and needs a reply.
  await expect(reader.getByText("Needs a reply").first()).toBeVisible();

  // Mute then unmute the thread: the button label follows the state.
  const muted = postedTo(reader, /\/inbox\/[^/]+\/mute$/);
  await reader
    .getByRole("button", { name: "Mute notifications for this item" })
    .click();
  expect((await muted).status()).toBeLessThan(300);
  const unmuted = postedTo(reader, /\/inbox\/[^/]+\/unmute$/);
  await reader
    .getByRole("button", {
      name: "Turn notifications back on for this item",
    })
    .click();
  expect((await unmuted).status()).toBeLessThan(300);

  // Done leaves the default list and shows under the Done filter.
  const done = postedTo(reader, /\/inbox\/[^/]+\/done$/);
  await reader.getByRole("button", { name: "Mark as done" }).click();
  expect((await done).status()).toBeLessThan(300);
  await expect(reader.getByText("States check")).toHaveCount(0);
  await reader.getByRole("button", { name: "Done", exact: true }).click();
  await expect(reader.getByText("States check").first()).toBeVisible();

  // Move it back into the inbox.
  const reopened = postedTo(reader, /\/inbox\/[^/]+\/reopen$/);
  await reader.getByRole("button", { name: "Move back to inbox" }).click();
  expect((await reopened).status()).toBeLessThan(300);
  await reader.getByRole("button", { name: "Done", exact: true }).click();
  await expect(reader.getByText("States check").first()).toBeVisible();

  // Snoozing hides it from the default list.
  const snoozed = postedTo(reader, /\/inbox\/[^/]+\/snooze$/);
  await reader.getByRole("button", { name: /Remind me later/ }).click();
  await reader.getByRole("menuitem", { name: "In 1 hour" }).click();
  expect((await snoozed).status()).toBeLessThan(300);
  await expect(reader.getByText("States check")).toHaveCount(0);
  await context.close();
});

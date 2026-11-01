import { expect, test } from "@playwright/test";
import {
  apiRead,
  apiWrite,
  openBoard,
  realUsers,
  signIn,
} from "./support/real-session";

test.use({ viewport: { width: 1440, height: 900 } });

/**
 * The card popup's column picker on the real stack. The board is grouped by Status, so the picker is the
 * Status: picking a lane writes the Status (the same write a drag between lanes makes), the picker is busy
 * while the write is out, and the card is in the new lane after a reload. A member who may not edit sees the
 * column as a label.
 */
test("a member moves a card to another column from its popup and the move survives a reload", async ({
  page,
}) => {
  await signIn(page, realUsers.workspaceAdmin);
  const { workspace, board } = await openBoard(
    page,
    "Workspace 3",
    "Engineering Tasks",
  );
  const unique = Date.now().toString(36);
  const created = await apiWrite<{ id: string }>(
    page,
    "POST",
    `/boards/${board.id}/items`,
    {
      groupId: (
        await apiRead<{ lists: { id: string }[] }>(
          page,
          `/boards/${board.id}/full`,
        )
      ).lists[0]!.id,
      title: `Move ${unique}`,
    },
  );
  expect(created.status).toBeLessThan(300);
  const itemId = created.data!.id;

  await page.goto(
    `/workspaces/${workspace.id}/boards/${board.id}?item=${itemId}`,
  );
  const popup = page.getByRole("dialog");
  const picker = popup.locator('button[aria-haspopup="listbox"]').first();
  await expect(picker).toBeVisible();
  const before = (await picker.innerText()).trim();

  await picker.click();
  const list = page.getByRole("listbox", { name: "Move to column" });
  const options = await list.getByRole("option").allInnerTexts();
  expect(options.length).toBeGreaterThan(1);
  const target = options.find((name) => name.trim() !== before)!.trim();
  await expect(
    list.getByRole("option", { name: before, exact: true }),
  ).toHaveAttribute("aria-selected", "true");

  const written = page.waitForResponse(
    (response) =>
      response.request().method() === "PATCH" &&
      response.url().includes(`/board-items/${itemId}/status`),
  );
  await list.getByRole("option", { name: target, exact: true }).click();
  expect((await written).status()).toBeLessThan(300);
  await expect(picker).toContainText(target);
  await expect(picker).toHaveAttribute("aria-busy", "false");

  await page.reload();
  await expect(
    page.getByRole("dialog").locator('button[aria-haspopup="listbox"]').first(),
  ).toContainText(target);
});

import { expect, test } from "@playwright/test";
import {
  findWorkspace,
  openBoard,
  realUsers,
  signIn,
} from "./support/real-session";

test("workspace transition does not retain the previous workspace board", async ({
  page,
}) => {
  await signIn(page, realUsers.admin);
  const { board } = await openBoard(page, "Workspace 2", "Bug Tracker");
  const other = await findWorkspace(page, "Workspace 3");

  await page
    .getByRole("button", { name: /open the Notrelix system rail/ })
    .click();
  await page.getByRole("button", { name: /^Workspace 3(,|$)/ }).click();

  await expect(page).toHaveURL(new RegExp(`/workspaces/${other.id}`));
  await expect(page).not.toHaveURL(new RegExp(board.id));
  await expect(
    page.getByRole("heading", { level: 1, name: board.title }),
  ).toHaveCount(0);
});

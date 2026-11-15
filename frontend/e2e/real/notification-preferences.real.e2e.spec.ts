import { expect, test } from "@playwright/test";
import { freshMember } from "./support/fresh-person";
import { apiRead } from "./support/real-session";
import { createOwnedWorkspace } from "./support/real-workspace";

interface Matrix {
  version: number;
  rows: { type: string; inApp: boolean; email: boolean }[];
}

const mentionInApp = /^(Mentions|Nhắc tên): (In the app|Trong ứng dụng)$/;

test("a person's notification choices are read, saved once and the same in every workspace", async ({
  page,
}) => {
  const member = await freshMember(page, "prefs");

  // Read: a person who never saved sees the defaults at version 0 on the account route.
  const first = await apiRead<Matrix>(
    page,
    "/profile/notification-preferences",
  );
  expect(first.version).toBe(0);
  expect(first.rows.find((row) => row.type === "mention")?.inApp).toBe(true);

  await page.goto(`/workspaces/${member.workspaceId}/account/notifications`);
  const mention = page.getByRole("checkbox", { name: mentionInApp });
  await expect(mention).toBeChecked();

  // Save: switch the mention in-app cell off through the page.
  await mention.click();
  await page
    .getByRole("button", { name: /Save preferences|Lưu tùy chọn/ })
    .click();
  await expect
    .poll(
      async () =>
        (
          await apiRead<Matrix>(page, "/profile/notification-preferences")
        ).rows.find((row) => row.type === "mention")?.inApp,
    )
    .toBe(false);

  // The choice survives a reload ...
  await page.reload();
  await expect(
    page.getByRole("checkbox", { name: mentionInApp }),
  ).not.toBeChecked();

  // ... and it is the same matrix in a second workspace of the same person.
  const second = await createOwnedWorkspace(page, "prefs-two");
  await page.goto(`/workspaces/${second.id}/account/notifications`);
  await expect(
    page.getByRole("checkbox", { name: mentionInApp }),
  ).not.toBeChecked();
  const saved = await apiRead<Matrix>(
    page,
    "/profile/notification-preferences",
  );
  expect(saved.version).toBe(1);
});

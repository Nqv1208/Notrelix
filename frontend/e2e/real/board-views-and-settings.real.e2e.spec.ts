import { expect, test } from "@playwright/test";
import {
  apiRead,
  apiWrite,
  openBoard,
  realUsers,
  signIn,
} from "./support/real-session";

test.use({ viewport: { width: 1440, height: 900 } });

interface FullBoard {
  lists: { id: string; title: string }[];
}

test("Calendar and Timeline draw the board's dated items without asking for plan items", async ({
  page,
}) => {
  const planRequests: string[] = [];
  page.on("request", (request) => {
    if (request.url().includes("/plan-items")) planRequests.push(request.url());
  });
  await signIn(page, realUsers.workspaceAdmin);
  const { workspace, board } = await openBoard(
    page,
    "Workspace 3",
    "Engineering Tasks",
  );
  const title = `Dated ${Date.now().toString(36)}`;
  const full = await apiRead<FullBoard>(page, `/boards/${board.id}/full`);
  const created = await apiWrite<{ id: string }>(
    page,
    "POST",
    `/boards/${board.id}/items`,
    { groupId: full.lists[0]!.id, title },
  );
  expect(created.status).toBeLessThan(300);
  const itemId = created.data!.id;
  const start = new Date();
  start.setUTCHours(9, 0, 0, 0);
  const due = new Date(start.getTime() + 2 * 86_400_000);
  const dated = await apiWrite(
    page,
    "PATCH",
    `/board-items/${itemId}/due-date`,
    {
      startDate: start.toISOString(),
      dueDate: due.toISOString(),
    },
  );
  expect(dated.status).toBeLessThan(300);

  for (const view of ["calendar", "timeline"]) {
    await page.goto(
      `/workspaces/${workspace.id}/boards/${board.id}?view=${view}`,
    );
    await expect(page.getByText(title).first()).toBeVisible();
    await expect(page.getByText(/couldn.t load/i)).toHaveCount(0);
  }
  expect(planRequests).toHaveLength(0);
});

test("Data fields list a readable key and how many items hold a value", async ({
  page,
}) => {
  await signIn(page, realUsers.workspaceAdmin);
  const { workspace, board } = await openBoard(
    page,
    "Workspace 3",
    "Engineering Tasks",
  );
  await page.goto(`/workspaces/${workspace.id}/boards/${board.id}/settings`);
  await page.getByRole("button", { name: /^Data fields/ }).click();
  await expect(page.getByText("status", { exact: true }).first()).toBeVisible();
  await expect(
    page.getByText(/^\d+ items? ha(s|ve) a value$/).first(),
  ).toBeVisible();
});

test("a member who does not manage the board never asks for its automations", async ({
  page,
}) => {
  const automationCalls: string[] = [];
  page.on("request", (request) => {
    if (/\/automations\b/.test(request.url())) {
      automationCalls.push(request.url());
    }
  });
  await signIn(page, realUsers.admin);
  const { workspace, board } = await openBoard(
    page,
    "Workspace 2",
    "Bug Tracker",
  );
  await page.goto(`/workspaces/${workspace.id}/boards/${board.id}/settings`);
  await expect(page.getByText(/Only board managers/)).toBeVisible();
  expect(automationCalls).toHaveLength(0);
});

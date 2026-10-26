import { expect, test } from "@playwright/test";
import { findWorkspace, realUsers, signIn } from "./support/real-session";

const apiUrl = process.env.VITE_API_URL ?? "http://127.0.0.1:58000/api/v1";

test("a workspace guest sees no board that was not shared and cannot read one", async ({
  browser,
  page,
}) => {
  // The admin is a Member of Workspace 2 and reads its boards; the guest is a Guest of it.
  const adminContext = await browser.newContext();
  const adminPage = await adminContext.newPage();
  await signIn(adminPage, realUsers.admin);
  const adminWorkspace = await findWorkspace(adminPage, "Workspace 2");
  const boards = (await (
    await adminPage.request.get(
      `${apiUrl}/workspaces/${adminWorkspace.id}/boards`,
    )
  ).json()) as { id: string }[];
  expect(boards.length).toBeGreaterThan(0);
  await adminContext.close();

  await signIn(page, realUsers.guest);
  const workspace = await findWorkspace(page, "Workspace 2");
  expect(workspace.id).toBe(adminWorkspace.id);

  const listed = await page.request.get(
    `${apiUrl}/workspaces/${workspace.id}/boards`,
  );
  expect(await listed.json()).toEqual([]);

  const read = await page.request.get(`${apiUrl}/boards/${boards[0].id}`);
  expect([403, 404]).toContain(read.status());
});

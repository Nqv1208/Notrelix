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
 * The people a Guest's pickers read (assignee, Person field, mention): only people on the granted board, and no
 * email. A Guest capped at Viewer sees the Person column read-only, and a request for the workspace's members is
 * refused.
 */
test("a workspace guest's pickers list only the people on the board, with no email, and the workspace members stay closed", async ({
  browser,
  page,
}) => {
  await signIn(page, realUsers.workspaceAdmin);
  const { workspace, board } = await openBoard(
    page,
    "Workspace 3",
    "Engineering Tasks",
  );
  const guestContext = await browser.newContext();
  const guest = await guestContext.newPage();
  await signIn(guest, realUsers.workspaceGuest);
  const me = await apiRead<{ id: string }>(guest, "/auth/me");
  const grant = await apiWrite(
    page,
    "POST",
    `/resources/work-management.board/${board.id}/permissions`,
    { subjectType: "User", subjectId: me.id, level: "Viewer" },
  );
  expect(grant.status).toBeLessThan(300);
  try {
    const people = await apiRead<
      { userId: string; name: string; email?: unknown }[]
    >(guest, `/boards/${board.id}/people`);
    expect(Array.isArray(people)).toBe(true);
    expect(people.length).toBeGreaterThan(0);
    // No email, and nobody the board does not name: the manager's view of the same board is the upper bound.
    expect(JSON.stringify(people)).not.toMatch(/@/);
    const boardPeople = await apiRead<{ userId: string }[]>(
      page,
      `/boards/${board.id}/people`,
    );
    const allowed = new Set(boardPeople.map((entry) => entry.userId));
    for (const entry of people) expect(allowed.has(entry.userId)).toBe(true);

    // The workspace's member list is not a Guest's to read.
    const members = await guest.request.get(
      `${process.env.VITE_API_URL ?? "http://127.0.0.1:58000/api/v1"}/workspaces/${workspace.id}/members`,
    );
    expect(members.status()).toBe(403);
  } finally {
    const rows = await apiRead<
      { id: string; subjectId: string; isRevoked: boolean }[]
    >(page, `/resources/work-management.board/${board.id}/permissions`);
    for (const row of rows.filter(
      (entry) => entry.subjectId === me.id && !entry.isRevoked,
    ))
      await apiWrite(
        page,
        "DELETE",
        `/resources/work-management.board/${board.id}/permissions/${row.id}`,
      );
    await guestContext.close();
  }
});

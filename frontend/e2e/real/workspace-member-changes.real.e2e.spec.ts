import { expect, test, type Page } from "@playwright/test";
import { apiRead, apiWrite, realUsers, signIn } from "./support/real-session";
import {
  createOwnedWorkspace,
  joinAs,
  membersOf,
  type MemberRow,
} from "./support/real-workspace";

test.setTimeout(180_000);

const apiUrl = process.env.VITE_API_URL ?? "http://127.0.0.1:58000/api/v1";

async function rowOf(page: Page, workspaceId: string, userId: string) {
  const row = (await membersOf(page, workspaceId)).find(
    (member) => member.userId === userId,
  );
  expect(row, `member ${userId}`).toBeDefined();
  return row as MemberRow;
}

async function meOf(page: Page, workspaceId: string) {
  return apiRead<MemberRow>(page, `/workspaces/${workspaceId}/members/me`);
}

/** The owner's workspace with two Members (user4 and user6) who have answered their invitations. */
async function scene(
  browser: Parameters<typeof joinAs>[0],
  page: Page,
  label: string,
) {
  await signIn(page, realUsers.admin);
  const workspace = await createOwnedWorkspace(page, label);
  const first = await joinAs(
    browser,
    page,
    workspace.id,
    realUsers.workspaceAdmin,
    "Member",
  );
  const second = await joinAs(
    browser,
    page,
    workspace.id,
    realUsers.workspaceGuest,
    "Member",
  );
  const owner = await meOf(page, workspace.id);
  return {
    workspace,
    owner,
    first: { ...first, me: await meOf(first.page, workspace.id) },
    second: { ...second, me: await meOf(second.page, workspace.id) },
  };
}

test("an Owner changes a member's role from the roster; a stale write, a Member and the last Owner change nothing", async ({
  browser,
  page,
}) => {
  const { workspace, owner, first, second } = await scene(
    browser,
    page,
    "Role change",
  );
  const before = await rowOf(page, workspace.id, first.me.userId);
  expect(before).toMatchObject({ role: "Member", status: "Active" });

  // Main flow: the Owner raises the Member to Admin in the roster.
  await page.goto(`/workspaces/${workspace.id}/members`);
  const roleSelect = page.getByRole("combobox", {
    name: `Role for ${before.name}`,
  });
  await roleSelect.click();
  const changed = page.waitForResponse(
    (response) =>
      response.request().method() === "PATCH" &&
      response.url().endsWith(`/members/${first.me.userId}`),
  );
  await page.getByRole("option", { name: /^Admin/ }).click();
  expect((await changed).status()).toBeLessThan(300);
  await expect(page.getByText(`${before.name} is now Admin.`)).toBeVisible();
  const raised = await rowOf(page, workspace.id, first.me.userId);
  expect(raised.role).toBe("Admin");
  expect(raised.version).toBeGreaterThan(before.version);

  // The new Admin sees what an Admin sees: the roster is theirs to change, their own row is not.
  const other = await rowOf(page, workspace.id, second.me.userId);
  await first.page.goto(`/workspaces/${workspace.id}/members`);
  await expect(
    first.page.getByRole("combobox", { name: `Role for ${other.name}` }),
  ).toBeVisible();
  await expect(
    first.page.getByRole("combobox", { name: `Role for ${before.name}` }),
  ).toHaveCount(0);

  // Lowering asks first; a confirmed lowering is written.
  await roleSelect.click();
  await page.getByRole("option", { name: /^Member/ }).click();
  await page
    .getByRole("alertdialog", { name: `Change ${before.name} to Member?` })
    .getByRole("button", { name: "Change role" })
    .click();
  await expect(page.getByText(`${before.name} is now Member.`)).toBeVisible();
  const lowered = await rowOf(page, workspace.id, first.me.userId);
  expect(lowered.role).toBe("Member");

  // A role that is already the member's is a no-op: no version moves.
  const same = await apiWrite(
    page,
    "PATCH",
    `/workspaces/${workspace.id}/members/${first.me.userId}`,
    { role: "Member", expectedVersion: lowered.version },
  );
  expect(same.status).toBeLessThan(300);
  expect((await rowOf(page, workspace.id, first.me.userId)).version).toBe(
    lowered.version,
  );

  // Rejection 1: a write that carries a version the row has moved past is refused and changes nothing.
  const stale = await apiWrite(
    page,
    "PATCH",
    `/workspaces/${workspace.id}/members/${first.me.userId}`,
    { role: "Guest", expectedVersion: before.version },
  );
  expect(stale.status).toBe(412);
  expect(await rowOf(page, workspace.id, first.me.userId)).toMatchObject({
    role: "Member",
    version: lowered.version,
  });

  // Rejection 2: a Member cannot change anyone's role.
  const byMember = await apiWrite(
    first.page,
    "PATCH",
    `/workspaces/${workspace.id}/members/${second.me.userId}`,
    { role: "Guest", expectedVersion: second.me.version },
  );
  expect(byMember.status).toBe(403);
  expect((await rowOf(page, workspace.id, second.me.userId)).role).toBe(
    "Member",
  );

  // Rejection 3: the only Owner cannot be demoted, by anyone, themselves included.
  const lastOwner = await apiWrite(
    page,
    "PATCH",
    `/workspaces/${workspace.id}/members/${owner.userId}`,
    { role: "Admin", expectedVersion: owner.version },
  );
  expect(lastOwner.status).toBeGreaterThanOrEqual(400);
  expect(lastOwner.status).toBeLessThan(500);
  expect(await rowOf(page, workspace.id, owner.userId)).toMatchObject({
    role: "Owner",
    version: owner.version,
  });

  await first.context.close();
  await second.context.close();
});

test("an Owner removes a member from the roster; the member loses the workspace at once, and a stale write, a Member and a self-removal change nothing", async ({
  browser,
  page,
}) => {
  const { workspace, owner, first, second } = await scene(
    browser,
    page,
    "Remove member",
  );
  const first0 = await rowOf(page, workspace.id, first.me.userId);

  // A role change moves the member's version, so a removal that still carries the old one is stale.
  expect(
    (
      await apiWrite(
        page,
        "PATCH",
        `/workspaces/${workspace.id}/members/${first.me.userId}`,
        { role: "Guest", expectedVersion: first0.version },
      )
    ).status,
  ).toBeLessThan(300);
  const target = await rowOf(page, workspace.id, first.me.userId);
  expect(target.version).toBeGreaterThan(first0.version);

  // Rejections first, while the member is still there.
  const stale = await apiWrite(
    page,
    "DELETE",
    `/workspaces/${workspace.id}/members/${first.me.userId}?expectedVersion=${first0.version}`,
  );
  expect(stale.status).toBe(412);
  const byMember = await apiWrite(
    second.page,
    "DELETE",
    `/workspaces/${workspace.id}/members/${first.me.userId}?expectedVersion=${target.version}`,
  );
  expect(byMember.status).toBe(403);
  const self = await apiWrite(
    page,
    "DELETE",
    `/workspaces/${workspace.id}/members/${owner.userId}?expectedVersion=${owner.version}`,
  );
  expect(self.status).toBeGreaterThanOrEqual(400);
  expect(self.status).toBeLessThan(500);
  expect(await rowOf(page, workspace.id, first.me.userId)).toMatchObject({
    status: "Active",
    version: target.version,
  });
  expect((await rowOf(page, workspace.id, owner.userId)).role).toBe("Owner");

  // Main flow: the row menu asks for confirmation, then the person is gone.
  await page.goto(`/workspaces/${workspace.id}/members`);
  await page
    .getByRole("button", { name: `Actions for ${target.name}` })
    .click();
  await page.getByRole("menuitem", { name: "Remove from workspace" }).click();
  const removed = page.waitForResponse(
    (response) =>
      response.request().method() === "DELETE" &&
      response.url().includes(`/members/${first.me.userId}`),
  );
  await page
    .getByRole("alertdialog", { name: `Remove ${target.name}?` })
    .getByRole("button", { name: "Remove member" })
    .click();
  expect((await removed).status()).toBeLessThan(300);
  await expect(
    page.getByText(`${target.name} was removed from the workspace.`),
  ).toBeVisible();
  await expect(
    page.getByRole("row", { name: new RegExp(`^${target.name}\\b`) }),
  ).toHaveCount(0);

  // The removed person reads nothing of the workspace; the other member is untouched.
  const gone = await first.page.request.get(
    `${apiUrl}/workspaces/${workspace.id}/members/me`,
  );
  expect([403, 404]).toContain(gone.status());
  expect(
    (
      await first.page.request.get(`${apiUrl}/workspaces/${workspace.id}`)
    ).status(),
  ).toBeGreaterThanOrEqual(400);
  expect(
    (await membersOf(page, workspace.id)).map((member) => member.userId),
  ).not.toContain(first.me.userId);
  expect((await meOf(second.page, workspace.id)).status).toBe("Active");

  await first.context.close();
  await second.context.close();
});

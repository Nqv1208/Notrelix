import { expect, test, type Page } from "@playwright/test";
import {
  apiRead,
  apiWrite,
  findWorkspace,
  realUsers,
  signIn,
} from "./support/real-session";

const apiUrl = process.env.VITE_API_URL ?? "http://127.0.0.1:58000/api/v1";

interface MemberRow {
  readonly userId: string;
  readonly name: string;
  readonly role: string;
  readonly status: string;
  readonly version: number;
}
interface MembersPage {
  readonly items: readonly MemberRow[];
  readonly nextCursor: string | null;
}

async function statusOf(page: Page, path: string) {
  return (await page.request.get(`${apiUrl}${path}`)).status();
}

/** Every page of the list, `size` people at a time. */
async function walk(page: Page, workspaceId: string, size: number) {
  const pages: MemberRow[][] = [];
  let cursor: string | null = null;
  do {
    const query: string = `limit=${size}${cursor ? `&cursor=${cursor}` : ""}`;
    const read: MembersPage = await apiRead<MembersPage>(
      page,
      `/workspaces/${workspaceId}/members?${query}`,
    );
    pages.push([...read.items]);
    cursor = read.nextCursor;
  } while (cursor);
  return pages;
}

test("the owner reads the people of a workspace, a guest is refused the list but reads their own membership", async ({
  browser,
  page,
}) => {
  // Main flow: the Owner of Workspace 3 opens Members and sees everyone once.
  await signIn(page, realUsers.workspaceOwner);
  const workspace = await findWorkspace(page, "Workspace 3");
  const everyone = await apiRead<MembersPage>(
    page,
    `/workspaces/${workspace.id}/members`,
  );
  expect(everyone.nextCursor).toBeNull();
  expect(everyone.items.length).toBeGreaterThan(2);

  await page.goto(`/workspaces/${workspace.id}/members`);
  await expect(
    page.getByRole("heading", { level: 1, name: "Members" }),
  ).toBeVisible();
  const table = page.getByRole("table").first();
  await expect(table.getByRole("row")).toHaveCount(everyone.items.length + 1);
  for (const member of everyone.items) {
    await expect(
      table.getByRole("row", { name: new RegExp(`^${member.name}\\b`) }),
    ).toHaveCount(1);
  }
  await expect(table.getByText("You", { exact: true })).toHaveCount(1);

  // The filter narrows the list by name.
  const first = everyone.items[0]!;
  await page
    .getByRole("searchbox", { name: "Filter members by name" })
    .fill(first.name);
  await expect(
    table.getByRole("row", { name: new RegExp(`^${first.name}\\b`) }),
  ).toHaveCount(1);

  // Pages: every person once, whatever the page size, in a stable order.
  const pages = await walk(page, workspace.id, 2);
  expect(pages.map((rows) => rows.length)).toEqual(
    Array.from({ length: Math.ceil(everyone.items.length / 2) }, (_, index) =>
      Math.min(2, everyone.items.length - index * 2),
    ),
  );
  expect(pages.flat().map((row) => row.userId)).toEqual(
    everyone.items.map((row) => row.userId),
  );

  // A page the producer did not issue or a size it does not allow is refused.
  expect(
    await statusOf(page, `/workspaces/${workspace.id}/members?limit=0`),
  ).toBe(400);
  expect(
    await statusOf(page, `/workspaces/${workspace.id}/members?limit=201`),
  ).toBe(400);
  expect(
    await statusOf(
      page,
      `/workspaces/${workspace.id}/members?cursor=not-a-cursor`,
    ),
  ).toBe(400);

  // My own membership: role, status and the version a member write carries.
  const mine = await apiRead<MemberRow>(
    page,
    `/workspaces/${workspace.id}/members/me`,
  );
  expect(mine).toMatchObject({ role: "Owner", status: "Active" });
  expect(mine.version).toBeGreaterThan(0);
  expect(
    everyone.items.find((row) => row.userId === mine.userId),
  ).toMatchObject({
    version: mine.version,
  });

  // Rejection: a Guest of the same workspace is refused the list, reads only
  // their own row, and a workspace nobody can open answers no differently to a
  // read of its people.
  const guestContext = await browser.newContext();
  const guestPage = await guestContext.newPage();
  await signIn(guestPage, realUsers.workspaceGuest);
  expect(await statusOf(guestPage, `/workspaces/${workspace.id}/members`)).toBe(
    403,
  );
  const own = await apiRead<MemberRow>(
    guestPage,
    `/workspaces/${workspace.id}/members/me`,
  );
  expect(own).toMatchObject({ role: "Guest", status: "Active" });

  await guestPage.goto(`/workspaces/${workspace.id}/members`);
  await expect(
    guestPage.getByRole("heading", { level: 1, name: "Members" }),
  ).toBeVisible();
  await expect(
    guestPage.getByText(
      "The member list is closed to guests. You see your own membership only.",
    ),
  ).toBeVisible();
  await expect(
    guestPage.getByRole("table").first().getByRole("row"),
  ).toHaveCount(2);
  await expect(guestPage.getByText("You", { exact: true })).toHaveCount(1);

  const nowhere = "00000000-0000-4000-8000-000000000001";
  expect([403, 404]).toContain(
    await statusOf(guestPage, `/workspaces/${nowhere}/members`),
  );
  expect([403, 404]).toContain(
    await statusOf(guestPage, `/workspaces/${nowhere}/members/me`),
  );
  await guestContext.close();
});

interface Invitation {
  readonly email: string;
}
interface InvitationsPage {
  readonly items: readonly Invitation[];
}

test("inviting a person who is already an active member is refused, says so in the dialog and sends nothing", async ({
  page,
}) => {
  // user4 is an Admin of Workspace 3 and may invite; its Owner is already an Active member.
  await signIn(page, realUsers.workspaceAdmin);
  const workspace = await findWorkspace(page, "Workspace 3");
  const member = realUsers.workspaceOwner.email;
  const invitationsOf = async () =>
    (
      await apiRead<InvitationsPage>(
        page,
        `/workspaces/${workspace.id}/invitations`,
      )
    ).items.filter((invitation) => invitation.email === member);
  const before = (await invitationsOf()).length;

  // The producer answers 409 with its own code and creates nothing.
  const refused = await apiWrite<{ errorCode?: string }>(
    page,
    "POST",
    `/workspaces/${workspace.id}/members`,
    { email: member, role: "Member" },
  );
  expect(refused.status).toBe(409);
  expect(refused.data?.errorCode).toBe("workspaces.invitation.already-member");

  // The invite form shows the catalog copy next to the field and keeps what was typed.
  await page.goto(`/workspaces/${workspace.id}/members?tab=invitations`);
  const email = page.getByLabel("E-mail address");
  await email.fill(member);
  await page.getByRole("button", { name: "Send invitation" }).click();
  await expect(
    page.getByText(
      `${member} is already a member of this workspace, so no invitation was sent.`,
    ),
  ).toBeVisible();
  await expect(email).toHaveValue(member);

  expect((await invitationsOf()).length).toBe(before);
});

import { expect, test, type Page } from "@playwright/test";
import { apiRead, apiWrite, realUsers, signIn } from "./support/real-session";
import {
  createOwnedWorkspace,
  joinAs,
  membersOf,
} from "./support/real-workspace";

test.setTimeout(300_000);

interface TeamRow {
  readonly id: string;
  readonly name: string;
  readonly description: string | null;
  readonly status: string;
  readonly version: number;
}
interface TeamMemberRow {
  readonly userId: string;
  readonly role: string;
  readonly status: string;
}
interface Paged<T> {
  readonly items: readonly T[];
}

const teamsOf = async (page: Page, workspaceId: string) =>
  (await apiRead<Paged<TeamRow>>(page, `/workspaces/${workspaceId}/teams`))
    .items;
const teamOf = (page: Page, workspaceId: string, teamId: string) =>
  apiRead<TeamRow>(page, `/workspaces/${workspaceId}/teams/${teamId}`);
const peopleOf = async (page: Page, workspaceId: string, teamId: string) =>
  await apiRead<TeamMemberRow[] | Paged<TeamMemberRow>>(
    page,
    `/workspaces/${workspaceId}/teams/${teamId}/members`,
  ).then((read) => (Array.isArray(read) ? read : read.items));

test("an Admin creates a Team with a lead and a person, renames and describes it, changes a role, removes a person, archives and restores it; stale writes and repeats change nothing", async ({
  browser,
  page,
}) => {
  await signIn(page, realUsers.admin);
  const workspace = await createOwnedWorkspace(page, "Teams flow");
  const member = await joinAs(
    browser,
    page,
    workspace.id,
    realUsers.workspaceAdmin,
    "Member",
  );
  const people = await membersOf(page, workspace.id);
  const owner = people.find((row) => row.role === "Owner")!;
  const other = people.find((row) => row.userId !== owner.userId)!;
  const name = `Platform ${Date.now()}`;

  // L02 empty: the list says there is no Team yet.
  await page.goto(`/workspaces/${workspace.id}/teams`);
  await expect(
    page.getByRole("heading", { level: 2, name: "Teams" }),
  ).toBeVisible();
  await expect(page.getByText("No Teams yet")).toBeVisible();

  // L01 and L05: the dialog creates the Team with the owner as lead and the other person as a member.
  await page.getByRole("button", { name: "New Team" }).click();
  const dialog = page.getByRole("dialog", { name: "New Team" });
  await dialog.getByLabel("Team name").fill(name);
  await dialog.getByRole("combobox", { name: /Team lead/ }).click();
  await page.getByRole("option", { name: new RegExp(owner.name) }).click();
  await dialog.getByPlaceholder("Type a name or email").fill(other.name);
  await page.keyboard.press("Enter");
  await dialog.getByRole("button", { name: "Create Team" }).click();
  await expect(page).toHaveURL(/\/teams\/[^/]+$/);
  const teamId = page.url().split("/").pop()!;
  await expect(page.getByRole("heading", { level: 2, name })).toBeVisible();

  // L02 and L03: the producer reads the Team and its two active people, the owner marked Lead.
  let team = await teamOf(page, workspace.id, teamId);
  expect(team.status).toBe("Active");
  let roster = await peopleOf(page, workspace.id, teamId);
  expect(roster.map((row) => [row.userId, row.role]).sort()).toEqual(
    [
      [owner.userId, "Lead"],
      [other.userId, "Member"],
    ].sort(),
  );
  await expect(
    page.getByRole("button", { name: `Role of ${owner.name}: Team lead` }),
  ).toBeVisible();

  // L04: the edit dialog renames the Team and writes its description; the version moves once per change.
  const renamed = `${name} core`;
  const before = team.version;
  await page.getByRole("button", { name: "Edit name and description" }).click();
  const edit = page.getByRole("dialog", { name: "Edit name and description" });
  await edit.getByLabel("Team name").fill(renamed);
  await edit.getByLabel(/Description/).fill("Runs the shared platform");
  await edit.getByRole("button", { name: "Save" }).click();
  await expect(
    page.getByRole("heading", { level: 2, name: renamed }),
  ).toBeVisible();
  await expect
    .poll(async () => (await teamOf(page, workspace.id, teamId)).version)
    .toBeGreaterThan(before);
  team = await teamOf(page, workspace.id, teamId);
  expect(team.name).toBe(renamed);
  expect(team.description).toBe("Runs the shared platform");

  // L04 no-op: the same name again is accepted and bumps no version.
  const noop = await apiWrite(
    page,
    "PUT",
    `/workspaces/${workspace.id}/teams/${teamId}`,
    {
      name: renamed,
      expectedVersion: team.version,
    },
  );
  expect(noop.status).toBe(204);
  expect((await teamOf(page, workspace.id, teamId)).version).toBe(team.version);

  // L04 stale: an old version is refused with 412 and the name stays.
  const stale = await apiWrite(
    page,
    "PUT",
    `/workspaces/${workspace.id}/teams/${teamId}`,
    {
      name: "Stale name",
      expectedVersion: before,
    },
  );
  expect(stale.status).toBe(412);
  expect((await teamOf(page, workspace.id, teamId)).name).toBe(renamed);

  // L06: the role menu makes the other person a Lead.
  await page
    .getByRole("button", { name: `Role of ${other.name}: Member` })
    .click();
  await page.getByRole("menuitemradio", { name: /Team lead/ }).click();
  await expect(
    page.getByRole("button", { name: `Role of ${other.name}: Team lead` }),
  ).toBeVisible();
  roster = await peopleOf(page, workspace.id, teamId);
  expect(roster.find((row) => row.userId === other.userId)?.role).toBe("Lead");

  // L07: removing the owner (a Lead while another Lead stays) ends the membership.
  await page
    .getByRole("button", { name: `Remove ${owner.name} from the Team` })
    .click();
  await expect
    .poll(async () =>
      (await peopleOf(page, workspace.id, teamId)).map((r) => r.userId),
    )
    .toEqual([other.userId]);
  // L06 and L07 rejection: the only Lead cannot be lowered or removed.
  team = await teamOf(page, workspace.id, teamId);
  const lower = await apiWrite(
    page,
    "PUT",
    `/workspaces/${workspace.id}/teams/${teamId}/members/${other.userId}/role`,
    { newRole: "Member", expectedVersion: team.version },
  );
  expect(lower.status).toBe(400);
  const drop = await apiWrite(
    page,
    "DELETE",
    `/workspaces/${workspace.id}/teams/${teamId}/members/${other.userId}?expectedVersion=${team.version}`,
  );
  expect(drop.status).toBe(400);
  expect(await peopleOf(page, workspace.id, teamId)).toHaveLength(1);

  // L05 through the page: add the owner back as a Member.
  await page.getByRole("combobox").fill(owner.name);
  await page.keyboard.press("Enter");
  await expect
    .poll(async () => (await peopleOf(page, workspace.id, teamId)).length)
    .toBe(2);

  // L08: Archive Team asks first; the Team is then archived and refuses a change.
  await page.getByRole("button", { name: "Archive Team" }).click();
  await page
    .getByRole("dialog", { name: /^Archive / })
    .getByRole("button", { name: "Archive Team" })
    .click();
  await expect
    .poll(async () => (await teamOf(page, workspace.id, teamId)).status)
    .toBe("Archived");
  team = await teamOf(page, workspace.id, teamId);
  const archivedWrite = await apiWrite(
    page,
    "PUT",
    `/workspaces/${workspace.id}/teams/${teamId}`,
    { name: "Archived rename", expectedVersion: team.version },
  );
  expect(archivedWrite.status).toBe(400);
  expect((await teamOf(page, workspace.id, teamId)).name).toBe(renamed);
  // L08 no-op: archiving an archived Team changes no version.
  const again = await apiWrite(
    page,
    "POST",
    `/workspaces/${workspace.id}/teams/${teamId}/archive`,
    { expectedVersion: team.version },
  );
  expect(again.status).toBe(204);
  expect((await teamOf(page, workspace.id, teamId)).version).toBe(team.version);

  // The list keeps the archived Team apart and Restore brings it back.
  await page.goto(`/workspaces/${workspace.id}/teams`);
  await page.getByRole("button", { name: `Restore ${renamed}` }).click();
  await expect
    .poll(async () => (await teamOf(page, workspace.id, teamId)).status)
    .toBe("Active");
  await expect(
    page
      .getByRole("table", { name: /^Teams in / })
      .getByRole("link", { name: new RegExp(renamed) }),
  ).toBeVisible();

  await member.context.close();
});

test("outsiders, a Member, a Guest, a foreign user, a clashing name and a repeated create change nothing", async ({
  browser,
  page,
}) => {
  await signIn(page, realUsers.admin);
  const workspace = await createOwnedWorkspace(page, "Teams rules");
  const member = await joinAs(
    browser,
    page,
    workspace.id,
    realUsers.workspaceAdmin,
    "Member",
  );
  const guest = await joinAs(
    browser,
    page,
    workspace.id,
    realUsers.workspaceGuest,
    "Guest",
  );
  const people = await membersOf(page, workspace.id);
  const owner = people.find((row) => row.role === "Owner")!;
  const guestRow = people.find((row) => row.role === "Guest")!;
  const base = `/workspaces/${workspace.id}/teams`;
  const name = `Rules ${Date.now()}`;

  // L01: a create under one Idempotency-Key returns the one Team on a retry; another body under the key is refused.
  const key = crypto.randomUUID();
  const first = await apiWrite<string>(page, "POST", base, { name }, key);
  expect(first.status).toBe(201);
  const retry = await apiWrite<string>(page, "POST", base, { name }, key);
  expect(retry.status).toBe(201);
  expect(retry.data).toBe(first.data);
  const changed = await apiWrite(
    page,
    "POST",
    base,
    { name: `${name} 2` },
    key,
  );
  expect(changed.status).toBeGreaterThanOrEqual(400);
  expect(changed.status).toBeLessThan(500);
  expect((await teamsOf(page, workspace.id)).map((row) => row.name)).toEqual([
    name,
  ]);
  const teamId = first.data as string;

  // L01: the same name in another case is refused.
  const clash = await apiWrite(page, "POST", base, {
    name: name.toUpperCase(),
  });
  expect(clash.status).toBe(400);
  expect(JSON.stringify(clash.data)).toContain("Workspaces_Team_DuplicateName");
  expect(await teamsOf(page, workspace.id)).toHaveLength(1);

  // L05: a Guest and a person who is no member are refused; the Team gains nobody.
  let team = await teamOf(page, workspace.id, teamId);
  const addGuest = await apiWrite(page, "POST", `${base}/${teamId}/members`, {
    userId: guestRow.userId,
    role: "Member",
    expectedVersion: team.version,
  });
  expect(addGuest.status).toBe(400);
  expect(JSON.stringify(addGuest.data)).toContain(
    "Workspaces_Team_GuestCannotJoin",
  );
  const addStranger = await apiWrite(
    page,
    "POST",
    `${base}/${teamId}/members`,
    {
      userId: crypto.randomUUID(),
      role: "Member",
      expectedVersion: team.version,
    },
  );
  expect(addStranger.status).toBe(404);
  expect(await peopleOf(page, workspace.id, teamId)).toHaveLength(1);
  expect((await teamOf(page, workspace.id, teamId)).version).toBe(team.version);

  // L02 and L03: a Member reads the Team and its people.
  const memberTeams = await apiRead<Paged<TeamRow>>(member.page, base);
  expect(memberTeams.items.map((row) => row.id)).toEqual([teamId]);
  expect(await peopleOf(member.page, workspace.id, teamId)).toHaveLength(1);

  // L01, L04, L05, L06, L07, L08: a Member's writes are refused with 403 and change nothing.
  team = await teamOf(page, workspace.id, teamId);
  const denied = [
    await apiWrite(member.page, "POST", base, { name: "Member team" }),
    await apiWrite(member.page, "PUT", `${base}/${teamId}`, {
      name: "Member rename",
      expectedVersion: team.version,
    }),
    await apiWrite(member.page, "POST", `${base}/${teamId}/archive`, {
      expectedVersion: team.version,
    }),
    await apiWrite(member.page, "POST", `${base}/${teamId}/members`, {
      userId: owner.userId,
      role: "Member",
      expectedVersion: team.version,
    }),
  ];
  expect(denied.map((row) => row.status)).toEqual([403, 403, 403, 403]);
  expect(await teamsOf(page, workspace.id)).toHaveLength(1);
  expect((await teamOf(page, workspace.id, teamId)).version).toBe(team.version);

  // L02: the Teams page tells a Member whom to ask and lists no Team.
  await member.page.goto(`${base}`);
  await expect(
    member.page.getByRole("heading", {
      name: "Only workspace admins manage Teams.",
    }),
  ).toBeVisible();
  await expect(member.page.getByRole("table")).toHaveCount(0);

  // Outsiders get the identical NotFound as for an unknown Team, and a Team never opens under another workspace.
  const outsider = await browser.newContext();
  const outsiderPage = await outsider.newPage();
  await signIn(outsiderPage, realUsers.workspaceOwner);
  const unknown = await outsiderPage.request.get(
    `${process.env.VITE_API_URL ?? "http://127.0.0.1:58000/api/v1"}${base}/${crypto.randomUUID()}`,
  );
  const hidden = await outsiderPage.request.get(
    `${process.env.VITE_API_URL ?? "http://127.0.0.1:58000/api/v1"}${base}/${teamId}`,
  );
  expect(unknown.status()).toBe(hidden.status());
  expect(hidden.status()).toBe(404);
  const elsewhere = await createOwnedWorkspace(page, "Teams elsewhere");
  const crossTenant = await page.request.get(
    `${process.env.VITE_API_URL ?? "http://127.0.0.1:58000/api/v1"}/workspaces/${elsewhere.id}/teams/${teamId}`,
  );
  expect(crossTenant.status()).toBe(404);
  const crossWrite = await apiWrite(
    page,
    "POST",
    `/workspaces/${elsewhere.id}/teams/${teamId}/archive`,
    { expectedVersion: team.version },
  );
  expect(crossWrite.status).toBe(404);
  expect((await teamOf(page, workspace.id, teamId)).status).toBe("Active");

  await outsider.close();
  await member.context.close();
  await guest.context.close();
});

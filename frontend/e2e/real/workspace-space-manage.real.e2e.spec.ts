import { expect, test, type Page } from "@playwright/test";
import { apiRead, apiWrite, realUsers, signIn } from "./support/real-session";
import {
  createOwnedWorkspace,
  joinAs,
  membersOf,
} from "./support/real-workspace";

test.setTimeout(300_000);

interface SpaceRead {
  readonly id: string;
  readonly name: string;
  readonly description: string | null;
  readonly visibility: string;
  readonly isArchived: boolean;
  readonly version: number;
}
interface SpaceMemberRow {
  readonly subjectType: string;
  readonly subjectId: string;
  readonly role: string;
}
interface Paged<T> {
  readonly items: readonly T[];
}

const apiUrl = process.env.VITE_API_URL ?? "http://127.0.0.1:58000/api/v1";
const spaceOf = (page: Page, workspaceId: string, spaceId: string) =>
  apiRead<SpaceRead>(page, `/workspaces/${workspaceId}/spaces/${spaceId}`);
const entriesOf = async (page: Page, workspaceId: string, spaceId: string) =>
  (
    await apiRead<Paged<SpaceMemberRow>>(
      page,
      `/workspaces/${workspaceId}/spaces/${spaceId}/members`,
    )
  ).items;
const makeSpace = async (
  page: Page,
  workspaceId: string,
  name: string,
  visibility = "Workspace",
) => {
  const made = await apiWrite<string>(
    page,
    "POST",
    `/workspaces/${workspaceId}/spaces`,
    { name, visibility, spaceType: "Department" },
  );
  expect(made.status, `create ${name}`).toBe(201);
  return made.data as string;
};

test("an Admin renames and describes a department, switches its visibility, archives and unarchives it; stale writes, repeats and outsiders change nothing", async ({
  browser,
  page,
}) => {
  await signIn(page, realUsers.admin);
  const workspace = await createOwnedWorkspace(page, "Space settings");
  const member = await joinAs(
    browser,
    page,
    workspace.id,
    realUsers.workspaceAdmin,
    "Member",
  );
  const stranger = await browser.newContext();
  const outsider = await stranger.newPage();
  await signIn(outsider, realUsers.workspaceOwner);
  const base = `/workspaces/${workspace.id}/spaces`;
  const ops = await makeSpace(page, workspace.id, "Ops", "Private");
  await makeSpace(page, workspace.id, "Finance");
  const temp = await makeSpace(page, workspace.id, "Temp");
  const url = (id: string) => `${base}/${id}`;

  // Rename and description: the settings tab saves both, the version moves once per changed field.
  const first = await spaceOf(page, workspace.id, ops);
  await page.goto(`${url(ops)}?tab=settings`);
  await page
    .getByRole("textbox", { name: "Department name" })
    .fill("Operations");
  await page
    .getByRole("textbox", { name: "Description" })
    .fill("Runs the floor");
  await page.getByRole("button", { name: "Save", exact: true }).click();
  await expect(page.getByText("Saved", { exact: true })).toBeVisible();
  await expect(
    page.getByRole("heading", { level: 1, name: /^Operations/ }),
  ).toBeVisible();
  let ops1 = await spaceOf(page, workspace.id, ops);
  expect(ops1.name).toBe("Operations");
  expect(ops1.description).toBe("Runs the floor");
  expect(ops1.version).toBe(first.version + 2);

  // Rejections: a stale version, a sibling name in another case, a Member and an outsider change nothing.
  const staleRename = await apiWrite(page, "PUT", url(ops), {
    name: "Stale",
    expectedVersion: first.version,
  });
  expect(staleRename.status).toBe(412);
  const clash = await apiWrite(page, "PUT", url(ops), {
    name: "finance",
    expectedVersion: ops1.version,
  });
  expect([400, 409]).toContain(clash.status);
  const byMember = await apiWrite(member.page, "PUT", url(ops), {
    name: "By member",
    expectedVersion: ops1.version,
  });
  expect([403, 404]).toContain(byMember.status);
  const byOutsider = await apiWrite(outsider, "PUT", url(ops), {
    name: "By outsider",
    expectedVersion: ops1.version,
  });
  expect(byOutsider.status).toBe(404);
  const outsiderVisibility = await apiWrite(
    outsider,
    "PUT",
    `${url(ops)}/visibility`,
    { visibility: "Workspace", expectedVersion: ops1.version },
  );
  expect(outsiderVisibility.status).toBe(404);
  expect(await spaceOf(page, workspace.id, ops)).toEqual(ops1);

  // No-ops: the same name, description and visibility answer success and bump nothing.
  for (const [method, path, body] of [
    ["PUT", url(ops), { name: "Operations" }],
    ["PUT", `${url(ops)}/description`, { description: "Runs the floor" }],
    ["PUT", `${url(ops)}/visibility`, { visibility: "Private" }],
  ] as const) {
    const noop = await apiWrite(page, method, path, {
      ...body,
      expectedVersion: ops1.version,
    });
    expect(noop.status, `${path} no-op`).toBeLessThan(300);
  }
  expect((await spaceOf(page, workspace.id, ops)).version).toBe(ops1.version);

  // A retry of a guarded write has no second effect: the version it carried has moved, so the repeat is a 412.
  const sent = { name: "Operations HQ", expectedVersion: ops1.version };
  expect((await apiWrite(page, "PUT", url(ops), sent)).status).toBeLessThan(
    300,
  );
  expect((await apiWrite(page, "PUT", url(ops), sent)).status).toBe(412);
  ops1 = await spaceOf(page, workspace.id, ops);
  expect(ops1.name).toBe("Operations HQ");
  expect(ops1.version).toBe(first.version + 3);

  // Visibility: the radio saves at once; a stale version is refused; the Member now sees it, an
  // outsider of the workspace still does not.
  await page.reload();
  await page.getByRole("radio", { name: /Open to the workspace/ }).click();
  await expect
    .poll(async () => (await spaceOf(page, workspace.id, ops)).visibility)
    .toBe("Workspace");
  const opened = await spaceOf(page, workspace.id, ops);
  expect(opened.version).toBe(ops1.version + 1);
  const staleVisibility = await apiWrite(
    page,
    "PUT",
    `${url(ops)}/visibility`,
    {
      visibility: "Private",
      expectedVersion: ops1.version,
    },
  );
  expect(staleVisibility.status).toBe(412);
  expect((await spaceOf(page, workspace.id, ops)).visibility).toBe("Workspace");
  expect((await spaceOf(member.page, workspace.id, ops)).name).toBe(
    "Operations HQ",
  );
  const narrowed = await apiWrite(page, "PUT", `${url(ops)}/visibility`, {
    visibility: "Private",
    expectedVersion: opened.version,
  });
  expect(narrowed.status).toBeLessThan(300);
  expect((await member.page.request.get(`${apiUrl}${url(ops)}`)).status()).toBe(
    404,
  );

  // Archive: a department with a Board in it is refused with the reason; an empty one asks first.
  const board = await apiWrite<string>(
    page,
    "POST",
    `/workspaces/${workspace.id}/boards`,
    { title: "Keeps Temp busy", spaceId: temp },
  );
  expect(board.status).toBe(201);
  const busy = await spaceOf(page, workspace.id, temp);
  const refused = await apiWrite(page, "POST", `${url(temp)}/archive`, {
    expectedVersion: busy.version,
  });
  expect(refused.status).toBeGreaterThanOrEqual(400);
  expect(refused.status).toBeLessThan(500);
  expect((await spaceOf(page, workspace.id, temp)).isArchived).toBe(false);
  await page.goto(`${url(temp)}?tab=settings`);
  await expect(
    page.getByRole("button", { name: "Archive", exact: true }),
  ).toHaveAttribute("aria-disabled", "true");
  await expect(page.getByText(/still has 1 Board/)).toBeVisible();

  const empty = await makeSpace(page, workspace.id, "Empty");
  const before = await spaceOf(page, workspace.id, empty);
  await page.goto(`${url(empty)}?tab=settings`);
  await page.getByRole("button", { name: "Archive", exact: true }).click();
  const confirm = page.getByRole("alertdialog", { name: "Archive Empty?" });
  await expect(confirm).toBeVisible();
  await confirm.getByRole("button", { name: "Archive", exact: true }).click();
  await expect
    .poll(async () => (await spaceOf(page, workspace.id, empty)).isArchived)
    .toBe(true);
  const archived = await spaceOf(page, workspace.id, empty);
  expect(archived.version).toBe(before.version + 1);
  // Archived: a stale archive is refused, a repeat is a no-op, a rename is refused.
  const staleArchive = await apiWrite(page, "POST", `${url(empty)}/archive`, {
    expectedVersion: before.version,
  });
  expect(staleArchive.status).toBe(412);
  const repeat = await apiWrite(page, "POST", `${url(empty)}/archive`, {
    expectedVersion: archived.version,
  });
  expect(repeat.status).toBeLessThan(300);
  expect((await spaceOf(page, workspace.id, empty)).version).toBe(
    archived.version,
  );
  const renameArchived = await apiWrite(page, "PUT", url(empty), {
    name: "Renamed",
    expectedVersion: archived.version,
  });
  expect(renameArchived.status).toBeGreaterThanOrEqual(400);
  expect(renameArchived.status).toBeLessThan(500);
  // Unarchive: stale refused, then it restores; a repeat is a no-op.
  const staleUnarchive = await apiWrite(
    page,
    "POST",
    `${url(empty)}/unarchive`,
    {
      expectedVersion: before.version,
    },
  );
  expect(staleUnarchive.status).toBe(412);
  const unarchive = await apiWrite(page, "POST", `${url(empty)}/unarchive`, {
    expectedVersion: archived.version,
  });
  expect(unarchive.status).toBeLessThan(300);
  const restored = await spaceOf(page, workspace.id, empty);
  expect(restored.isArchived).toBe(false);
  expect(restored.version).toBe(archived.version + 1);
  const again = await apiWrite(page, "POST", `${url(empty)}/unarchive`, {
    expectedVersion: restored.version,
  });
  expect(again.status).toBeLessThan(300);
  expect((await spaceOf(page, workspace.id, empty)).version).toBe(
    restored.version,
  );

  await member.context.close();
  await stranger.close();
});

test("an Admin adds a person and a Team to a department, changes a role and removes them; the last admin, a foreign id, stale writes and repeats change nothing", async ({
  browser,
  page,
}) => {
  await signIn(page, realUsers.admin);
  const workspace = await createOwnedWorkspace(page, "Space people");
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
  const space = await makeSpace(page, workspace.id, "People");
  const base = `/workspaces/${workspace.id}/spaces/${space}`;

  // The creator is the first Space Admin.
  expect(await entriesOf(page, workspace.id, space)).toEqual([
    expect.objectContaining({ subjectId: owner.userId, role: "Admin" }),
  ]);

  // K11 main flow: the Add dialog adds the other person as a Viewer; they appear once.
  await page.goto(`${base}?tab=members`);
  await page.getByRole("button", { name: "Add members" }).click();
  const dialog = page.getByRole("dialog", { name: "Add to People" });
  await dialog
    .getByRole("combobox", { name: "Add people or teams" })
    .fill(other.name);
  await dialog.getByRole("option", { name: new RegExp(other.name) }).click();
  await expect(
    dialog.getByRole("button", { name: `Remove ${other.name}` }),
  ).toBeVisible();
  await dialog.getByRole("button", { name: /Member/ }).click();
  await page.getByRole("option", { name: /Viewer/ }).click();
  await dialog.getByRole("button", { name: "Add 1 person" }).click();
  await expect(dialog).toBeHidden();
  const row = (name: string) =>
    page.locator('[data-f="mr"]').filter({ hasText: name });
  await expect(row(other.name)).toHaveCount(1);
  let entries = await entriesOf(page, workspace.id, space);
  expect(entries.filter((entry) => entry.subjectId === other.userId)).toEqual([
    expect.objectContaining({ role: "Viewer" }),
  ]);
  let current = await spaceOf(page, workspace.id, space);

  // K11 rejections: a foreign user id and a Team of nowhere are refused; a stale version, a Member
  // and a repeat change nothing.
  const foreign = await apiWrite(page, "POST", `${base}/members`, {
    subjectType: "User",
    subjectId: "00000000-0000-4000-8000-0000000000bb",
    role: "Member",
    expectedVersion: current.version,
  });
  expect(foreign.status).toBe(404);
  const noTeam = await apiWrite(page, "POST", `${base}/members`, {
    subjectType: "Team",
    subjectId: "00000000-0000-4000-8000-0000000000cc",
    role: "Member",
    expectedVersion: current.version,
  });
  expect(noTeam.status).toBe(404);
  const staleAdd = await apiWrite(page, "POST", `${base}/members`, {
    subjectType: "User",
    subjectId: other.userId,
    role: "Viewer",
    expectedVersion: current.version - 1,
  });
  expect(staleAdd.status).toBe(412);
  const byMember = await apiWrite(member.page, "POST", `${base}/members`, {
    subjectType: "User",
    subjectId: other.userId,
    role: "Admin",
    expectedVersion: current.version,
  });
  expect([403, 404]).toContain(byMember.status);
  const sameRole = await apiWrite(page, "POST", `${base}/members`, {
    subjectType: "User",
    subjectId: other.userId,
    role: "Viewer",
    expectedVersion: current.version,
  });
  expect(sameRole.status).toBeLessThan(300);
  const otherRole = await apiWrite(page, "POST", `${base}/members`, {
    subjectType: "User",
    subjectId: other.userId,
    role: "Member",
    expectedVersion: current.version,
  });
  expect(otherRole.status).toBeGreaterThanOrEqual(400);
  expect(otherRole.status).toBeLessThan(500);
  expect(await entriesOf(page, workspace.id, space)).toHaveLength(2);
  expect((await spaceOf(page, workspace.id, space)).version).toBe(
    current.version,
  );

  // K11: a Team joins as an entry of its own.
  const team = await apiWrite<string>(
    page,
    "POST",
    `/workspaces/${workspace.id}/teams`,
    { name: `Crew ${Date.now()}` },
  );
  expect(team.status).toBe(201);
  const addTeam = await apiWrite(page, "POST", `${base}/members`, {
    subjectType: "Team",
    subjectId: team.data,
    role: "Member",
    expectedVersion: current.version,
  });
  expect(addTeam.status).toBeLessThan(300);
  current = await spaceOf(page, workspace.id, space);
  expect(
    (await entriesOf(page, workspace.id, space)).find(
      (entry) => entry.subjectId === team.data,
    ),
  ).toEqual(expect.objectContaining({ subjectType: "Team", role: "Member" }));

  // K12 main flow: the role menu lifts the person to Member.
  await page.reload();
  await row(other.name)
    .getByRole("button", { name: /Viewer/ })
    .click();
  await page.getByRole("menuitemradio", { name: /^Member/ }).click();
  await expect
    .poll(
      async () =>
        (await entriesOf(page, workspace.id, space)).find(
          (entry) => entry.subjectId === other.userId,
        )?.role,
    )
    .toBe("Member");
  const lifted = await spaceOf(page, workspace.id, space);
  expect(lifted.version).toBe(current.version + 1);

  // K12 rejections: the only Admin cannot be lowered; stale, repeat and Member change nothing.
  const roleUrl = (id: string) => `${base}/members/User/${id}`;
  const lowerLast = await apiWrite(page, "PATCH", roleUrl(owner.userId), {
    role: "Member",
    expectedVersion: lifted.version,
  });
  expect(lowerLast.status).toBe(400);
  const staleRole = await apiWrite(page, "PATCH", roleUrl(other.userId), {
    role: "Viewer",
    expectedVersion: current.version,
  });
  expect(staleRole.status).toBe(412);
  const sameMemberRole = await apiWrite(page, "PATCH", roleUrl(other.userId), {
    role: "Member",
    expectedVersion: lifted.version,
  });
  expect(sameMemberRole.status).toBeLessThan(300);
  const roleByMember = await apiWrite(
    member.page,
    "PATCH",
    roleUrl(owner.userId),
    {
      role: "Viewer",
      expectedVersion: lifted.version,
    },
  );
  expect([403, 404]).toContain(roleByMember.status);
  const noEntry = await apiWrite(
    page,
    "PATCH",
    roleUrl("00000000-0000-4000-8000-0000000000dd"),
    { role: "Viewer", expectedVersion: lifted.version },
  );
  expect(noEntry.status).toBeGreaterThanOrEqual(400);
  expect(noEntry.status).toBeLessThan(500);
  expect((await spaceOf(page, workspace.id, space)).version).toBe(
    lifted.version,
  );
  entries = await entriesOf(page, workspace.id, space);
  expect(entries.find((entry) => entry.subjectId === owner.userId)?.role).toBe(
    "Admin",
  );

  // K13 rejections first: the last Admin cannot be removed; a stale remove changes nothing.
  const dropLast = await apiWrite(
    page,
    "DELETE",
    `${roleUrl(owner.userId)}?expectedVersion=${lifted.version}`,
  );
  expect(dropLast.status).toBe(400);
  const staleDrop = await apiWrite(
    page,
    "DELETE",
    `${roleUrl(other.userId)}?expectedVersion=${current.version}`,
  );
  expect(staleDrop.status).toBe(412);
  const dropByMember = await apiWrite(
    member.page,
    "DELETE",
    `${roleUrl(other.userId)}?expectedVersion=${lifted.version}`,
  );
  expect([403, 404]).toContain(dropByMember.status);
  expect(await entriesOf(page, workspace.id, space)).toHaveLength(3);

  // K13 main flow: the menu asks first, then removes the person; removing the Team follows by the API.
  await row(other.name)
    .getByRole("button", { name: /^Actions for/ })
    .click();
  await page
    .getByRole("menuitem", { name: /Remove from the department/ })
    .click();
  const remove = page.getByRole("dialog", {
    name: new RegExp(`^Remove ${other.name}`),
  });
  await expect(remove).toBeVisible();
  await remove.getByRole("button", { name: "Remove", exact: true }).click();
  await expect(row(other.name)).toHaveCount(0);
  const dropped = await spaceOf(page, workspace.id, space);
  expect(dropped.version).toBe(lifted.version + 1);
  // An entry that is already gone is a no-op, not an error and not a version.
  const absent = await apiWrite(
    page,
    "DELETE",
    `${roleUrl(other.userId)}?expectedVersion=${dropped.version}`,
  );
  expect(absent.status).toBeLessThan(300);
  expect((await spaceOf(page, workspace.id, space)).version).toBe(
    dropped.version,
  );
  const dropTeam = await apiWrite(
    page,
    "DELETE",
    `${base}/members/Team/${team.data}?expectedVersion=${dropped.version}`,
  );
  expect(dropTeam.status).toBeLessThan(300);
  expect(await entriesOf(page, workspace.id, space)).toEqual([
    expect.objectContaining({ subjectId: owner.userId, role: "Admin" }),
  ]);
  await member.context.close();
});

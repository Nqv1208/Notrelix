import AxeBuilder from "@axe-core/playwright";
import { expect, test, type Page } from "@playwright/test";
import { apiRead, apiWrite, realUsers, signIn } from "./support/real-session";
import { createOwnedWorkspace, joinAs } from "./support/real-workspace";

test.setTimeout(300_000);

interface SpaceRead {
  readonly id: string;
  readonly name: string;
  readonly version: number;
}
interface DeletedRow {
  readonly id: string;
  readonly name: string;
  readonly deletedAt: string;
  readonly restorableUntil: string;
}

const apiUrl = process.env.VITE_API_URL ?? "http://127.0.0.1:58000/api/v1";
const spaceOf = (page: Page, workspaceId: string, spaceId: string) =>
  apiRead<SpaceRead>(page, `/workspaces/${workspaceId}/spaces/${spaceId}`);
const deletedOf = (page: Page, workspaceId: string) =>
  apiRead<DeletedRow[]>(page, `/workspaces/${workspaceId}/spaces/deleted`);
const statusOf = async (page: Page, path: string) =>
  (await page.request.get(`${apiUrl}${path}`)).status();
const makeSpace = async (
  page: Page,
  workspaceId: string,
  name: string,
  parentSpaceId?: string,
) => {
  const made = await apiWrite<string>(
    page,
    "POST",
    `/workspaces/${workspaceId}/spaces`,
    { name, visibility: "Workspace", spaceType: "Department", parentSpaceId },
  );
  expect(made.status, `create ${name}`).toBe(201);
  return made.data as string;
};

test("an Admin deletes a department from its settings and restores it from the deleted list; stale deletes, a department with content, a Member, an outsider, a repeat and a deleted parent change nothing", async ({
  browser,
  page,
}) => {
  await signIn(page, realUsers.admin);
  const workspace = await createOwnedWorkspace(page, "Space trash");
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
  const w = `/workspaces/${workspace.id}`;
  const url = (id: string) => `${w}/spaces/${id}`;
  const doomed = await makeSpace(page, workspace.id, "Doomed");
  const busy = await makeSpace(page, workspace.id, "Busy");
  const board = await apiWrite<string>(page, "POST", `${w}/boards`, {
    title: "Keeps Busy busy",
    spaceId: busy,
  });
  expect(board.status).toBe(201);
  const before = await spaceOf(page, workspace.id, doomed);

  // Rejections change nothing: a stale version, a department with a Board, a Member and an outsider.
  const stale = await apiWrite(
    page,
    "DELETE",
    `${url(doomed)}?expectedVersion=${before.version + 4}`,
  );
  expect(stale.status).toBe(412);
  const withContent = await apiWrite(
    page,
    "DELETE",
    `${url(busy)}?expectedVersion=${(await spaceOf(page, workspace.id, busy)).version}`,
  );
  expect(withContent.status).toBeGreaterThanOrEqual(400);
  expect(withContent.status).toBeLessThan(500);
  const byMember = await apiWrite(
    member.page,
    "DELETE",
    `${url(doomed)}?expectedVersion=${before.version}`,
  );
  expect([403, 404]).toContain(byMember.status);
  const byOutsider = await apiWrite(
    outsider,
    "DELETE",
    `${url(doomed)}?expectedVersion=${before.version}`,
  );
  expect(byOutsider.status).toBe(404);
  expect(await spaceOf(page, workspace.id, doomed)).toEqual(before);
  expect(await spaceOf(page, workspace.id, busy)).toEqual(
    expect.objectContaining({ id: busy }),
  );
  // The deleted list is for Owners and Admins: a Member is refused and an outsider gets NotFound.
  expect([403, 404]).toContain(
    await statusOf(member.page, `${w}/spaces/deleted`),
  );
  expect(await statusOf(outsider, `${w}/spaces/deleted`)).toBe(404);

  // Settings: a department with content explains why Delete is closed; an empty one words the 30 days.
  await page.goto(`${url(busy)}?tab=settings`);
  await expect(
    page.getByRole("button", { name: "Delete department" }),
  ).toHaveAttribute("aria-disabled", "true");
  await expect(page.getByText(/Move or delete them first/)).toBeVisible();

  // Main flow: the confirmation asks for the name, then the deleted list shows the department.
  await page.goto(`${url(doomed)}?tab=settings`);
  await expect(page.getByText(/Restorable for 30 days/)).toBeVisible();
  await page.getByRole("button", { name: "Delete department" }).click();
  const group = page.getByRole("group", { name: /^Delete Doomed from/ });
  await expect(group).toBeVisible();
  const confirm = group.getByRole("button", { name: "Delete department" });
  await expect(confirm).toHaveAttribute("aria-disabled", "true");
  await group.getByRole("textbox").fill("Doomed");
  const axeOpen = await new AxeBuilder({ page }).analyze();
  expect(
    axeOpen.violations.filter((v) =>
      ["serious", "critical"].includes(v.impact ?? ""),
    ),
  ).toEqual([]);
  await confirm.click();
  await expect(
    page.getByRole("heading", { level: 1, name: "Deleted departments" }),
  ).toBeVisible();
  await expect(page.getByText("Doomed", { exact: true })).toBeVisible();
  await expect(page.getByText(/Restorable until/)).toBeVisible();
  const axeList = await new AxeBuilder({ page }).analyze();
  expect(
    axeList.violations.filter((v) =>
      ["serious", "critical"].includes(v.impact ?? ""),
    ),
  ).toEqual([]);

  // The department left the tree; the list gives it 30 days.
  expect(await statusOf(page, url(doomed))).toBe(404);
  const listed = (await deletedOf(page, workspace.id)).find(
    (row) => row.id === doomed,
  )!;
  expect(
    Date.parse(listed.restorableUntil) - Date.parse(listed.deletedAt),
  ).toBe(30 * 24 * 60 * 60 * 1000);
  // A repeat of the delete finds nothing and changes nothing.
  const repeat = await apiWrite(
    page,
    "DELETE",
    `${url(doomed)}?expectedVersion=${before.version}`,
  );
  expect(repeat.status).toBe(404);
  // A Member and an outsider cannot restore it.
  const memberRestore = await apiWrite(
    member.page,
    "POST",
    `${url(doomed)}/restore`,
    {},
  );
  expect([403, 404]).toContain(memberRestore.status);
  const outsiderRestore = await apiWrite(
    outsider,
    "POST",
    `${url(doomed)}/restore`,
    {},
  );
  expect(outsiderRestore.status).toBe(404);
  expect((await deletedOf(page, workspace.id)).map((row) => row.id)).toContain(
    doomed,
  );

  // Restore from the list; the version moved twice (delete, restore) and a repeat is a no-op.
  await page.getByRole("button", { name: "Restore", exact: true }).click();
  await expect(
    page.getByText(/No deleted department in the last 30 days/),
  ).toBeVisible();
  const back = await spaceOf(page, workspace.id, doomed);
  expect(back.version).toBe(before.version + 2);
  const again = await apiWrite(page, "POST", `${url(doomed)}/restore`, {});
  expect(again.status).toBeLessThan(300);
  expect((await spaceOf(page, workspace.id, doomed)).version).toBe(
    back.version,
  );

  // A department cannot come back under a deleted parent; the list shows the producer's words.
  const parent = await makeSpace(page, workspace.id, "Parent");
  const kid = await makeSpace(page, workspace.id, "Kid", parent);
  for (const id of [kid, parent]) {
    const deleted = await apiWrite(
      page,
      "DELETE",
      `${url(id)}?expectedVersion=${(await spaceOf(page, workspace.id, id)).version}`,
    );
    expect(deleted.status, `delete ${id}`).toBeLessThan(300);
  }
  const underDeleted = await apiWrite(page, "POST", `${url(kid)}/restore`, {});
  expect(underDeleted.status).toBeGreaterThanOrEqual(400);
  expect(underDeleted.status).toBeLessThan(500);
  await page.goto(`${w}/deleted-spaces`);
  const kidRow = page.getByRole("listitem").filter({ hasText: "Kid" });
  await kidRow.getByRole("button", { name: "Restore" }).click();
  await expect(kidRow.getByRole("alert")).toBeVisible();
  expect(await statusOf(page, url(kid))).toBe(404);
  await page
    .getByRole("listitem")
    .filter({ hasText: "Parent" })
    .getByRole("button", { name: "Restore" })
    .click();
  await expect(
    page.getByRole("listitem").filter({ hasText: "Parent" }),
  ).toHaveCount(0);
  await kidRow.getByRole("button", { name: "Restore" }).click();
  await expect(
    page.getByText(/No deleted department in the last 30 days/),
  ).toBeVisible();
  expect(await statusOf(page, url(kid))).toBe(200);

  await member.context.close();
  await stranger.close();
});

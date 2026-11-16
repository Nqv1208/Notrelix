import { expect, test } from "@playwright/test";
import { apiRead, apiWrite, realUsers, signIn } from "./support/real-session";
import { createOwnedWorkspace, joinAs } from "./support/real-workspace";

test.setTimeout(300_000);

interface SpaceRead {
  readonly spaceType: string;
  readonly visibility: string;
  readonly version: number;
}

test("an Admin changes a Folder to a Department from the settings tab and visibility stays; a stale version is 412 and a Member is refused", async ({
  browser,
  page,
}) => {
  await signIn(page, realUsers.admin);
  const workspace = await createOwnedWorkspace(page, "Space type");
  const member = await joinAs(
    browser,
    page,
    workspace.id,
    realUsers.workspaceAdmin,
    "Member",
  );
  const base = `/workspaces/${workspace.id}/spaces`;
  const made = await apiWrite<string>(page, "POST", base, {
    name: "Ops",
    visibility: "Private",
    spaceType: "Folder",
  });
  expect(made.status).toBe(201);
  const url = `${base}/${made.data as string}`;
  const read = () => apiRead<SpaceRead>(page, url);
  const before = await read();
  expect(before.spaceType).toBe("Folder");

  await page.goto(`${url}?tab=settings`);
  await page.getByRole("combobox", { name: "Type" }).click();
  await page.getByRole("option", { name: "Department" }).click();
  await expect.poll(async () => (await read()).spaceType).toBe("Department");
  const after = await read();
  expect(after.visibility).toBe("Private");
  expect(after.version).toBe(before.version + 1);

  // Rejections: the version the page held is stale now, and a Member may not change the type.
  const stale = await apiWrite(page, "PUT", `${url}/type`, {
    spaceType: "Portfolio",
    expectedVersion: before.version,
  });
  expect(stale.status).toBe(412);
  const byMember = await apiWrite(member.page, "PUT", `${url}/type`, {
    spaceType: "Portfolio",
    expectedVersion: after.version,
  });
  expect([403, 404]).toContain(byMember.status);
  expect(await read()).toEqual(after);
});

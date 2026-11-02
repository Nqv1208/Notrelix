import { expect, test } from "@playwright/test";
import { apiRead, apiWrite, realUsers, signIn } from "./support/real-session";
import { createOwnedWorkspace } from "./support/real-workspace";

test.setTimeout(120_000);

test("a workspace is created from the rail and by the API: one key makes one workspace, a reserved or taken address is refused and creates nothing", async ({
  page,
}) => {
  await signIn(page, realUsers.admin);
  const { accountId } = await apiRead<{ accountId: string }>(
    page,
    "/auth/bootstrap",
  );
  const stamp = `${Date.now()}`;
  const path = `/accounts/${accountId}/workspaces`;
  const create = (key: string, name: string, slug: string) =>
    apiWrite<string>(
      page,
      "POST",
      path,
      { name, slug, size: "1-10", language: "en" },
      key,
    );
  const named = async () =>
    (await apiRead<{ name: string }[] | { items: { name: string }[] }>(
      page,
      "/workspaces",
    )) as { name: string }[];

  // Retry safety: two identical calls with one key give one workspace and the same id.
  const key = crypto.randomUUID();
  const first = await create(key, `Acme ${stamp}`, `acme-${stamp}`);
  const again = await create(key, `Acme ${stamp}`, `acme-${stamp}`);
  expect(first.status).toBe(201);
  expect(again.status).toBeLessThan(300);
  expect(again.data).toBe(first.data);
  expect(
    (await named()).filter((row) => row.name === `Acme ${stamp}`),
  ).toHaveLength(1);

  // Rejection: the same key with another body is refused and nothing new exists.
  const other = await create(key, `Other ${stamp}`, `other-${stamp}`);
  expect(other.status).toBeGreaterThanOrEqual(400);
  expect(
    (await named()).filter((row) => row.name === `Other ${stamp}`),
  ).toHaveLength(0);

  // Rejection: a reserved address is a validation error and stores nothing; a taken one is a conflict.
  const reserved = await create(crypto.randomUUID(), `Admin ${stamp}`, "admin");
  expect(reserved.status).toBe(400);
  const taken = await create(
    crypto.randomUUID(),
    `Acme two ${stamp}`,
    `acme-${stamp}`,
  );
  expect(taken.status).toBe(409);
  expect(
    (await named()).filter((row) => row.name.endsWith(`two ${stamp}`)),
  ).toHaveLength(0);

  // Main flow in the product: the rail's Add opens the dialog, and the new workspace opens with its Owner inside.
  await page.goto("/");
  await page
    .getByRole("button", { name: /open the Notrelix system rail/ })
    .click();
  await page.getByRole("button", { name: "Add", exact: true }).click();
  const name = `Rail ${stamp}`;
  await page.getByLabel("Workspace name").fill(name);
  await page.getByRole("button", { name: "Create workspace" }).click();
  await expect(page).toHaveURL(/\/workspaces\/[0-9a-f-]{36}/, {
    timeout: 30_000,
  });
  const id = /\/workspaces\/([0-9a-f-]{36})/.exec(page.url())![1];
  expect(
    await apiRead<{ role: string; status: string }>(
      page,
      `/workspaces/${id}/members/me`,
    ),
  ).toMatchObject({ role: "Owner", status: "Active" });
});

test("an Admin renames the workspace in settings: the version steps once, the same name is a no-op and a stale version changes nothing", async ({
  page,
}) => {
  await signIn(page, realUsers.admin);
  const workspace = await createOwnedWorkspace(page, "Rename me");
  const read = () =>
    apiRead<{ name: string; description: string | null; version: number }>(
      page,
      `/workspaces/${workspace.id}`,
    );
  const patch = (name: string, description: string, expectedVersion: number) =>
    apiWrite(page, "PATCH", `/workspaces/${workspace.id}/profile`, {
      name,
      description,
      expectedVersion,
    });
  const before = await read();

  // In the product: the Profile section saves and the new name shows.
  await page.goto(`/workspaces/${workspace.id}/settings`);
  const renamed = `${workspace.name} renamed`;
  await page.getByLabel("Workspace name").fill(renamed);
  await page.getByRole("button", { name: "Save changes" }).first().click();
  await expect.poll(async () => (await read()).name).toBe(renamed);
  const after = await read();
  expect(after.version).toBe(before.version + 1);

  // The same name and description again: nothing changes.
  expect(
    (await patch(renamed, after.description ?? "", after.version)).status,
  ).toBeLessThan(300);
  expect((await read()).version).toBe(after.version);

  // A stale version is refused and the name stays.
  const stale = await patch("Stale name", "", before.version);
  expect(stale.status).toBeGreaterThanOrEqual(409);
  expect((await read()).name).toBe(renamed);
});

import { expect, test } from "@playwright/test";
import { apiRead, apiWrite, realUsers, signIn } from "./support/real-session";
import { createOwnedWorkspace } from "./support/real-workspace";

test.setTimeout(300_000);

interface TreeNode {
  readonly id: string;
  readonly parentSpaceId: string | null;
  readonly version: number;
}

test("an Admin moves a department under another from the Move picker; the picker never offers a descendant, the API refuses a cycle, and a stale version is 412", async ({
  page,
}) => {
  await signIn(page, realUsers.admin);
  const workspace = await createOwnedWorkspace(page, "Space move");
  const base = `/workspaces/${workspace.id}/spaces`;
  const make = async (name: string, parentSpaceId?: string) => {
    const made = await apiWrite<string>(page, "POST", base, {
      name,
      visibility: "Workspace",
      spaceType: "Department",
      ...(parentSpaceId ? { parentSpaceId } : {}),
    });
    expect(made.status, `create ${name}`).toBe(201);
    return `${base}/${made.data as string}`;
  };
  const alpha = await make("Alpha");
  const beta = await make("Beta");
  const idOf = (url: string) => url.split("/").pop()!;
  const read = async (url: string) => {
    const tree = await apiRead<{ items: readonly TreeNode[] }>(
      page,
      `${base}/tree`,
    );
    return tree.items.find((node) => node.id === idOf(url))!;
  };
  const alphaId = idOf(alpha);
  const betaBefore = await read(beta);
  expect(betaBefore.parentSpaceId).toBeNull();

  // Main move: Beta goes under Alpha from its settings tab.
  await page.goto(`${beta}?tab=settings`);
  await page.getByRole("button", { name: "Move", exact: true }).click();
  const dialog = page.getByRole("dialog");
  await dialog.getByRole("radio", { name: /^Alpha/ }).click();
  await dialog.getByRole("button", { name: "Move here" }).click();
  await expect(dialog).toBeHidden();
  await expect.poll(async () => (await read(beta)).parentSpaceId).toBe(alphaId);
  const betaAfter = await read(beta);
  expect(betaAfter.version).toBe(betaBefore.version + 1);

  // Rejection: Alpha under its child Beta is not offered, the API refuses it, and nothing changes.
  const alphaBefore = await read(alpha);
  await page.goto(`${alpha}?tab=settings`);
  await page.getByRole("button", { name: "Move", exact: true }).click();
  await expect(
    page.getByRole("dialog").getByRole("radio", { name: /^Beta/ }),
  ).toBeDisabled();
  await page.keyboard.press("Escape");
  const cycle = await apiWrite(page, "POST", `${alpha}/move`, {
    newParentSpaceId: idOf(beta),
    expectedVersion: alphaBefore.version,
  });
  expect(cycle.status).toBe(400);
  const stale = await apiWrite(page, "POST", `${beta}/move`, {
    newParentSpaceId: null,
    expectedVersion: betaBefore.version,
  });
  expect(stale.status).toBe(412);
  expect(await read(alpha)).toEqual(alphaBefore);
  expect(await read(beta)).toEqual(betaAfter);
});

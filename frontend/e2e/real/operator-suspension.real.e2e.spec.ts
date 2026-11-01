import { expect, test } from "@playwright/test";
import { openTenantInConsole } from "./support/operator-console";
import {
  apiRead,
  apiWrite,
  findWorkspace,
  realUsers,
  signIn,
} from "./support/real-session";

test("the operator suspends a tenant: its members still read but cannot write, and lifting restores writing", async ({
  page,
  browser,
}) => {
  await signIn(page, realUsers.admin);
  const workspace = await findWorkspace(page, "Workspace 1");
  const boards = `/workspaces/${workspace.id}/boards`;
  const create = () =>
    apiWrite<{ code?: string; ruleCode?: string }>(page, "POST", `${boards}/`, {
      title: `Suspension probe ${Date.now()}`.slice(0, 40),
    });

  expect((await create()).status).toBeLessThan(300);

  const operatorPage = await (await browser.newContext()).newPage();
  try {
    await openTenantInConsole(operatorPage);

    await operatorPage.getByRole("button", { name: "Suspend" }).click();
    const suspend = operatorPage.getByRole("dialog");
    await suspend.getByLabel(/Reason/).fill("real e2e suspension");
    await suspend.getByRole("button", { name: "Suspend" }).click();
    await expect(operatorPage.getByText("Suspended").first()).toBeVisible();

    const refused = await create();
    expect(refused.status).toBe(403);
    expect(JSON.stringify(refused.data)).toContain("account.suspended");
    expect(await apiRead(page, boards)).toBeTruthy();

    await operatorPage.getByRole("button", { name: "Lift suspension" }).click();
    const lift = operatorPage.getByRole("dialog");
    await lift.getByLabel(/Reason/).fill("real e2e lift");
    await lift.getByRole("button", { name: "Lift suspension" }).click();
    await expect(
      operatorPage.getByRole("button", { name: "Suspend" }).first(),
    ).toBeVisible();

    expect((await create()).status).toBeLessThan(300);
  } finally {
    await operatorPage.context().close();
  }
});

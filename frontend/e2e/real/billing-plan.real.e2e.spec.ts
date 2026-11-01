import { expect, test, type Page } from "@playwright/test";
import { openTenantInConsole } from "./support/operator-console";
import {
  findWorkspace,
  openBoard,
  realUsers,
  signIn,
} from "./support/real-session";

test.use({ viewport: { width: 1440, height: 900 } });
// Two sign-ins, three plan changes and a cold backend: longer than the default 30 seconds.
test.setTimeout(120_000);

async function moveTenantTo(page: Page, planOption: string) {
  await page.getByLabel("Move to plan revision").selectOption({
    label: planOption,
  });
  await page.getByRole("button", { name: "Change plan", exact: true }).click();
  const dialog = page.getByRole("dialog");
  await dialog.getByLabel(/Reason/).fill("real e2e plan change");
  await dialog.getByRole("button", { name: "Change plan" }).click();
  await expect(dialog).toBeHidden();
}

test("the operator moves a tenant between plans and the tenant sees the new limits", async ({
  page,
  browser,
}) => {
  // The tenant owner has no way to switch the plan: the card shows it and nothing more.
  await signIn(page, realUsers.admin);
  const workspace = await findWorkspace(page, "Workspace 1");
  const billingUrl = `/workspaces/${workspace.id}/billing`;
  await page.goto(billingUrl);
  const limits = page.getByRole("list", {
    name: "Capabilities granted by billing",
  });
  await expect(limits.getByText(/^\d+ of 500 used$/)).toBeVisible();
  await expect(page.getByRole("button", { name: /Switch to/ })).toHaveCount(0);

  const operatorPage = await (await browser.newContext()).newPage();
  try {
    await openTenantInConsole(operatorPage);

    await moveTenantTo(operatorPage, "Free r1");
    await page.reload();
    await expect(limits.getByText(/^\d+ of 10 used$/)).toBeVisible();
    await expect(limits.getByText(/^\d+(\.\d+)? of 2 GB used$/)).toBeVisible();

    await moveTenantTo(operatorPage, "Enterprise r1");
    await page.reload();
    await expect(limits.getByText("Unlimited").first()).toBeVisible();

    await moveTenantTo(operatorPage, "Pro r1");
    await page.reload();
    await expect(limits.getByText(/^\d+ of 500 used$/)).toBeVisible();

    // Every change is in the audit trail with its reason.
    await expect(
      operatorPage.getByRole("cell", { name: "real e2e plan change" }).first(),
    ).toBeVisible();
  } finally {
    await operatorPage.context().close();
  }
});

test("a workspace admin who is not the account owner cannot read billing and has no switcher", async ({
  page,
}) => {
  await signIn(page, realUsers.workspaceAdmin);
  const { workspace } = await openBoard(
    page,
    "Workspace 3",
    "Engineering Tasks",
  );
  await page.goto(`/workspaces/${workspace.id}/billing`);

  // Billing is read by the workspace Owner (owner decision 2026-10-31); the plan is the operator's to change.
  await expect(
    page.getByRole("heading", {
      name: "Only workspace admins can see billing",
    }),
  ).toBeVisible();
  await expect(page.getByRole("button", { name: /Switch to/ })).toHaveCount(0);
});

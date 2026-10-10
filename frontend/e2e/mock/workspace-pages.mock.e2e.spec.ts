import { expect, test } from "@playwright/test";

const scenario = process.env.VITE_MOCK_STATE ?? "default";
const workspace = "/workspaces/mock-workspace-primary";

test.beforeEach(async ({ page }) => {
  test.skip(scenario !== "default", "Assertions need the default world data");
  await page.addInitScript(() => {
    try {
      window.localStorage.setItem("notrelix-locale", "en");
    } catch {
      // Private mode: the default locale applies.
    }
  });
});

test("Home resumes work: Continue, My work and activity sections (WX-HOME-001)", async ({
  page,
}) => {
  await page.goto(workspace);
  await expect(
    page.getByRole("heading", { level: 1, name: "Home" }),
  ).toBeVisible();
  await expect(page.getByRole("region", { name: "Continue" })).toBeVisible();
  const myWork = page.getByRole("region", { name: "My work" });
  await expect(myWork.getByRole("heading", { name: /Overdue/ })).toBeVisible();
  await expect(
    page.getByRole("complementary", { name: "Workspace updates" }),
  ).toBeVisible();
  await myWork
    .getByRole("button", { name: /^Mark .* complete$/ })
    .first()
    .click();
  await expect(page.getByText(/is complete\.$/)).toBeVisible();
});

test("Boards & Docs pins a row into the sidebar (WX-DIR-001)", async ({
  page,
}) => {
  await page.goto(`${workspace}/library`);
  await expect(
    page.getByRole("heading", { level: 1, name: "Boards & Docs" }),
  ).toBeVisible();
  await page.getByRole("button", { name: "Pin Product Roadmap" }).click({
    force: true,
  });
  await expect(
    page
      .getByRole("navigation", { name: "Workspace navigation" })
      .getByRole("link", { name: /Product Roadmap/ }),
  ).toHaveCount(1);
});

test("Members changes a role and confirms removal (WX-MEM-001)", async ({
  page,
}) => {
  await page.goto(`${workspace}/members`);
  await page
    .getByRole("combobox", { name: "Role for Jordan Lee (Member)" })
    .click();
  await page.getByRole("option", { name: "Admin" }).click();
  await expect(page.getByText(/is now Admin\.$/)).toBeVisible();

  await page
    .getByRole("button", { name: "Actions for Taylor Morgan (Viewer)" })
    .click();
  await page.getByRole("menuitem", { name: "Remove from workspace" }).click();
  await page
    .getByRole("alertdialog")
    .getByRole("button", { name: "Remove member" })
    .click();
  await expect(page.locator("tbody tr")).toHaveCount(3);
});

test("Settings explains why saving is off without a workspace version (WX-SET-001)", async ({
  page,
}) => {
  await page.goto(`${workspace}/settings`);
  await expect(page.getByLabel("Workspace name")).toBeDisabled();
  await expect(page.getByText(/Saving is turned off for now/)).toBeVisible();
});

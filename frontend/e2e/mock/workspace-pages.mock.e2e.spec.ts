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

test("Settings saves the profile with the workspace version, and the next save carries the advanced one (WX-SET-001)", async ({
  page,
}) => {
  await page.goto(`${workspace}/settings`);
  const name = page.getByLabel("Workspace name");
  await expect(name).toBeEnabled();
  await expect(page.getByText(/Saving is turned off for now/)).toHaveCount(0);

  // The first save sends the read version (1); the producer advances it.
  await name.fill("Product Platform");
  const profile = page.getByRole("region", { name: "Profile" });
  await profile.getByRole("button", { name: "Save changes" }).click();
  await expect(page.getByText("Profile saved.").first()).toBeVisible();
  // The re-read workspace (version 2) names the page.
  await expect(page.getByText(/Product Platform/).first()).toBeVisible();

  // A second save must carry the advanced version: with the stale one the
  // producer answers 412 and the surface shows the conflict notice instead.
  await name.fill("Product Platform Team");
  await profile.getByRole("button", { name: "Save changes" }).click();
  await expect(page.getByText("Profile saved.").first()).toBeVisible();
  await expect(
    page.getByText(/Someone changed these settings while you were editing/),
  ).toHaveCount(0);
});

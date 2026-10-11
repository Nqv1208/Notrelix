import { expect, test } from "@playwright/test";

const scenario = process.env.VITE_MOCK_STATE ?? "default";
const persona = process.env.VITE_MOCK_PERSONA ?? "owner";
const workspace = "/workspaces/mock-workspace-primary";

test.beforeEach(async ({ page }) => {
  test.skip(
    scenario !== "default" || persona !== "owner",
    "Manages Teams: needs the default world and a workspace owner",
  );
  await page.addInitScript(() => {
    try {
      window.localStorage.setItem("notrelix-locale", "en");
    } catch {
      // Private mode: the default locale applies.
    }
  });
});

test("Teams: create a Team, add a member, promote and demote Leads, remove, archive (WX-TEAM-001)", async ({
  page,
}) => {
  await page.goto(workspace);

  // Open Teams from the sidebar (in-app navigation: a reload would reset the
  // in-page mock world).
  const nav = page.getByRole("navigation", { name: "Workspace navigation" });
  await nav.getByRole("link", { name: "Teams" }).click();
  await expect(page).toHaveURL(/\/teams$/);
  await expect(
    page.getByRole("heading", { level: 1, name: "Teams" }),
  ).toBeVisible();
  const list = page.getByRole("list", { name: "Teams" });
  await expect(list.getByRole("link", { name: "Helpdesk" })).toBeVisible();

  // Create a Team: the creator becomes its first Lead.
  await page.getByRole("button", { name: "New Team" }).click();
  const create = page.getByRole("dialog", { name: "New Team" });
  await create.getByLabel("Team name").fill("Phòng Kế toán");
  await create.getByRole("button", { name: "Create Team" }).click();
  await expect(page.getByText("Phòng Kế toán was created.")).toBeVisible();
  await expect(page).toHaveURL(/\/teams\/[^/]+$/);
  await expect(
    page.getByRole("heading", { level: 1, name: /Phòng Kế toán/ }),
  ).toBeVisible();
  const members = page.getByRole("list", { name: "Members" });
  await expect(members.getByRole("listitem")).toHaveCount(1);
  // The only Lead is shown as plain text: it cannot be demoted or removed.
  await expect(members.getByText("UI Developer (Owner)")).toBeVisible();
  await expect(members.getByText("The only Lead.")).toBeVisible();

  // Add a workspace member as a Member.
  const add = page.getByRole("form", { name: "Add a member" });
  await add.getByRole("combobox", { name: "Person" }).click();
  await page.getByRole("option", { name: "Jordan Lee (Member)" }).click();
  await add.getByRole("button", { name: "Add to Team" }).click();
  await expect(
    page.getByText("Jordan Lee (Member) was added to the Team."),
  ).toBeVisible();
  await expect(members.getByRole("listitem")).toHaveCount(2);
  const jordanRole = page.getByRole("combobox", {
    name: "Team role for Jordan Lee (Member)",
  });
  await expect(jordanRole).toHaveText("Member");

  // Promote to Lead; with two Leads the original Lead may be demoted.
  await jordanRole.click();
  await page.getByRole("option", { name: "Lead" }).click();
  await expect(
    page.getByText("Jordan Lee (Member) is now Lead."),
  ).toBeVisible();
  await expect(jordanRole).toHaveText("Lead");
  const ownerRole = page.getByRole("combobox", {
    name: "Team role for UI Developer (Owner)",
  });
  await ownerRole.click();
  await page.getByRole("option", { name: "Member" }).click();
  await expect(
    page.getByText("UI Developer (Owner) is now Member."),
  ).toBeVisible();
  await expect(ownerRole).toHaveText("Member");

  // Remove the demoted person after confirming.
  await page
    .getByRole("button", {
      name: "Remove UI Developer (Owner) from the Team",
    })
    .click();
  await page
    .getByRole("alertdialog", { name: /^Remove UI Developer \(Owner\) from/ })
    .getByRole("button", { name: "Remove from Team" })
    .click();
  await expect(
    page.getByText("UI Developer (Owner) was removed from the Team."),
  ).toBeVisible();
  await expect(members.getByRole("listitem")).toHaveCount(1);

  // Archive the Team; it moves to the Archived filter.
  await page.getByRole("button", { name: "Archive Team" }).click();
  await page
    .getByRole("alertdialog", { name: /^Archive Phòng Kế toán/ })
    .getByRole("button", { name: "Archive Team" })
    .click();
  await expect(page.getByText("Phòng Kế toán was archived.")).toBeVisible();
  await expect(
    page.getByRole("button", { name: "Unarchive Team" }),
  ).toBeVisible();

  await nav.getByRole("link", { name: "Teams" }).click();
  await expect(
    page.getByRole("heading", { level: 1, name: "Teams" }),
  ).toBeVisible();
  await expect(
    page.getByRole("list", { name: "Teams" }).getByRole("link", {
      name: "Phòng Kế toán",
    }),
  ).toHaveCount(0);
  await page
    .getByRole("radiogroup", { name: "Show Teams" })
    .getByRole("radio", { name: "Archived" })
    .click();
  await expect(
    page.getByRole("list", { name: "Teams" }).getByRole("link", {
      name: "Phòng Kế toán",
    }),
  ).toBeVisible();
});

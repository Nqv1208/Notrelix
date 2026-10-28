import { expect, test, type Page } from "@playwright/test";

/**
 * What a commenter and a viewer can and cannot do on the ERP board's Table,
 * Calendar, item sheet and settings page. The commenter checks run when the
 * server was started with VITE_MOCK_PERSONA=commenter, the viewer checks with
 * VITE_MOCK_PERSONA=viewer; the Kanban and Timeline have their own specs.
 */
const persona = process.env.VITE_MOCK_PERSONA ?? "owner";
const scenario = process.env.VITE_MOCK_STATE ?? "default";
const BOARD = "/workspaces/mock-workspace-primary/boards/mock-board-erp";
const ERP_142 = "mock-erp-item-142";

test.skip(
  !["commenter", "viewer"].includes(persona) || scenario !== "default",
  "Needs the server started with VITE_MOCK_PERSONA=commenter or viewer",
);
test.use({ viewport: { width: 1440, height: 900 } });

test.beforeEach(async ({ page }) => {
  await page.clock.setFixedTime(new Date("2026-10-12T10:40:00+07:00"));
  await page.addInitScript(() => {
    try {
      window.localStorage.setItem("notrelix-locale", "en");
    } catch {
      // Private mode: the default locale applies.
    }
  });
});

async function openItem(page: Page, view: string) {
  await page.goto(`${BOARD}?view=${view}&item=${ERP_142}`);
  const sheet = page.getByRole("dialog");
  await expect(sheet.getByRole("button", { name: /^Status: / })).toBeVisible();
  return sheet;
}

test.describe("commenter", () => {
  test.skip(persona !== "commenter", "Needs VITE_MOCK_PERSONA=commenter");

  test("reads the Table and Calendar but changes nothing and adds nothing", async ({
    page,
  }) => {
    await page.goto(`${BOARD}?view=table`);
    await expect(page.getByRole("note")).toContainText(
      "You can comment on this board",
    );
    await expect(
      page.getByRole("button", { name: "Change status of ERP-142" }),
    ).toBeDisabled();
    await expect(page.getByRole("button", { name: /^Add item/ })).toHaveCount(
      0,
    );
    await expect(page.getByRole("button", { name: "Settings" })).toHaveCount(0);

    await page.goto(`${BOARD}?view=calendar`);
    await expect(
      page.getByRole("button", { name: /^ERP-142 / }).first(),
    ).toBeVisible();
    await expect(page.getByRole("button", { name: "Add item" })).toHaveCount(0);
  });

  test("comments in the sheet but cannot edit the item", async ({ page }) => {
    const sheet = await openItem(page, "table");
    for (const name of [
      /^Status: /,
      /^Assignee: /,
      /^Due: /,
      /^Priority: /,
      /^Add property/,
    ]) {
      await expect(sheet.getByRole("button", { name })).toBeDisabled();
    }
    await expect(
      sheet.getByRole("textbox", { name: "Edit title" }),
    ).toHaveCount(0);
    await sheet
      .getByRole("textbox", { name: /comment/i })
      .fill("Commenter note");
    await sheet.getByRole("button", { name: "Send" }).click();
    await expect(sheet.getByText("Commenter note")).toBeVisible();
  });

  test("the settings page is closed to a non-manager", async ({ page }) => {
    await page.goto(`${BOARD}/settings`);
    await expect(
      page.getByText("Only board managers open the settings."),
    ).toBeVisible();
  });
});

test.describe("viewer", () => {
  test.skip(persona !== "viewer", "Needs VITE_MOCK_PERSONA=viewer");

  test("reads the Table and Calendar but changes nothing and adds nothing", async ({
    page,
  }) => {
    await page.goto(`${BOARD}?view=table`);
    await expect(page.getByRole("note")).toContainText(
      "You can view this board",
    );
    await expect(
      page.getByRole("button", { name: "Change status of ERP-142" }),
    ).toBeDisabled();
    await expect(page.getByRole("button", { name: /^Add item/ })).toHaveCount(
      0,
    );
    await expect(page.getByRole("button", { name: "Settings" })).toHaveCount(0);

    await page.goto(`${BOARD}?view=calendar`);
    await expect(
      page.getByRole("button", { name: /^ERP-142 / }).first(),
    ).toBeVisible();
    await expect(page.getByRole("button", { name: "Add item" })).toHaveCount(0);
  });

  test("reads the sheet but can neither edit nor comment", async ({ page }) => {
    const sheet = await openItem(page, "table");
    await expect(
      sheet.getByRole("button", { name: /^Status: / }),
    ).toBeDisabled();
    await expect(
      sheet.getByRole("textbox", { name: "Edit title" }),
    ).toHaveCount(0);
    await expect(
      sheet.getByRole("textbox", { name: /comment/i }),
    ).toBeDisabled();
  });

  test("the settings page is closed to a non-manager", async ({ page }) => {
    await page.goto(`${BOARD}/settings`);
    await expect(
      page.getByText("Only board managers open the settings."),
    ).toBeVisible();
  });
});

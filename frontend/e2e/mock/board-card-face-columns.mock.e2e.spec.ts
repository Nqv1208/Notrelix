import { expect, test, type Page } from "@playwright/test";

/**
 * The card face and the extra table columns on the ERP mock world: the Kanban
 * card names its priority with a glyph and a word, keeps its second row on one
 * line, and the table offers Checklist, Bình luận and Tệp columns through the
 * "Cột" menu.
 */
const persona = process.env.VITE_MOCK_PERSONA ?? "owner";
const scenario = process.env.VITE_MOCK_STATE ?? "default";
const faults = process.env.VITE_MOCK_FAULTS ?? "";
const BOARD = "/workspaces/mock-workspace-primary/boards/mock-board-erp";
const NOW = new Date("2026-10-12T10:40:00+07:00");

async function open(page: Page, view: "kanban" | "table") {
  await page.clock.setFixedTime(NOW);
  await page.addInitScript(() => {
    try {
      window.localStorage.setItem("notrelix-locale", "vi");
    } catch {
      // Private mode: the default locale applies.
    }
  });
  await page.goto(`${BOARD}?view=${view}`);
}

test.describe("Card face and table columns (owner)", () => {
  test.skip(
    persona !== "owner" || scenario !== "default" || faults !== "",
    "Needs the default owner world without faults",
  );
  test.use({ viewport: { width: 1440, height: 900 } });

  test("a Kanban card shows its priority glyph with a name and a one-line second row", async ({
    page,
  }) => {
    await open(page, "kanban");
    const card = page.locator("article", { hasText: "ERP-142" }).first();
    await expect(card).toBeVisible();
    const priority = card.locator("[data-priority]");
    await expect(priority).toHaveCount(1);
    await expect(priority.locator("svg")).toHaveCount(1);
    await expect(priority).toContainText("Ưu tiên");
    // Row 2 never wraps: its height stays one line of badges.
    const meta = card.locator("[id$='-meta']");
    const box = (await meta.boundingBox())!;
    expect(box.height).toBeLessThan(34);
    const overflow = await meta.evaluate(
      (element) => element.scrollWidth <= element.clientWidth,
    );
    expect(overflow).toBe(true);
  });

  test("the Cột menu adds Checklist, Bình luận and Tệp to the table", async ({
    page,
  }) => {
    await open(page, "table");
    await expect(page.getByRole("rowgroup").first()).toBeVisible();
    await expect(
      page.getByRole("columnheader", { name: "Checklist" }),
    ).toHaveCount(0);
    await page.getByRole("button", { name: /^Cột \d+\/\d+/ }).click();
    for (const name of ["Checklist", "Bình luận", "Tệp"]) {
      await page.getByRole("menuitemcheckbox", { name }).click();
    }
    await page.keyboard.press("Escape");
    for (const name of ["Checklist", "Bình luận", "Tệp"]) {
      await expect(page.getByRole("columnheader", { name })).toBeVisible();
    }
  });
});

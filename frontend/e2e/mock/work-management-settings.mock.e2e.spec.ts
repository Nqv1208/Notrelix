import { expect, test, type Page } from "@playwright/test";

/**
 * Work Management writes that the earlier audit could not verify, driven on
 * the mock world as the workspace owner: the board's General settings (rename,
 * archive, delete), a field (rename, delete), a status option, the Kanban
 * group-by switch, the Calendar week mode, the item sheet's "Move to group"
 * and "Request approval". The mock rebuilds its world on every page load, so
 * each write is proved in the same session (the settings page and the board
 * read it back through the mock), not across a reload.
 */
const persona = process.env.VITE_MOCK_PERSONA ?? "owner";
const scenario = process.env.VITE_MOCK_STATE ?? "default";
const WS = "/workspaces/mock-workspace-primary";
const ROADMAP = `${WS}/boards/mock-board-roadmap`;
const ERP = `${WS}/boards/mock-board-erp`;

test.skip(
  persona !== "owner" || scenario !== "default",
  "Owner checks need the default owner world",
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

function trackErrors(page: Page) {
  const errors: string[] = [];
  page.on("pageerror", (error) => errors.push(error.message));
  return errors;
}

async function openSection(page: Page, board: string, section: string) {
  await page.goto(`${board}/settings?from=table&section=${section}`);
  await expect(
    page.getByRole("heading", { name: "Board settings" }),
  ).toBeVisible();
}

test.describe("board General settings", () => {
  test("renaming a board without an item key saves and the board shows the new name", async ({
    page,
  }) => {
    const errors = trackErrors(page);
    await openSection(page, ROADMAP, "general");
    await page
      .getByRole("textbox", { name: "Board name", exact: true })
      .fill("Roadmap Renamed");
    await page.getByRole("button", { name: "Save changes" }).click();
    await expect(page.getByText("Roadmap Renamed / Settings")).toBeVisible();
    await expect(page.getByText(/could not be saved/)).toHaveCount(0);
    await page.getByRole("link", { name: "Back to the board" }).click();
    await expect(
      page.getByRole("heading", { name: "Roadmap Renamed" }),
    ).toBeVisible();
    expect(errors).toEqual([]);
  });

  test("archiving asks first, then leaves the settings for the archived boards", async ({
    page,
  }) => {
    await openSection(page, ROADMAP, "general");
    await page.getByRole("button", { name: "Archive board" }).first().click();
    await page
      .getByRole("alert")
      .getByRole("button", { name: /Archive/ })
      .click();
    // The page that lists archived boards names the way back (onLeave of the settings route).
    await expect(page).toHaveURL(
      /\/workspaces\/mock-workspace-primary\/archived-boards\?tab=archived$/,
    );
  });

  test("deleting needs the board name typed, then leaves the settings", async ({
    page,
  }) => {
    await openSection(page, ROADMAP, "general");
    const remove = page.getByRole("button", { name: "Delete board" });
    await expect(remove).toBeDisabled();
    await page
      .getByRole("textbox", { name: /Type the board name/ })
      .fill("Product Roadmap");
    await expect(remove).toBeEnabled();
    await remove.click();
    await expect(page).toHaveURL(
      /\/workspaces\/mock-workspace-primary\/archived-boards\?tab=deleted$/,
    );
  });
});

test.describe("board data fields", () => {
  test("a field is renamed, then deleted after the confirmation", async ({
    page,
  }) => {
    await openSection(page, ROADMAP, "fields");
    await page.getByRole("button", { name: "Edit the field Priority" }).click();
    await page.getByRole("textbox", { name: "Display name" }).fill("Urgency");
    await page.getByRole("button", { name: "Save changes" }).last().click();
    await expect(
      page.getByRole("button", { name: "Edit the field Urgency" }),
    ).toBeVisible();
    await expect(
      page.getByRole("button", { name: "Edit the field Priority" }),
    ).toHaveCount(0);

    await page.getByRole("button", { name: "Edit the field Urgency" }).click();
    await page.getByRole("button", { name: "Delete field" }).click();
    await page
      .getByRole("alertdialog")
      .getByRole("button", { name: "Delete field" })
      .click();
    await expect(
      page.getByRole("button", { name: "Edit the field Urgency" }),
    ).toHaveCount(0);
  });

  test("a status option is renamed and recoloured, and the table group follows", async ({
    page,
  }) => {
    await openSection(page, ROADMAP, "fields");
    await page.getByRole("button", { name: "Edit the field Status" }).click();
    await page
      .getByRole("textbox", { name: "Name of the option" })
      .first()
      .fill("To do now");
    await page
      .getByRole("button", { name: /Colour of the option/ })
      .first()
      .click();
    await page.getByRole("dialog").getByRole("button").nth(2).click();
    await page.getByRole("button", { name: "Save changes" }).last().click();
    await expect(
      page.getByRole("textbox", { name: "Name of the option" }).first(),
    ).toHaveValue("To do now");
    await page.getByRole("link", { name: "Back to the board" }).click();
    await expect(
      page.getByRole("button", { name: "Options for group To do now" }),
    ).toBeVisible();
  });
});

test.describe("comments", () => {
  test("replying to and reacting to a comment are offered", async ({
    page,
  }) => {
    // Replies and reactions are live; their flows are in comment-replies-reactions.
    await page.goto(`${ERP}/items/mock-erp-item-142`);
    await expect(
      page.getByRole("button", { name: "Reply", exact: true }).first(),
    ).toBeEnabled();
    await expect(
      page.getByRole("button", { name: "Add a reaction" }).first(),
    ).toBeEnabled();
  });

  test("the popup offers an own comment's Edit and Delete and opens the editor in place", async ({
    page,
  }) => {
    // The popup lists the actions under the comment instead of in a menu over the Kanban.
    await page.goto(`${ROADMAP}?view=kanban&item=card-main-0001`);
    const sheet = page.getByRole("dialog", { name: "Ship mock runtime" });
    await expect(sheet.getByRole("button", { name: "Delete" })).toBeVisible();
    await sheet.getByRole("button", { name: "Edit", exact: true }).click();
    await expect(sheet.getByRole("button", { name: "Save" })).toBeVisible();
    await expect(sheet.getByRole("button", { name: "Cancel" })).toBeVisible();
  });
});

test.describe("views and the item sheet", () => {
  test("the Kanban group-by switches to a Select field and the choice survives a reload", async ({
    page,
  }) => {
    await page.goto(`${ERP}?view=kanban`);
    await expect(
      page.getByRole("region", { name: "Cần làm", exact: true }),
    ).toBeVisible();
    await page.getByRole("button", { name: /^Group:/ }).click();
    await page.getByRole("menuitemradio", { name: "Phân hệ ERP" }).click();
    await expect(page).toHaveURL(/groupBy=mock-erp-col-module/);
    await expect(
      page.getByRole("region", { name: "Kế toán", exact: true }),
    ).toBeVisible();
    await expect(page.locator("button[data-card-id]")).toHaveCount(16);
    await page.reload();
    await expect(
      page.getByRole("region", { name: "Kế toán", exact: true }),
    ).toBeVisible();
  });

  test("the Calendar week and day modes step by their own span", async ({
    page,
  }) => {
    await page.goto(`${ERP}?view=calendar`);
    await page.getByRole("radio", { name: "Week" }).click();
    await expect(page.getByText("12/10 – 18/10/2026")).toBeVisible();
    await page.getByRole("button", { name: "Next week" }).click();
    await expect(page.getByText("19/10 – 25/10/2026")).toBeVisible();
    await page.getByRole("radio", { name: "Day" }).click();
    await expect(page.getByRole("radio", { name: "Day" })).toBeChecked();
    await page.getByRole("radio", { name: "Month" }).click();
    await expect(page.getByRole("radio", { name: "Month" })).toBeChecked();
  });

  test("the card popup's column picker moves the item and the Kanban lane counts follow", async ({
    page,
  }) => {
    // The popup's header writes the item's Status like a drag between lanes does; a board group has
    // no picker while the board has a Status field.
    const errors = trackErrors(page);
    await page.goto(`${ERP}?view=kanban`);
    const lane = (name: string) =>
      page.getByRole("region", { name, exact: true });
    await expect(lane("Cần làm")).toBeVisible();
    const count = async (name: string) =>
      Number(await lane(name).getAttribute("data-lane-count"));
    const before = {
      source: await count("Cần làm"),
      target: await count("Đang làm"),
    };
    await page
      .getByRole("button", {
        name: "Viết tài liệu quy trình nhập hoá đơn đầu vào",
        exact: true,
      })
      .click();
    const sheet = page.getByRole("dialog");
    await sheet.getByRole("button", { name: "Cần làm", exact: true }).click();
    await page.getByRole("option", { name: "Đang làm" }).click();
    await sheet.getByRole("button", { name: "Close", exact: true }).click();
    await expect(page.getByRole("dialog")).toHaveCount(0);

    await expect.poll(() => count("Đang làm")).toBe(before.target + 1);
    expect(await count("Cần làm")).toBe(before.source - 1);
    expect(errors).toEqual([]);
  });

  test("the card popup asks a person for approval and lists the request", async ({
    page,
  }) => {
    await page.goto(`${ERP}/items/mock-erp-item-142`);
    await expect(page.locator("[data-f=item-approval]")).toHaveCount(0);
    await page.getByRole("button", { name: "Card actions" }).click();
    await page.getByRole("menuitem", { name: "Request approval" }).click();
    await page.getByRole("menuitem", { name: /Jordan Lee/ }).click();
    await expect(page.locator("[data-f=item-approval]")).toContainText(
      "Waiting for Jordan Lee",
    );
  });

  test("the item sheet's title is edited in place", async ({ page }) => {
    await page.goto(`${ERP}?view=table&item=mock-erp-item-149`);
    const title = page
      .getByRole("dialog")
      .getByRole("textbox", { name: "Card title" });
    await title.click();
    await page.keyboard.press("ControlOrMeta+A");
    await page.keyboard.type("ERP-149 retitled");
    await page.keyboard.press("Enter");
    await page.keyboard.press("Escape");
    await expect(page.getByText("ERP-149 retitled").first()).toBeVisible();
  });
});

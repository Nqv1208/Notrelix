import { expect, test, type Page } from "@playwright/test";

const scenario = process.env.VITE_MOCK_STATE ?? "default";
const workspace = "/workspaces/mock-workspace-primary";

test.beforeEach(async ({ page }) => {
  test.skip(scenario !== "default", "Assertions need the default world data");
  await page.addInitScript(() => {
    try {
      window.localStorage.setItem("notrelix-locale", "vi");
    } catch {
      // Private mode: the default locale applies.
    }
  });
});

const PRIMARY = ["Trang chủ", "Việc của tôi", "Hộp thư", "Tài liệu", "Báo cáo"];

function sidebarNav(page: Page) {
  return page.getByRole("navigation", {
    name: "Điều hướng không gian làm việc",
  });
}

function chip(page: Page) {
  return page.getByRole("button", { name: /mở thanh hệ thống Notrelix$/ });
}

function rail(page: Page) {
  return page.getByRole("navigation", { name: "Hệ thống Notrelix" });
}

/**
 * Horizontal overflow at the page level AND per control (the helper of
 * account-pages.mock.e2e.spec.ts, widened to the whole document so the
 * open navigation drawer is measured too).
 */
async function overflow(page: Page) {
  return page.evaluate(() => {
    const viewport = window.innerWidth;
    const controls = Array.from(
      document.querySelectorAll<HTMLElement>(
        "input, button, [role='switch'], [role='radio'], [role='combobox'], a",
      ),
    );
    const outside = controls
      .map((element) => ({
        label:
          element.getAttribute("aria-label") ??
          element.textContent?.trim().slice(0, 40) ??
          element.tagName,
        box: element.getBoundingClientRect(),
        visible: element.checkVisibility(),
      }))
      .filter(
        ({ box, visible }) =>
          visible &&
          box.width > 0 &&
          (box.right > viewport + 0.5 || box.left < -0.5),
      )
      .map(({ label, box }) => `${label} [${box.left}..${box.right}]`);
    return {
      page: document.documentElement.scrollWidth - viewport,
      controls: controls.length,
      outside,
    };
  });
}

test("Sidebar lists the five primary items and opens the Reports list", async ({
  page,
}) => {
  await page.setViewportSize({ width: 1440, height: 900 });
  await page.goto(workspace);
  const primary = sidebarNav(page).getByRole("list", { name: "Chính" });
  await expect(primary.getByRole("link")).toHaveCount(PRIMARY.length);
  const names = await primary
    .getByRole("link")
    .evaluateAll((links) =>
      links.map((link) => link.getAttribute("aria-label") ?? link.textContent),
    );
  expect(names.map((name) => name?.split(",")[0]?.trim())).toEqual(PRIMARY);

  await primary.getByRole("link", { name: /^Báo cáo/ }).click();
  await expect(page).toHaveURL(
    /\/workspaces\/mock-workspace-primary\/reports$/,
  );
  await expect(
    page.getByRole("heading", { level: 1, name: "Báo cáo" }),
  ).toBeVisible();
  await expect(
    page.getByRole("table", { name: "Báo cáo" }).getByRole("row"),
  ).toHaveCount(4);
  // The desktop design has no top bar, so no breadcrumb either.
  await expect(page.getByRole("navigation", { name: "Đường dẫn" })).toHaveCount(
    0,
  );
});

test("The workspace chip opens the system rail; Escape closes it and returns focus to the chip", async ({
  page,
}) => {
  await page.setViewportSize({ width: 1440, height: 900 });
  await page.goto(workspace);
  await expect(rail(page)).toHaveCount(0);

  await chip(page).click();
  await expect(rail(page)).toBeVisible();
  await expect(chip(page)).toHaveAttribute("aria-expanded", "true");
  // Focus moves to the current workspace in the rail.
  await expect(rail(page).locator('[aria-current="page"]')).toBeFocused();

  await page.keyboard.press("Escape");
  await expect(rail(page)).toHaveCount(0);
  await expect(chip(page)).toBeFocused();
  await expect(chip(page)).toHaveAttribute("aria-expanded", "false");

  // A click that does not focus the chip (WebKit) still returns focus to it.
  await page.evaluate(() => (document.activeElement as HTMLElement).blur());
  await chip(page).dispatchEvent("click");
  await expect(rail(page)).toBeVisible();
  await page.keyboard.press("Escape");
  await expect(rail(page)).toHaveCount(0);
  await expect(chip(page)).toBeFocused();
});

test("Switching workspace from the rail goes through the workspace route", async ({
  page,
}) => {
  await page.setViewportSize({ width: 1440, height: 900 });
  await page.goto(workspace);
  await chip(page).click();
  const other = rail(page).locator("button[title]:not([aria-current])").first();
  await other.click();
  // The workspace route's beforeLoad runs prepareWorkspaceTransition.
  await expect(page).toHaveURL(/\/workspaces\/mock-workspace-secondary$/);
  await expect(rail(page)).toHaveCount(0);
});

test("Pinning the system rail survives a reload", async ({ page }) => {
  await page.setViewportSize({ width: 1440, height: 900 });
  await page.goto(workspace);
  await chip(page).click();
  const pin = rail(page).getByRole("button", {
    name: "Giữ thanh hệ thống Notrelix luôn mở",
  });
  await pin.click();
  await expect(rail(page)).toHaveAttribute("data-presentation", "pinned");
  await expect(pin).toHaveAttribute("aria-pressed", "true");
  // Pinning keeps the rail mounted: focus stays on the pin button.
  await expect(pin).toBeFocused();

  await page.reload();
  await expect(rail(page)).toHaveAttribute("data-presentation", "pinned");
  // The pinned rail pushes the sidebar: they do not overlap.
  const railBox = await rail(page).boundingBox();
  const sidebarBox = await sidebarNav(page)
    .getByRole("list", { name: "Chính" })
    .boundingBox();
  expect(railBox && sidebarBox).toBeTruthy();
  expect(sidebarBox!.x).toBeGreaterThanOrEqual(railBox!.x + railBox!.width);

  // Unpin: hidden again after a reload.
  await rail(page)
    .getByRole("button", { name: "Giữ thanh hệ thống Notrelix luôn mở" })
    .click();
  await expect(rail(page)).toHaveCount(0);
  await page.reload();
  await expect(chip(page)).toBeVisible();
  await expect(rail(page)).toHaveCount(0);
});

test("The sidebar still collapses and expands while the system rail is pinned", async ({
  page,
}) => {
  await page.setViewportSize({ width: 1440, height: 900 });
  await page.goto(workspace);
  await chip(page).click();
  const pin = rail(page).getByRole("button", {
    name: "Giữ thanh hệ thống Notrelix luôn mở",
  });
  await pin.click();
  await expect(rail(page)).toHaveAttribute("data-presentation", "pinned");

  const container = page.locator('[data-slot="sidebar-container"]');
  const main = page.locator("#workspace-main");
  // The desktop design has no top bar; the shortcut toggles the sidebar.
  const toggle = { click: () => page.keyboard.press("ControlOrMeta+b") };
  const railBox = await rail(page).boundingBox();
  const railRight = railBox!.x + railBox!.width;

  // Expanded: the sidebar sits right of the rail (after its 200 ms slide).
  await expect
    .poll(async () => (await container.boundingBox())?.x ?? -1)
    .toBeGreaterThanOrEqual(railRight - 0.5);

  // Collapse: the sidebar container leaves the screen and main is uncovered.
  await toggle.click();
  await expect
    .poll(async () => {
      const current = await container.boundingBox();
      return current ? current.x + current.width : 0;
    })
    .toBeLessThanOrEqual(0.5);
  await expect
    .poll(async () => (await main.boundingBox())?.x ?? -1)
    .toBeLessThanOrEqual(railRight + 0.5);
  const mainBox = await main.boundingBox();
  const hit = await page.evaluate(
    ({ x, y }) =>
      document.elementFromPoint(x, y)?.closest("#workspace-main") !== null,
    { x: mainBox!.x + 40, y: mainBox!.y + 40 },
  );
  expect(hit).toBe(true);

  // Expand again: the sidebar returns beside the rail.
  await toggle.click();
  await expect
    .poll(async () => (await container.boundingBox())?.x ?? -1)
    .toBeGreaterThanOrEqual(railRight - 0.5);
  const box = await container.boundingBox();
  expect(box!.x).toBeLessThanOrEqual(railRight + 0.5);
});

test("The workspace chip moves focus into a pinned rail", async ({ page }) => {
  await page.setViewportSize({ width: 1440, height: 900 });
  await page.goto(workspace);
  await chip(page).click();
  await rail(page)
    .getByRole("button", { name: "Giữ thanh hệ thống Notrelix luôn mở" })
    .click();
  await expect(rail(page)).toHaveAttribute("data-presentation", "pinned");

  await chip(page).focus();
  await page.keyboard.press("Enter");
  await expect(rail(page).locator('[aria-current="page"]')).toBeFocused();
  await expect(chip(page)).toHaveAttribute("aria-expanded", "true");
  await expect(rail(page)).toHaveAttribute("data-presentation", "pinned");
});

test("At 320 px the sidebar is the off-canvas sheet with the workspaces at its top and nothing overflows", async ({
  page,
}) => {
  await page.setViewportSize({ width: 320, height: 800 });
  await page.goto(workspace);
  await expect(
    page.getByRole("heading", { level: 1, name: /^Chào / }),
  ).toBeVisible();
  await expect(page.getByRole("status")).toHaveCount(0);
  let result = await overflow(page);
  expect(result.page).toBeLessThanOrEqual(0);
  expect(result.controls).toBeGreaterThan(0);
  expect(result.outside).toEqual([]);

  await page.getByRole("button", { name: "Hiện hoặc ẩn điều hướng" }).click();
  const sheet = page.getByRole("dialog", {
    name: "Điều hướng không gian làm việc",
  });
  await expect(sheet).toBeVisible();
  // Measure after the sheet's slide-in settles.
  await expect
    .poll(async () => (await sheet.boundingBox())?.x ?? -1)
    .toBeGreaterThanOrEqual(0);
  const drawerRail = sheet.getByRole("navigation", {
    name: "Hệ thống Notrelix",
  });
  await expect(drawerRail).toHaveAttribute("data-presentation", "drawer");
  await expect(drawerRail.locator('[aria-current="page"]')).toBeVisible();

  // The workspaces sit at the top of the sheet, above the workspace chip.
  const sheetBox = await sheet.boundingBox();
  const railBox = await drawerRail.boundingBox();
  const chipBox = await sheet
    .getByRole("button", { name: /mở thanh hệ thống Notrelix$/ })
    .boundingBox();
  expect(railBox!.y - sheetBox!.y).toBeLessThanOrEqual(1);
  expect(chipBox!.y).toBeGreaterThanOrEqual(railBox!.y + railBox!.height - 1);
  // On mobile the chip opens no rail, so it claims no expanded state.
  await expect(
    sheet.getByRole("button", { name: /mở thanh hệ thống Notrelix$/ }),
  ).not.toHaveAttribute("aria-expanded", /.*/);

  result = await overflow(page);
  expect(result.page).toBeLessThanOrEqual(0);
  expect(result.outside).toEqual([]);
});

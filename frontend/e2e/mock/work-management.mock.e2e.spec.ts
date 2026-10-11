import { expect, test } from "@playwright/test";

const scenario = process.env.VITE_MOCK_STATE ?? "default";

test("renders a production Work Management route through mock transport", async ({
  page,
}) => {
  const errors: string[] = [];
  page.on("pageerror", (error) => errors.push(error.message));

  if (scenario !== "default") {
    // Work management board requires default world data.
    // For other scenarios, just verify home loads without mock errors.
    await page.goto("/home");
    expect(
      errors.filter((message) =>
        message.includes("MockUnhandledOperationError"),
      ),
    ).toEqual([]);
    return;
  }

  await page.goto(
    "/workspaces/mock-workspace-primary/boards/mock-board-roadmap",
  );
  await expect(page.getByText("Product Roadmap").first()).toBeVisible();
  await expect(page.getByText("Ship mock runtime").first()).toBeVisible();
  expect(
    errors.filter((message) => message.includes("MockUnhandledOperationError")),
  ).toEqual([]);
});

test.describe("Kanban item detail through the item search parameter", () => {
  test.skip(scenario !== "default", "Assertions need the default world data");

  const board = "/workspaces/mock-workspace-primary/boards/mock-board-roadmap";

  test.beforeEach(async ({ page }) => {
    await page.addInitScript(() => {
      try {
        window.localStorage.setItem("notrelix-locale", "en");
      } catch {
        // Private mode: the default locale applies.
      }
    });
  });

  test("opening a card writes item, a reload reopens it and Escape removes it", async ({
    page,
  }) => {
    const errors: string[] = [];
    page.on("pageerror", (error) => errors.push(error.message));

    await page.goto(`${board}?view=kanban&filter=mine&sort=due&groupBy=status`);
    await page
      .getByRole("button", { name: "Ship mock runtime", exact: true })
      .click();

    const dialog = page.getByRole("dialog", { name: "Ship mock runtime" });
    await expect(dialog).toBeVisible();
    await expect(page).toHaveURL(/[?&]item=/);
    const opened = new URL(page.url());
    const item = opened.searchParams.get("item");
    expect(item).toBeTruthy();
    expect(opened.searchParams.get("view")).toBe("kanban");
    expect(opened.searchParams.get("filter")).toBe("mine");
    expect(opened.searchParams.get("sort")).toBe("due");
    expect(opened.searchParams.get("groupBy")).toBe("status");

    await page.reload();
    await expect(
      page.getByRole("dialog", { name: "Ship mock runtime" }),
    ).toBeVisible();
    expect(new URL(page.url()).searchParams.get("item")).toBe(item);

    await page.keyboard.press("Escape");
    await expect(page.getByRole("dialog")).toHaveCount(0);
    const closed = new URL(page.url());
    expect(closed.searchParams.has("item")).toBe(false);
    expect(closed.searchParams.get("view")).toBe("kanban");
    expect(closed.searchParams.get("filter")).toBe("mine");
    expect(closed.searchParams.get("sort")).toBe("due");
    expect(closed.searchParams.get("groupBy")).toBe("status");
    // Focus returns to the card that opened the sheet.
    await expect(
      page.getByRole("button", { name: "Ship mock runtime", exact: true }),
    ).toBeFocused();

    // Closing replaced the history entry, so Back does not reopen the sheet.
    await page.goBack();
    await expect(page.getByRole("dialog")).toHaveCount(0);
    expect(errors).toEqual([]);
  });

  test("an unknown item id shows no detail and does not throw", async ({
    page,
  }) => {
    const errors: string[] = [];
    page.on("pageerror", (error) => errors.push(error.message));

    await page.goto(`${board}?view=kanban&item=no-such-card`);
    await expect(
      page.getByRole("button", { name: "Ship mock runtime", exact: true }),
    ).toBeVisible();
    await expect(page.getByRole("dialog")).toHaveCount(0);
    // The stale id is cleared from the URL once the lookup fails.
    await expect
      .poll(() => new URL(page.url()).searchParams.has("item"), {
        timeout: 15_000,
      })
      .toBe(false);
    expect(new URL(page.url()).searchParams.get("view")).toBe("kanban");
    expect(errors).toEqual([]);
  });
});

test.describe("Board header and view bar at 320 px", () => {
  test.skip(scenario !== "default", "Assertions need the default world data");

  const board = "/workspaces/mock-workspace-primary/boards/mock-board-roadmap";

  test.beforeEach(async ({ page }) => {
    await page.setViewportSize({ width: 320, height: 640 });
    await page.addInitScript(() => {
      try {
        window.localStorage.setItem("notrelix-locale", "en");
      } catch {
        // Private mode: the default locale applies.
      }
    });
  });

  test("the header fits the viewport, the tabs are reachable by keyboard and the Add view menu stacks above the board", async ({
    page,
  }) => {
    const errors: string[] = [];
    page.on("pageerror", (error) => errors.push(error.message));

    await page.goto(`${board}?view=kanban`);
    await expect(
      page.getByRole("button", { name: "Ship mock runtime", exact: true }),
    ).toBeVisible();

    const overflow = await page.evaluate(
      () => document.documentElement.scrollWidth - window.innerWidth,
    );
    expect(overflow).toBeLessThanOrEqual(0);

    // The header controls are inside the viewport, not clipped off to the right.
    const addView = page.getByRole("button", { name: "Add view" });
    await expect(addView).toBeVisible();
    const addBox = await addView.boundingBox();
    expect(addBox).not.toBeNull();
    expect((addBox?.x ?? -1) + (addBox?.width ?? 0)).toBeLessThanOrEqual(320);
    expect(addBox?.x ?? -1).toBeGreaterThanOrEqual(0);

    // Keyboard reaches every view tab; the focused tab is scrolled into view.
    const tablist = page.getByRole("tablist", { name: "Board views" });
    await expect(tablist.getByRole("tab", { name: "Board" })).toHaveAttribute(
      "aria-selected",
      "true",
    );
    await tablist.getByRole("tab", { name: "Board" }).focus();
    for (const name of ["Table", "Calendar", "Timeline"]) {
      await page.keyboard.press("ArrowRight");
      await expect(
        tablist.getByRole("tab", { name, exact: true }),
      ).toBeFocused();
    }
    // Focus scrolls the tab strip, so the focused tab ends up inside the
    // viewport (polled: the browser may animate the scroll).
    await expect
      .poll(async () => {
        const box = await tablist
          .getByRole("tab", { name: "Timeline" })
          .boundingBox();
        return box ? box.x >= 0 && box.x + box.width <= 320 : false;
      })
      .toBe(true);
    // Arrow keys move focus only; the view does not change until Enter.
    expect(new URL(page.url()).searchParams.get("view")).toBe("kanban");

    // The Add view menu opens above the board and closes on Escape.
    await addView.click();
    const menu = page.getByRole("menu");
    await expect(menu).toBeVisible();
    const item = menu.getByRole("menuitem", { name: "Calendar" });
    const itemBox = await item.boundingBox();
    expect(itemBox).not.toBeNull();
    const topmostIsMenu = await page.evaluate(
      ({ x, y }) => {
        const hit = document.elementFromPoint(x, y);
        return Boolean(hit?.closest('[role="menu"]'));
      },
      {
        x: (itemBox?.x ?? 0) + (itemBox?.width ?? 0) / 2,
        y: (itemBox?.y ?? 0) + (itemBox?.height ?? 0) / 2,
      },
    );
    expect(topmostIsMenu).toBe(true);
    const menuBox = await menu.boundingBox();
    expect((menuBox?.x ?? -1) + (menuBox?.width ?? 0)).toBeLessThanOrEqual(320);
    await page.keyboard.press("Escape");
    await expect(menu).toHaveCount(0);
    await expect(addView).toBeFocused();

    // An outside press closes it too, without choosing a view.
    await addView.click();
    await expect(menu).toBeVisible();
    await page.mouse.click(5, 400);
    await expect(menu).toHaveCount(0);
    expect(new URL(page.url()).searchParams.get("view")).toBe("kanban");

    // Choosing a view from the menu switches to it.
    await addView.click();
    await page.getByRole("menuitem", { name: "Calendar" }).click();
    await expect(page).toHaveURL(/[?&]view=calendar/);
    expect(errors).toEqual([]);
  });
});

test.describe("Board header in a real browser", () => {
  test.skip(scenario !== "default", "Assertions need the default world data");

  const board = "/workspaces/mock-workspace-primary/boards/mock-board-roadmap";

  test.beforeEach(async ({ page }) => {
    await page.addInitScript(() => {
      try {
        window.localStorage.setItem("notrelix-locale", "en");
      } catch {
        // Private mode: the default locale applies.
      }
    });
  });

  for (const mode of ["light", "dark"] as const) {
    test(`the selected view tab looks different from the others in ${mode} and no tab references a missing panel`, async ({
      page,
    }) => {
      await page.addInitScript((value) => {
        try {
          window.localStorage.setItem("notrelix-theme-mode", value);
        } catch {
          // Private mode: the default theme applies.
        }
      }, mode);
      await page.goto(`${board}?view=table`);
      const selected = page.getByRole("tab", { name: "Table" });
      const other = page.getByRole("tab", { name: "Board" });
      await expect(selected).toHaveAttribute("aria-selected", "true");
      const paint = (locator: typeof selected) =>
        locator.evaluate((element) => {
          const style = getComputedStyle(element);
          return {
            background: style.backgroundColor,
            underline: style.borderBottomColor,
          };
        });
      const selectedPaint = await paint(selected);
      const otherPaint = await paint(other);
      // The selected tab carries a visible tint that unselected tabs do not.
      expect(selectedPaint.background).not.toBe(otherPaint.background);
      expect(selectedPaint.background).not.toMatch(/rgba\(0, 0, 0, 0\)|\/ 0\)/);
      expect(selectedPaint.underline).not.toBe(otherPaint.underline);

      // Hover gives an unselected tab a background too.
      await other.hover();
      await expect
        .poll(async () => (await paint(other)).background)
        .not.toBe(otherPaint.background);

      const dangling = await page.evaluate(() =>
        Array.from(document.querySelectorAll('[role="tab"][aria-controls]'))
          .map((tab) => tab.getAttribute("aria-controls") ?? "")
          .filter((id) => !document.getElementById(id)),
      );
      expect(dangling).toEqual([]);
    });
  }

  for (const width of [320, 1280]) {
    test(`keyboard order follows the visual order at ${width} px`, async ({
      page,
    }) => {
      await page.setViewportSize({ width, height: 700 });
      await page.goto(`${board}?view=kanban`);
      const tab = page.getByRole("tab", { name: "Board" });
      const addView = page.getByRole("button", { name: "Add view" });
      await expect(addView).toBeVisible();
      await tab.focus();
      await page.keyboard.press("Tab");
      await expect(addView).toBeFocused();
      const tabBox = await tab.boundingBox();
      const addBox = await addView.boundingBox();
      if (!tabBox || !addBox) throw new Error("header controls not laid out");
      // The next control in focus order is later in reading order: on a lower
      // row, or on the same row to the right.
      const centre = (box: { y: number; height: number }) =>
        box.y + box.height / 2;
      const sameRow =
        Math.abs(centre(addBox) - centre(tabBox)) <
        Math.min(addBox.height, tabBox.height) / 2;
      expect(sameRow ? addBox.x > tabBox.x : addBox.y > tabBox.y).toBe(true);
    });
  }

  for (const [scheme, width] of [
    ["light", 1280],
    ["dark", 1280],
    ["light", 320],
    ["dark", 320],
  ] as const) {
    test(`header screenshot ${scheme} ${width}`, async ({ page }) => {
      test.skip(!process.env.HEADER_SHOTS_DIR, "Manual capture only");
      await page.addInitScript((mode) => {
        try {
          window.localStorage.setItem("notrelix-theme-mode", mode);
        } catch {
          // Private mode: the default theme applies.
        }
      }, scheme);
      await page.setViewportSize({ width, height: 400 });
      await page.goto(`${board}?view=table`);
      await expect(page.getByRole("tablist")).toBeVisible();
      await page.getByRole("tab", { name: "Table" }).hover();
      await page.getByRole("tab", { name: "Calendar" }).focus();
      await page.keyboard.press("Shift+Tab");
      await page.getByRole("tab", { name: "Board" }).hover();
      await page.waitForTimeout(300);
      await page.screenshot({
        path: `${process.env.HEADER_SHOTS_DIR}/header-${scheme}-${width}.png`,
        clip: { x: 0, y: 0, width, height: 240 },
      });
    });
  }
});

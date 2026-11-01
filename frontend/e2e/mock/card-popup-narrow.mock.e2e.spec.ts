import AxeBuilder from "@axe-core/playwright";
import { expect, test } from "@playwright/test";

/**
 * UC-WM-A24 at 390 px: a custom text field whose value is wider than its row ends in an ellipsis
 * (the full value in the title) instead of being cut at the edge of the popup.
 */

const scenario = process.env.VITE_MOCK_STATE ?? "default";
const BOARD = "/workspaces/mock-workspace-primary/boards/mock-board-erp";
const CARD = "Chuyển dữ liệu khách hàng từ hệ thống cũ sang ERP";
const THEME_KEY = "notrelix-theme-mode";

test.beforeEach(() => {
  test.skip(scenario !== "default", "Assertions need the default world data");
});

for (const [width, height] of [
  [390, 844],
  [1440, 900],
] as const) {
  for (const dark of [false, true]) {
    test(`a long custom field value ends in an ellipsis with the full value as title, ${width} ${dark ? "dark" : "light"}`, async ({
      page,
    }) => {
      await page.setViewportSize({ width, height });
      await page.addInitScript(
        ({ theme, mode }) => {
          try {
            window.localStorage.setItem("notrelix-locale", "vi");
            window.localStorage.setItem(theme, mode);
          } catch {
            // Private mode: defaults apply.
          }
        },
        { theme: THEME_KEY, mode: dark ? "dark" : "light" },
      );
      await page.goto(BOARD);
      await page.getByRole("button", { name: CARD, exact: true }).click();
      const dialog = page.getByRole("dialog", { name: CARD });
      await expect(dialog).toBeVisible();
      await page.waitForTimeout(700);

      const fields = dialog.locator("[data-field-row] input[type=text]");
      await expect(fields.first()).toBeVisible();
      const frame = (await dialog.boundingBox())!;
      const report = await fields.evaluateAll((inputs, frameRight) => {
        return (inputs as HTMLInputElement[]).map((input) => {
          const box = input.getBoundingClientRect();
          return {
            name: input.getAttribute("aria-label"),
            value: input.value,
            title: input.title,
            overflows: input.scrollWidth > input.clientWidth,
            textOverflow: getComputedStyle(input).textOverflow,
            right: box.right,
            frameRight,
          };
        });
      }, frame.x + frame.width);
      expect(report.length).toBeGreaterThan(0);
      for (const field of report) {
        expect(field.title, `${field.name}: full value in the title`).toBe(
          field.value,
        );
        expect(field.textOverflow, `${field.name} text-overflow`).toBe(
          "ellipsis",
        );
        expect(
          field.right,
          `${field.name} stays inside the popup`,
        ).toBeLessThanOrEqual(field.frameRight + 0.5);
      }
      // At 390 px the value of the "Task" field is wider than its row: it is the case this guards.
      if (width < 500)
        expect(
          report.some((field) => field.overflows),
          "a field value overflows its row at 390 px",
        ).toBe(true);
    });
  }
}

for (const [width, height] of [
  [390, 844],
  [1440, 900],
] as const) {
  for (const dark of [false, true]) {
    for (const blocked of [false, true]) {
      test(`copy link in the card popup: ${blocked ? "a blocked clipboard says so" : "the toast confirms"}, ${width} ${dark ? "dark" : "light"}`, async ({
        page,
      }) => {
        await page.setViewportSize({ width, height });
        await page.addInitScript(
          ({ theme, mode, deny }) => {
            try {
              window.localStorage.setItem("notrelix-locale", "vi");
              window.localStorage.setItem(theme, mode);
            } catch {
              // Private mode: defaults apply.
            }
            const writes: string[] = [];
            (window as unknown as { __clip: string[] }).__clip = writes;
            Object.defineProperty(navigator, "clipboard", {
              configurable: true,
              value: {
                writeText: (text: string) => {
                  if (deny) return Promise.reject(new Error("blocked"));
                  writes.push(text);
                  return Promise.resolve();
                },
              },
            });
          },
          { theme: THEME_KEY, mode: dark ? "dark" : "light", deny: blocked },
        );
        await page.goto(BOARD);
        await page.getByRole("button", { name: CARD, exact: true }).click();
        const dialog = page.getByRole("dialog", { name: CARD });
        await expect(dialog).toBeVisible();
        // Keyboard only: focus the actions button, open the menu with Enter, activate the item with Enter.
        await dialog.getByRole("button", { name: "Thao tác trên thẻ" }).focus();
        await page.keyboard.press("Enter");
        const item = page.getByRole("menuitem", { name: /^Sao chép liên kết/ });
        await expect(item).toBeVisible();
        await item.focus();
        await page.keyboard.press("Enter");
        const toast = page.getByText(
          blocked ? "Không sao chép được liên kết" : "Đã sao chép liên kết",
        );
        await expect(toast).toBeVisible();
        const written = await page.evaluate(
          () => (window as unknown as { __clip: string[] }).__clip,
        );
        if (blocked) expect(written).toEqual([]);
        else expect(written[0]).toContain("item=");
        const box = (await toast.boundingBox())!;
        expect(box.x, "toast inside the viewport").toBeGreaterThanOrEqual(0);
        expect(
          box.x + box.width,
          "toast inside the viewport",
        ).toBeLessThanOrEqual(width);
        await page.waitForTimeout(500);
        const results = await new AxeBuilder({ page }).analyze();
        expect(
          results.violations
            .filter((v) => v.impact === "serious" || v.impact === "critical")
            .map((v) => ({
              id: v.id,
              nodes: v.nodes.map((n) => n.target.join(" ")),
            })),
        ).toEqual([]);
      });
    }
  }
}

for (const [width, height] of [
  [390, 844],
  [1440, 900],
] as const) {
  for (const dark of [false, true]) {
    for (const blocked of [false, true]) {
      test(`copy board link from the board header: ${blocked ? "a blocked clipboard says so" : "the toast confirms"}, ${width} ${dark ? "dark" : "light"}`, async ({
        page,
      }) => {
        await page.setViewportSize({ width, height });
        await page.addInitScript(
          ({ theme, mode, deny }) => {
            try {
              window.localStorage.setItem("notrelix-locale", "vi");
              window.localStorage.setItem(theme, mode);
            } catch {
              // Private mode: defaults apply.
            }
            const writes: string[] = [];
            (window as unknown as { __clip: string[] }).__clip = writes;
            Object.defineProperty(navigator, "clipboard", {
              configurable: true,
              value: {
                writeText: (text: string) => {
                  if (deny) return Promise.reject(new Error("blocked"));
                  writes.push(text);
                  return Promise.resolve();
                },
              },
            });
          },
          { theme: THEME_KEY, mode: dark ? "dark" : "light", deny: blocked },
        );
        await page.goto(BOARD);
        await expect(
          page.getByRole("button", { name: CARD, exact: true }),
        ).toBeVisible();
        // The board header comes first in the page; the cards below carry their own "more" buttons.
        const more = page
          .getByRole("button", { name: "Thêm tuỳ chọn" })
          .first();
        await more.focus();
        await page.keyboard.press("Enter");
        const item = page.getByRole("menuitem", {
          name: "Sao chép liên kết board",
        });
        await expect(item).toBeVisible();
        await item.focus();
        await page.keyboard.press("Enter");
        const toast = page.getByText(
          blocked ? "Không sao chép được liên kết" : "Đã sao chép liên kết",
        );
        await expect(toast).toBeVisible();
        const written = await page.evaluate(
          () => (window as unknown as { __clip: string[] }).__clip,
        );
        if (blocked) expect(written).toEqual([]);
        else {
          expect(written[0]).toContain("/boards/mock-board-erp");
          expect(written[0]).not.toContain("item=");
        }
        const box = (await toast.boundingBox())!;
        expect(box.x).toBeGreaterThanOrEqual(0);
        expect(box.x + box.width).toBeLessThanOrEqual(width);
        await page.waitForTimeout(500);
        const results = await new AxeBuilder({ page }).analyze();
        expect(
          results.violations
            .filter((v) => v.impact === "serious" || v.impact === "critical")
            .map((v) => ({
              id: v.id,
              nodes: v.nodes.map((n) => n.target.join(" ")),
            })),
        ).toEqual([]);
      });
    }
  }
}

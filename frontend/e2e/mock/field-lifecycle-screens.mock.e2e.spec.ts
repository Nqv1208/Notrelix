import AxeBuilder from "@axe-core/playwright";
import { expect, test, type Page } from "@playwright/test";

/**
 * The screen review of the field delete question, which no artboard draws (`WM-DeletedFields` draws the list a
 * deleted field goes to): the Data fields page with a field's panel and the question open, in Vietnamese, light
 * and dark, at 1440, 1024 and 390. It names what stays with the field (restorable for 30 days) and must keep
 * inside the window (no sideways scroll, nothing clipped at the right edge), report no serious axe violation,
 * and at 390 every control be at least 44 px in both directions. The screenshots go to
 * `test-results/field-lifecycle-screens` to be opened and read; they are not baselines.
 */
const WS = "/workspaces/mock-workspace-primary";
const ROADMAP = `${WS}/boards/mock-board-roadmap`;
const persona = process.env.VITE_MOCK_PERSONA ?? "owner";
const scenario = process.env.VITE_MOCK_STATE ?? "default";
test.skip(
  persona !== "owner" || scenario !== "default",
  "Owner checks need the default owner world",
);

const SCREENS = {
  "field-delete-question": async (page: Page) => {
    await page.goto(`${ROADMAP}/settings?from=table&section=fields`);
    await page.getByRole("button", { name: "Sửa trường Priority" }).click();
    await page.getByRole("button", { name: "Xoá trường" }).first().click();
    const dialog = page.getByRole("alertdialog");
    await expect(dialog).toBeVisible();
    await expect(dialog).toContainText("30 ngày");
  },
  "field-deleted-list": async (page: Page) => {
    await page.goto(`${ROADMAP}/settings?from=table&section=fields`);
    await page.getByRole("tab", { name: /^Đã xoá/ }).click();
    await expect(page.getByRole("tabpanel")).toBeVisible();
  },
} as const;

for (const [name, show] of Object.entries(SCREENS)) {
  for (const width of [1440, 1024, 390]) {
    for (const dark of [false, true]) {
      const mode = dark ? "dark" : "light";
      test(`${name} at ${width} ${mode}: inside the window, no serious axe violation${width === 390 ? ", 44 px controls" : ""}`, async ({
        page,
      }) => {
        await page.setViewportSize({ width, height: 900 });
        await page.clock.setFixedTime(new Date("2026-10-12T10:40:00+07:00"));
        await page.addInitScript((theme) => {
          try {
            window.localStorage.setItem("notrelix-locale", "vi");
            window.localStorage.setItem("notrelix-theme-mode", theme);
          } catch {
            // Private mode: the defaults apply.
          }
        }, mode);
        await show(page);
        await page.waitForTimeout(400);
        await page.screenshot({
          path: `test-results/field-lifecycle-screens/${name}-${width}-${mode}.png`,
        });

        expect(
          await page.evaluate(
            () => document.documentElement.scrollWidth - window.innerWidth,
          ),
          "sideways scroll",
        ).toBeLessThanOrEqual(0);
        const clipped = await page.evaluate(() => {
          const limit = window.innerWidth;
          return [
            ...document.querySelectorAll<HTMLElement>(
              "main *, [role=dialog] *",
            ),
          ]
            .filter((node) => {
              const rect = node.getBoundingClientRect();
              const style = getComputedStyle(node);
              return (
                rect.width > 0 &&
                style.visibility !== "hidden" &&
                rect.right > limit + 1 &&
                !node.closest("[aria-hidden=true]") &&
                // A region that scrolls sideways by design (a tab strip, a wide table) clips on purpose.
                !(() => {
                  for (
                    let up: HTMLElement | null = node.parentElement;
                    up;
                    up = up.parentElement
                  ) {
                    const overflowX = getComputedStyle(up).overflowX;
                    if (overflowX === "auto" || overflowX === "scroll")
                      return true;
                  }
                  return false;
                })()
              );
            })
            .slice(0, 5)
            .map(
              (node) =>
                `${node.tagName.toLowerCase()} ${Math.round(node.getBoundingClientRect().right)}`,
            );
        });
        expect(clipped, "elements past the right edge").toEqual([]);

        const violations = (
          await new AxeBuilder({ page }).analyze()
        ).violations.filter((v) =>
          ["serious", "critical"].includes(v.impact ?? ""),
        );
        expect(violations.map((v) => `${v.id}: ${v.nodes.length}`)).toEqual([]);

        if (width === 390) {
          const small = await page.evaluate(() =>
            [
              ...document.querySelectorAll<HTMLElement>(
                "main :is(button, a[href], input:not([type=hidden]), select, textarea, [role=tab], [role=checkbox], [role=radio]), [role=dialog] :is(button, a[href], input:not([type=hidden]), select, textarea)",
              ),
            ]
              .filter((node) => {
                const rect = node.getBoundingClientRect();
                const style = getComputedStyle(node);
                return (
                  rect.width > 0 &&
                  style.visibility !== "hidden" &&
                  !node.closest("[aria-hidden=true]") &&
                  (rect.width < 44 || rect.height < 44) &&
                  // A row marked as the target (a checkbox with its text) counts by the row.
                  !(
                    (node.closest("[data-hit-row]")?.getBoundingClientRect()
                      .height ?? 0) >= 44
                  )
                );
              })
              .slice(0, 8)
              .map((node) => {
                const rect = node.getBoundingClientRect();
                return `${node.tagName.toLowerCase()}[${(node.getAttribute("aria-label") ?? node.textContent ?? "").trim().slice(0, 24)}] ${Math.round(rect.width)}x${Math.round(rect.height)}`;
              }),
          );
          expect(small, "controls under 44 px").toEqual([]);
        }
      });
    }
  }
}

import AxeBuilder from "@axe-core/playwright";
import { expect, test } from "@playwright/test";

/**
 * UC-IDN-D02 at 390 px: the sign-in page that opens after a session ended keeps its footer in the
 * viewport, the language switch reads on one line, and every footer control is a 44 px target.
 */

const scenario = process.env.VITE_MOCK_STATE ?? "default";
const THEME_KEY = "notrelix-theme-mode";
const BEFORE_1440 = { x: 1210.34, y: 842, width: 181.66, height: 34 };

test.beforeEach(() => {
  test.skip(scenario !== "default", "Assertions need the default world data");
});

for (const locale of ["vi", "en"] as const) {
  for (const dark of [false, true]) {
    test(`sign-in footer at 390 px: one-line language switch, ${locale} ${dark ? "dark" : "light"}`, async ({
      page,
    }) => {
      await page.setViewportSize({ width: 390, height: 844 });
      await page.addInitScript(
        ({ lang, theme, mode }) => {
          try {
            window.localStorage.setItem("notrelix-locale", lang);
            window.localStorage.setItem(theme, mode);
          } catch {
            // Private mode: defaults apply.
          }
        },
        { lang: locale, theme: THEME_KEY, mode: dark ? "dark" : "light" },
      );
      await page.goto("/sign-in?reason=session-ended");
      const group = page.getByRole("group", {
        name: locale === "vi" ? "Ngôn ngữ" : "Language",
      });
      await expect(group).toBeVisible();

      const report = await group.evaluate((element) => {
        const buttons = Array.from(element.querySelectorAll("button"));
        return {
          viewport: window.innerWidth,
          pageOverflow:
            document.documentElement.scrollWidth - window.innerWidth,
          group: element.getBoundingClientRect().toJSON(),
          buttons: buttons.map((button) => {
            const range = document.createRange();
            range.selectNodeContents(button.firstChild ?? button);
            const lines = new Set(
              Array.from(range.getClientRects()).map((r) => Math.round(r.top)),
            );
            const box = button.getBoundingClientRect();
            return {
              label: button.textContent?.trim(),
              lines: lines.size,
              width: Math.round(box.width),
              height: Math.round(box.height),
            };
          }),
        };
      });
      for (const button of report.buttons) {
        expect(button.lines, `${button.label} wraps`).toBe(1);
        expect(
          button.height,
          `${button.label} is a 44 px target`,
        ).toBeGreaterThanOrEqual(44);
      }
      expect(
        report.group.right,
        "switch stays in the viewport",
      ).toBeLessThanOrEqual(report.viewport);
      expect(report.pageOverflow, "page scrolls sideways").toBeLessThanOrEqual(
        0,
      );

      // The page fades in on load; axe must read the settled colours.
      await page.waitForTimeout(900);
      const results = await new AxeBuilder({ page }).analyze();
      expect(
        results.violations
          .filter((v) => v.impact === "serious" || v.impact === "critical")
          .map((v) => ({
            id: v.id,
            nodes: v.nodes.map(
              (n) => `${n.target.join(" ")} ${n.any[0]?.message ?? ""}`,
            ),
          })),
      ).toEqual([]);
    });
  }
}

test("sign-in footer at 1440 px keeps the language switch where it was", async ({
  page,
}) => {
  await page.setViewportSize({ width: 1440, height: 900 });
  await page.addInitScript(() => {
    try {
      window.localStorage.setItem("notrelix-locale", "vi");
    } catch {
      // Private mode: defaults apply.
    }
  });
  await page.goto("/sign-in");
  const group = page.getByRole("group", { name: "Ngôn ngữ" });
  await expect(group).toBeVisible();
  await page.waitForTimeout(300);
  const box = await group.boundingBox();
  // Measured on develop before the 390 px change: the pill is the same box at desktop width.
  for (const key of ["x", "y", "width", "height"] as const)
    expect(Math.abs(box![key] - BEFORE_1440[key]), key).toBeLessThanOrEqual(1);
});

for (const dark of [false, true]) {
  test(`sign-in page after a session ended is axe-clean at 1440 px, ${dark ? "dark" : "light"}`, async ({
    page,
  }) => {
    await page.setViewportSize({ width: 1440, height: 900 });
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
    await page.goto("/sign-in?reason=session-ended");
    await expect(
      page.getByRole("status").filter({ hasText: "phiên" }),
    ).toBeVisible();
    await page.waitForTimeout(900);
    const results = await new AxeBuilder({ page }).analyze();
    expect(
      results.violations
        .filter((v) => v.impact === "serious" || v.impact === "critical")
        .map((v) => v.id),
    ).toEqual([]);
  });
}

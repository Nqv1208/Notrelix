import AxeBuilder from "@axe-core/playwright";
import { expect, type Locator, type Page } from "@playwright/test";

/**
 * The shared acceptance audit of the Workspaces screens the canvas does not draw (design line 3): at 1440, 1024 and
 * 390 px in light and dark the page does not scroll sideways, axe reports no serious or critical violation (colour
 * contrast included) and, at 390 px, every control is a 44 px target. Set `WSP_SHOTS` to a folder to also write a
 * screenshot of each state.
 */

const shots = process.env.WSP_SHOTS;
const THEME_KEY = "notrelix-theme-mode";
export const WORKSPACE = "/workspaces/mock-workspace-primary";

export const viewports = [
  { width: 1440, height: 900 },
  { width: 1024, height: 768 },
  { width: 390, height: 844 },
] as const;

const INTERACTIVE =
  'button, a[href], input:not([type="hidden"]), select, textarea, [role="button"], [role="tab"], [role="combobox"], [role="menuitem"], [role="radio"], [role="checkbox"], [role="switch"]';

export async function prepare(
  page: Page,
  viewport: (typeof viewports)[number],
  dark: boolean,
) {
  await page.setViewportSize(viewport);
  await page.addInitScript(
    ({ theme, mode }) => {
      try {
        window.localStorage.setItem("notrelix-locale", "en");
        window.localStorage.setItem(theme, mode);
      } catch {
        // Private mode: the defaults apply.
      }
    },
    { theme: THEME_KEY, mode: dark ? "dark" : "light" },
  );
}

/** The visible controls under `scope` whose smaller side is under 44 px or that end beyond the viewport. */
async function undersized(scope: Locator) {
  return scope.evaluate((root, selector) => {
    const found: string[] = [];
    for (const element of Array.from(root.querySelectorAll(selector))) {
      // A radio or checkbox inside a label is operated through the whole label row.
      const isChoice =
        element.matches('input[type="radio"], input[type="checkbox"]') ||
        ["radio", "checkbox"].includes(element.getAttribute("role") ?? "");
      const target = (isChoice && element.closest("label")) || element;
      const box = target.getBoundingClientRect();
      const style = getComputedStyle(element);
      if (box.width === 0 || box.height === 0) continue;
      if (style.visibility === "hidden" || style.display === "none") continue;
      if (element.closest(".sr-only, [aria-hidden='true']")) continue;
      if (box.right > window.innerWidth + 1) {
        found.push(
          `${element.tagName.toLowerCase()} "${element.getAttribute("aria-label") ?? (element.textContent ?? "").trim().slice(0, 30)}" ends at ${Math.round(box.right)} of ${window.innerWidth}`,
        );
        continue;
      }
      if (Math.min(box.width, box.height) >= 43.5) continue;
      const label =
        element.getAttribute("aria-label") ??
        (element.textContent ?? "").trim().slice(0, 40) ??
        element.tagName;
      found.push(
        `${element.tagName.toLowerCase()} "${label}" ${Math.round(box.width)}x${Math.round(box.height)}`,
      );
    }
    return found;
  }, INTERACTIVE);
}

export async function audit(
  page: Page,
  name: string,
  viewport: (typeof viewports)[number],
  dark: boolean,
  scope: Locator,
) {
  // Pages fade in on load; axe must read the settled colours.
  await page.waitForTimeout(900);
  const overflow = await page.evaluate(
    () => document.documentElement.scrollWidth - window.innerWidth,
  );
  expect
    .soft(overflow, `${name}: page scrolls sideways`)
    .toBeLessThanOrEqual(0);
  if (viewport.width === 390) {
    expect
      .soft(await undersized(scope), `${name}: targets under 44 px`)
      .toEqual([]);
  }
  const results = await new AxeBuilder({ page }).analyze();
  expect
    .soft(
      results.violations
        .filter((violation) =>
          ["serious", "critical"].includes(violation.impact ?? ""),
        )
        .map(
          (violation) =>
            `${violation.id}: ${violation.nodes
              .map(
                (node) =>
                  `${node.target.join(" ")} ${node.failureSummary?.split("\n")[1] ?? ""}`,
              )
              .join(" | ")}`,
        ),
      `${name}: axe`,
    )
    .toEqual([]);
  if (shots) {
    await page.screenshot({
      path: `${shots}/${name}-${viewport.width}-${dark ? "dark" : "light"}.png`,
      fullPage: true,
    });
  }
}

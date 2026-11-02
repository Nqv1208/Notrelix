import AxeBuilder from "@axe-core/playwright";
import { expect, test, type Page } from "@playwright/test";

/**
 * The Board share dialog in the states the canvas does not draw (design line 3), at 1440, 1024 and 390 px in light
 * and dark, in Vietnamese: the people list that could not be read (offline), a person who may not manage the Board
 * (permission denied), and a level change refused because a second manager changed the grant first (conflict).
 * The page does not scroll sideways, the dialog stays inside the window, axe finds nothing serious and, at 390 px,
 * every control is a 44 px target. `GOV_SHOTS` writes a screenshot of each state for review.
 *
 * Each state needs its own server (the mock world and its faults are set at start):
 *   offline:   VITE_MOCK_FAULTS=/boards/mock-board-erp/sharing=network
 *   conflict:  VITE_MOCK_FAULTS=/resources/work-management.board/mock-board-erp/permissions=stale
 *   denied:    VITE_MOCK_PERSONA=viewer
 */
const persona = process.env.VITE_MOCK_PERSONA ?? "owner";
const scenario = process.env.VITE_MOCK_STATE ?? "default";
const faults = process.env.VITE_MOCK_FAULTS ?? "";
const shots = process.env.GOV_SHOTS ?? "";
const BOARD = "/workspaces/mock-workspace-primary/boards/mock-board-erp";
const OFFLINE_FAULT = "/boards/mock-board-erp/sharing=network";
const STALE_FAULT =
  "/resources/work-management.board/mock-board-erp/permissions=stale";

const sizes = [
  { width: 1440, height: 900 },
  { width: 1024, height: 768 },
  { width: 390, height: 844 },
] as const;

async function openShare(
  page: Page,
  size: (typeof sizes)[number],
  dark: boolean,
) {
  await page.setViewportSize(size);
  await page.addInitScript(
    (mode) => {
      try {
        window.localStorage.setItem("notrelix-locale", "vi");
        window.localStorage.setItem("notrelix-theme-mode", mode);
        window.localStorage.setItem("notrelix-system-rail", "pinned");
      } catch {
        // Private mode: the defaults apply.
      }
    },
    dark ? "dark" : "light",
  );
  // A person who may not manage the Board has no Share button: the address `?share=true` opens the dialog.
  await page.goto(persona === "owner" ? BOARD : `${BOARD}?share=true`);
  if (persona === "owner")
    await page
      .getByRole("button", { name: /^Chia sẻ/ })
      .first()
      .click();
  await expect(page.getByRole("dialog", { name: /^Chia sẻ/ })).toBeVisible();
  // The dialog zooms in and the pages fade in: read it at rest.
  await page.waitForTimeout(900);
}

/** The dialog sits inside the window, the page does not scroll sideways, 44 px targets on the phone. */
async function expectFits(page: Page, size: (typeof sizes)[number]) {
  const dialog = page.getByRole("dialog", { name: /^Chia sẻ/ });
  const box = (await dialog.boundingBox())!;
  expect(box.x, "dialog left").toBeGreaterThanOrEqual(-0.5);
  expect(box.x + box.width, "dialog right").toBeLessThanOrEqual(
    size.width + 0.5,
  );
  expect(box.y + box.height, "dialog bottom").toBeLessThanOrEqual(
    size.height + 0.5,
  );
  expect(
    await page.evaluate(
      () => document.documentElement.scrollWidth - window.innerWidth,
    ),
    "the page scrolls sideways",
  ).toBeLessThanOrEqual(0);
  const clipped = await dialog.evaluate((root) =>
    Array.from(root.querySelectorAll("*"))
      .filter((element) => {
        const rect = element.getBoundingClientRect();
        const text = (element.textContent ?? "").trim();
        if (!text || element.children.length > 0 || rect.width === 0)
          return false;
        const style = getComputedStyle(element);
        if (style.visibility === "hidden") return false;
        return (
          style.textOverflow !== "ellipsis" &&
          element.scrollWidth > element.clientWidth + 1 &&
          style.overflowX !== "visible"
        );
      })
      .map((element) => (element.textContent ?? "").trim().slice(0, 40)),
  );
  expect(clipped, "text cut by its box").toEqual([]);
  if (size.width === 390) {
    const small = await dialog.evaluate((root) =>
      Array.from(
        root.querySelectorAll(
          'button, a[href], input:not([type="hidden"]), [role="button"], [role="combobox"], [role="menuitem"], [role="checkbox"]',
        ),
      )
        .filter((element) => {
          const rect = element.getBoundingClientRect();
          const style = getComputedStyle(element);
          return (
            rect.width > 0 &&
            rect.height > 0 &&
            style.visibility !== "hidden" &&
            !element.closest("[aria-hidden='true'], .sr-only") &&
            Math.min(rect.width, rect.height) < 43.5
          );
        })
        .map((element) => {
          const rect = element.getBoundingClientRect();
          return `${element.getAttribute("aria-label") ?? (element.textContent ?? "").trim().slice(0, 30)} ${Math.round(rect.width)}x${Math.round(rect.height)}`;
        }),
    );
    expect(small, "targets under 44 px").toEqual([]);
  }
}

const seriousViolations = async (page: Page) =>
  (await new AxeBuilder({ page }).analyze()).violations.filter((violation) =>
    ["serious", "critical"].includes(violation.impact ?? ""),
  );

async function review(
  page: Page,
  name: string,
  size: (typeof sizes)[number],
  dark: boolean,
) {
  await expectFits(page, size);
  if (shots)
    await page.screenshot({
      path: `${shots}/${name}-${size.width}-${dark ? "dark" : "light"}.png`,
    });
  expect(
    (await seriousViolations(page)).map((violation) => violation.id),
    `${name}: axe`,
  ).toEqual([]);
}

test.describe("the people list could not be read (offline)", () => {
  test.skip(
    persona !== "owner" || scenario !== "default" || faults !== OFFLINE_FAULT,
    `Needs the owner world with VITE_MOCK_FAULTS=${OFFLINE_FAULT}`,
  );
  for (const size of sizes) {
    for (const dark of [false, true]) {
      test(`says it, offers a retry and fits, ${size.width} ${dark ? "dark" : "light"}`, async ({
        page,
      }) => {
        await openShare(page, size, dark);
        const dialog = page.getByRole("dialog", { name: /^Chia sẻ/ });
        const retry = dialog.getByRole("button", { name: /Thử lại/ });
        await expect(retry).toBeVisible();
        await expect(dialog.locator('[data-f="pr"]')).toHaveCount(0);
        await review(page, "share-offline", size, dark);
      });
    }
  }
});

test.describe("a person who may not manage the Board (permission denied)", () => {
  test.skip(
    !["member", "commenter", "viewer"].includes(persona) ||
      scenario !== "default" ||
      faults !== "",
    "Needs the viewer persona without faults",
  );
  for (const size of sizes) {
    for (const dark of [false, true]) {
      test(`shows who can open it and offers no write, ${size.width} ${dark ? "dark" : "light"}`, async ({
        page,
      }) => {
        await openShare(page, size, dark);
        const dialog = page.getByRole("dialog", { name: /^Chia sẻ/ });
        await expect(dialog.getByRole("combobox")).toHaveCount(0);
        // The add row is drawn inert (aria-disabled, read-only), never live.
        await expect(dialog.locator('[data-f="invite"]')).toHaveAttribute(
          "aria-disabled",
          "true",
        );
        await expect(
          dialog.getByRole("button", { name: /Có thể (xem|sửa|bình luận)/ }),
        ).toHaveCount(0);
        await review(page, "share-denied", size, dark);
      });
    }
  }
});

test.describe("a level change refused because the grant changed meanwhile (conflict)", () => {
  test.skip(
    persona !== "owner" || scenario !== "default" || faults !== STALE_FAULT,
    `Needs the owner world with VITE_MOCK_FAULTS=${STALE_FAULT}`,
  );
  for (const size of sizes) {
    for (const dark of [false, true]) {
      test(`names the conflict in a toast inside the window and keeps the level, ${size.width} ${dark ? "dark" : "light"}`, async ({
        page,
      }) => {
        await openShare(page, size, dark);
        const dialog = page.getByRole("dialog", { name: /^Chia sẻ/ });
        const row = dialog.locator('[data-f="pr"]').nth(1);
        await expect(row).toContainText("Có thể xem");
        await row.getByRole("button", { name: /Có thể xem/ }).click();
        await page.getByRole("menuitemradio", { name: /Có thể sửa/ }).click();
        const toast = page.locator("[data-sonner-toast]").last();
        await expect(toast).toBeVisible();
        await expect(toast).toContainText("Chưa đổi được quyền.");
        await expect(toast).toContainText("Người khác vừa đổi quyền");
        // The toast has slid in.
        await page.waitForTimeout(800);
        const box = (await toast.boundingBox())!;
        expect(box.x, "toast left").toBeGreaterThanOrEqual(-0.5);
        expect(box.x + box.width, "toast right").toBeLessThanOrEqual(
          size.width + 0.5,
        );
        expect(box.y + box.height, "toast bottom").toBeLessThanOrEqual(
          size.height + 0.5,
        );
        // The row is back at the level the server holds.
        await expect(row).toContainText("Có thể xem");
        await review(page, "share-conflict", size, dark);
      });
    }
  }
});

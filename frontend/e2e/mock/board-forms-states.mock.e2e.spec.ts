import AxeBuilder from "@axe-core/playwright";
import { expect, test, type Page } from "@playwright/test";

/**
 * The four Work Management form surfaces in the states the design canvas does not draw, at 1440, 1024 and
 * 390 px in light and dark: the forms list (board settings), the builder (published, draft, closed), the
 * member fill page (published, draft that is not open yet, closed) and the response inbox (every tab, and a
 * form that has no response). Each state keeps inside the page (no sideways scroll), has no serious or
 * critical axe violation, and at 390 px every control of the surface is a 44 px target. `WM_SHOTS_DIR` writes
 * a screenshot of each state for review. Keyboard journeys close the file: Tab and Enter create a form from
 * the list, reorder a question in the builder, fill and send the member form, and move through the inbox
 * with the arrow keys.
 *
 * Not covered, because the mock world has no scenario for it: an archived board and a form that holds a
 * file or signature question (the manager's explanation of hidden questions); both are proved by the
 * component tests. The failure states need the server started with VITE_MOCK_FAULTS.
 */
const persona = process.env.VITE_MOCK_PERSONA ?? "owner";
const scenario = process.env.VITE_MOCK_STATE ?? "default";
const faults = process.env.VITE_MOCK_FAULTS ?? "";
const shots = process.env.WM_SHOTS_DIR ?? "";
const BOARD = "/workspaces/mock-workspace-primary/boards/mock-board-erp";
const NOW = new Date("2026-10-12T10:40:00+07:00");

test.beforeEach(() => {
  test.skip(
    scenario !== "default" || persona !== "owner" || faults !== "",
    "Assertions need the default owner world without faults",
  );
});

async function open(
  page: Page,
  path: string,
  width: number,
  height: number,
  dark: boolean,
) {
  await page.setViewportSize({ width, height });
  await page.clock.setFixedTime(NOW);
  await page.addInitScript(
    (mode) => {
      try {
        window.localStorage.setItem("notrelix-locale", "vi");
        window.localStorage.setItem("notrelix-theme-mode", mode);
      } catch {
        // Private mode: the defaults apply.
      }
    },
    dark ? "dark" : "light",
  );
  await page.goto(path, { waitUntil: "domcontentloaded" });
}

const seriousViolations = async (page: Page) =>
  (await new AxeBuilder({ page }).analyze()).violations.filter((violation) =>
    ["serious", "critical"].includes(violation.impact ?? ""),
  );

/** The visible controls under `scope` that are smaller than a 44 px target at a phone width. */
async function smallTargets(page: Page, scope: string): Promise<string[]> {
  return page.evaluate((selector) => {
    const root = document.querySelector(selector);
    if (!root) return ["no scope " + selector];
    const small: string[] = [];
    const controls = root.querySelectorAll(
      "button, input:not([type=hidden]), select, textarea, [role=tab], [role=option], [role=radio], [role=checkbox], [role=switch]",
    );
    for (const control of controls) {
      const element = control as HTMLElement;
      if (element.closest("[aria-hidden=true]")) continue;
      const rect = element.getBoundingClientRect();
      if (rect.width === 0 || rect.height === 0) continue;
      const style = getComputedStyle(element);
      if (style.visibility === "hidden" || style.display === "none") continue;
      // A checkbox or radio inside a label is hit through its label: the label's box is the target.
      const label =
        element instanceof HTMLInputElement &&
        ["checkbox", "radio"].includes(element.type)
          ? element.closest("label")
          : null;
      const height = (label ?? element).getBoundingClientRect().height;
      if (height < 43.5)
        small.push(
          `${element.tagName.toLowerCase()} "${(element.getAttribute("aria-label") ?? element.textContent ?? "").trim().slice(0, 30)}" ${height.toFixed(1)}px`,
        );
    }
    return small;
  }, scope);
}

interface Surface {
  readonly name: string;
  readonly path: string;
  readonly scope: string;
  readonly ready: (page: Page) => Promise<void>;
  readonly before?: (page: Page) => Promise<void>;
}

const SURFACES: readonly Surface[] = [
  {
    name: "list",
    path: `${BOARD}/settings?section=forms`,
    scope: "section[aria-labelledby=bs-forms-h]",
    ready: (page) =>
      expect(
        page.getByRole("heading", { name: "Biểu mẫu", level: 2 }),
      ).toBeVisible(),
  },
  {
    name: "builder-published",
    path: `${BOARD}/forms/mock-form-it-support/edit`,
    scope: "main",
    ready: (page) =>
      expect(page.getByRole("list", { name: "Câu hỏi" })).toBeVisible(),
    before: async (page) => {
      await page
        .getByRole("button", { name: /^Bạn cần gì/ })
        .first()
        .click();
    },
  },
  {
    name: "builder-draft",
    path: `${BOARD}/forms/mock-form-purchase/edit`,
    scope: "main",
    ready: (page) =>
      expect(page.getByRole("textbox", { name: "Tên biểu mẫu" })).toBeVisible(),
  },
  {
    name: "builder-closed",
    path: `${BOARD}/forms/mock-form-erp-bug/edit`,
    scope: "main",
    ready: (page) =>
      expect(page.getByRole("textbox", { name: "Tên biểu mẫu" })).toBeVisible(),
  },
  {
    name: "fill-published",
    path: `${BOARD}/forms/mock-form-it-support`,
    scope: "main",
    ready: (page) =>
      expect(
        page.getByRole("heading", { name: "Yêu cầu hỗ trợ IT" }),
      ).toBeVisible(),
  },
  {
    name: "fill-draft",
    path: `${BOARD}/forms/mock-form-purchase`,
    scope: "main",
    ready: (page) =>
      expect(page.getByRole("heading", { level: 1 })).toBeVisible(),
  },
  {
    name: "fill-closed",
    path: `${BOARD}/forms/mock-form-erp-bug`,
    scope: "main",
    ready: (page) =>
      expect(page.getByRole("heading", { level: 1 })).toBeVisible(),
  },
  {
    name: "inbox-new",
    path: `${BOARD}/forms/mock-form-it-support/responses`,
    scope: "main",
    ready: (page) =>
      expect(page.getByRole("tab", { name: /Mới/ })).toBeVisible(),
    before: async (page) => {
      await page.getByRole("option").first().click();
    },
  },
  ...(["Đã xử lý", "Từ chối", "Spam"] as const).map((tab, index): Surface => ({
    name: `inbox-${["processed", "rejected", "spam"][index]}`,
    path: `${BOARD}/forms/mock-form-it-support/responses`,
    scope: "main",
    ready: (page) =>
      expect(page.getByRole("tab", { name: /Mới/ })).toBeVisible(),
    before: async (page) => {
      await page.getByRole("tab", { name: new RegExp(tab) }).click();
      await page.getByRole("option").first().click();
    },
  })),
  {
    name: "inbox-empty",
    path: `${BOARD}/forms/mock-form-purchase/responses`,
    scope: "main",
    ready: (page) =>
      expect(page.getByRole("tab", { name: /Mới/ })).toBeVisible(),
  },
];

for (const surface of SURFACES) {
  for (const [width, height] of [
    [1440, 900],
    [1024, 768],
    [390, 844],
  ] as const) {
    for (const dark of [false, true]) {
      const mode = dark ? "dark" : "light";
      test(`${surface.name} keeps inside the page, has no serious axe violation and 44 px targets on a phone, ${width} ${mode}`, async ({
        page,
      }) => {
        test.setTimeout(90_000);
        await open(page, surface.path, width, height, dark);
        await surface.ready(page);
        await surface.before?.(page);
        await page.waitForTimeout(500);

        const overflow = await page.evaluate(
          () => document.documentElement.scrollWidth - window.innerWidth,
        );
        expect(overflow, "the page scrolls sideways").toBeLessThanOrEqual(0);
        if (shots)
          await page.screenshot({
            path: `${shots}/forms-${surface.name}-${width}-${mode}.png`,
          });
        expect(await seriousViolations(page)).toEqual([]);
        if (width < 768)
          expect(
            await smallTargets(page, surface.scope),
            "targets under 44 px",
          ).toEqual([]);
      });
    }
  }
}

/** Presses Tab until `matches` accepts the focused element, at most `limit` times. */
async function tabTo(page: Page, matches: () => Promise<boolean>, limit = 80) {
  for (let step = 0; step < limit; step += 1) {
    await page.keyboard.press("Tab");
    if (await matches()) return;
  }
  throw new Error("focus never reached the control");
}

const focusedName = (page: Page) =>
  page.evaluate(() => {
    const element = document.activeElement as HTMLElement | null;
    if (!element) return "";
    const labelled = (element as HTMLInputElement).labels?.[0]?.textContent;
    return (
      element.getAttribute("aria-label") ??
      labelled ??
      element.textContent ??
      ""
    ).trim();
  });

test.describe("Keyboard journeys at 1440 px", () => {
  test.use({ viewport: { width: 1440, height: 900 } });

  test("list: Tab reaches Tạo biểu mẫu, Enter opens the name field, typing a name and Enter creates the form", async ({
    page,
  }) => {
    await open(page, `${BOARD}/settings?section=forms`, 1440, 900, false);
    await expect(
      page.getByRole("heading", { name: "Biểu mẫu", level: 2 }),
    ).toBeVisible();
    await tabTo(page, async () =>
      (await focusedName(page)).startsWith("Tạo biểu mẫu"),
    );
    await page.keyboard.press("Enter");
    const name = page.getByRole("textbox", { name: "Tên biểu mẫu mới" });
    await expect(name).toBeFocused();
    await page.keyboard.type("Yêu cầu cấp quyền");
    await page.keyboard.press("Enter");
    await expect(page).toHaveURL(/\/forms\/[^/]+\/edit$/);
    await expect(
      page.getByRole("textbox", { name: "Tên biểu mẫu" }),
    ).toHaveValue("Yêu cầu cấp quyền");
  });

  test("builder: Tab reaches Thêm câu hỏi and Enter opens the menu; the move buttons work with Enter", async ({
    page,
  }) => {
    await open(
      page,
      `${BOARD}/forms/mock-form-it-support/edit`,
      1440,
      900,
      false,
    );
    const list = page.getByRole("list", { name: "Câu hỏi" });
    await expect(list).toBeVisible();
    await tabTo(page, async () =>
      (await focusedName(page)).startsWith("Thêm câu hỏi"),
    );
    const add = page.getByRole("button", { name: "Thêm câu hỏi" });
    await expect(add).toBeFocused();
    await page.keyboard.press("Enter");
    await expect(add).toHaveAttribute("aria-expanded", "true");
    await page.keyboard.press("Enter");
    await expect(add).toHaveAttribute("aria-expanded", "false");

    const down = page.getByRole("button", {
      name: "Chuyển câu hỏi 'Bạn cần gì?' xuống",
    });
    await down.focus();
    await page.keyboard.press("Enter");
    await expect(list.locator("li").first()).toContainText("Mô tả chi tiết");
    const up = page.getByRole("button", {
      name: "Chuyển câu hỏi 'Bạn cần gì?' lên",
    });
    await up.focus();
    await page.keyboard.press("Enter");
    await expect(list.locator("li").first()).toContainText("Bạn cần gì?");
  });

  test("fill page: Tab follows the questions in order, and Enter on the submit button sends", async ({
    page,
  }) => {
    await open(page, `${BOARD}/forms/mock-form-it-support`, 1440, 900, false);
    await expect(
      page.getByRole("heading", { name: "Yêu cầu hỗ trợ IT" }),
    ).toBeVisible();
    const order = [
      /^Bạn cần gì/,
      /^Mô tả chi tiết/,
      /^Mức độ ưu tiên/,
      /^Email liên hệ/,
    ];
    const seen: string[] = [];
    for (let step = 0; step < 40; step += 1) {
      await page.keyboard.press("Tab");
      const name = await focusedName(page);
      seen.push(name);
      if (name === "Gửi yêu cầu") break;
    }
    expect(seen.at(-1), "focus reached the submit button").toBe("Gửi yêu cầu");
    let last = -1;
    for (const pattern of order) {
      const index = seen.findIndex(
        (name, at) => at > last && pattern.test(name),
      );
      expect(
        index,
        `${String(pattern)} follows the one before it`,
      ).toBeGreaterThan(last);
      last = index;
    }
    await page.keyboard.press("Enter");
    await expect(page.getByText("Chưa gửi được.")).toBeVisible();
  });

  test("inbox: the arrow keys move through the responses of the list", async ({
    page,
  }) => {
    await open(
      page,
      `${BOARD}/forms/mock-form-it-support/responses`,
      1440,
      900,
      false,
    );
    const options = page.getByRole("option");
    await expect(options.first()).toBeVisible();
    await options.first().focus();
    await page.keyboard.press("ArrowDown");
    await expect(options.nth(1)).toHaveAttribute("aria-selected", "true");
    await expect(options.nth(1)).toBeFocused();
    await page.keyboard.press("ArrowUp");
    await expect(options.first()).toHaveAttribute("aria-selected", "true");
  });
});

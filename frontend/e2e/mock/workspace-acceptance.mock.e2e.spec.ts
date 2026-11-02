import AxeBuilder from "@axe-core/playwright";
import { expect, test, type Locator, type Page } from "@playwright/test";

/**
 * Acceptance line 3 for the Workspaces screens the canvas does not draw (roster, invitations, join page states,
 * Space create and placement dialogs) and for the shell top bar: at 1440, 1024 and 390 px in light and dark the
 * page does not overflow sideways, axe reports no serious or critical violation (colour contrast included), and at
 * 390 px every control is a 44 px target. Set `WSP_SHOTS` to a folder to also write a screenshot of each state.
 */

const scenario = process.env.VITE_MOCK_STATE ?? "default";
const persona = process.env.VITE_MOCK_PERSONA ?? "owner";
const shots = process.env.WSP_SHOTS;
const THEME_KEY = "notrelix-theme-mode";
const WORKSPACE = "/workspaces/mock-workspace-primary";

const viewports = [
  { width: 1440, height: 900 },
  { width: 1024, height: 768 },
  { width: 390, height: 844 },
] as const;

const INTERACTIVE =
  'button, a[href], input:not([type="hidden"]), select, textarea, [role="button"], [role="tab"], [role="combobox"], [role="menuitem"], [role="radio"], [role="checkbox"], [role="switch"]';

test.beforeEach(() => {
  test.skip(
    scenario !== "default" || persona !== "owner",
    "Needs the default world and a workspace owner",
  );
});

async function prepare(
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

async function audit(
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
    });
  }
}

for (const viewport of viewports) {
  for (const dark of [false, true]) {
    const mode = `${viewport.width} ${dark ? "dark" : "light"}`;

    test(`shell top bar, ${mode}`, async ({ page }) => {
      await prepare(page, viewport, dark);
      await page.goto(WORKSPACE);
      // Desktop and tablet have no top bar (the sidebar carries it); only the phone does.
      const topbar = page.locator("header").first();
      if (viewport.width < 768) await expect(topbar).toBeVisible();
      else await expect(page.locator("main")).toBeVisible();
      await audit(
        page,
        "shell",
        viewport,
        dark,
        viewport.width < 768 ? topbar : page.locator("main"),
      );
    });

    test(`roster with its row menu open, ${mode}`, async ({ page }) => {
      await prepare(page, viewport, dark);
      await page.goto(`${WORKSPACE}/members`);
      await expect(
        page.getByRole("heading", { level: 1, name: "Members" }),
      ).toBeVisible();
      await audit(page, "roster", viewport, dark, page.locator("main"));
      await page
        .getByRole("button", { name: /^Actions for .*\(Viewer\)/ })
        .first()
        .click();
      await expect(page.getByRole("menuitem").first()).toBeVisible();
      await audit(page, "roster-menu", viewport, dark, page.locator("main"));
      await page
        .getByRole("menuitem", { name: "Remove from workspace" })
        .click();
      const confirm = page.getByRole("alertdialog");
      await expect(confirm).toBeVisible();
      await audit(page, "roster-confirm", viewport, dark, confirm);
    });

    test(`role-lowering confirmation, ${mode}`, async ({ page }) => {
      await prepare(page, viewport, dark);
      await page.goto(`${WORKSPACE}/members`);
      await expect(
        page.getByRole("heading", { level: 1, name: "Members" }),
      ).toBeVisible();
      await page
        .getByRole("combobox", { name: /^Role for .*\(Admin\)/ })
        .first()
        .click();
      await page
        .getByRole("option", { name: /^Member/ })
        .first()
        .click();
      const confirm = page.getByRole("alertdialog");
      await expect(confirm).toBeVisible();
      await audit(page, "roster-lower-role", viewport, dark, confirm);
    });

    test(`invitations with two waiting, ${mode}`, async ({ page }) => {
      await prepare(page, viewport, dark);
      await page.goto(`${WORKSPACE}/members?tab=invitations`);
      const form = page.getByRole("form", { name: "Invite by e-mail" });
      for (const address of [
        "first.person@example.com",
        "second@example.com",
      ]) {
        await form.getByLabel("E-mail address").fill(address);
        await form.getByRole("button", { name: "Send invitation" }).click();
        await expect(
          page.locator("tbody tr", { hasText: address }),
        ).toBeVisible();
      }
      await audit(page, "invitations", viewport, dark, page.locator("main"));
    });

    for (const [token, label] of [
      ["invite-wsp-member", "join-workspace"],
      ["invite-erp-guest", "join-new"],
      ["invite-erp-existing", "join-existing"],
      ["invite-erp-expired", "join-expired"],
      ["invite-erp-used", "join-used"],
      ["khong-co-loi-moi-nay", "join-missing"],
    ] as const) {
      test(`join page ${label}, ${mode}`, async ({ page }) => {
        await prepare(page, viewport, dark);
        await page.goto(`/invite/${token}`);
        await expect(page.getByRole("heading", { level: 1 })).toBeVisible();
        await audit(page, label, viewport, dark, page.locator("body"));
      });
    }

    test(`workspace not found, ${mode}`, async ({ page }) => {
      await prepare(page, viewport, dark);
      await page.goto("/workspaces/no-such-workspace");
      await expect(
        page.getByRole("button", { name: "Try again" }),
      ).toBeVisible();
      await audit(
        page,
        "workspace-not-found",
        viewport,
        dark,
        page.locator("body"),
      );
    });

    test(`Space create dialog, ${mode}`, async ({ page }) => {
      await prepare(page, viewport, dark);
      await page.goto(WORKSPACE);
      if (viewport.width < 768) {
        await page
          .getByRole("button", { name: "Show or hide navigation" })
          .first()
          .click();
      }
      await page
        .getByRole("navigation", { name: "Workspace navigation" })
        .getByRole("button", { name: "Create department" })
        .click();
      const dialog = page.getByRole("dialog", { name: "New Space" });
      await expect(dialog).toBeVisible();
      await audit(page, "space-create", viewport, dark, dialog);
    });

    test(`Space placement dialog, ${mode}`, async ({ page }) => {
      await prepare(page, viewport, dark);
      await page.goto(`${WORKSPACE}/library`);
      const move = page
        .getByRole("button", { name: /^Move .* to a Space$/ })
        .first();
      // The button shows on hover of its row on a desktop; on a phone it is always there and must take a plain tap.
      if (viewport.width < 768) await move.click();
      else await move.click({ force: true });
      const dialog = page.getByRole("dialog", { name: /to a Space$/ });
      await expect(dialog).toBeVisible();
      await audit(page, "space-placement", viewport, dark, dialog);
    });
  }
}

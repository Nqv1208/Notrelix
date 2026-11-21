import AxeBuilder from "@axe-core/playwright";
import { expect, test, type Page } from "@playwright/test";
import { vi as copy } from "../../packages/features/chat/src/web/components/chat.copy";

/**
 * The invite dialog and the member list with the role and remove buttons, in Vietnamese, at 1440, 1024 and 390, light and
 * dark: no horizontal overflow, the dialog stays inside the window, axe finds nothing serious. The frames are undrawn as one
 * screen (the canvas boards `Chat-Var-Invite` and `Chat-Var-Remove-Member` draw a different shape), so this is a review, not a
 * fidelity claim. Set CHAT_STATE_SHOTS to a directory to also write one screenshot per frame.
 */
const persona = process.env.VITE_MOCK_PERSONA ?? "owner";
const scenario = process.env.VITE_MOCK_STATE ?? "default";
test.skip(
  persona !== "owner" || scenario !== "default",
  "Needs the default owner world",
);

const SHOTS = process.env.CHAT_STATE_SHOTS;
const CHANNEL =
  "/workspaces/mock-workspace-primary/chat/c1000000-0000-4000-8000-000000000001";

async function open(
  page: Page,
  width: number,
  height: number,
  mode: "light" | "dark",
) {
  await page.setViewportSize({ width, height });
  await page.addInitScript((m) => {
    try {
      window.localStorage.setItem("notrelix-locale", "vi");
      window.localStorage.setItem("notrelix-theme-mode", m);
    } catch {
      // Private mode: the defaults apply.
    }
  }, mode);
  await page.goto(CHANNEL);
  await expect(page.locator("[data-bubble]").first()).toBeVisible();
  await page
    .locator("main header")
    .getByRole("button", { name: copy.panel.channelInfo })
    .click();
  await expect(page.locator("[data-testid=chat-side-panel]")).toBeVisible();
  await page.waitForTimeout(400);
}

const noOverflow = (page: Page) =>
  page.evaluate(
    () =>
      document.documentElement.scrollWidth <=
      document.documentElement.clientWidth,
  );

for (const [width, height] of [
  [1440, 900],
  [1024, 768],
  [390, 844],
] as const) {
  for (const mode of ["light", "dark"] as const) {
    test(`the member list and the invite dialog fit at ${width} in ${mode}`, async ({
      page,
    }) => {
      await open(page, width, height, mode);
      const panel = page.locator("[data-testid=chat-side-panel]");
      const members = panel.getByRole("button", {
        name: new RegExp(`^${copy.header.membersTitle}`),
      });
      if ((await members.getAttribute("aria-expanded")) !== "true")
        await members.click();
      const invite = page.getByRole("button", {
        name: copy.membership.invite,
      });
      await expect(invite).toBeVisible();
      expect(await noOverflow(page)).toBe(true);
      if (SHOTS)
        await page.screenshot({
          path: `${SHOTS}/members-${width}-${mode}.png`,
        });

      await invite.click();
      const dialog = page.getByRole("dialog");
      await expect(dialog).toBeVisible();
      await page.waitForTimeout(300);
      const box = (await dialog.boundingBox())!;
      expect(box.x).toBeGreaterThanOrEqual(0);
      expect(box.x + box.width).toBeLessThanOrEqual(width);
      expect(box.y + box.height).toBeLessThanOrEqual(height);
      expect(await noOverflow(page)).toBe(true);
      const axe = await new AxeBuilder({ page })
        .disableRules(["color-contrast"])
        .analyze();
      expect(
        axe.violations
          .filter((v) => ["serious", "critical"].includes(v.impact ?? ""))
          .map((v) => `${v.id}: ${v.nodes[0]?.html}`),
      ).toEqual([]);
      if (SHOTS)
        await page.screenshot({ path: `${SHOTS}/invite-${width}-${mode}.png` });
    });
  }
}

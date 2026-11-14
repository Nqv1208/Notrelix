import { expect, test } from "@playwright/test";
import {
  audit,
  prepare,
  viewports,
  WORKSPACE,
} from "./support/acceptance-audit";

/**
 * The roster as a Guest reads it (own row only with the reason shown, no invite and no row menu), at 1440, 1024 and 390 px in light and
 * dark: no sideways overflow, 44 px targets at 390 px and axe clean. It runs in the viewer world
 * (`VITE_MOCK_PERSONA=viewer VITE_MOCK_STATE=permission-limited`); the owner world has its own roster test in
 * `workspace-acceptance.mock.e2e.spec.ts`.
 */

const persona = process.env.VITE_MOCK_PERSONA ?? "owner";

test.beforeEach(() => {
  test.skip(persona !== "viewer", "Needs the viewer world");
});

for (const viewport of viewports) {
  for (const dark of [false, true]) {
    test(`roster as a guest, ${viewport.width} ${dark ? "dark" : "light"}`, async ({
      page,
    }) => {
      await prepare(page, viewport, dark);
      await page.goto(`${WORKSPACE}/members`);
      await expect(
        page.getByRole("heading", { level: 1, name: "Members" }),
      ).toBeVisible();
      await expect(
        page.getByText(
          "The member list is closed to guests. You see your own membership only.",
        ),
      ).toBeVisible();
      await expect(page.getByRole("table").first()).toBeVisible();
      await expect(
        page.getByRole("button", { name: "Invite people" }),
      ).toHaveCount(0);
      await expect(
        page.getByRole("button", { name: /^Actions for / }),
      ).toHaveCount(0);
      await audit(
        page,
        "roster-read-only",
        viewport,
        dark,
        page.locator("main"),
      );
    });
  }
}

import { expect, test } from "@playwright/test";
import {
  audit,
  prepare,
  viewports,
  WORKSPACE,
} from "./support/acceptance-audit";

/**
 * Acceptance line 3 for the Workspaces screens the canvas does not draw (roster, invitations, join page states,
 * Space create and placement dialogs) and for the shell top bar: at 1440, 1024 and 390 px in light and dark the
 * page does not overflow sideways, axe reports no serious or critical violation (colour contrast included), and at
 * 390 px every control is a 44 px target. Set `WSP_SHOTS` to a folder to also write a screenshot of each state.
 */

const scenario = process.env.VITE_MOCK_STATE ?? "default";
const persona = process.env.VITE_MOCK_PERSONA ?? "owner";

// A cold dev server compiles the first page it serves: longer than the default 30 seconds.
test.setTimeout(60_000);

test.beforeEach(() => {
  test.skip(
    scenario !== "default" || persona !== "owner",
    "Needs the default world and a workspace owner",
  );
});

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
      // The last toast would sit over the rows the screenshot is for.
      await expect(page.locator("[data-sonner-toast]")).toHaveCount(0, {
        timeout: 15_000,
      });
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

    test(`invitee flyout with two waiting, then empty, ${mode}`, async ({
      page,
    }) => {
      await prepare(page, viewport, dark);
      await page.goto(WORKSPACE);
      // The rail is pinned on a desktop and opens from the workspace chip on a tablet and a phone.
      const trigger = page.getByRole("button", {
        name: "Pending workspace invitations",
      });
      if (!(await trigger.isVisible())) {
        if (viewport.width < 768) {
          await page
            .getByRole("button", { name: "Show or hide navigation" })
            .first()
            .click();
        }
        await page
          .getByRole("button", { name: /open the Notrelix system rail/ })
          .first()
          .click();
      }
      await trigger.click();
      const flyout = page.locator("[data-rail-flyout]");
      await expect(flyout).toBeVisible();
      await expect(flyout.getByText("Studio Sao Mai")).toBeVisible();
      // The card itself stays inside the screen (on a phone it opens beside the 76 px rail).
      const card = await flyout.boundingBox();
      expect(
        card!.x + card!.width,
        "flyout ends inside the viewport",
      ).toBeLessThanOrEqual(viewport.width);
      await audit(page, "invitee-flyout", viewport, dark, flyout);

      // Declining both leaves the empty state; the decline leaves the row at once and the buttons of the other wait.
      for (const workspace of ["Studio Sao Mai", "Kế toán Minh An"]) {
        await flyout
          .getByRole("button", {
            name: `Decline the invitation to ${workspace}`,
          })
          .click();
        await expect(flyout.getByText(workspace)).toHaveCount(0);
      }
      await expect(flyout.getByText("No pending invitations")).toBeVisible();
      await audit(page, "invitee-flyout-empty", viewport, dark, flyout);
    });

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

    test(`Space tree with a department open, ${mode}`, async ({ page }) => {
      await prepare(page, viewport, dark);
      await page.goto(WORKSPACE);
      if (viewport.width < 768) {
        await page
          .getByRole("button", { name: "Show or hide navigation" })
          .first()
          .click();
      }
      const nav = page.getByRole("navigation", {
        name: "Workspace navigation",
      });
      const tree = nav.getByRole("tree", { name: "Departments" });
      await expect(tree.getByRole("treeitem").first()).toBeVisible();
      // Open the first department that has anything under it (its Boards and sub-Spaces list in place).
      const expandable = tree.locator(
        '[role="treeitem"][aria-expanded="false"]',
      );
      await expandable.first().focus();
      await page.keyboard.press("ArrowRight");
      await expect(
        tree.locator('[role="treeitem"][aria-expanded="true"]'),
      ).toHaveCount(1);
      await audit(page, "space-tree", viewport, dark, nav);
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

test("invitee flyout: the keyboard opens it, reaches Accept and Decline, and Escape closes it", async ({
  page,
}) => {
  await prepare(page, viewports[0], false);
  await page.goto(WORKSPACE);
  const trigger = page.getByRole("button", {
    name: "Pending workspace invitations",
  });
  const chip = page
    .getByRole("button", { name: /open the Notrelix system rail/ })
    .first();
  await chip.focus();
  await page.keyboard.press("Enter");
  await trigger.focus();
  await page.keyboard.press("Enter");
  const flyout = page.locator("[data-rail-flyout]");
  await expect(flyout).toBeVisible();
  // Focus moves into the flyout on its first button (Accept of the first row); Tab reaches its Decline.
  await expect(
    flyout.getByRole("button", { name: /^Accept the invitation to / }).first(),
  ).toBeFocused();
  await page.keyboard.press("Tab");
  await expect(
    flyout.getByRole("button", { name: /^Decline the invitation to / }).first(),
  ).toBeFocused();
  await page.keyboard.press("Escape");
  await expect(flyout).toBeHidden();
  await expect(trigger).toBeFocused();
});

test("roster: the keyboard filters, opens a row menu, reaches Remove and Escape returns to the row", async ({
  page,
}) => {
  await prepare(page, viewports[0], false);
  await page.goto(`${WORKSPACE}/members`);
  await expect(
    page.getByRole("heading", { level: 1, name: "Members" }),
  ).toBeVisible();
  const filter = page.getByRole("searchbox", {
    name: "Filter members by name",
  });
  await filter.focus();
  await page.keyboard.type("alex");
  await expect(page.getByRole("row", { name: /^Alex Rivera/ })).toHaveCount(1);
  await expect(page.getByRole("row", { name: /^Jordan Lee/ })).toHaveCount(0);
  const actions = page.getByRole("button", { name: /^Actions for Alex/ });
  await actions.focus();
  await page.keyboard.press("Enter");
  const remove = page.getByRole("menuitem", { name: "Remove from workspace" });
  await expect(remove).toBeVisible();
  await page.keyboard.press("ArrowDown");
  await page.keyboard.press("Escape");
  await expect(remove).toBeHidden();
  await expect(actions).toBeFocused();
});

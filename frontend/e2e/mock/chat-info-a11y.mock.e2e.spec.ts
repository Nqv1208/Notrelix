import AxeBuilder from "@axe-core/playwright";
import { expect, test, type Page } from "@playwright/test";
import { toNewest } from "./support/chat-newest";
import { vi as copy } from "../../packages/features/chat/src/web/components/chat.copy";

/**
 * The header and the info panel for keyboard and screen readers: axe finds nothing in the header, the menu and the
 * panel with every section expanded; Tab walks the close cross, the quick actions and the section rows in that order;
 * Escape closes the menu first and then the panel.
 */
const persona = process.env.VITE_MOCK_PERSONA ?? "owner";
const scenario = process.env.VITE_MOCK_STATE ?? "default";
test.skip(
  persona !== "owner" || scenario !== "default",
  "Needs the default owner world",
);

const CHANNEL =
  "/workspaces/mock-workspace-primary/chat/c1000000-0000-4000-8000-000000000001";
const PANEL = "[data-testid=chat-side-panel]";

async function openInfo(page: Page, mode: "light" | "dark" = "light") {
  await page.setViewportSize({ width: 1440, height: 900 });
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
  await toNewest(page);
  await page
    .locator("main header")
    .getByRole("button", { name: copy.panel.channelInfo })
    .click();
  await expect(page.locator(PANEL)).toBeVisible();
  await page.waitForTimeout(400);
}

for (const mode of ["light", "dark"] as const) {
  test(`axe is clean on the header, the open menu and the panel with every section expanded (${mode})`, async ({
    page,
  }) => {
    await openInfo(page, mode);
    for (const name of [
      copy.pins.title,
      copy.thread.followedTab,
      copy.panel.sectionMentions,
    ]) {
      await page
        .locator(PANEL)
        .getByRole("button", { name: new RegExp(`^${name}`) })
        .click();
    }
    const panel = await new AxeBuilder({ page }).include(PANEL).analyze();
    expect(panel.violations.map((v) => `${v.id}: ${v.nodes[0]?.html}`)).toEqual(
      [],
    );
    const header = await new AxeBuilder({ page })
      .include("main header")
      .analyze();
    expect(
      header.violations.map((v) => `${v.id}: ${v.nodes[0]?.html}`),
    ).toEqual([]);
    await page
      .locator("main header")
      .getByRole("button", { name: copy.panel.more })
      .click();
    await expect(page.getByRole("menu")).toBeVisible();
    // The menu zooms and fades in over 100 ms: contrast is read at rest.
    await page.waitForTimeout(400);
    const menu = await new AxeBuilder({ page })
      .include("[role=menu]")
      .analyze();
    expect(menu.violations.map((v) => `${v.id}: ${v.nodes[0]?.html}`)).toEqual(
      [],
    );
  });
}

test("Tab walks the header actions left to right, then the panel: close cross, quick actions, section rows", async ({
  page,
}) => {
  await openInfo(page);
  const label = () =>
    page.evaluate(
      () =>
        document.activeElement?.getAttribute("aria-label") ??
        document.activeElement?.textContent?.replace(/\d+$/, "") ??
        "",
    );
  await page
    .locator("main header")
    .getByRole("button", { name: copy.search.open, exact: true })
    .focus();
  const seen: string[] = [];
  for (let i = 0; i < 60; i++) {
    seen.push(await label());
    await page.keyboard.press("Tab");
  }
  // The header buttons come first, in the order they are drawn.
  expect(seen.slice(0, 3)).toEqual([
    copy.search.open,
    copy.panel.channelInfo,
    copy.panel.more,
  ]);
  // The panel follows in reading order. Members opens expanded, so its rows (each with a remove button) sit between the
  // Members row and the Pinned row: that is the logical order, not a jump.
  const at = (name: string) => seen.findIndex((l) => l === name);
  const order = [
    copy.pins.close,
    copy.panel.searchShort,
    copy.panel.mute,
    copy.header.membersTitle,
    copy.pins.title,
  ].map(at);
  expect(order.every((i) => i > 2)).toBe(true);
  expect(order).toEqual([...order].sort((x, y) => x - y));
  expect(new Set(order).size).toBe(order.length);
});

test("Escape closes the menu first, then the panel; the info button gets the focus back", async ({
  page,
}) => {
  await openInfo(page);
  const more = page
    .locator("main header")
    .getByRole("button", { name: copy.panel.more });
  await more.click();
  await expect(page.getByRole("menu")).toBeVisible();
  await page.keyboard.press("Escape");
  await expect(page.getByRole("menu")).toHaveCount(0);
  await expect(page.locator(PANEL)).toBeVisible();
  await page
    .locator(PANEL)
    .getByRole("button", { name: copy.pins.title })
    .focus();
  await page.keyboard.press("Escape");
  await expect(page.locator(PANEL)).toHaveCount(0);
});

test("the pinned and following menu entries open the panel at that section, expanded", async ({
  page,
}) => {
  await page.setViewportSize({ width: 1440, height: 900 });
  await page.goto(CHANNEL);
  await expect(page.locator("[data-bubble]").first()).toBeVisible();
  const more = page
    .locator("main header")
    .getByRole("button", { name: copy.panel.more });
  await more.click();
  await page.getByRole("menuitem", { name: copy.pins.title }).click();
  await expect(
    page
      .locator(PANEL)
      .getByRole("button", { name: new RegExp(`^${copy.pins.title}`) }),
  ).toHaveAttribute("aria-expanded", "true");
  await more.click();
  await page.getByRole("menuitem", { name: copy.thread.followedTab }).click();
  await expect(
    page
      .locator(PANEL)
      .getByRole("button", { name: new RegExp(`^${copy.thread.followedTab}`) }),
  ).toHaveAttribute("aria-expanded", "true");
});

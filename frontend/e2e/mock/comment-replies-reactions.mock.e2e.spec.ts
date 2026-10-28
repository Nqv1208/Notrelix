import { expect, test, type Page } from "@playwright/test";

/**
 * Replies and reactions on the item page's comments: a reply under its
 * comment, a reply to a reply landing in the same top-level thread, the
 * thread folding past three replies, emoji toggled on and off, and a viewer
 * who reads the chips but cannot react. The owner checks run for the default
 * persona, the viewer check with VITE_MOCK_PERSONA=viewer.
 */
const persona = process.env.VITE_MOCK_PERSONA ?? "owner";
const scenario = process.env.VITE_MOCK_STATE ?? "default";
const ITEM =
  "/workspaces/mock-workspace-primary/boards/mock-board-erp/items/mock-erp-item-142?tab=comments";
const NOW = new Date("2026-10-12T10:40:00+07:00");

test.use({ viewport: { width: 1440, height: 900 } });

async function open(page: Page) {
  await page.clock.setFixedTime(NOW);
  await page.addInitScript(() => {
    try {
      window.localStorage.setItem("notrelix-locale", "en");
    } catch {
      // Private mode: the default locale applies.
    }
  });
  await page.goto(ITEM);
  await expect(page.getByText(/Bên mình chốt xong số dư/)).toBeVisible();
}

/** The first comment of the item and the box that holds its replies. */
const replies = (page: Page) =>
  page.getByRole("group", { name: /^Replies to / });

async function reply(
  page: Page,
  replyButton: ReturnType<Page["locator"]>,
  text: string,
) {
  await replyButton.click();
  await page.getByRole("textbox", { name: /^Reply to / }).fill(text);
  await page.getByRole("button", { name: "Send reply" }).click();
  await expect(page.getByText(text)).toBeVisible();
}

test.describe("owner", () => {
  test.skip(
    persona !== "owner" || scenario !== "default",
    "Needs the default owner scenario",
  );

  test("replies, replies to a reply into the same thread, and folds past three", async ({
    page,
  }) => {
    await open(page);
    const topReply = page.getByRole("button", { name: "Reply", exact: true });

    await reply(page, topReply.first(), "First reply");
    await expect(replies(page)).toHaveCount(1);
    await expect(replies(page).getByText("First reply")).toBeVisible();

    // Replying to the reply lands in the same thread, not one level deeper.
    await reply(
      page,
      replies(page).getByRole("button", { name: "Reply", exact: true }).first(),
      "Reply to the reply",
    );
    await expect(replies(page)).toHaveCount(1);
    await expect(replies(page).getByText("Reply to the reply")).toBeVisible();

    for (const text of ["Third", "Fourth", "Fifth"]) {
      await reply(page, topReply.first(), text);
    }
    // A sent reply opens the thread; coming back to the tab folds it again.
    const tabs = page.getByRole("navigation", { name: "Filter activity" });
    await tabs.getByRole("link", { name: /^History/ }).click();
    await tabs.getByRole("link", { name: /^Comments/ }).click();
    await expect(replies(page).getByText("Third")).toBeVisible();
    await expect(replies(page).getByText("Fourth")).toHaveCount(0);
    await page.getByRole("button", { name: "Show 2 more replies" }).click();
    await expect(replies(page).getByText("Fourth")).toBeVisible();
    await expect(replies(page).getByText("Fifth")).toBeVisible();
  });

  test("reacts, unreacts and picks from the palette", async ({ page }) => {
    await open(page);
    // Others already chose 👍 and 🎉 on the first comment.
    const thumbs = page.getByRole("button", { name: "👍 1", exact: true });
    await expect(thumbs).toHaveAttribute("aria-pressed", "false");

    await thumbs.click();
    const mine = page.getByRole("button", { name: "👍 2, you reacted" });
    await expect(mine).toHaveAttribute("aria-pressed", "true");
    await mine.click();
    await expect(
      page.getByRole("button", { name: "👍 1", exact: true }),
    ).toHaveAttribute("aria-pressed", "false");

    await page.getByRole("button", { name: "Add a reaction" }).first().click();
    await page.getByRole("button", { name: "React with 🚀" }).click();
    await expect(
      page.getByRole("button", { name: "🚀 1, you reacted" }),
    ).toHaveAttribute("aria-pressed", "true");
    await page.getByRole("button", { name: "🚀 1, you reacted" }).click();
    await expect(page.getByRole("button", { name: /^🚀/ })).toHaveCount(0);
  });
});

test.describe("viewer", () => {
  test.skip(
    persona !== "viewer" || scenario !== "default",
    "Needs the default viewer scenario",
  );

  test("sees the chips and who reacted but cannot react or reply", async ({
    page,
  }) => {
    await open(page);
    const chip = page.getByRole("button", { name: "👍 1", exact: true });
    await expect(chip).toBeVisible();
    await expect(chip).toHaveAttribute("aria-disabled", "true");
    await chip.hover();
    await expect(page.getByRole("tooltip")).toContainText("Reacted by");

    await chip.click({ force: true });
    await expect(chip).toHaveAttribute("aria-pressed", "false");
    await expect(
      page.getByRole("button", { name: "Add a reaction" }),
    ).toHaveCount(0);
    await expect(
      page.getByRole("button", { name: "Reply", exact: true }),
    ).toHaveCount(0);
  });
});

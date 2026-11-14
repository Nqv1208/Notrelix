import { expect, test } from "@playwright/test";

const ITEM =
  "/workspaces/mock-workspace-primary/boards/mock-board-erp?item=mock-erp-item-142";

test.skip(
  (process.env.VITE_MOCK_PERSONA ?? "owner") !== "owner" ||
    (process.env.VITE_MOCK_STATE ?? "default") !== "default",
  "Needs the default owner world",
);

/**
 * The comment actions are drawn as 12.5 px text links. Below 600 px each has an invisible hit area at least 44 px
 * tall and as wide as its word and the shared gap to its neighbour (the 12 px gap is split between the two), so a touch anywhere in a 44 px band lands on it.
 */
test("at 390 px every comment action is hit within a 44 px band around its text", async ({
  page,
}) => {
  await page.setViewportSize({ width: 390, height: 844 });
  await page.goto(ITEM);
  const composer = page
    .getByRole("button", { name: "Viết bình luận…", exact: true })
    .first();
  await expect(composer).toBeVisible();
  await composer.click();
  await page.keyboard.type("Đã đối chiếu xong nhóm A.");
  await page.keyboard.press("Control+Enter");
  const row = page.locator("[data-comment]").first();
  await expect(row).toBeVisible();
  const names = ["Sửa", "Xoá", "Trả lời", "Giải quyết"];
  for (const name of names) {
    const button = row.getByRole("button", { name, exact: true });
    await button.scrollIntoViewIfNeeded();
    const box = (await button.boundingBox())!;
    expect(box.height, `${name} glyph box is unchanged`).toBeLessThan(24);
    const cx = box.x + box.width / 2;
    const cy = box.y + box.height / 2;
    // A word under 44 px wide is hit just past its glyph (the 12 px gap to a neighbour is shared); a wider one inside it.
    const reach = box.width < 44 ? box.width / 2 + 1 : 20;
    for (const [dx, dy] of [
      [0, -21],
      [0, 21],
      [-reach, 0],
      [reach, 0],
    ] as const) {
      const hit = await page.evaluate(
        ([x, y, label]) =>
          document
            .elementFromPoint(x!, y!)
            ?.closest("button")
            ?.textContent?.trim() === label,
        [cx + dx, cy + dy, name] as const,
      );
      expect(hit, `${name} at ${dx},${dy}`).toBe(true);
    }
  }
});

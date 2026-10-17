import { expect, test, type Locator, type Page } from "@playwright/test";

/**
 * Dragging a Kanban card and adding a lane, on the mock world: the whole card
 * is the handle, a lifted copy follows the pointer, the card's slot stays as a
 * gap of its own height at the drop place, the lane under the pointer is
 * outlined, a drop writes the lane's status (and the place), Escape puts
 * everything back, and "+ Thêm trạng thái" after the last lane adds a status.
 * The page clock is fixed to the fixtures' 12/10/2026 10:40. Owner persona
 * (the default server).
 */
const BOARD = "/workspaces/mock-workspace-primary/boards/mock-board-erp";
const NOW = new Date("2026-10-12T10:40:00+07:00");
const ERP_149 = "Viết tài liệu quy trình nhập hoá đơn đầu vào";
const ERP_142 = "Chuyển dữ liệu khách hàng từ hệ thống cũ sang ERP";
const ERP_144 = "Kiểm thử phân hệ công nợ với số liệu tháng 9";

async function openBoard(page: Page, viewport = { width: 1440, height: 900 }) {
  await page.setViewportSize(viewport);
  await page.clock.setFixedTime(NOW);
  await page.addInitScript(() => {
    try {
      window.localStorage.setItem("notrelix-locale", "vi");
    } catch {
      // Private mode: the default locale applies.
    }
  });
  await page.goto(`${BOARD}?view=kanban`);
  await expect(lane(page, "Cần làm")).toBeVisible();
}

function lane(page: Page, name: string) {
  return page.getByRole("region", { name, exact: true });
}

function card(page: Page, title: string): Locator {
  return page.locator("article").filter({ hasText: title }).first();
}

async function titlesIn(region: Locator): Promise<string[]> {
  return region.locator("article h3").allTextContents();
}

async function box(locator: Locator) {
  const result = await locator.boundingBox();
  if (!result) throw new Error("element has no box");
  return result;
}

/** Presses on the card's body and moves past the activation distance. */
async function pickUp(page: Page, title: string) {
  const from = await box(card(page, title));
  const grab = { x: from.x + 90, y: from.y + 50 };
  await page.mouse.move(grab.x, grab.y);
  await page.mouse.down();
  await page.mouse.move(grab.x + 6, grab.y + 6, { steps: 2 });
  await page.mouse.move(grab.x + 14, grab.y + 10, { steps: 2 });
  return { from, grab };
}

test.describe("Kanban drag", () => {
  test("a click still opens the card and starts no drag", async ({ page }) => {
    await openBoard(page);
    await card(page, ERP_149).getByRole("button", { name: ERP_149 }).click();
    await expect(page).toHaveURL(/item=/);
    await expect(page.locator("[data-kanban-drag-overlay]")).toHaveCount(0);
  });

  test("the whole card is lifted into a copy that follows the pointer, its slot stays as a gap and the target lane is outlined", async ({
    page,
  }) => {
    await openBoard(page);
    const idle = await box(card(page, ERP_149));
    // Idle card geometry is the mockup's (248 wide, 12 padding, radius 11).
    expect(Math.round(idle.width)).toBe(248);
    const { grab } = await pickUp(page, ERP_149);

    const overlay = page.locator("[data-kanban-drag-overlay]");
    await expect(overlay).toBeVisible();
    // The copy is the card's size, tilted and lifted (shadow), and it keeps
    // the pointer where the card was grabbed.
    const lifted = await box(overlay);
    expect(Math.abs(lifted.width - idle.width)).toBeLessThan(14);
    const tilt = await overlay.evaluate((element) => {
      const style = getComputedStyle(element);
      return { rotate: style.rotate, scale: style.scale };
    });
    expect(tilt.rotate).not.toBe("none");
    expect(Number.parseFloat(tilt.scale)).toBeGreaterThan(1);
    const shadow = await overlay.evaluate(
      (element) => getComputedStyle(element).boxShadow,
    );
    expect(shadow).not.toBe("none");
    await page.mouse.move(grab.x + 200, grab.y + 140, { steps: 8 });
    const moved = await box(overlay);
    expect(moved.x - lifted.x).toBeGreaterThan(150);
    expect(Math.abs(moved.y - lifted.y - 126)).toBeLessThan(30);

    // The card's slot is a gap with the card's own height.
    const gap = page.locator("article[data-dragging]");
    await expect(gap).toHaveCount(1);
    const gapBox = await box(gap);
    expect(Math.abs(gapBox.height - idle.height)).toBeLessThan(1.5);
    expect(Math.round(gapBox.width)).toBe(248);

    // Over the "Đang làm" lane: the gap is in that lane and the lane is outlined.
    const doing = lane(page, "Đang làm");
    const doingBox = await box(doing);
    await page.mouse.move(doingBox.x + 120, doingBox.y + 120, { steps: 10 });
    await expect(doing).toHaveAttribute("data-drop-target", "");
    await expect(doing.locator("article[data-dragging]")).toHaveCount(1);
    await expect(lane(page, "Cần làm")).not.toHaveAttribute(
      "data-drop-target",
      "",
    );
    // The lane counts follow the preview.
    await expect(doing).toHaveAttribute("data-lane-count", "4");
    await expect(lane(page, "Cần làm")).toHaveAttribute("data-lane-count", "5");
    await page.mouse.up();
    await expect(page.locator("[data-kanban-drag-overlay]")).toHaveCount(0);
  });

  test("a drop between two cards of another lane writes the status and keeps the place", async ({
    page,
  }) => {
    await openBoard(page);
    await pickUp(page, ERP_149);
    const doing = lane(page, "Đang làm");
    const first = await box(card(page, ERP_142));
    const second = await box(card(page, ERP_144));
    // Between the first and the second card of the lane.
    const between = (first.y + first.height + second.y) / 2;
    await page.mouse.move(first.x + 120, between, { steps: 14 });
    const gap = doing.locator("article[data-dragging]");
    await expect(gap).toHaveCount(1);
    // The gap sits between them while dragging.
    const order = await doing
      .locator("article")
      .evaluateAll((nodes) =>
        nodes.map((node) =>
          node.hasAttribute("data-dragging")
            ? "GAP"
            : (node.querySelector("h3")?.textContent ?? ""),
        ),
      );
    expect(order.slice(0, 3)).toEqual([ERP_142, "GAP", ERP_144]);
    await page.mouse.up();
    await expect(doing).toHaveAttribute("data-lane-count", "4");
    await expect(lane(page, "Cần làm")).toHaveAttribute("data-lane-count", "5");
    await expect(doing.locator("article h3").nth(1)).toHaveText(ERP_149);
    expect((await titlesIn(doing)).slice(0, 3)).toEqual([
      ERP_142,
      ERP_149,
      ERP_144,
    ]);
    // The server kept it: the board read after the refetch agrees.
    await page.waitForTimeout(400);
    expect((await titlesIn(doing)).slice(0, 3)).toEqual([
      ERP_142,
      ERP_149,
      ERP_144,
    ]);
  });

  test("after a drop the card never shows in its old lane, not even for a frame", async ({
    page,
  }) => {
    await openBoard(page);
    await pickUp(page, ERP_149);
    const first = await box(card(page, ERP_142));
    const second = await box(card(page, ERP_144));
    await page.mouse.move(
      first.x + 120,
      (first.y + first.height + second.y) / 2,
      { steps: 14 },
    );
    await expect(
      lane(page, "Đang làm").locator("article[data-dragging]"),
    ).toHaveCount(1);
    const target = await box(lane(page, "Đang làm"));
    // One sample per animation frame, from the drop until the board is
    // settled: the lane the card itself is in and where the lifted copy is
    // while it settles (it must head for the new place, never the old one).
    await page.evaluate((title) => {
      const samples: { lane: string; copyX: number | null }[] = [];
      (window as unknown as { __frames: typeof samples }).__frames = samples;
      const tick = () => {
        const where = Array.from(document.querySelectorAll("article"))
          .filter((node) => !node.closest("[data-kanban-drag-overlay]"))
          .find((node) => node.querySelector("h3")?.textContent === title)
          ?.closest("section,[role=region]")
          ?.getAttribute("aria-label");
        const copy = document
          .querySelector("[data-kanban-drag-overlay]")
          ?.getBoundingClientRect();
        samples.push({
          lane: where ?? "none",
          copyX: copy ? copy.x + copy.width / 2 : null,
        });
        if (samples.length < 90) requestAnimationFrame(tick);
      };
      requestAnimationFrame(tick);
    }, ERP_149);
    await page.mouse.up();
    await page.waitForTimeout(1600);
    const frames = await page.evaluate(
      () =>
        (
          window as unknown as {
            __frames: { lane: string; copyX: number | null }[];
          }
        ).__frames,
    );
    expect(frames.length).toBeGreaterThan(20);
    expect(frames.filter((frame) => frame.lane === "Cần làm")).toEqual([]);
    expect(frames.at(-1)?.lane).toBe("Đang làm");
    const outside = frames.filter(
      (frame) =>
        frame.copyX !== null &&
        (frame.copyX < target.x || frame.copyX > target.x + target.width),
    );
    expect(outside).toEqual([]);
  });

  test("a drop inside the lane reorders it", async ({ page }) => {
    await openBoard(page);
    const todo = lane(page, "Cần làm");
    const before = await titlesIn(todo);
    // The lane runs through the board's groups; an order change is made
    // between cards of one group (the second and third card share one).
    const moved = before[1]!;
    await pickUp(page, moved);
    const third = await box(card(page, before[2]!));
    await page.mouse.move(third.x + 100, third.y + third.height - 6, {
      steps: 12,
    });
    await page.mouse.up();
    // The card left its place and the others kept their order.
    await expect
      .poll(async () => (await titlesIn(todo)).indexOf(moved))
      .toBeGreaterThan(1);
    expect((await titlesIn(todo)).filter((title) => title !== moved)).toEqual(
      before.filter((title) => title !== moved),
    );
  });

  test("Escape puts the card back and writes nothing", async ({ page }) => {
    await openBoard(page);
    const todo = lane(page, "Cần làm");
    const before = await titlesIn(todo);
    const writes: string[] = [];
    page.on("request", (request) => {
      if (request.method() !== "GET") writes.push(request.url());
    });
    await pickUp(page, ERP_149);
    const doing = lane(page, "Đang làm");
    const doingBox = await box(doing);
    await page.mouse.move(doingBox.x + 120, doingBox.y + 140, { steps: 10 });
    await expect(doing).toHaveAttribute("data-lane-count", "4");
    await page.keyboard.press("Escape");
    await expect(page.locator("[data-kanban-drag-overlay]")).toHaveCount(0);
    await expect(page.locator("article[data-dragging]")).toHaveCount(0);
    await expect(doing).toHaveAttribute("data-lane-count", "3");
    expect(await titlesIn(todo)).toEqual(before);
    await page.mouse.up();
    expect(await titlesIn(todo)).toEqual(before);
    expect(writes).toEqual([]);
  });

  test("the board scrolls sideways when a card is dragged to its edge", async ({
    page,
  }) => {
    await openBoard(page, { width: 1000, height: 900 });
    const scroller = page.locator("[data-kanban-board] > div").nth(1);
    expect(
      await scroller.evaluate((el) => el.scrollWidth > el.clientWidth),
    ).toBe(true);
    const start = await scroller.evaluate((el) => el.scrollLeft);
    const { grab } = await pickUp(page, ERP_149);
    const edge = await box(scroller);
    await page.mouse.move(edge.x + edge.width - 8, grab.y, { steps: 10 });
    await page.waitForTimeout(900);
    const scrolled = await scroller.evaluate((el) => el.scrollLeft);
    expect(scrolled).toBeGreaterThan(start + 40);
    await page.keyboard.press("Escape");
    await page.mouse.up();
  });
});

test.describe("Kanban lanes", () => {
  test("the slot after the last lane adds a status as the last lane", async ({
    page,
  }) => {
    // Wide enough for the new lane and the card's lane to be on screen.
    await openBoard(page, { width: 2300, height: 900 });
    const slot = page.getByRole("button", { name: "Thêm trạng thái" });
    const last = await box(lane(page, "Hoàn thành"));
    const slotBox = await box(slot);
    // As wide as a lane, one gap (12) after the last lane, top aligned.
    expect(Math.round(slotBox.width)).toBe(264);
    expect(
      Math.abs(slotBox.x - (last.x + last.width + 12)),
    ).toBeLessThanOrEqual(1);
    expect(Math.abs(slotBox.y - last.y)).toBeLessThanOrEqual(1);

    await slot.click();
    const name = page.getByRole("textbox", { name: "Tên trạng thái" });
    await expect(name).toBeFocused();
    // Empty and duplicate names are refused with a message.
    await name.press("Enter");
    await expect(page.getByRole("alert")).toHaveText("Nhập tên trạng thái.");
    await name.fill("Chờ duyệt");
    await expect(page.getByRole("alert")).toContainText("Đã có trạng thái");
    // Name, colour and group; Enter confirms.
    await name.fill("Chờ khách duyệt");
    await page.getByRole("radio", { name: "Đỏ" }).click();
    await page.getByRole("radio", { name: "Hoàn thành" }).click();
    await name.press("Enter");

    const added = lane(page, "Chờ khách duyệt");
    await expect(added).toBeVisible();
    // (The lane is drawn again with the server's id once the board is read.)
    await expect
      .poll(async () => Math.round((await added.boundingBox())?.width ?? 0))
      .toBe(264);
    expect((await box(added)).x).toBeGreaterThan(
      (await box(lane(page, "Hoàn thành"))).x,
    );
    await expect(added).toHaveAttribute("data-lane-count", "0");
    // It is the last lane and the slot moved after it.
    const names = await page
      .locator("[data-kanban-lanes] > section")
      .evaluateAll((nodes) => nodes.map((n) => n.getAttribute("aria-label")));
    expect(names.at(-1)).toBe("Chờ khách duyệt");
    const dot = added.locator("h2").locator("xpath=preceding-sibling::span[1]");
    expect(
      await dot.evaluate((el) => getComputedStyle(el).backgroundColor),
    ).toBe("rgb(200, 20, 63)");
    // Survives the refetch (the mock kept it).
    await page.waitForTimeout(400);
    await expect(lane(page, "Chờ khách duyệt")).toBeVisible();
    // A card can be dropped on the new lane.
    await pickUp(page, ERP_149);
    const target = await box(lane(page, "Chờ khách duyệt"));
    await page.mouse.move(target.x + 100, target.y + 90, { steps: 12 });
    await page.mouse.up();
    await expect(lane(page, "Chờ khách duyệt")).toHaveAttribute(
      "data-lane-count",
      "1",
    );
  });

  test("Escape closes the form without adding a lane", async ({ page }) => {
    await openBoard(page);
    await page.getByRole("button", { name: "Thêm trạng thái" }).click();
    const name = page.getByRole("textbox", { name: "Tên trạng thái" });
    await name.fill("Tạm dừng");
    await name.press("Escape");
    await expect(name).toHaveCount(0);
    await expect(lane(page, "Tạm dừng")).toHaveCount(0);
  });

  test("a lane's menu renames and recolours its status", async ({ page }) => {
    await openBoard(page);
    await page.getByRole("button", { name: "Tuỳ chọn cột Chờ duyệt" }).click();
    await page.getByRole("menuitem", { name: "Sửa trạng thái" }).click();
    const name = page.getByRole("textbox", { name: "Tên trạng thái" });
    await expect(name).toHaveValue("Chờ duyệt");
    await name.fill("Chờ phê duyệt");
    await page.getByRole("radio", { name: "Xanh ngọc" }).click();
    await page.getByRole("button", { name: "Lưu", exact: true }).click();
    const renamed = lane(page, "Chờ phê duyệt");
    await expect(renamed).toBeVisible();
    await expect(renamed).toHaveAttribute("data-lane-count", "2");
    await expect(lane(page, "Chờ duyệt")).toHaveCount(0);
    await page.waitForTimeout(400);
    await expect(lane(page, "Chờ phê duyệt")).toBeVisible();
  });
});

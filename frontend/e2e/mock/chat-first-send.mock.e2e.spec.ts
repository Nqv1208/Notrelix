import { expect, test } from "@playwright/test";

const WORKSPACE = "mock-workspace-primary";

type Seen = { shown: boolean; blinks: number };

test("the first send from an older window shows one bubble at the bottom and ends scrolled to it", async ({
  page,
}) => {
  await page.goto(`/workspaces/${WORKSPACE}/chat`);
  const log = page.getByRole("log");
  await expect(log).toBeVisible();

  // A search hit reopens the history around message 101: a window with newer pages behind it.
  await page
    .getByRole("button", { name: /^(Search messages|Tìm tin nhắn)$/ })
    .first()
    .click();
  await page.locator('input[type="search"]').fill("Tin nhắn 101.");
  await page.getByRole("listbox").getByRole("option").first().click();
  await expect(log.getByText(/Tin nhắn 101\./).first()).toBeVisible();
  await expect(log.getByText(/Tin nhắn 320\./)).toHaveCount(0);

  const composer = page.getByRole("combobox", {
    name: /^(Message #general|Tin nhắn tới #general)$/,
  });
  const text = `Older window ${Date.now()}`;
  await composer.fill(text);
  // Once the bubble shows, count every painted frame without it.
  await page.evaluate((needle) => {
    const w = window as unknown as { __seen: Seen };
    w.__seen = { shown: false, blinks: 0 };
    const count = () =>
      Array.from(document.querySelectorAll('[role="log"] *')).filter(
        (el) =>
          (el.textContent ?? "").includes(needle) &&
          !Array.from(el.children).some((c) =>
            (c.textContent ?? "").includes(needle),
          ),
      ).length;
    const frame = () => {
      if (count() > 0) w.__seen.shown = true;
      else if (w.__seen.shown) w.__seen.blinks += 1;
      requestAnimationFrame(frame);
    };
    requestAnimationFrame(frame);
  }, text);
  await composer.press("Enter");

  // The newest window is there, with the message once and no pending copy.
  await expect(log.getByText(/Tin nhắn 320\./)).toHaveCount(1);
  await expect(log.getByText(text)).toHaveCount(1);
  await expect(page.getByTestId("chat-pending-message")).toHaveCount(0);
  // The list ends on the bubble.
  await expect
    .poll(() =>
      page.evaluate((needle) => {
        const all = Array.from(
          document.querySelectorAll<HTMLElement>(
            '[role="log"], [role="log"] *',
          ),
        );
        const bubble = all.find(
          (el) =>
            el.textContent?.includes(needle) &&
            !Array.from(el.children).some((c) =>
              c.textContent?.includes(needle),
            ),
        );
        const scroller = all.find(
          (el) =>
            el.scrollHeight > el.clientHeight + 50 &&
            getComputedStyle(el).overflowY !== "visible",
        );
        if (!bubble || !scroller) return false;
        const gap =
          scroller.scrollHeight - scroller.scrollTop - scroller.clientHeight;
        const inView =
          bubble.getBoundingClientRect().bottom <=
          scroller.getBoundingClientRect().bottom + 1;
        return gap <= 2 && inView;
      }, text),
    )
    .toBe(true);
  const seen = await page.evaluate(
    () => (window as unknown as { __seen: Seen }).__seen,
  );
  expect(seen).toEqual({ shown: true, blinks: 0 });
});

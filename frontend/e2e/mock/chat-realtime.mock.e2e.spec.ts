import { expect, test, type Page } from "@playwright/test";

// Chat realtime (UC-CHT-C02, C05, C09, D03): a message from another member arrives
// live, the unread badge of another channel moves, a reconnect keeps the channel
// current, and a message while the reader is scrolled up does not move the list.
// Written for the mock backend; NOT run in this change. The mock store lives in the
// browser page, so the "other member" is the dev hook `__notrelixMockInjectChat`
// (a second browser context would not share the store). Never opens a socket.

const WORKSPACE = "mock-workspace-primary";
const GENERAL = "c1000000-0000-4000-8000-000000000001";
const ENGINEERING = "c1000000-0000-4000-8000-000000000002";
const OTHER_MEMBER = "mock-user-member";

type MockHost = {
  __notrelixMockInjectChat?: (
    spaceId: string,
    authorId: string,
    content: string,
  ) => boolean;
  __notrelixMockRealtime?: { simulateState(state: string): void };
};

async function inject(page: Page, spaceId: string, content: string) {
  const ok = await page.evaluate(
    ([space, author, text]) =>
      (globalThis as MockHost).__notrelixMockInjectChat?.(
        space!,
        author!,
        text!,
      ) ?? false,
    [spaceId, OTHER_MEMBER, content] as const,
  );
  expect(ok).toBe(true);
}

async function openChannel(page: Page, channelId: string) {
  await page.goto(`/workspaces/${WORKSPACE}/chat/${channelId}`);
  await expect(page.getByRole("log")).toBeVisible();
}

test("a message from another member appears live and is announced politely", async ({
  page,
}) => {
  await openChannel(page, GENERAL);
  const text = `Live ${Date.now()}`;
  await inject(page, GENERAL, text);

  const log = page.getByRole("log");
  await expect(log.getByText(text)).toBeVisible();
  await expect(page.getByRole("status").filter({ hasText: text })).toHaveCount(
    1,
  );
});

test("the unread badge of a background channel moves without opening it", async ({
  page,
}) => {
  await openChannel(page, GENERAL);
  const channels = page.getByRole("navigation", { name: /^(Channels|Kênh)$/ });
  const engineering = channels.getByRole("button", { name: /engineering/ });
  const before = (await engineering.textContent()) ?? "";

  await inject(page, ENGINEERING, `Badge ${Date.now()}`);

  await expect
    .poll(async () => (await engineering.textContent()) ?? "")
    .not.toBe(before);
});

test("a reconnect keeps the open channel current", async ({ page }) => {
  await openChannel(page, GENERAL);
  await page.evaluate(() =>
    (globalThis as MockHost).__notrelixMockRealtime?.simulateState(
      "reconnecting",
    ),
  );
  await page.evaluate(() =>
    (globalThis as MockHost).__notrelixMockRealtime?.simulateState("connected"),
  );

  const text = `After reconnect ${Date.now()}`;
  await inject(page, GENERAL, text);
  await expect(page.getByRole("log").getByText(text)).toBeVisible();
});

test("a message while the reader is scrolled up does not move the list; the jump control brings them back", async ({
  page,
}) => {
  await openChannel(page, GENERAL);
  const log = page.getByRole("log");
  await log.evaluate((el) => {
    el.scrollTop = Math.max(0, el.scrollTop - 400);
  });
  const before = await log.evaluate((el) => el.scrollTop);

  const text = `Below ${Date.now()}`;
  await inject(page, GENERAL, text);
  await expect(log.getByText(text)).toBeAttached();

  expect(await log.evaluate((el) => el.scrollTop)).toBeLessThanOrEqual(
    before + 1,
  );
  const jump = page.getByRole("button", {
    name: /^(Jump to latest|Xem tin mới nhất)$/,
  });
  await expect(jump).toBeVisible();
  // The control sits outside the log, so it never covers a message.
  expect(
    await jump.evaluate((node) => Boolean(node.closest('[role="log"]'))),
  ).toBe(false);

  await jump.click();
  await expect(log.getByText(text)).toBeInViewport();
});

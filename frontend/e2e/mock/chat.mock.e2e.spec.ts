import { expect, test } from "@playwright/test";

// Chat (CHT-001..004, CHT-009): opens the seeded channels, sends a message and
// sees it in the list. Written for the mock backend; not run in this change.
const WORKSPACE = "mock-workspace-primary";

test("opens a channel, sends a message and sees it at the end of the list", async ({
  page,
}) => {
  await page.goto(`/workspaces/${WORKSPACE}/chat`);

  // A bare /chat opens the first channel the caller is in.
  await expect(page).toHaveURL(
    new RegExp(`/workspaces/${WORKSPACE}/chat/[0-9a-f-]+$`),
  );

  const channels = page.getByRole("navigation", { name: /^(Channels|Kênh)$/ });
  await expect(
    channels.getByRole("button", { name: /general/ }),
  ).toHaveAttribute("aria-current", "page");

  const log = page.getByRole("log");
  await expect(log).toBeVisible();

  const composer = page.getByRole("combobox", {
    name: /^(Message #general|Tin nhắn tới #general)$/,
  });
  const text = `Chat e2e ${Date.now()}`;
  await composer.fill(text);
  // Count the copies of the text on every DOM change, so a moment with two copies cannot pass unseen.
  await page.evaluate((needle) => {
    const w = window as unknown as { __maxCopies: number };
    w.__maxCopies = 0;
    const count = () =>
      Array.from(document.querySelectorAll('[role="log"] *')).filter(
        (el) =>
          (el.textContent ?? "").includes(needle) &&
          !Array.from(el.children).some((c) =>
            (c.textContent ?? "").includes(needle),
          ),
      ).length;
    new MutationObserver(() => {
      w.__maxCopies = Math.max(w.__maxCopies, count());
    }).observe(document.body, {
      childList: true,
      subtree: true,
      characterData: true,
    });
  }, text);
  await composer.press("Enter");

  // The pending bubble is replaced in place by the stored message: never two copies.
  await expect(log.getByText(text)).toHaveCount(1);
  await expect(page.getByTestId("chat-pending-message")).toHaveCount(0);
  expect(
    await page.evaluate(
      () => (window as unknown as { __maxCopies: number }).__maxCopies,
    ),
  ).toBe(1);
  await expect(log.getByText(text)).toBeVisible();
  await expect(composer).toHaveValue("");
});

test("a message over 4000 characters is refused in the composer", async ({
  page,
}) => {
  await page.goto(`/workspaces/${WORKSPACE}/chat`);
  const composer = page.getByRole("combobox", {
    name: /^(Message #general|Tin nhắn tới #general)$/,
  });
  await composer.fill("a".repeat(4001));
  // Past the limit the composer is invalid and offers no Send button (the microphone stays); Enter sends nothing.
  await expect(composer).toHaveAttribute("aria-invalid", "true");
  await expect(page.getByRole("button", { name: /^(Send|Gửi)$/ })).toHaveCount(
    0,
  );
  await composer.press("Enter");
  await expect(composer).toHaveValue("a".repeat(4001));
  await expect(page.getByTestId("chat-pending-message")).toHaveCount(0);
});

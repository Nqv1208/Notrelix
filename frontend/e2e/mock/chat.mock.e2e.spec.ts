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

  const composer = page.getByRole("textbox", {
    name: /^(Message #general|Tin nhắn tới #general)$/,
  });
  const text = `Chat e2e ${Date.now()}`;
  await composer.fill(text);
  await composer.press("Enter");

  await expect(log.getByText(text)).toBeVisible();
  await expect(composer).toHaveValue("");
});

test("a message over 4000 characters is refused in the composer", async ({
  page,
}) => {
  await page.goto(`/workspaces/${WORKSPACE}/chat`);
  const composer = page.getByRole("textbox", {
    name: /^(Message #general|Tin nhắn tới #general)$/,
  });
  await composer.fill("a".repeat(4001));
  await expect(
    page.getByRole("button", { name: /^(Send|Gửi)$/ }),
  ).toBeDisabled();
});

import { expect, test } from "@playwright/test";

// The Chat sidebar search field filters the lists at once, moves with the arrows, opens with Enter,
// clears with Escape and, when nothing matches, hands the text to the message search.
const WORKSPACE = "mock-workspace-primary";
const FIELD = /^(Jump to or search|Chuyển tới hoặc tìm kiếm)$/;

test("the sidebar field finds a channel, ignores accents and falls back to message search", async ({
  page,
}) => {
  await page.goto(`/workspaces/${WORKSPACE}/chat`);
  const nav = page.getByRole("navigation", { name: /^(Channels|Kênh)$/ });
  const field = nav.getByRole("combobox", { name: FIELD });
  await expect(field).toHaveAttribute("aria-keyshortcuts", "/");

  // "/" from the page body jumps to the field.
  await page.locator("body").press("/");
  await expect(field).toBeFocused();

  await field.fill("RAND");
  const options = nav.getByRole("option");
  await expect(options).toHaveCount(1);
  await expect(options.first()).toContainText(/random/);
  await field.press("Enter");
  await expect(field).toHaveValue("");
  await expect(nav.getByRole("button", { name: /random/ })).toHaveAttribute(
    "aria-current",
    "page",
  );

  await field.fill("xyzzy");
  await expect(nav.getByText(/^(Nothing found|Không tìm thấy)$/)).toBeVisible();
  await field.press("Escape");
  await expect(field).toHaveValue("");

  await field.fill("xyzzy");
  await nav.getByRole("button", { name: /xyzzy/ }).click();
  await expect(page.getByRole("dialog").locator("input").first()).toHaveValue(
    "xyzzy",
  );
});

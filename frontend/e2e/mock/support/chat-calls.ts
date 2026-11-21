import { expect, type Page } from "@playwright/test";
import { vi as copy } from "../../../packages/features/chat/src/web/components/chat.copy";

/** Shared steps of the one-to-one call specs on the mock backend (the other person is the dev hook `__notrelixMockCall`). */
export const WORKSPACE = "mock-workspace-primary";
export { copy };

export async function setMode(page: Page, mode: "light" | "dark") {
  await page.addInitScript((m) => {
    try {
      window.localStorage.setItem("notrelix-locale", "vi");
      window.localStorage.setItem("notrelix-theme-mode", m);
    } catch {
      // Private mode: the defaults apply.
    }
  }, mode);
}

export async function openDm(page: Page, mode: "light" | "dark" = "light") {
  await setMode(page, mode);
  await page.goto(`/workspaces/${WORKSPACE}/chat`);
  const newMessage = page
    .getByRole("button", { name: /^(New message|Tin nhắn mới)$/ })
    .first();
  await expect(newMessage).toBeVisible();
  await newMessage.click();
  const dialog = page.getByRole("dialog");
  await dialog.getByRole("checkbox").nth(0).click();
  await dialog
    .getByRole("button", { name: /^(Start conversation|Bắt đầu trò chuyện)$/ })
    .click();
  await expect(page).toHaveURL(/\/chat\/dm\/[^/]+$/);
  const space = page.url().split("/").pop()!;
  const peer = await hook(page, "peer", space);
  expect(peer).not.toBeNull();
  return { space, peer: peer as string };
}

export const hook = (page: Page, name: string, ...args: unknown[]) =>
  page.evaluate(
    ([n, a]) =>
      (
        (
          globalThis as unknown as {
            __notrelixMockCall: Record<string, (...x: unknown[]) => unknown>;
          }
        ).__notrelixMockCall[n as string] as (...x: unknown[]) => unknown
      )(...(a as unknown[])),
    [name, args] as const,
  );

/** The voice or video button of the conversation header. */
export const startCall = (page: Page, kind: "voice" | "video") =>
  page
    .locator("main header")
    .getByRole("button", {
      name: kind === "voice" ? copy.panel.voiceCall : copy.panel.videoCall,
    })
    .click();

/** Sends one message so the conversation has a latest message under the call surfaces. */
export async function sendMessage(page: Page, text: string) {
  const field = page.locator("main form textarea").first();
  await field.fill(text);
  await field.press("Enter");
  await expect(page.locator("[data-bubble]").last()).toContainText(text);
}

export const SURFACES =
  "[data-testid=chat-call-stage]:not([hidden]), [data-testid=chat-call-incoming], [data-testid=chat-call-strip], [data-testid=chat-call-dock]";

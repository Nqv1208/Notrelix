import { expect, test, type Page } from "@playwright/test";

/**
 * Moving a block with Alt+Shift+Arrow right after a click: the key must act on
 * the block the person clicked, not on a caret the editor has not read yet.
 */
const DOC = "/workspaces/mock-workspace-primary/docs/mock-doc-invoice-process";

async function openEditor(page: Page) {
  await page.setViewportSize({ width: 1440, height: 900 });
  await page.addInitScript(() => {
    try {
      window.localStorage.setItem("notrelix-locale", "vi");
    } catch {
      // Private mode: the defaults apply.
    }
  });
  await page.goto(DOC);
  const editor = page.locator(".ProseMirror").first();
  await expect(editor.locator("> *").nth(2)).toBeVisible({ timeout: 30_000 });
  return editor;
}

test("a key in the same task as the click moves the clicked block", async ({
  page,
}) => {
  const editor = await openEditor(page);
  const order = () => editor.locator("> *").allTextContents();
  const before = await order();
  const target = editor.locator("> *").nth(2);
  const targetText = await target.innerText();
  expect(before.indexOf(targetText)).toBe(2);

  // The browser queues `selectionchange`; the key runs before it is delivered.
  await target.evaluate((node) => {
    const root = node.closest(".ProseMirror") as HTMLElement;
    root.focus();
    const text = node.firstChild as Text;
    document.getSelection()?.setBaseAndExtent(text, 2, text, 2);
    root.dispatchEvent(
      new KeyboardEvent("keydown", {
        key: "ArrowUp",
        keyCode: 38,
        altKey: true,
        shiftKey: true,
        bubbles: true,
        cancelable: true,
      }),
    );
  });

  const after = await order();
  expect(after.indexOf(targetText)).toBe(1);
  expect(after.length).toBe(before.length);
});

test("a click then Alt+Shift+ArrowDown moves the clicked block and announces its place", async ({
  page,
}) => {
  const editor = await openEditor(page);
  const target = editor.locator("> *").nth(2);
  const targetText = await target.innerText();

  await target.click();
  await page.keyboard.press("Alt+Shift+ArrowDown");

  await expect
    .poll(async () =>
      (await editor.locator("> *").allTextContents()).indexOf(targetText),
    )
    .toBe(3);
  await expect(
    page.getByRole("status").filter({ hasText: /\d/ }),
  ).not.toHaveCount(0);
});

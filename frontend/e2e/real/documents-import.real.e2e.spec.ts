import { expect, test } from "@playwright/test";
import {
  apiRead,
  findWorkspace,
  realUsers,
  signIn,
} from "./support/real-session";

test.setTimeout(240_000);

const apiUrl = process.env.VITE_API_URL ?? "http://127.0.0.1:58000/api/v1";
const FILE_MENU = /^(Tệp|File)$/;
const IMPORT_ITEM =
  /^(Nhập từ Markdown hoặc Word…|Import from Markdown or Word…)$/;
const FILE_INPUT = /Tệp Markdown hoặc Word|Markdown or Word file/;

type BlockRow = { type: string };

test("importing a Markdown file makes a page titled by its heading with a list and no script, a wrong type is refused in the dialog, and the dialog holds at 1440, 1024 and 390 px in both themes", async ({
  page,
}, testInfo) => {
  const stamp = Date.now();
  await signIn(page, realUsers.workspaceOwner);
  await page.evaluate(() => localStorage.setItem("notrelix-locale", "vi"));
  const workspace = await findWorkspace(page, "Workspace 3");
  const created = await page.request.fetch(
    `${apiUrl}/workspaces/${workspace.id}/pages`,
    { method: "GET" },
  );
  expect(created.ok()).toBe(true);
  const body = (await created.json()) as
    { id: string }[] | { items: { id: string }[] };
  const rows = Array.isArray(body) ? body : body.items;
  expect(rows.length).toBeGreaterThan(0);
  const host = rows[0]!.id;

  const md = `# Imported ${stamp}\n\nIntro text.\n\n- one\n- two\n\n<script>alert(1)</script>\n`;
  let viewed = 0;
  for (const mode of ["light", "dark"] as const) {
    await page.addInitScript((value) => {
      try {
        localStorage.setItem("notrelix-theme-mode", value);
      } catch {
        /* storage blocked */
      }
    }, mode);
    for (const width of [1440, 1024, 390]) {
      await page.setViewportSize({ width, height: width === 390 ? 844 : 900 });
      await page.goto(`/workspaces/${workspace.id}/docs/${host}`);
      await page.getByRole("menuitem", { name: FILE_MENU }).click();
      await page.getByRole("menuitem", { name: IMPORT_ITEM }).click();
      const dialog = page.getByRole("dialog");
      await expect(dialog).toBeVisible();
      const box = (await dialog.boundingBox())!;
      expect(box.x).toBeGreaterThanOrEqual(0);
      expect(box.x + box.width).toBeLessThanOrEqual(width);
      expect(
        await dialog.evaluate((n) => n.scrollWidth - n.clientWidth),
      ).toBeLessThanOrEqual(0);
      if (width === 390) {
        const small = await dialog.locator("button").evaluateAll((nodes) =>
          nodes
            .filter((n) => {
              const r = n.getBoundingClientRect();
              return r.width > 0 && r.height < 44;
            })
            .map((n) => n.textContent),
        );
        expect(small, "targets under 44 px at 390").toEqual([]);
      }
      // Error state: a wrong type is refused inside the dialog.
      await dialog.getByLabel(FILE_INPUT).setInputFiles({
        name: "a.pdf",
        mimeType: "application/pdf",
        buffer: Buffer.from("%PDF"),
      });
      await expect(dialog.getByRole("alert")).toBeVisible();
      viewed += 1;
      await testInfo.attach(`import-error-${mode}-${width}`, {
        body: await page.screenshot(),
        contentType: "image/png",
      });
      await page.keyboard.press("Escape");
    }
  }
  expect(viewed).toBe(6);

  // Happy path.
  await page.setViewportSize({ width: 1440, height: 900 });
  await page.goto(`/workspaces/${workspace.id}/docs/${host}`);
  await page.getByRole("menuitem", { name: FILE_MENU }).click();
  await page.getByRole("menuitem", { name: IMPORT_ITEM }).click();
  await page
    .getByRole("dialog")
    .getByLabel(FILE_INPUT)
    .setInputFiles({
      name: "note.md",
      mimeType: "text/markdown",
      buffer: Buffer.from(md),
    });
  await expect(page.locator("article h1")).toHaveText(`Imported ${stamp}`);
  const id = page.url().split("/").pop()!;
  const blocks = await apiRead<BlockRow[] | { items: BlockRow[] }>(
    page,
    `/pages/${id}/blocks`,
  );
  const list = Array.isArray(blocks) ? blocks : blocks.items;
  expect(list.some((b) => /list|bullet/i.test(b.type))).toBe(true);
  expect(JSON.stringify(list)).not.toMatch(/script|alert\(1\)/i);
});

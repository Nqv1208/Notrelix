import { expect, test, type Page } from "@playwright/test";

/**
 * The read-only page as a page-level Viewer (workspace guest persona), in
 * Vietnamese: the read-only note and the locked editor at 1440, 1024 and 390,
 * light and dark. The Doc boards draw no Viewer state, so the review is by
 * measurement: the note sits above the title inside the page column, the
 * page never scrolls sideways, the text is legible against its surface, and
 * no write control is offered.
 */
const persona = process.env.VITE_MOCK_PERSONA ?? "owner";
const DOC = "/workspaces/mock-workspace-primary/docs/mock-doc-invoice-process";
const NOW = new Date("2026-10-12T10:40:00+07:00");
const NOTE = "Bạn chỉ có quyền xem trang này.";

async function open(page: Page, width: number, dark: boolean) {
  await page.setViewportSize({ width, height: width < 500 ? 844 : 900 });
  await page.clock.setFixedTime(NOW);
  await page.addInitScript(
    ({ dark }) => {
      try {
        window.localStorage.setItem("notrelix-locale", "vi");
        if (dark) window.localStorage.setItem("notrelix-theme-mode", "dark");
      } catch {
        // Private mode: the defaults apply.
      }
    },
    { dark },
  );
  await page.goto(DOC);
  await expect(page.getByRole("heading", { level: 1 })).toBeVisible();
  await page.evaluate(() => document.fonts.ready);
}

/** WCAG contrast of the note's text on its first opaque background. */
function contrast(page: Page) {
  return page.evaluate(() => {
    const rgb = (c: string) => {
      const canvas = document.createElement("canvas");
      canvas.width = canvas.height = 1;
      const ctx = canvas.getContext("2d")!;
      ctx.fillStyle = c;
      ctx.fillRect(0, 0, 1, 1);
      const d = ctx.getImageData(0, 0, 1, 1).data;
      return [d[0]!, d[1]!, d[2]!, d[3]! / 255] as const;
    };
    const lum = (c: readonly number[]) => {
      const f = (v: number) => {
        const s = v / 255;
        return s <= 0.03928 ? s / 12.92 : ((s + 0.055) / 1.055) ** 2.4;
      };
      return 0.2126 * f(c[0]!) + 0.7152 * f(c[1]!) + 0.0722 * f(c[2]!);
    };
    const note = [...document.querySelectorAll("[role=status]")].find((e) =>
      e.textContent?.includes("chỉ có quyền xem"),
    )!;
    const fg = rgb(getComputedStyle(note.querySelector("span")!).color);
    let bg: readonly number[] = [255, 255, 255, 1];
    for (let e: Element | null = note; e; e = e.parentElement) {
      const c = rgb(getComputedStyle(e).backgroundColor);
      if (c[3] > 0.99) {
        bg = c;
        break;
      }
    }
    const [a, b] = [lum(fg), lum(bg)];
    return (Math.max(a, b) + 0.05) / (Math.min(a, b) + 0.05);
  });
}

test.describe("Read-only page (Viewer)", () => {
  test.skip(persona !== "viewer", "Needs the viewer (guest) persona");

  for (const width of [1440, 1024, 390]) {
    for (const dark of [false, true]) {
      const name = `${width}px ${dark ? "dark" : "light"}`;
      test(`read-only note and locked editor at ${name}`, async ({
        page,
      }, info) => {
        await open(page, width, dark);
        const note = page.getByRole("status").filter({ hasText: NOTE });
        await expect(note).toBeVisible();
        await expect(note).toContainText("Liên hệ người quản lý trang");

        const title = page.getByRole("heading", { level: 1 });
        expect(await title.getAttribute("contenteditable")).toBe("false");
        const boxes = page.getByRole("textbox");
        const count = await boxes.count();
        for (let i = 0; i < count; i++)
          expect(await boxes.nth(i).getAttribute("aria-readonly")).toBe("true");

        // The note is whole inside the viewport and above the title.
        const n = (await note.boundingBox())!;
        const t = (await title.boundingBox())!;
        expect(n.x).toBeGreaterThanOrEqual(0);
        expect(n.x + n.width).toBeLessThanOrEqual(width + 0.5);
        expect(n.y + n.height).toBeLessThanOrEqual(t.y + 0.5);

        // No sideways page scroll, and the note's text is never cut off.
        const overflow = await page.evaluate(() => {
          const el = [...document.querySelectorAll("[role=status]")].find((e) =>
            e.textContent?.includes("chỉ có quyền xem"),
          )!;
          return {
            page: document.documentElement.scrollWidth - window.innerWidth,
            note: el.scrollWidth - el.clientWidth,
          };
        });
        expect(overflow.page).toBeLessThanOrEqual(0);
        expect(overflow.note).toBeLessThanOrEqual(0);

        // Legible: WCAG AA (4.5:1) for the note's text.
        expect(await contrast(page)).toBeGreaterThanOrEqual(4.5);

        // Nothing offers a write: formatting is disabled where it is shown.
        const bold = page.getByRole("button", { name: /In đậm/ });
        if (await bold.count()) await expect(bold.first()).toBeDisabled();
        await expect(
          page.getByRole("button", { name: /Lưu phiên bản/ }),
        ).toHaveCount(0);
        await page.screenshot({
          path: `${process.env.RO_SHOTS ?? info.outputDir}/ro-${width}-${dark ? "dark" : "light"}.png`,
        });
      });
    }
  }
});

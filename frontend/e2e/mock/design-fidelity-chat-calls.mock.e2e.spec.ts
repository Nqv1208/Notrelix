import AxeBuilder from "@axe-core/playwright";
import { expect, test, type Locator, type Page } from "@playwright/test";
import { hook, openDm, sendMessage, startCall } from "./support/chat-calls";

/**
 * The one-to-one call surfaces against the Chat canvas call boards (rendered with render.py, light and dark), at 1440, 1024 and 390,
 * tolerance 1 px. Board facts:
 *  - Chat-Call-DM-Outgoing: the whole conversation pane is the call view (dark, in both themes). Header 64 high with the name (18) and
 *    the call topic (12.5). Centre column, content centred between header and bar: a 120 avatar in a pulse halo, 34 gap to the name
 *    (24 px, line 34.8), 10 to "Đang đổ chuông…" (14, line 20.3), 16 to a 28 high kind chip. Control bar 104 high: 48 round buttons,
 *    10 apart, a 12 px label 6 under each.
 *  - Chat-Call-DM-Incoming / -Incoming-T: a toast, 400 wide (440 on a tablet, left 10 top 16 of the pane), right 20 top 20 on a desktop,
 *    radius 16, padding 14, a 48 avatar, three 40 high buttons (Từ chối, Thoại, Trả lời) 8 apart: 118 wide at 400, 131.3 at 440.
 *    Chat-Call-Incoming-M: a full-screen sheet, a 120 avatar, three 64 round buttons 100 apart (36 between).
 *  - Chat-Call (in-call video): no header, the video fills the pane above an 80 high bar of 48 buttons 14 apart; the own picture is
 *    160 x 210, 16 from the right and bottom edges, radius 14, a 2 px border; the timer pill is 24 high, 14 from the top left.
 *  - Chat-Call-DM-Ended: a 72 square tick, 20 to the title (24), 14 to the length (34, line 49.3), 14 to the sentence, 28 to a row of
 *    48 high pills 10 apart. Chat-Call-DM-Declined: the halo avatar and title as in Outgoing, 28 from the sentence to the pills.
 *  - Chat-Call-Bar-M (the connection bar): radius 12, padding 8 10, 32 high buttons (the end button 44 wide).
 * Left out on purpose (no model or API behind them): the quality question after a call, screen share, group calls and rooms, the
 * "Nhắc tôi sau" chip, the call menu of Chat-Call-DM-Start (the header already has a voice and a video button). The bar sits in the
 * pane's flow as one row, because the board's picture-in-picture would cover the composer (the owner's rule, chat-calls-layout spec).
 * Every frame was reviewed by eye; set CHAT_CALL_SHOTS to a directory to write the screenshots.
 */
const SHOTS = process.env.CHAT_CALL_SHOTS;
const persona = process.env.VITE_MOCK_PERSONA ?? "owner";
test.skip(persona !== "owner", "Needs the default owner world");

const SIZES = [
  { width: 1440, height: 900 },
  { width: 1024, height: 768 },
  { width: 390, height: 844 },
] as const;

type Box = { x: number; y: number; w: number; h: number };
const STAGE = "[data-testid=chat-call-stage]";

async function box(loc: Locator): Promise<Box> {
  const b = await loc.first().boundingBox();
  if (!b) throw new Error(`no box for ${loc}`);
  return { x: b.x, y: b.y, w: b.width, h: b.height };
}
const near = (actual: number, expected: number, what: string, tol = 1) =>
  expect(
    Math.abs(actual - expected),
    `${what}: ${actual.toFixed(1)} vs ${expected}`,
  ).toBeLessThanOrEqual(tol);
const css = (loc: Locator, prop: string) =>
  loc
    .first()
    .evaluate((el, p) => getComputedStyle(el).getPropertyValue(p), prop);

async function settle(page: Page, name: string) {
  await page.waitForTimeout(500);
  if (SHOTS) await page.screenshot({ path: `${SHOTS}/${name}.png` });
}

async function axe(page: Page, include: string, what: string) {
  const result = await new AxeBuilder({ page }).include(include).analyze();
  expect(
    result.violations.map(
      (v) =>
        `${what}: ${v.id} ${v.nodes.map((n) => `${n.target} ${n.any[0]?.message ?? ""}`).join(" | ")}`,
    ),
  ).toEqual([]);
}

async function noSideScroll(page: Page) {
  expect(
    await page.evaluate(
      () => document.documentElement.scrollWidth - window.innerWidth,
    ),
  ).toBeLessThanOrEqual(0);
}

/** The centre column: avatar, name, line and chip against the 120 / 34 / 10 / 16 / 28 facts, centred between header and bar. */
async function expectCentreColumn(page: Page, withChip: boolean) {
  const stage = await box(page.locator(STAGE));
  const header = await box(page.locator(`${STAGE} > header`));
  near(header.h, 64, "header height");
  const avatar = await box(
    page.locator(`${STAGE} [data-testid=chat-call-halo] [data-slot=avatar]`),
  );
  near(avatar.w, 120, "avatar width");
  near(avatar.h, 120, "avatar height");
  near(avatar.x + avatar.w / 2, stage.x + stage.w / 2, "avatar centred");
  const h3 = await box(page.locator(`${STAGE} h3`));
  near(h3.h / Math.max(1, Math.round(h3.h / 34.8)), 34.8, "title line");
  near(h3.y - (avatar.y + avatar.h), 44, "avatar to title");
  const line = await box(page.locator(`${STAGE} h3 + p`));
  near(line.y - (h3.y + h3.h), 10, "title to line");
  near(line.h, 20.3, "sentence line");
  if (withChip) {
    const chip = await box(page.getByTestId("chat-call-kind"));
    near(chip.h, 28, "chip height");
    near(chip.y - (line.y + line.h), 16, "line to chip");
    return { avatar, bottom: chip.y + chip.h, stage, header };
  }
  return { avatar, bottom: line.y + line.h, stage, header };
}

async function expectControls(
  page: Page,
  facts: {
    bar: number;
    pitchOk: (pitch: number) => boolean;
    labelled: boolean;
  },
) {
  const stage = await box(page.locator(STAGE));
  const bar = await box(page.getByTestId("chat-call-controls"));
  near(bar.h, facts.bar, "control bar height");
  near(bar.y + bar.h, stage.y + stage.h, "control bar sits at the bottom");
  const buttons = page.getByTestId("chat-call-controls").getByRole("button");
  const count = await buttons.count();
  const centres: number[] = [];
  for (let i = 0; i < count; i++) {
    const b = await box(buttons.nth(i));
    near(b.w, 48, `button ${i} width`);
    near(b.h, 48, `button ${i} height`);
    centres.push(b.x + b.w / 2);
  }
  expect(facts.pitchOk(centres[1]! - centres[0]!)).toBe(true);
  if (facts.labelled) {
    const first = await box(buttons.first());
    const label = await box(
      page.getByTestId("chat-call-controls").locator("span").first(),
    );
    near(label.y - (first.y + first.h), 6, "label under the button");
  }
}

for (const mode of ["light", "dark"] as const) {
  for (const size of SIZES) {
    const tag = `${size.width}-${mode}-vi`;
    test.describe(`${size.width} ${mode}`, () => {
      test.use({ viewport: { width: size.width, height: size.height } });

      test("ringing out: the call view fills the pane, centred, with the labelled control bar", async ({
        page,
      }) => {
        await openDm(page, mode);
        await startCall(page, "video");
        await expect(page.locator(STAGE)).toHaveAttribute(
          "data-phase",
          "outgoing-ringing",
        );
        await settle(page, `ringing-${tag}`);
        const column = await expectCentreColumn(page, true);
        const bar = await box(page.getByTestId("chat-call-controls"));
        near(
          (column.avatar.y + column.bottom) / 2,
          (column.header.y + column.header.h + bar.y) / 2,
          "content centred between header and bar",
        );
        await expectControls(page, {
          bar: 104,
          labelled: true,
          pitchOk: (pitch) => Math.abs(pitch - 58) <= 1,
        });
        // The call view is dark in both themes.
        expect(await css(page.locator(STAGE), "background-color")).not.toBe(
          "rgba(0, 0, 0, 0)",
        );
        await noSideScroll(page);
        await axe(page, STAGE, "ringing out");
      });

      test("an incoming video ring: toast on a desktop and a tablet, a full-screen sheet on a phone", async ({
        page,
      }) => {
        const { space, peer } = await openDm(page, mode);
        await sendMessage(page, "Mình đây");
        await hook(page, "ring", space, peer, "video");
        const ring = page.getByTestId("chat-call-incoming");
        await expect(ring).toBeVisible();
        await settle(page, `incoming-${tag}`);
        const r = await box(ring);
        const buttons = ring.getByRole("button");
        expect(await buttons.count()).toBe(3);
        if (size.width === 390) {
          near(r.w, 390, "sheet width");
          near(r.h, 844, "sheet height");
          const avatar = await box(ring.locator("[data-slot=avatar]"));
          near(avatar.w, 120, "avatar");
          near(avatar.x + avatar.w / 2, 195, "avatar centred");
          const centres: number[] = [];
          for (let i = 0; i < 3; i++) {
            const b = await box(buttons.nth(i));
            near(b.w, 64, "round button");
            near(b.h, 64, "round button");
            centres.push(b.x + b.w / 2);
          }
          near(centres[1]! - centres[0]!, 100, "button pitch");
          near(centres[2]! - centres[1]!, 100, "button pitch");
        } else {
          const pane = await box(
            page.locator("[data-testid=chat-call-toasts]").locator(".."),
          );
          const wide = size.width >= 1280;
          near(r.w, wide ? 400 : 440, "toast width");
          near(r.h, 132, "toast height");
          if (wide) {
            near(pane.x + pane.w - (r.x + r.w), 20, "toast right gap");
            near(r.y - pane.y, 20, "toast top gap");
          } else {
            near(r.x - pane.x, 10, "toast left gap");
            near(r.y - pane.y, 16, "toast top gap");
          }
          expect(await css(ring, "border-radius")).toBe("16px");
          const avatar = await box(ring.locator("[data-slot=avatar]"));
          near(avatar.w, 48, "avatar");
          near(avatar.x - r.x, 15, "avatar inset");
          near(avatar.y - r.y, 15, "avatar inset");
          const each = wide ? 118 : 131.3;
          for (let i = 0; i < 3; i++) {
            const b = await box(buttons.nth(i));
            near(b.h, 40, "toast button height");
            near(b.w, each, "toast button width");
            near(b.y - r.y, 77, "toast button top");
          }
          const second = await box(buttons.nth(1));
          const first = await box(buttons.first());
          near(second.x - (first.x + first.w), 8, "button gap");
        }
        await noSideScroll(page);
        await axe(page, "[data-testid=chat-call-incoming]", "incoming ring");
      });

      test("in the call (video): the picture fills the pane above an 80 high bar, own picture 160 x 210", async ({
        page,
      }) => {
        const { space, peer } = await openDm(page, mode);
        await startCall(page, "video");
        const id = (await hook(page, "live", space)) as string;
        await hook(page, "act", id, peer, "answer");
        await expect(page.locator(STAGE)).toHaveAttribute(
          "data-phase",
          "connected",
        );
        await expect(page.locator(STAGE)).toHaveAttribute("data-video", "on");
        await settle(page, `connected-video-${tag}`);
        const stage = await box(page.locator(STAGE));
        await expect(page.locator(`${STAGE} > header`)).toHaveCount(0);
        const video = await box(page.getByTestId("chat-call-video"));
        const bar = await box(page.getByTestId("chat-call-controls"));
        near(bar.h, 80, "bar height");
        near(video.y + video.h, bar.y, "picture above the bar");
        near(video.h + bar.h, stage.h, "picture and bar fill the pane");
        const self = await box(page.getByTestId("chat-call-self"));
        near(self.w, 160, "own picture width");
        near(self.h, 210, "own picture height");
        near(video.x + video.w - (self.x + self.w), 16, "own picture right");
        near(video.y + video.h - (self.y + self.h), 16, "own picture bottom");
        expect(
          await css(page.getByTestId("chat-call-self"), "border-radius"),
        ).toBe("14px");
        const pill = await box(page.locator(`${STAGE} [role=timer]`));
        near(pill.h, 24, "timer pill height");
        near(pill.x - video.x, 14, "timer pill left");
        near(pill.y - video.y, 14, "timer pill top");
        await expectControls(page, {
          bar: 80,
          labelled: false,
          pitchOk: (pitch) => Math.abs(pitch - 62) <= 1,
        });
        await noSideScroll(page);
        await axe(page, STAGE, "in the call");
      });

      test("ended: the tick, the length and the pills; declined: the halo and three pills", async ({
        page,
      }) => {
        const { space, peer } = await openDm(page, mode);
        await startCall(page, "video");
        const id = (await hook(page, "live", space)) as string;
        await hook(page, "act", id, peer, "answer");
        await expect(page.locator(STAGE)).toHaveAttribute(
          "data-phase",
          "connected",
        );
        await hook(page, "act", id, peer, "end");
        await expect(page.locator(STAGE)).toHaveAttribute(
          "data-phase",
          "ended-completed",
        );
        await settle(page, `ended-${tag}`);
        const icon = await box(page.getByTestId("chat-call-ended-icon"));
        near(icon.w, 72, "tick width");
        near(icon.h, 72, "tick height");
        const h3 = await box(page.locator(`${STAGE} h3`));
        near(h3.y - (icon.y + icon.h), 20, "tick to title");
        const length = await box(page.locator(`${STAGE} h3 + p`));
        near(length.y - (h3.y + h3.h), 14, "title to length");
        near(length.h, 49.3, "length line");
        const sentence = await box(page.locator(`${STAGE} h3 + p + p`));
        near(sentence.y - (length.y + length.h), 14, "length to sentence");
        const actions = page.getByTestId("chat-call-actions");
        near(
          (await box(actions)).y - (sentence.y + sentence.h),
          28,
          "sentence to pills",
        );
        const pills = actions.getByRole("button");
        expect(await pills.count()).toBe(2);
        const a = await box(pills.nth(0));
        const b = await box(pills.nth(1));
        near(a.h, 48, "pill height");
        near(b.x - (a.x + a.w), 10, "pill gap");
        await noSideScroll(page);
        await axe(page, STAGE, "ended");
        await pills.nth(1).click();

        await startCall(page, "voice");
        const next = (await hook(page, "live", space)) as string;
        await hook(page, "act", next, peer, "decline");
        await expect(page.locator(STAGE)).toHaveAttribute(
          "data-phase",
          "ended-declined",
        );
        await settle(page, `declined-${tag}`);
        const column = await expectCentreColumn(page, false);
        const gap = (await box(page.getByTestId("chat-call-actions"))).y;
        near(gap - column.bottom, 28, "sentence to pills");
        const three = page.getByTestId("chat-call-actions").getByRole("button");
        expect(await three.count()).toBe(3);
        for (let i = 0; i < 3; i++)
          near((await box(three.nth(i))).h, 48, "pill");
        await axe(page, STAGE, "declined");
      });

      test("back in the conversation: the bar is one row in the pane's flow", async ({
        page,
      }) => {
        const { space, peer } = await openDm(page, mode);
        await sendMessage(page, "Đang gọi nhé");
        await startCall(page, "voice");
        const id = (await hook(page, "live", space)) as string;
        await hook(page, "act", id, peer, "answer");
        await expect(page.locator(STAGE)).toHaveAttribute(
          "data-phase",
          "connected",
        );
        await page
          .getByRole("button", { name: "Quay lại cuộc trò chuyện" })
          .first()
          .click();
        const strip = page.getByTestId("chat-call-strip");
        await expect(strip).toBeVisible();
        await settle(page, `strip-${tag}`);
        const s = await box(strip);
        const pane = await box(strip.locator(".."));
        near(s.x - pane.x, 8, "bar left margin");
        near(pane.x + pane.w - (s.x + s.w), 8, "bar right margin");
        near(s.h, 50, "bar height");
        expect(await css(strip, "border-radius")).toBe("12px");
        const buttons = strip.getByRole("button");
        const count = await buttons.count();
        for (let i = 0; i < count; i++) {
          const b = await box(buttons.nth(i));
          near(b.h, 32, "bar button height");
          near(b.w, i === count - 1 ? 44 : 32, "bar button width");
        }
        await noSideScroll(page);
        await axe(page, "[data-testid=chat-call-strip]", "bar");
      });
    });
  }
}

import AxeBuilder from "@axe-core/playwright";
import { expect, test, type Page } from "@playwright/test";
import { vi as copy } from "../../packages/features/chat/src/web/components/chat.copy";

/**
 * One-to-one calls in Chat on the mock backend: start, ring, answer, mute, end, decline, miss, an incoming ring answered and
 * declined, and the call line in the timeline, in Vietnamese. The mock store lives in the page, so the other person is the dev hook
 * `__notrelixMockCall` (a second browser context would not share the store). Nothing opens a socket or a microphone.
 * Set CHAT_CALL_SHOTS to a directory to also write the review screenshots.
 */
const WORKSPACE = "mock-workspace-primary";
const SHOTS = process.env.CHAT_CALL_SHOTS;
const persona = process.env.VITE_MOCK_PERSONA ?? "owner";
test.skip(persona !== "owner", "Needs the default owner world");

type Hook = {
  ring: (
    space: string,
    person: string,
    kind: "voice" | "video",
  ) => string | null;
  act: (call: string, person: string, action: string) => boolean;
  miss: (call: string) => boolean;
  live: (space: string) => string | null;
  peer: (space: string) => string | null;
};

const card = (page: Page) => page.getByTestId("chat-call-card");

async function openDm(page: Page, mode: "light" | "dark" = "light") {
  await page.addInitScript((m) => {
    try {
      window.localStorage.setItem("notrelix-locale", "vi");
      window.localStorage.setItem("notrelix-theme-mode", m);
    } catch {
      // Private mode: the defaults apply.
    }
  }, mode);
  await page.goto(`/workspaces/${WORKSPACE}/chat`);
  await expect(page.getByRole("log")).toBeVisible();
  await page
    .getByRole("button", { name: /^(New message|Tin nhắn mới)$/ })
    .first()
    .click();
  const dialog = page.getByRole("dialog");
  await dialog.getByRole("checkbox").nth(0).click();
  await dialog
    .getByRole("button", {
      name: /^(Start conversation|Bắt đầu cuộc trò chuyện)$/,
    })
    .click();
  await expect(page).toHaveURL(/\/chat\/dm\/[^/]+$/);
  const space = page.url().split("/").pop()!;
  const peer = await page.evaluate(
    (id) =>
      (
        globalThis as unknown as { __notrelixMockCall: Hook }
      ).__notrelixMockCall.peer(id),
    space,
  );
  expect(peer).not.toBeNull();
  return { space, peer: peer! };
}

const hook = <A extends unknown[]>(page: Page, name: keyof Hook, ...args: A) =>
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
const startCall = (page: Page, _space: string, kind: "voice" | "video") =>
  page
    .locator("main header")
    .getByRole("button", {
      name: kind === "voice" ? copy.panel.voiceCall : copy.panel.videoCall,
    })
    .click();

async function expectNoViolations(page: Page, what: string) {
  const result = await new AxeBuilder({ page })
    .include("[data-testid=chat-call-dock]")
    .analyze();
  expect(result.violations.map((v) => `${what}: ${v.id}`)).toEqual([]);
}

async function shot(page: Page, name: string) {
  if (SHOTS) await page.screenshot({ path: `${SHOTS}/${name}.png` });
}

test("rings out, connects when the other person answers, mutes and ends with the call line in the timeline", async ({
  page,
}) => {
  await page.setViewportSize({ width: 1440, height: 900 });
  const { space, peer } = await openDm(page);
  await startCall(page, space, "voice");
  await expect(card(page)).toHaveAttribute("data-phase", "outgoing-ringing");
  await expect(
    card(page).getByRole("button", { name: copy.calls.cancel }),
  ).toBeVisible();
  await expectNoViolations(page, "ringing out");
  await shot(page, "voice-ringing-1440-light-vi");

  const id = (await hook(page, "live", space)) as string;
  await hook(page, "act", id, peer, "answer");
  await expect(card(page)).toHaveAttribute("data-phase", "connected");
  await expect(card(page).getByRole("timer")).toHaveText(/^\d+:\d\d$/);
  const mute = card(page).getByRole("button", { name: copy.calls.mute });
  await mute.click();
  await expect(
    card(page).getByRole("button", { name: copy.calls.unmute }),
  ).toHaveAttribute("aria-pressed", "true");
  await expectNoViolations(page, "connected");
  await shot(page, "voice-connected-1440-light-vi");

  await card(page).getByRole("button", { name: copy.calls.hangUp }).click();
  await expect(card(page)).toContainText(copy.calls.endedCompleted);
  await expect(page.getByTestId("chat-system-message").last()).toContainText(
    /cuộc gọi thoại đã kết thúc, kéo dài \d+s/,
  );
  await expectNoViolations(page, "ended");
  await shot(page, "voice-ended-1440-light-vi");
  await card(page).getByRole("button", { name: copy.calls.close }).click();
  await expect(card(page)).toHaveCount(0);
});

test("a video call shows the stage and the camera control; the other person declines and misses are named", async ({
  page,
}) => {
  await page.setViewportSize({ width: 1440, height: 900 });
  const { space, peer } = await openDm(page);
  await startCall(page, space, "video");
  await expect(card(page)).toHaveAttribute("data-phase", "outgoing-ringing");
  const id = (await hook(page, "live", space)) as string;
  await hook(page, "act", id, peer, "answer");
  await expect(card(page)).toHaveAttribute("data-phase", "connected");
  await expect(page.getByTestId("chat-call-remote-tile")).toBeVisible();
  await expect(
    card(page).getByRole("button", { name: copy.calls.cameraOff }),
  ).toBeVisible();
  await expectNoViolations(page, "video connected");
  await shot(page, "video-connected-1440-light-vi");
  await hook(page, "act", id, peer, "end");
  await expect(card(page)).toContainText(copy.calls.endedCompleted);
  await card(page).getByRole("button", { name: copy.calls.close }).click();

  await startCall(page, space, "voice");
  await expect(card(page)).toHaveAttribute("data-phase", "outgoing-ringing");
  await hook(
    page,
    "act",
    (await hook(page, "live", space)) as string,
    peer,
    "decline",
  );
  await expect(card(page)).toContainText(/đã từ chối cuộc gọi/);
  await shot(page, "voice-declined-1440-light-vi");
  await card(page).getByRole("button", { name: copy.calls.close }).click();

  await startCall(page, space, "voice");
  await expect(card(page)).toHaveAttribute("data-phase", "outgoing-ringing");
  await hook(page, "miss", (await hook(page, "live", space)) as string);
  await expect(card(page)).toContainText(/không trả lời/);
  await expect(page.getByTestId("chat-system-message").last()).toContainText(
    /cuộc gọi thoại nhỡ/,
  );
  await card(page).getByRole("button", { name: copy.calls.close }).click();

  await startCall(page, space, "voice");
  await card(page).getByRole("button", { name: copy.calls.cancel }).click();
  await expect(card(page)).toContainText(copy.calls.endedCancelled);
});

test("an incoming ring is answered, and another is declined", async ({
  page,
}) => {
  await page.setViewportSize({ width: 1440, height: 900 });
  const { space, peer } = await openDm(page);
  await hook(page, "ring", space, peer, "voice");
  const ring = page.getByTestId("chat-call-incoming");
  await expect(ring).toBeVisible();
  await expectNoViolations(page, "incoming ring");
  await shot(page, "incoming-ring-1440-light-vi");
  await ring.getByRole("button", { name: copy.calls.answer }).click();
  await expect(card(page)).toHaveAttribute("data-phase", "connected");
  await expect(ring).toHaveCount(0);
  await card(page).getByRole("button", { name: copy.calls.hangUp }).click();
  await card(page).getByRole("button", { name: copy.calls.close }).click();

  await hook(page, "ring", space, peer, "video");
  await expect(ring).toBeVisible();
  await ring.getByRole("button", { name: copy.calls.decline }).click();
  await expect(ring).toHaveCount(0);
  await expect(page.getByTestId("chat-system-message").last()).toContainText(
    /cuộc gọi video bị từ chối/,
  );

  // The caller cancels a ring nobody answered: it leaves the screen.
  await hook(page, "ring", space, peer, "voice");
  await expect(ring).toBeVisible();
  await hook(
    page,
    "act",
    (await hook(page, "live", space)) as string,
    peer,
    "cancel",
  );
  await expect(ring).toHaveCount(0);
});

for (const [width, height] of [
  [1440, 900],
  [1024, 768],
  [390, 844],
] as const) {
  for (const mode of ["light", "dark"] as const) {
    test(`review screenshots at ${width} in ${mode}: no horizontal scroll, the controls fit`, async ({
      page,
    }) => {
      test.skip(!SHOTS, "Set CHAT_CALL_SHOTS to write the review screenshots");
      await page.setViewportSize({ width, height });
      const { space, peer } = await openDm(page, mode);
      const tag = `${width}-${mode}-vi`;
      await startCall(page, space, "video");
      await expect(card(page)).toHaveAttribute(
        "data-phase",
        "outgoing-ringing",
      );
      await shot(page, `review-ringing-${tag}`);
      const id = (await hook(page, "live", space)) as string;
      await hook(page, "act", id, peer, "answer");
      await expect(card(page)).toHaveAttribute("data-phase", "connected");
      await shot(page, `review-connected-video-${tag}`);
      expect(
        await page.evaluate(
          () => document.documentElement.scrollWidth <= window.innerWidth,
        ),
      ).toBe(true);
      await hook(page, "act", id, peer, "end");
      await expect(card(page)).toContainText(copy.calls.endedCompleted);
      await shot(page, `review-ended-${tag}`);
      await card(page).getByRole("button", { name: copy.calls.close }).click();
      await hook(page, "ring", space, peer, "voice");
      await expect(page.getByTestId("chat-call-incoming")).toBeVisible();
      await shot(page, `review-incoming-${tag}`);
      expect(
        await page.evaluate(
          () => document.documentElement.scrollWidth <= window.innerWidth,
        ),
      ).toBe(true);
    });
  }
}

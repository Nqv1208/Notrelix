import AxeBuilder from "@axe-core/playwright";
import { expect, test, type Browser, type Page } from "@playwright/test";
import {
  apiRead,
  apiWrite,
  findWorkspace,
  realUsers,
  signIn,
} from "./support/real-session";

// One-to-one calls on the real stack (the backend with Chat:Calls:Media:Provider=LiveKit and the compose `calls` profile, so a real
// SFU): A calls B in a direct conversation, B answers, both see the call connected and B receives A's audio, A hangs up and both see
// it ended with the call line in the timeline; then B declines a second call and A sees it declined. Two Chrome browsers with fake
// camera and microphone. Start it with REAL_E2E_CALLS=1 CHAT_CALLS_MEDIA_PROVIDER=LiveKit LIVEKIT_CLIENT_URL=ws://127.0.0.1:7880.
test.use({
  viewport: { width: 1440, height: 900 },
  actionTimeout: 30_000,
  launchOptions: {
    args: [
      "--use-fake-device-for-media-stream",
      "--use-fake-ui-for-media-stream",
    ],
  },
});
test.setTimeout(300_000);

const english = () => {
  try {
    window.localStorage.setItem("notrelix-locale", "en");
  } catch {
    // Private mode: the default language applies.
  }
};

async function person(browser: Browser, user: Parameters<typeof signIn>[1]) {
  const context = await browser.newContext({
    viewport: { width: 1440, height: 900 },
    permissions: ["microphone", "camera"],
  });
  const page = await context.newPage();
  await page.addInitScript(english);
  await signIn(page, user);
  return { context, page };
}

const card = (page: Page) => page.getByTestId("chat-call-card");

const startCall = (page: Page, spaceId: string, kind: "voice" | "video") =>
  page.evaluate(
    ([s, k]) =>
      window.dispatchEvent(
        new CustomEvent("notrelix:chat-start-call", {
          detail: { spaceId: s, kind: k },
        }),
      ),
    [spaceId, kind] as const,
  );

async function serious(page: Page) {
  const result = await new AxeBuilder({ page })
    .include("[data-testid=chat-call-dock]")
    .withTags(["wcag2a", "wcag2aa"])
    .analyze();
  return result.violations.filter(
    (v) => v.impact === "serious" || v.impact === "critical",
  );
}

async function pair(browser: Browser) {
  const a = await person(browser, realUsers.workspaceOwner);
  const b = await person(browser, realUsers.workspaceAdmin);
  const ws = (await findWorkspace(a.page, "Workspace 3")).id;
  const base = `/workspaces/${ws}`;
  const bId = (await apiRead<{ userId: string }>(b.page, `${base}/members/me`))
    .userId;
  const started = await apiWrite<{ conversation: { id: string } }>(
    a.page,
    "POST",
    `${base}/chat/conversations`,
    { kind: "direct", userId: bId },
  );
  expect(started.status).toBeLessThan(300);
  const dm = started.data!.conversation.id;
  // Both read the conversation, so the channel stream carries every call frame to both pages.
  for (const { page } of [a, b]) {
    await page.goto(`${base}/chat/dm/${dm}`);
    await expect(page.getByRole("log")).toBeVisible();
  }
  const done = async () => {
    await a.context.close();
    await b.context.close();
  };
  return { a, b, dm, done };
}

const liveAudioIn = (page: Page) =>
  page.evaluate(() =>
    Array.from(document.querySelectorAll("audio")).some((el) =>
      ((el.srcObject as MediaStream | null)?.getAudioTracks() ?? []).some(
        (track) => track.readyState === "live",
      ),
    ),
  );

const liveVideoIn = (page: Page) =>
  page.evaluate(() =>
    Array.from(document.querySelectorAll("video")).some((el) =>
      ((el.srcObject as MediaStream | null)?.getVideoTracks() ?? []).some(
        (track) => track.readyState === "live",
      ),
    ),
  );

test("A calls B, B answers and hears A, A hangs up; a video call is declined", async ({
  browser,
}) => {
  const { a, b, dm, done } = await pair(browser);
  try {
    await startCall(a.page, dm, "voice");
    await expect(card(a.page)).toHaveAttribute(
      "data-phase",
      "outgoing-ringing",
      { timeout: 30_000 },
    );
    const ring = b.page.getByTestId("chat-call-incoming");
    await expect(ring).toBeVisible({ timeout: 30_000 });
    await ring.getByRole("button", { name: "Answer" }).click();

    // Connected on both ends through the SFU.
    await expect(card(a.page)).toHaveAttribute("data-phase", "connected", {
      timeout: 60_000,
    });
    await expect(card(b.page)).toHaveAttribute("data-phase", "connected", {
      timeout: 60_000,
    });
    await expect(card(a.page).getByRole("timer")).toHaveText(/^\d+:\d\d$/);
    expect(await serious(a.page)).toEqual([]);
    expect(await serious(b.page)).toEqual([]);

    // Hear: each end receives a live audio track from the other through the SFU.
    await expect
      .poll(() => liveAudioIn(b.page), { timeout: 30_000 })
      .toBe(true);
    await expect
      .poll(() => liveAudioIn(a.page), { timeout: 30_000 })
      .toBe(true);

    // End: A hangs up, both see it ended, the timeline says how it went.
    await card(a.page).getByRole("button", { name: "Hang up" }).click();
    await expect(card(a.page)).toContainText("Call ended");
    await expect(card(b.page)).toContainText("Call ended", { timeout: 30_000 });
    await expect(
      a.page.getByTestId("chat-system-message").last(),
    ).toContainText(/voice call ended, lasted \d+s/);
    await card(a.page).getByRole("button", { name: "Close" }).click();
    await card(b.page).getByRole("button", { name: "Close" }).click();

    // Rejection: B declines a second call and A sees it declined.
    await startCall(a.page, dm, "video");
    await expect(ring).toBeVisible({ timeout: 30_000 });
    await ring.getByRole("button", { name: "Decline" }).click();
    await expect(card(a.page)).toContainText(/declined the call/, {
      timeout: 30_000,
    });
    await expect(
      a.page.getByTestId("chat-system-message").last(),
    ).toContainText(/video call declined/);
  } finally {
    await done();
  }
});

test("a video call carries video both ways, the controls reach the other person, and a call nobody answers is missed", async ({
  browser,
}) => {
  const { a, b, dm, done } = await pair(browser);
  try {
    await startCall(a.page, dm, "video");
    const ring = b.page.getByTestId("chat-call-incoming");
    await expect(ring).toBeVisible({ timeout: 30_000 });
    await ring.getByRole("button", { name: "Answer" }).click();
    await expect(card(a.page)).toHaveAttribute("data-phase", "connected", {
      timeout: 60_000,
    });
    await expect(card(b.page)).toHaveAttribute("data-phase", "connected", {
      timeout: 60_000,
    });
    await expect
      .poll(() => liveVideoIn(b.page), { timeout: 30_000 })
      .toBe(true);
    await expect
      .poll(() => liveVideoIn(a.page), { timeout: 30_000 })
      .toBe(true);
    expect(await serious(a.page)).toEqual([]);

    // A turns the camera off: B shows the tile of a person without video; A turns it on again and the tile goes.
    await card(a.page).getByRole("button", { name: "Turn camera off" }).click();
    await expect(b.page.getByTestId("chat-call-remote-tile")).toBeVisible({
      timeout: 30_000,
    });
    await card(a.page).getByRole("button", { name: "Turn camera on" }).click();
    await expect(b.page.getByTestId("chat-call-remote-tile")).toHaveCount(0, {
      timeout: 30_000,
    });
    await card(a.page).getByRole("button", { name: "Mute microphone" }).click();
    await expect(
      card(a.page).getByRole("button", { name: "Unmute microphone" }),
    ).toHaveAttribute("aria-pressed", "true");

    // B ends the call for both.
    await card(b.page).getByRole("button", { name: "Hang up" }).click();
    await expect(card(b.page)).toContainText("Call ended");
    await expect(card(a.page)).toContainText("Call ended", { timeout: 30_000 });
    await card(a.page).getByRole("button", { name: "Close" }).click();
    await card(b.page).getByRole("button", { name: "Close" }).click();

    // Nobody answers: the ring runs out and A sees the call missed; B's ring leaves the screen.
    await startCall(a.page, dm, "voice");
    await expect(ring).toBeVisible({ timeout: 30_000 });
    await expect(card(a.page)).toContainText(/did not answer/, {
      timeout: 150_000,
    });
    await expect(ring).toHaveCount(0, { timeout: 30_000 });
    await expect(
      a.page.getByTestId("chat-system-message").last(),
    ).toContainText(/missed voice call/);
  } finally {
    await done();
  }
});

import AxeBuilder from "@axe-core/playwright";
import {
  expect,
  test,
  type Browser,
  type Locator,
  type Page,
} from "@playwright/test";
import {
  apiRead,
  apiWrite,
  findWorkspace,
  realUsers,
  signIn,
} from "./support/real-session";

// Chat read state on the real stack (UC-CHT-D01 to D06): unread and mention badges per space and in total,
// the read boundary and its two devices, the New messages divider, a muted space and the notification level.
// Set CHAT_STATE_SHOTS to a directory to also write one screenshot per reviewed state (no baseline is kept).
test.use({ viewport: { width: 1440, height: 900 } });
test.setTimeout(240_000);

// The send limit is 30 a minute per person, so the conversation holds 15 unread here; 99+ is proved by the component test.
const COUNT = 15;
const SHOTS = process.env.CHAT_STATE_SHOTS;

interface Channel {
  id: string;
}
interface SpaceUnread {
  spaceId: string;
  readSequence: number;
  lastSequence: number;
  unreadCount: number;
  firstUnreadSequence: number | null;
  mentionCount: number;
  notificationLevel: string;
  muted: boolean;
}
interface UnreadSummary {
  spaces: SpaceUnread[];
  totalUnread: number;
  totalMentions: number;
}
interface Boundary {
  readSequence: number;
}
interface MemberRow {
  userId: string;
  name: string;
}
interface InboxEntry {
  excerpt: string | null;
}
interface Conversation {
  id: string;
}

const english = () => {
  try {
    window.localStorage.setItem("notrelix-locale", "en");
  } catch {
    // Private mode: the default language applies.
  }
};

const log = (page: Page) => page.getByRole("log");
const channelButton = (page: Page, channel: string) =>
  page
    .getByRole("navigation", { name: "Channels" })
    .getByRole("button", { name: new RegExp(`: ${channel}(,|$)`) });

async function person(browser: Browser, user: Parameters<typeof signIn>[1]) {
  const context = await browser.newContext({
    viewport: { width: 1440, height: 900 },
  });
  const page = await context.newPage();
  await page.addInitScript(english);
  await signIn(page, user);
  return { context, page };
}

test("unread and mention badges, the read boundary on two devices, the first unread, a mute and the notification level work through the real stack", async ({
  page: owner,
  browser,
}) => {
  const unique = Date.now().toString(36);
  const names = {
    home: `rs-home-${unique}`,
    x: `rs-x-${unique}`,
    y: `rs-y-${unique}`,
    z: `rs-z-${unique}`,
  };
  await owner.addInitScript(english);
  await signIn(owner, realUsers.workspaceOwner);
  const ws = (await findWorkspace(owner, "Workspace 3")).id;
  const base = `/workspaces/${ws}`;
  const chat = `${base}/chat`;

  const reader = await person(browser, realUsers.admin);
  const readerId = (
    await apiRead<{ userId: string }>(reader.page, `${base}/members/me`)
  ).userId;
  const roster = await apiRead<{ items: MemberRow[] }>(
    owner,
    `${base}/members?limit=200`,
  );
  const readerName = roster.items.find((m) => m.userId === readerId)!.name;

  const ids: Record<keyof typeof names, string> = {
    home: "",
    x: "",
    y: "",
    z: "",
  };
  for (const key of Object.keys(names) as (keyof typeof names)[]) {
    const made = await apiWrite<Channel>(owner, "POST", `${chat}/channels`, {
      name: names[key],
      isPrivate: false,
    });
    expect(made.status).toBeLessThan(300);
    ids[key] = made.data!.id;
    const added = await apiWrite(
      owner,
      "POST",
      `${chat}/channels/${ids[key]}/members`,
      { userIds: [readerId] },
    );
    expect(added.status).toBeLessThan(300);
  }

  const send = async (
    space: string,
    content: string,
    mentions: unknown[] = [],
  ) => {
    const r = await apiWrite(
      owner,
      "POST",
      `${chat}/spaces/${space}/messages`,
      {
        content,
        clientMessageId: crypto.randomUUID(),
        mentions,
      },
    );
    expect(r.status).toBeLessThan(300);
  };
  const summaryOf = (p: Page) => apiRead<UnreadSummary>(p, `${chat}/me/unread`);
  const spaceOf = async (p: Page, id: string) =>
    (await summaryOf(p)).spaces.find((s) => s.spaceId === id)!;
  const inboxOf = async (p: Page) =>
    (await apiRead<{ items: InboxEntry[] }>(p, `${base}/inbox?limit=50`)).items;
  const mentionOfReader = (text: string) => [
    {
      kind: "user",
      start: text.indexOf("@"),
      length: readerName.length + 1,
      userId: readerId,
      spaceId: null,
    },
  ];

  // Baseline: the join notices are read, so every count below is only what the owner sends next.
  for (const id of Object.values(ids)) {
    const s = await spaceOf(reader.page, id);
    const r = await apiWrite<Boundary>(
      reader.page,
      "PUT",
      `${chat}/spaces/${id}/read`,
      { upToSequence: s.lastSequence },
    );
    expect(r.status).toBeLessThan(300);
  }
  await reader.page.goto(`${chat}/${ids.home}`);
  await expect(
    reader.page.getByRole("combobox", { name: `Message #${names.home}` }),
  ).toBeVisible({ timeout: 20_000 });

  // D01: three messages make the channel read as unread (bold and a dot, no number) and the totals add up.
  const typed = [`first ${unique}`, `second ${unique}`, `third ${unique}`];
  for (const text of typed) await send(ids.x, text);
  const xButton = channelButton(reader.page, names.x);
  await expect(xButton).toHaveAccessibleName(/unread/i, { timeout: 20_000 });
  await expect(xButton.getByTestId("chat-unread-dot")).toBeVisible();
  await expect(xButton.getByTestId("chat-mention-badge")).toHaveCount(0);
  await expect(xButton).not.toContainText(/\b3\b/);
  const afterThree = await spaceOf(reader.page, ids.x);
  expect(afterThree.unreadCount).toBe(3);
  const unreadSummary = await summaryOf(reader.page);
  expect(unreadSummary.totalUnread).toBe(
    unreadSummary.spaces
      .filter((s) => !s.muted)
      .reduce((sum, s) => sum + s.unreadCount, 0),
  );

  // D04: a mention in channel Y is a badge of its own, apart from the dot.
  const ping = `@${readerName} look ${unique}`;
  await send(ids.y, ping, mentionOfReader(ping));
  const yButton = channelButton(reader.page, names.y);
  await expect(yButton.getByTestId("chat-mention-badge")).toHaveText("@1", {
    timeout: 20_000,
  });
  await expect(yButton).toHaveAccessibleName(/1 unread mention/);
  expect((await spaceOf(reader.page, ids.y)).mentionCount).toBe(1);

  // D01: a direct conversation shows the count; the divider still finds the first unread.
  const dm = await apiWrite<{ conversation: Conversation }>(
    owner,
    "POST",
    `${chat}/conversations`,
    { kind: "direct", userId: readerId },
  );
  expect(dm.status).toBeLessThan(300);
  const dmId = dm.data!.conversation.id;
  const dmFirst = `dm 001 ${unique}`;
  await send(dmId, dmFirst);
  for (let n = 2; n <= COUNT; n += 1) {
    await send(dmId, `dm ${String(n).padStart(3, "0")} ${unique}`);
  }
  await expect
    .poll(async () => (await spaceOf(reader.page, dmId)).unreadCount, {
      timeout: 20_000,
    })
    .toBe(COUNT);
  await reader.page.goto(`${chat}/dm`);
  const dmButton = reader.page.getByRole("button", {
    name: new RegExp(`^Direct message: .*, ${COUNT} unread messages$`),
  });
  await expect(dmButton).toBeVisible({ timeout: 20_000 });
  await expect(
    dmButton.getByText(String(COUNT), { exact: true }),
  ).toBeVisible();

  // D03 and D02: opening the conversation puts the divider on the first unread message and then reads it.
  await dmButton.click();
  await expect(reader.page.locator("[data-unread-divider]")).toBeVisible({
    timeout: 20_000,
  });
  await expect(
    reader.page
      .locator("[data-unread-divider]")
      .locator("xpath=ancestor::li[1]/following::*[@data-message-id][1]"),
  ).toContainText(dmFirst);
  await expect
    .poll(async () => (await spaceOf(reader.page, dmId)).unreadCount, {
      timeout: 20_000,
    })
    .toBe(0);

  // D03: channel X opens on the first of its three unread messages, then reads to the end.
  await reader.page.goto(`${chat}/${ids.home}`);
  await expect(xButton).toHaveAccessibleName(/unread/i, { timeout: 20_000 });
  // D02, two devices: the second device holds the home channel open and sees the badge clear.
  const second = await reader.context.newPage();
  await second.addInitScript(english);
  await second.goto(`${chat}/${ids.home}`);
  await expect(channelButton(second, names.x)).toHaveAccessibleName(/unread/i, {
    timeout: 20_000,
  });
  await xButton.click();
  await expect(reader.page.locator("[data-unread-divider]")).toBeVisible({
    timeout: 20_000,
  });
  await expect(
    reader.page
      .locator("[data-unread-divider]")
      .locator("xpath=ancestor::li[1]/following::*[@data-message-id][1]"),
  ).toContainText(typed[0]);
  await expect
    .poll(async () => (await spaceOf(reader.page, ids.x)).unreadCount, {
      timeout: 20_000,
    })
    .toBe(0);
  await expect(channelButton(second, names.x)).not.toHaveAccessibleName(
    /unread/i,
    { timeout: 20_000 },
  );
  await second.close();

  // D02: a lower sequence is a no-op, a private space the caller is not in is the same NotFound as an unknown one.
  const boundary = (await spaceOf(reader.page, ids.x)).readSequence;
  const lower = await apiWrite<Boundary>(
    reader.page,
    "PUT",
    `${chat}/spaces/${ids.x}/read`,
    { upToSequence: 1 },
  );
  expect(lower.status).toBeLessThan(300);
  expect(lower.data!.readSequence).toBe(boundary);
  const secret = await apiWrite<Channel>(owner, "POST", `${chat}/channels`, {
    name: `rs-secret-${unique}`,
    isPrivate: true,
  });
  // The answer names only the id the caller sent, so the two bodies match once that id and the trace are masked.
  const strip = (d: unknown) =>
    JSON.parse(
      JSON.stringify(d ?? {})
        .replace(
          /[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}/g,
          "ID",
        )
        .replace(/"(instance|traceId)":"[^"]*",?/g, ""),
    );
  const hidden = await apiWrite(
    reader.page,
    "PUT",
    `${chat}/spaces/${secret.data!.id}/read`,
    { upToSequence: 1 },
  );
  const unknown = await apiWrite(
    reader.page,
    "PUT",
    `${chat}/spaces/${crypto.randomUUID()}/read`,
    { upToSequence: 1 },
  );
  expect(hidden.status).toBe(404);
  expect(unknown.status).toBe(404);
  expect(strip(hidden.data)).toEqual(strip(unknown.data));

  // D05: Mute in the channel menu. The row is dimmed with the bell-off glyph and reads as muted; a plain message and a
  // mention keep counting for the space but make no dot and no badge, and the totals leave the space out.
  await channelButton(reader.page, names.z).click();
  await reader.page
    .getByRole("button", { name: "Notification settings" })
    .click();
  await reader.page.getByRole("menuitemradio", { name: "Muted" }).click();
  await expect
    .poll(async () => (await spaceOf(reader.page, ids.z)).muted, {
      timeout: 20_000,
    })
    .toBe(true);
  await expect(channelButton(reader.page, names.z)).toHaveAccessibleName(
    /Muted/,
  );
  await channelButton(reader.page, names.home).click();
  const mutedPlain = `muted plain ${unique}`;
  const mutedPing = `@${readerName} muted ${unique}`;
  await send(ids.z, mutedPlain);
  await send(ids.z, mutedPing, mentionOfReader(mutedPing));
  await expect
    .poll(async () => (await spaceOf(reader.page, ids.z)).mentionCount, {
      timeout: 20_000,
    })
    .toBe(1);
  const mutedSpace = await spaceOf(reader.page, ids.z);
  expect(mutedSpace.unreadCount).toBe(2);
  const mutedButton = channelButton(reader.page, names.z);
  await expect(mutedButton.getByTestId("chat-unread-dot")).toHaveCount(0);
  await expect(mutedButton.getByTestId("chat-mention-badge")).toHaveCount(0);
  const mutedTotals = await summaryOf(reader.page);
  expect(mutedTotals.totalUnread).toBe(
    mutedTotals.spaces
      .filter((s) => !s.muted)
      .reduce((sum, s) => sum + s.unreadCount, 0),
  );
  expect(mutedTotals.totalMentions).toBe(
    mutedTotals.spaces
      .filter((s) => !s.muted)
      .reduce((sum, s) => sum + s.mentionCount, 0),
  );

  // A control mention proves the muted one made no notification: it is sent after, so once it shows the earlier one is settled.
  // D06: at the Mentions level the same mention notifies; invalid input and a mute time in the past are refused or ignored.
  await channelButton(reader.page, names.z).click();
  await reader.page
    .getByRole("button", { name: "Notification settings" })
    .click();
  await reader.page
    .getByRole("menuitemradio", { name: "Mentions only" })
    .click();
  await expect
    .poll(async () => (await spaceOf(reader.page, ids.z)).notificationLevel, {
      timeout: 20_000,
    })
    .toBe("mentions");
  const loudPing = `@${readerName} loud ${unique}`;
  await send(ids.z, loudPing, mentionOfReader(loudPing));
  await expect
    .poll(
      async () =>
        (await inboxOf(reader.page)).filter((e) =>
          e.excerpt?.includes(loudPing),
        ).length,
      { timeout: 30_000 },
    )
    .toBe(1);
  expect(
    (await inboxOf(reader.page)).filter((e) => e.excerpt?.includes(mutedPing)),
  ).toHaveLength(0);
  const invalid = await apiWrite(
    reader.page,
    "PUT",
    `${chat}/spaces/${ids.z}/notification-level`,
    { level: "loud" },
  );
  expect(invalid.status).toBe(400);
  const mismatched = await apiWrite(
    reader.page,
    "PUT",
    `${chat}/spaces/${ids.z}/notification-level`,
    { level: "mentions", mutedUntil: "2999-01-01T00:00:00Z" },
  );
  expect(mismatched.status).toBe(400);
  const past = await apiWrite(
    reader.page,
    "PUT",
    `${chat}/spaces/${ids.z}/notification-level`,
    { level: "none", mutedUntil: "2020-01-01T00:00:00Z" },
  );
  expect(past.status).toBeLessThan(300);
  expect((await spaceOf(reader.page, ids.z)).notificationLevel).toBe(
    "mentions",
  );

  // D06 in a conversation: at the Mentions level a plain direct message notifies no one, at All it does.
  const level = (value: string) =>
    apiWrite(reader.page, "PUT", `${chat}/spaces/${dmId}/notification-level`, {
      level: value,
    });
  expect((await level("mentions")).status).toBeLessThan(300);
  const quiet = `dm quiet ${unique}`;
  const control = `dm control ${unique}`;
  await send(dmId, quiet);
  expect((await level("all")).status).toBeLessThan(300);
  await send(dmId, control);
  await expect
    .poll(
      async () =>
        (await inboxOf(reader.page)).filter((e) => e.excerpt?.includes(control))
          .length,
      { timeout: 30_000 },
    )
    .toBe(1);
  expect(
    (await inboxOf(reader.page)).filter((e) => e.excerpt?.includes(quiet)),
  ).toHaveLength(0);

  // axe finds nothing serious on the sidebar with a dot, a mention badge and a muted row.
  await send(ids.x, `again ${unique}`);
  await expect(xButton).toHaveAccessibleName(/unread/i, { timeout: 20_000 });
  const axe = await new AxeBuilder({ page: reader.page })
    .disableRules(["color-contrast", "scrollable-region-focusable"])
    .analyze();
  expect(
    axe.violations.filter((v) =>
      ["serious", "critical"].includes(v.impact ?? ""),
    ),
  ).toEqual([]);

  // Review frames: the sidebar with its three badge kinds, and the notification menu, at 1440, 1024 and 390, light
  // and dark, in Vietnamese. Each frame stays inside the viewport and never scrolls the page sideways.
  if (SHOTS) {
    // The muted row (bell-off, dimmed, no dot) is part of the reviewed sidebar.
    const mute = await apiWrite(
      reader.page,
      "PUT",
      `${chat}/spaces/${ids.z}/notification-level`,
      { level: "none" },
    );
    expect(mute.status).toBeLessThan(300);
    await send(ids.z, `one more ${unique}`);
    const state = await reader.context.storageState();
    const sizes = [
      { width: 1440, height: 900 },
      { width: 1024, height: 768 },
      { width: 390, height: 844 },
    ];
    for (const theme of ["light", "dark"]) {
      for (const size of sizes) {
        const context = await browser.newContext({
          viewport: size,
          storageState: state,
        });
        const frame = await context.newPage();
        await frame.addInitScript((mode) => {
          try {
            window.localStorage.setItem("notrelix-locale", "vi");
            window.localStorage.setItem("notrelix-theme-mode", mode);
          } catch {
            // Private mode: the defaults apply.
          }
        }, theme);
        const tag = `${size.width}-${theme}`;
        const settle = async (name: string) => {
          expect(
            await frame.evaluate(
              () =>
                document.documentElement.scrollWidth -
                document.documentElement.clientWidth,
            ),
          ).toBeLessThanOrEqual(0);
          await frame.waitForTimeout(500);
          await frame.screenshot({ path: `${SHOTS}/${name}.png` });
        };
        const inside = async (locator: Locator) => {
          const b = (await locator.first().boundingBox())!;
          expect(b.x).toBeGreaterThanOrEqual(-0.5);
          expect(b.x + b.width).toBeLessThanOrEqual(size.width + 0.5);
        };
        await frame.goto(size.width < 768 ? chat : `${chat}/${ids.home}`);
        const list = frame.getByRole("navigation", { name: "Kênh" });
        await expect(list).toBeVisible({ timeout: 20_000 });
        await expect(
          list.getByTestId("chat-mention-badge").first(),
        ).toBeVisible();
        await inside(list.getByTestId("chat-mention-badge").first());
        await settle(`${tag}-sidebar`);
        await frame.goto(`${chat}/${ids.z}`);
        await frame.getByRole("button", { name: "Cài đặt thông báo" }).click();
        const menu = frame.getByRole("menu");
        await expect(menu).toBeVisible();
        await inside(menu);
        await settle(`${tag}-level-menu`);
        await context.close();
      }
    }
  }

  // D07: Mark all as read clears the dot, the mention badge and the totals in one step; a message sent after it stays unread.
  const markAll = reader.page.getByRole("button", {
    name: "Mark all as read",
  });
  await expect(markAll).toBeVisible();
  await markAll.click();
  await expect(xButton.getByTestId("chat-unread-dot")).toHaveCount(0, {
    timeout: 20_000,
  });
  await expect(
    channelButton(reader.page, names.y).getByTestId("chat-mention-badge"),
  ).toHaveCount(0);
  await expect(markAll).toHaveCount(0);
  const cleared = await summaryOf(reader.page);
  expect(cleared.totalUnread).toBe(0);
  expect(cleared.totalMentions).toBe(0);
  await send(ids.x, `after all ${unique}`);
  await expect(xButton).toHaveAccessibleName(/unread/i, { timeout: 20_000 });
  await expect(markAll).toBeVisible();

  await reader.context.close();
});

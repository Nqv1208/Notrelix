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

// Direct and group conversations on the real stack (UC-CHT-E01 to E07): start a direct and a group conversation,
// add people from their join point, leave, list in order of activity, block and unblock, hide and restore.
// Set CHAT_STATE_SHOTS to a directory to also write one screenshot per reviewed state (no baseline is kept).
test.use({ viewport: { width: 1440, height: 900 }, actionTimeout: 30_000 });
test.setTimeout(600_000);

const SHOTS = process.env.CHAT_STATE_SHOTS;

// The seeded Member of every workspace who is not the owner, an admin or the first member.
const demo = {
  email: process.env.REAL_E2E_DEMO_EMAIL ?? "demo@notrelix.com",
  password: process.env.REAL_E2E_DEMO_PASSWORD ?? "Notrelix@123",
};

interface Conversation {
  id: string;
  kind: string;
  name: string | null;
  participants: { userId: string }[];
  isHidden: boolean;
  unreadCount: number;
}
interface Started {
  conversation: Conversation;
  created: boolean;
  skipped: string[];
}
interface MessageRow {
  kind: string;
  content: string | null;
}
interface MessagePage {
  items: MessageRow[];
  hasOlder: boolean;
}
interface MemberRow {
  userId: string;
  name: string;
}
interface SearchPage {
  items: unknown[];
}

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
  });
  const page = await context.newPage();
  await page.addInitScript(english);
  await signIn(page, user);
  return { context, page };
}

async function serious(page: Page) {
  const result = await new AxeBuilder({ page })
    .withTags(["wcag2a", "wcag2aa"])
    .analyze();
  return result.violations.filter(
    (v) => v.impact === "serious" || v.impact === "critical",
  );
}

// A button fades between its disabled and enabled look for a moment; axe is asked until that settles, and a real
// violation stays and fails the poll.
async function expectNoSerious(page: Page, what: string) {
  await expect
    .poll(() => serious(page), { message: what, timeout: 5_000 })
    .toEqual([]);
}

test("direct and group conversations, the join point, leaving, blocking and hiding work through the real stack", async ({
  page: owner,
  browser,
}) => {
  const unique = Date.now().toString(36);
  await owner.addInitScript(english);
  await signIn(owner, realUsers.workspaceOwner);
  const ws = (await findWorkspace(owner, "Workspace 3")).id;
  const base = `/workspaces/${ws}`;
  const chat = `${base}/chat`;

  const admin = await person(browser, realUsers.workspaceAdmin);
  const mate = await person(browser, demo);
  const outsider = await person(browser, realUsers.admin);
  const guest = await person(browser, realUsers.workspaceGuest);
  const idOf = async (page: Page) =>
    (await apiRead<{ userId: string }>(page, `${base}/members/me`)).userId;
  const ownerId = await idOf(owner);
  const adminId = await idOf(admin.page);
  const mateId = await idOf(mate.page);
  const outsiderId = await idOf(outsider.page);
  const roster = await apiRead<{ items: MemberRow[] }>(
    owner,
    `${base}/members?limit=200`,
  );
  const nameOf = (id: string) =>
    roster.items.find((m) => m.userId === id)!.name;

  // A block left by an earlier run on the same stack would refuse the starts below.
  for (const left of await apiRead<{ userId: string }[]>(
    owner,
    `${chat}/me/blocks`,
  )) {
    await apiWrite(owner, "PUT", `${chat}/me/blocks`, {
      userId: left.userId,
      blocked: false,
    });
  }

  const start = (page: Page, body: unknown) =>
    apiWrite<Started>(page, "POST", `${chat}/conversations`, body);
  const list = async (page: Page) =>
    (await apiRead<Conversation[]>(page, `${chat}/conversations`)) ?? [];
  const send = async (page: Page, space: string, content: string) => {
    const r = await apiWrite(page, "POST", `${chat}/spaces/${space}/messages`, {
      content,
      clientMessageId: crypto.randomUUID(),
    });
    expect(r.status, `send ${content}`).toBeLessThan(300);
  };
  const messages = async (page: Page, space: string) =>
    (
      await apiRead<MessagePage>(
        page,
        `${chat}/spaces/${space}/messages?limit=100`,
      )
    ).items;
  const row = (
    page: Page,
    kind: "Direct message" | "Group conversation",
    who: string,
  ) =>
    page.getByRole("button", {
      name: new RegExp(`^${kind}: ${who}(,|$)`),
    });

  // E01 through the page: the picker starts empty and cannot submit, a search with no match says so, one person starts a direct conversation.
  await owner.goto(chat);
  await owner.getByRole("button", { name: "New message" }).first().click();
  const dialog = owner.getByRole("dialog");
  await expect(
    dialog.getByRole("heading", { name: "New message" }),
  ).toBeVisible();
  const startButton = dialog.getByRole("button", {
    name: "Start conversation",
  });
  await expect(startButton).toBeDisabled();
  await dialog.getByLabel("Find people").fill("zzzz-nobody");
  await expect(dialog.getByText("No one matches.")).toBeVisible();
  await dialog.getByLabel("Find people").fill("");
  await dialog
    .getByRole("checkbox", { name: nameOf(adminId), exact: true })
    .check();
  await expect(dialog.getByText("1 person picked")).toBeVisible();
  await expectNoSerious(owner, "axe on the New message dialog");
  await startButton.click();
  await expect(dialog).toBeHidden({ timeout: 20_000 });
  const adminDm = row(owner, "Direct message", nameOf(adminId));
  await expect(adminDm).toHaveAttribute("aria-current", "page", {
    timeout: 20_000,
  });

  // E01: the same pair again is the same conversation (200), two starts at once store one, and the refusals are the stated ones.
  const dmId = (await list(owner)).find(
    (c) =>
      c.kind === "direct" && c.participants.some((p) => p.userId === adminId),
  )!.id;
  const again = await start(owner, { kind: "direct", userId: adminId });
  expect(again.status).toBe(200);
  expect(again.data!.conversation.id).toBe(dmId);
  expect(again.data!.created).toBe(false);
  // A pair that already talks (an earlier run of this spec on the same stack) creates nothing new.
  const pairExisted = (await list(owner)).some(
    (c) =>
      c.kind === "direct" && c.participants.some((p) => p.userId === mateId),
  );
  const [fromOwner, fromMate] = await Promise.all([
    start(owner, { kind: "direct", userId: mateId }),
    start(mate.page, { kind: "direct", userId: ownerId }),
  ]);
  expect(fromOwner.status).toBeLessThan(300);
  expect(fromMate.status).toBeLessThan(300);
  expect(fromOwner.data!.conversation.id).toBe(fromMate.data!.conversation.id);
  expect(
    [fromOwner, fromMate].filter((r) => r.data!.created),
    "exactly one of two simultaneous starts creates it",
  ).toHaveLength(pairExisted ? 0 : 1);
  const mateDmId = fromOwner.data!.conversation.id;
  expect((await start(owner, { kind: "direct", userId: ownerId })).status).toBe(
    400,
  );
  expect(
    (await start(owner, { kind: "direct", userId: crypto.randomUUID() }))
      .status,
  ).toBe(404);
  expect(
    (await start(guest.page, { kind: "direct", userId: ownerId })).status,
    "a Guest with no shared space reaches nobody",
  ).toBe(404);

  // E02 through the page: two people picked turn the dialog into a group with a name; each of them sees it and no one else does.
  const groupName = `Team ${unique}`;
  await owner.getByRole("button", { name: "New message" }).first().click();
  await dialog
    .getByRole("checkbox", { name: nameOf(adminId), exact: true })
    .check();
  await expect(
    dialog.getByRole("button", { name: "Start conversation" }),
  ).toBeVisible();
  await dialog
    .getByRole("checkbox", { name: nameOf(mateId), exact: true })
    .check();
  await expect(dialog.getByText("2 people picked")).toBeVisible();
  await dialog.getByLabel("Group name (optional)").fill(groupName);
  await expectNoSerious(owner, "axe on the group dialog");
  await dialog.getByRole("button", { name: "Create group" }).click();
  await expect(dialog).toBeHidden({ timeout: 20_000 });
  const groupRow = row(owner, "Group conversation", groupName);
  await expect(groupRow).toHaveAttribute("aria-current", "page", {
    timeout: 20_000,
  });
  const group = (await list(owner)).find((c) => c.name === groupName)!;
  expect(group.participants.map((p) => p.userId).sort()).toEqual(
    [ownerId, adminId, mateId].sort(),
  );
  await send(owner, group.id, `before the fourth ${unique}`);
  for (const p of [admin.page, mate.page]) {
    await p.goto(chat);
    await expect(row(p, "Group conversation", groupName)).toBeVisible({
      timeout: 20_000,
    });
  }
  expect(
    (await list(outsider.page)).some((c) => c.id === group.id),
    "an Admin outside the group does not see it",
  ).toBe(false);
  expect(
    await apiRead<MessagePage>(
      outsider.page,
      `${chat}/spaces/${group.id}/messages`,
    ).catch(() => null),
  ).toBeNull();
  // E02 refusals: over the cap is 422, a person outside the workspace is skipped and reported, one person is not a group.
  expect(
    (
      await start(owner, {
        kind: "group",
        userIds: Array.from({ length: 8 }, () => crypto.randomUUID()),
      })
    ).status,
  ).toBe(422);
  const ghost = crypto.randomUUID();
  const skippedRun = await start(owner, {
    kind: "group",
    userIds: [adminId, mateId, ghost],
    name: `Skip ${unique}`,
  });
  expect(skippedRun.status).toBe(201);
  expect(skippedRun.data!.skipped).toEqual([ghost]);
  expect(
    (await start(owner, { kind: "group", userIds: [adminId] })).status,
  ).toBe(400);

  // E03 through the page: a participant adds a fourth person; the newcomer reads from the join point only.
  await admin.page.goto(`${chat}/dm/${group.id}`);
  await admin.page
    .getByRole("button", { name: "Conversation details" })
    .click();
  const details = admin.page.getByRole("region", { name: "Group details" });
  await expect(details).toBeVisible({ timeout: 20_000 });
  await details.getByRole("button", { name: "Add people" }).click();
  const addDialog = admin.page.getByRole("dialog");
  await expect(addDialog.getByText("Add people to the group")).toBeVisible();
  await expect(
    addDialog.getByRole("button", { name: "Add", exact: true }),
  ).toBeDisabled();
  await addDialog
    .getByRole("checkbox", { name: nameOf(outsiderId), exact: true })
    .check();
  await expectNoSerious(admin.page, "axe on the Add people dialog");
  await addDialog.getByRole("button", { name: "Add", exact: true }).click();
  await expect(addDialog).toBeHidden({ timeout: 20_000 });
  await expect
    .poll(
      async () => (await list(outsider.page)).some((c) => c.id === group.id),
      {
        timeout: 20_000,
      },
    )
    .toBe(true);
  await send(owner, group.id, `after the fourth ${unique}`);
  await expect
    .poll(
      async () =>
        (await messages(outsider.page, group.id)).map((m) => m.content),
      { timeout: 20_000 },
    )
    .toContain(`after the fourth ${unique}`);
  const seen = (await messages(outsider.page, group.id)).map((m) => m.content);
  expect(seen, "nothing before the join point is readable").not.toContain(
    `before the fourth ${unique}`,
  );
  expect(
    (
      await apiRead<MessagePage>(
        outsider.page,
        `${chat}/spaces/${group.id}/messages?limit=100`,
      )
    ).hasOlder,
  ).toBe(false);
  const searchFor = (text: string) =>
    apiRead<SearchPage>(
      outsider.page,
      `${chat}/search?q=${encodeURIComponent(text)}`,
    );
  expect((await searchFor(`before the fourth ${unique}`)).items).toHaveLength(
    0,
  );
  expect((await searchFor(`after the fourth ${unique}`)).items).toHaveLength(1);
  // The people who were there keep the whole history.
  expect(
    (await messages(admin.page, group.id)).map((m) => m.content),
  ).toContain(`before the fourth ${unique}`);
  const unreadOfNew = (await list(outsider.page)).find(
    (c) => c.id === group.id,
  )!;
  expect(
    unreadOfNew.unreadCount,
    "only the message after the join is unread",
  ).toBe(1);
  // E03 refusals: a direct conversation takes no one (400), a non-participant gets NotFound.
  expect(
    (
      await apiWrite(
        owner,
        "POST",
        `${chat}/conversations/${dmId}/participants`,
        { userIds: [mateId] },
      )
    ).status,
  ).toBe(400);
  expect(
    (
      await apiWrite(
        outsider.page,
        "POST",
        `${chat}/conversations/${skippedRun.data!.conversation.id}/participants`,
        { userIds: [mateId] },
      )
    ).status,
  ).toBe(404);

  // E04 through the page: the newcomer leaves after a confirm; the group tells the others; nothing of it reaches them again.
  const systemBefore = (await messages(owner, group.id)).filter(
    (m) => m.kind === "system",
  ).length;
  await outsider.page.goto(`${chat}/dm/${group.id}`);
  await outsider.page
    .getByRole("button", { name: "Conversation details" })
    .click();
  await outsider.page
    .getByRole("region", { name: "Group details" })
    .getByRole("button", { name: "Leave group" })
    .click();
  const leave = outsider.page
    .getByRole("alertdialog")
    .or(outsider.page.getByRole("dialog"));
  await expect(leave.getByText("Leave this group?")).toBeVisible();
  await expectNoSerious(outsider.page, "axe on the leave confirm");
  await leave.getByRole("button", { name: "Leave group" }).click();
  await expect
    .poll(
      async () => (await list(outsider.page)).some((c) => c.id === group.id),
      {
        timeout: 20_000,
      },
    )
    .toBe(false);
  expect(
    (await messages(owner, group.id)).filter((m) => m.kind === "system").length,
  ).toBe(systemBefore + 1);
  const gone = await outsider.page.request.get(
    `${process.env.VITE_API_URL ?? "http://127.0.0.1:58000/api/v1"}${chat}/spaces/${group.id}/messages`,
  );
  expect(
    gone.status(),
    "a person who left gets the NotFound of a stranger",
  ).toBe(404);
  expect(
    (
      await apiWrite(
        outsider.page,
        "DELETE",
        `${chat}/conversations/${group.id}/participants/me`,
      )
    ).status,
    "leaving twice is NotFound",
  ).toBe(404);
  expect(
    (
      await apiWrite(
        owner,
        "DELETE",
        `${chat}/conversations/${dmId}/participants/me`,
      )
    ).status,
    "a direct conversation cannot be left",
  ).toBe(400);
  // The last person out closes the conversation.
  const closing = skippedRun.data!.conversation.id;
  for (const p of [owner, admin.page, mate.page]) {
    expect(
      (
        await apiWrite(
          p,
          "DELETE",
          `${chat}/conversations/${closing}/participants/me`,
        )
      ).status,
    ).toBeLessThan(300);
  }
  expect((await list(owner)).some((c) => c.id === closing)).toBe(false);

  // E05: a new message moves its conversation to the top of the list, on the page and in the answer.
  await send(admin.page, dmId, `ping ${unique}`);
  await owner.goto(chat);
  await expect
    .poll(async () => (await list(owner))[0]?.id, { timeout: 20_000 })
    .toBe(dmId);
  const rowsNow = owner
    .getByTestId("chat-dm-section")
    .getByRole("button", { name: /^(Direct message|Group conversation): / });
  await expect(rowsNow.first()).toHaveAccessibleName(
    new RegExp(`^Direct message: ${nameOf(adminId)}`),
    { timeout: 20_000 },
  );
  await send(mate.page, mateDmId, `ping ${unique}`);
  await expect(rowsNow.first()).toHaveAccessibleName(
    new RegExp(`^Direct message: ${nameOf(mateId)}`),
    { timeout: 20_000 },
  );

  // E07 through the page: Hide removes the conversation from my list only; a new message from the other person brings it back.
  await owner.goto(`${chat}/dm/${mateDmId}`);
  await owner.getByRole("button", { name: "Hide conversation" }).click();
  await expect
    .poll(async () => (await list(owner)).some((c) => c.id === mateDmId), {
      timeout: 20_000,
    })
    .toBe(false);
  expect(
    (await list(mate.page)).some((c) => c.id === mateDmId),
    "the other person keeps it",
  ).toBe(true);
  await owner.goto(chat);
  await expect(row(owner, "Direct message", nameOf(mateId))).toHaveCount(0);
  await send(mate.page, mateDmId, `back ${unique}`);
  await expect(row(owner, "Direct message", nameOf(mateId))).toBeVisible({
    timeout: 20_000,
  });
  expect((await list(owner)).some((c) => c.id === mateDmId)).toBe(true);
  const unknown = await apiWrite(
    owner,
    "PUT",
    `${chat}/conversations/${crypto.randomUUID()}/visibility`,
    { hidden: true },
  );
  expect(unknown.status).toBe(404);

  // E06 through the page: Block asks first, then both directions refuse a new conversation with the same NotFound,
  // the composer says why it is closed, and Blocked people lists the person with an Unblock.
  await owner.goto(`${chat}/dm/${dmId}`);
  await owner.getByRole("button", { name: "Conversation details" }).click();
  await owner
    .getByRole("region", { name: "Profile" })
    .getByRole("button", { name: "Block person" })
    .click();
  const blockDialog = owner
    .getByRole("alertdialog")
    .or(owner.getByRole("dialog"));
  await expect(
    blockDialog.getByText(`Block ${nameOf(adminId)}?`),
  ).toBeVisible();
  await expect(
    blockDialog.getByText(/does not hide their messages in channels/),
  ).toBeVisible();
  await expectNoSerious(owner, "axe on the block confirm");
  await blockDialog.getByRole("button", { name: "Block", exact: true }).click();
  await expect(
    owner.getByText(
      `You blocked ${nameOf(adminId)}. Unblock them to message them again.`,
    ),
  ).toBeVisible({ timeout: 20_000 });
  // The notice is drawn at once; the block is stored when the answer comes back.
  await expect
    .poll(
      async () =>
        (await apiRead<{ userId: string }[]>(owner, `${chat}/me/blocks`)).map(
          (b) => b.userId,
        ),
      { timeout: 20_000 },
    )
    .toEqual([adminId]);
  expect(
    (await start(admin.page, { kind: "direct", userId: ownerId })).status,
    "the blocked person cannot start a conversation",
  ).toBe(404);
  expect(
    (await start(owner, { kind: "direct", userId: adminId })).status,
    "the blocker cannot either",
  ).toBe(404);
  const blockedIds = (
    await apiRead<{ userId: string }[]>(owner, `${chat}/me/blocks`)
  ).map((b) => b.userId);
  expect(blockedIds).toEqual([adminId]);
  await owner.goto(chat);
  await owner.getByTestId("chat-open-blocked").click();
  const blocked = owner.getByTestId("chat-blocked-people");
  await expect(blocked.getByText(nameOf(adminId))).toBeVisible();
  await expectNoSerious(owner, "axe on Blocked people");
  await blocked
    .getByRole("button", { name: `Unblock ${nameOf(adminId)}` })
    .click();
  await expect(blocked.getByText("You have not blocked anyone.")).toBeVisible({
    timeout: 20_000,
  });
  await owner.keyboard.press("Escape");
  await expect
    .poll(
      async () =>
        (await apiRead<{ userId: string }[]>(owner, `${chat}/me/blocks`))
          .length,
      { timeout: 20_000 },
    )
    .toBe(0);
  const restored = await start(admin.page, { kind: "direct", userId: ownerId });
  expect(restored.status, "after Unblock the pair starts again").toBe(200);
  expect(restored.data!.conversation.id).toBe(dmId);

  // Review frames of the states no board draws, at 1440, 1024 and 390, light and dark, in Vietnamese.
  if (SHOTS) {
    // The empty sidebar belongs to a person with no conversation (the outsider left the group); the blocked frame needs a block.
    expect(
      (
        await apiWrite(owner, "PUT", `${chat}/me/blocks`, {
          userId: mateId,
          blocked: true,
        })
      ).status,
    ).toBeLessThan(300);
    const states = {
      owner: await owner.context().storageState(),
      empty: await outsider.context.storageState(),
    };
    const sizes = [
      { width: 1440, height: 900 },
      { width: 1024, height: 768 },
      { width: 390, height: 844 },
    ];
    for (const theme of ["light", "dark"]) {
      for (const size of sizes) {
        for (const who of ["owner", "empty"] as const) {
          const context = await browser.newContext({
            viewport: size,
            storageState: states[who],
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
          const tag = `e-${theme}-${size.width}-${who}`;
          const settle = async (name: string) => {
            expect(
              await frame.evaluate(
                () => document.documentElement.scrollWidth - window.innerWidth,
              ),
              `${name} scrolls sideways`,
            ).toBeLessThanOrEqual(0);
            await frame.waitForTimeout(500);
            await frame.screenshot({ path: `${SHOTS}/${name}.png` });
          };
          const inside = async (locator: Locator) => {
            const b = (await locator.first().boundingBox())!;
            expect(b.x).toBeGreaterThanOrEqual(-0.5);
            expect(b.x + b.width).toBeLessThanOrEqual(size.width + 0.5);
          };
          const modal = () =>
            frame.getByRole("alertdialog").or(frame.getByRole("dialog"));
          await frame.goto(chat);
          await expect(frame.getByTestId("chat-dm-section"))
            .toBeVisible({ timeout: 20_000 })
            .catch(async (error) => {
              await frame.screenshot({ path: `${SHOTS}/FAIL-${tag}.png` });
              throw error;
            });
          await settle(`${tag}-sidebar`);
          if (who === "empty") {
            await context.close();
            continue;
          }
          await frame
            .getByRole("button", { name: "Tin nhắn mới" })
            .first()
            .click();
          await expect(modal()).toBeVisible();
          const boxes = modal().getByRole("checkbox");
          await boxes.nth(0).check();
          await boxes.nth(1).check();
          await modal()
            .getByLabel("Tên nhóm (không bắt buộc)")
            .fill("Nhóm thử");
          await inside(modal());
          await settle(`${tag}-new-group`);
          await frame.keyboard.press("Escape");
          await frame.goto(`${chat}/dm/${group.id}`);
          await frame
            .getByRole("button", { name: "Thông tin cuộc trò chuyện" })
            .click();
          await expect(
            frame.getByRole("region", { name: "Thông tin nhóm" }),
          ).toBeVisible({ timeout: 20_000 });
          await settle(`${tag}-group-details`);
          await frame.getByRole("button", { name: "Rời nhóm" }).click();
          await expect(modal()).toBeVisible();
          await inside(modal());
          await settle(`${tag}-leave-confirm`);
          await frame.keyboard.press("Escape");
          await frame.goto(`${chat}/dm/${dmId}`);
          await frame
            .getByRole("button", { name: "Thông tin cuộc trò chuyện" })
            .click();
          await settle(`${tag}-dm-details`);
          await frame.getByRole("button", { name: "Chặn người này" }).click();
          await expect(modal()).toBeVisible();
          await inside(modal());
          await settle(`${tag}-block-confirm`);
          await frame.keyboard.press("Escape");
          await frame.goto(`${chat}/dm/${mateDmId}`);
          await expect(frame.getByText(/Bạn đã chặn/).first()).toBeVisible({
            timeout: 20_000,
          });
          await settle(`${tag}-blocked-room`);
          await frame.goto(chat);
          await frame.getByTestId("chat-open-blocked").click();
          await expect(frame.getByTestId("chat-blocked-people")).toBeVisible();
          await inside(frame.getByTestId("chat-blocked-people"));
          await settle(`${tag}-blocked-people`);
          await context.close();
        }
      }
    }
  }

  for (const p of [admin, mate, outsider, guest]) await p.context.close();
});

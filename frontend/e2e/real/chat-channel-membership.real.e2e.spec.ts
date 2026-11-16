import AxeBuilder from "@axe-core/playwright";
import { expect, test, type Page } from "@playwright/test";
import {
  apiRead,
  apiWrite,
  findWorkspace,
  realUsers,
  signIn,
} from "./support/real-session";

test.use({ viewport: { width: 1440, height: 900 } });
test.setTimeout(180_000);

interface Channel {
  id: string;
  name: string;
  version: number;
  isArchived: boolean;
  isMember: boolean;
}
interface Detail {
  channel: Channel;
  members: { userId: string; role: string }[];
}

const english = () => {
  try {
    window.localStorage.setItem("notrelix-locale", "en");
  } catch {
    // Private mode: the default language applies.
  }
};

async function serious(page: Page) {
  const axe = await new AxeBuilder({ page })
    .disableRules(["color-contrast", "scrollable-region-focusable"])
    .analyze();
  return axe.violations.filter((v) =>
    ["serious", "critical"].includes(v.impact ?? ""),
  );
}

test("a manager removes a member, a member leaves, the last manager cannot, and a channel is archived, unarchived and deleted on the real stack", async ({
  page,
  browser,
}) => {
  const unique = Date.now().toString(36);
  await page.addInitScript(english);
  await signIn(page, realUsers.workspaceOwner);
  const workspace = await findWorkspace(page, "Workspace 3");
  const ws = `/workspaces/${workspace.id}`;
  const channelsPath = `${ws}/chat/channels`;

  const adminContext = await browser.newContext({
    viewport: { width: 1440, height: 900 },
  });
  const admin = await adminContext.newPage();
  await admin.addInitScript(english);
  await signIn(admin, realUsers.workspaceAdmin);
  const adminId = (await apiRead<{ userId: string }>(admin, `${ws}/members/me`))
    .userId;
  const ownerId = (await apiRead<{ userId: string }>(page, `${ws}/members/me`))
    .userId;

  // ── Remove: the manager removes the admin of a private channel from the member list.
  const privateName = `ring-${unique}`;
  const priv = (
    await apiWrite<Channel>(page, "POST", channelsPath, {
      name: privateName,
      isPrivate: true,
    })
  ).data!;
  expect(
    (
      await apiWrite(page, "POST", `${channelsPath}/${priv.id}/members`, {
        userIds: [adminId],
      })
    ).status,
  ).toBeLessThan(300);
  expect(
    (await apiRead<Detail>(admin, `${channelsPath}/${priv.id}`)).members.map(
      (m) => m.userId,
    ),
  ).toContain(adminId);

  await page.goto(`${ws}/chat/${priv.id}`);
  await page.getByRole("button", { name: /^Members, 2/ }).click();
  expect(await serious(page)).toEqual([]);
  await page.getByRole("button", { name: /^Remove / }).click();
  await page
    .getByRole("alertdialog")
    .getByRole("button", { name: "Remove" })
    .click();
  await expect(page.getByRole("alertdialog")).toHaveCount(0);
  await expect
    .poll(async () =>
      (await apiRead<Detail>(page, `${channelsPath}/${priv.id}`)).members.map(
        (m) => m.userId,
      ),
    )
    .toEqual([ownerId]);
  // The removed person is told what anyone outside is told: not found.
  const gone = await admin.request.get(
    `${process.env.VITE_API_URL ?? "http://127.0.0.1:58000/api/v1"}${channelsPath}/${priv.id}`,
  );
  expect(gone.status()).toBe(404);

  // ── Leave: a member of a public channel leaves from the settings menu; it moves to Browse.
  const publicName = `open-${unique}`;
  const pub = (
    await apiWrite<Channel>(page, "POST", channelsPath, {
      name: publicName,
      isPrivate: false,
    })
  ).data!;
  expect(
    (await apiWrite(admin, "POST", `${channelsPath}/${pub.id}/members/me`))
      .status,
  ).toBeLessThan(300);
  await admin.goto(`${ws}/chat/${pub.id}`);
  await admin.getByRole("button", { name: "Channel settings" }).click();
  await admin.getByRole("menuitem", { name: "Leave channel" }).click();
  expect(await serious(admin)).toEqual([]);
  await admin
    .getByRole("alertdialog")
    .getByRole("button", { name: "Leave" })
    .click();
  await expect(admin.getByRole("button", { name: "Join channel" })).toBeVisible(
    { timeout: 20_000 },
  );
  expect(
    (await apiRead<Channel[]>(admin, `${channelsPath}?browse=true`)).map(
      (c) => c.id,
    ),
  ).toContain(pub.id);
  await adminContext.close();

  // ── The last manager cannot leave: told why, and still a member.
  await page.goto(`${ws}/chat/${pub.id}`);
  await page.getByRole("button", { name: "Channel settings" }).click();
  await page.getByRole("menuitem", { name: "Leave channel" }).click();
  await page
    .getByRole("alertdialog")
    .getByRole("button", { name: "Leave" })
    .click();
  await expect(
    page.getByText("You are the last manager of this channel"),
  ).toBeVisible({ timeout: 20_000 });
  expect(
    (await apiRead<Detail>(page, `${channelsPath}/${pub.id}`)).channel.isMember,
  ).toBe(true);

  // ── Archive, unarchive, then delete with the typed name; a stale version changes nothing.
  const menu = async (item: string) => {
    await page.getByRole("button", { name: "Channel settings" }).click();
    await page.getByRole("menuitem", { name: item }).click();
  };
  await menu("Archive channel");
  await page
    .getByRole("alertdialog")
    .getByRole("button", { name: "Archive" })
    .click();
  await expect(
    page.getByText("This channel is archived and read-only."),
  ).toBeVisible({ timeout: 20_000 });
  await menu("Unarchive channel");
  await page
    .getByRole("alertdialog")
    .getByRole("button", { name: "Unarchive" })
    .click();
  await expect(
    page.getByText("This channel is archived and read-only."),
  ).toHaveCount(0, { timeout: 20_000 });
  const current = (await apiRead<Detail>(page, `${channelsPath}/${pub.id}`))
    .channel;
  expect(current.isArchived).toBe(false);
  expect(
    (
      await apiWrite(
        page,
        "DELETE",
        `${channelsPath}/${pub.id}?expectedVersion=${current.version - 1}`,
      )
    ).status,
  ).toBe(412);

  await menu("Delete channel");
  const dialog = page.getByRole("dialog");
  const confirm = dialog.getByRole("button", { name: "Delete channel" });
  await expect(confirm).toBeDisabled();
  await dialog.getByLabel("Channel name").fill("not-the-name");
  await expect(confirm).toBeDisabled();
  expect(await serious(page)).toEqual([]);
  await dialog.getByLabel("Channel name").fill(publicName);
  await expect(confirm).toBeEnabled();
  await confirm.click();
  await expect(page.getByRole("dialog")).toHaveCount(0, { timeout: 20_000 });
  await expect
    .poll(async () =>
      (await apiRead<Channel[]>(page, channelsPath)).some(
        (c) => c.id === pub.id,
      ),
    )
    .toBe(false);
  expect(
    (
      await apiWrite(page, "POST", `${channelsPath}/${pub.id}/archive`, {
        archived: true,
        expectedVersion: current.version,
      })
    ).status,
  ).toBe(404);
});

// Set CHAT_STATE_SHOTS to a directory to also write one screenshot per reviewed frame.
const SHOTS = process.env.CHAT_STATE_SHOTS;

test("the member list, Leave, Delete and markup frames fit at 1440, 1024 and 390 in both themes, in Vietnamese", async ({
  page,
  browser,
}) => {
  test.skip(
    !SHOTS,
    "review frames are written only when CHAT_STATE_SHOTS is set",
  );
  const unique = Date.now().toString(36);
  await page.addInitScript(english);
  await signIn(page, realUsers.workspaceOwner);
  const workspace = await findWorkspace(page, "Workspace 3");
  const ws = `/workspaces/${workspace.id}`;
  const channelsPath = `${ws}/chat/channels`;
  const adminContext = await browser.newContext();
  const admin = await adminContext.newPage();
  await signIn(admin, realUsers.workspaceAdmin);
  const adminId = (await apiRead<{ userId: string }>(admin, `${ws}/members/me`))
    .userId;
  await adminContext.close();
  const channel = (
    await apiWrite<Channel>(page, "POST", channelsPath, {
      name: `look-${unique}`,
      isPrivate: false,
    })
  ).data!;
  await apiWrite(page, "POST", `${channelsPath}/${channel.id}/members`, {
    userIds: [adminId],
  });
  for (const content of [
    `Xin chào **cả nhóm** ${unique}`,
    "Xem https://example.com/tai-lieu và `mã` _nghiêng_",
  ]) {
    await apiWrite(page, "POST", `${ws}/chat/spaces/${channel.id}/messages`, {
      content,
      clientMessageId: crypto.randomUUID(),
    });
  }
  const state = await page.context().storageState();
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
      const tag = `m-${theme}-${size.width}`;
      // An open dropdown hides the app shell from assistive technology while its buttons stay focusable (aria-hidden-focus on the
      // sidebar at 1024 in the dark theme): a shell finding recorded in the catalog, so the menu frame is checked for overflow only.
      const settle = async (name: string, axe = true) => {
        expect(
          await frame.evaluate(
            () => document.documentElement.scrollWidth - window.innerWidth,
          ),
          `${name} scrolls sideways`,
        ).toBeLessThanOrEqual(0);
        if (axe) expect(await serious(frame), `${name} axe`).toEqual([]);
        await frame.waitForTimeout(400);
        await frame.screenshot({ path: `${SHOTS}/${name}.png` });
      };
      const modal = () =>
        frame.getByRole("alertdialog").or(frame.getByRole("dialog"));
      await frame.goto(`${ws}/chat/${channel.id}`);
      await expect(frame.getByRole("log")).toBeVisible({ timeout: 30_000 });
      await settle(`${tag}-channel-markup`);
      await frame.getByRole("button", { name: /^Thành viên, / }).click();
      await settle(`${tag}-members`);
      await frame.keyboard.press("Escape");
      await frame.getByRole("button", { name: "Cài đặt kênh" }).click();
      await settle(`${tag}-settings-menu`, false);
      await frame.getByRole("menuitem", { name: "Rời kênh" }).click();
      await expect(modal()).toBeVisible();
      await settle(`${tag}-leave-confirm`);
      await frame.keyboard.press("Escape");
      await frame.getByRole("button", { name: "Cài đặt kênh" }).click();
      await frame.getByRole("menuitem", { name: "Xóa kênh" }).click();
      await expect(modal()).toBeVisible();
      await settle(`${tag}-delete-dialog`);
      await frame.keyboard.press("Escape");
      await context.close();
    }
  }
});

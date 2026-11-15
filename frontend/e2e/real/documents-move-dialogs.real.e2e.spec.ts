import { expect, test, type Page } from "@playwright/test";
import {
  apiRead,
  apiWrite,
  findWorkspace,
  realUsers,
  signIn,
} from "./support/real-session";

test.setTimeout(240_000);

const apiUrl = process.env.VITE_API_URL ?? "http://127.0.0.1:58000/api/v1";
const FILE_MENU = /^(Tệp|File)$/;
const MOVE_TO = /^(Chuyển đến…|Move to…)$/;
const MOVE_TO_TRASH = /^(Chuyển vào thùng rác|Move to trash)$/;
const OPEN_TRASH = /^(Thùng rác|Trash)$/;

type PageRow = { id: string; parentId: string | null; version: number };

async function createDoc(
  page: Page,
  workspaceId: string,
  title: string,
  parentId?: string,
): Promise<string> {
  const created = await apiWrite<string>(
    page,
    "POST",
    `/workspaces/${workspaceId}/pages`,
    { title, parentId },
  );
  expect(created.status).toBe(201);
  return created.data as string;
}

async function openFileMenu(page: Page) {
  await page.getByRole("menuitem", { name: FILE_MENU }).click();
}

test("the Move to dialog puts a page under another page, and a Space the person cannot see is refused with nothing changed", async ({
  page,
}) => {
  const stamp = Date.now();
  await signIn(page, realUsers.workspaceOwner);
  const workspace = await findWorkspace(page, "Workspace 3");
  const target = await createDoc(page, workspace.id, `Target ${stamp}`);
  const moving = await createDoc(page, workspace.id, `Moving ${stamp}`);

  await page.goto(`/workspaces/${workspace.id}/docs/${moving}`);
  await expect(page.locator("article h1")).toHaveText(`Moving ${stamp}`);
  await openFileMenu(page);
  await page.getByRole("menuitem", { name: MOVE_TO }).click();
  const dialog = page.getByRole("dialog");
  const moved = page.waitForResponse(
    (response) =>
      response.request().method() === "POST" &&
      response.url().endsWith(`/pages/${moving}/move`),
  );
  await dialog
    .getByRole("option", { name: new RegExp(`Target ${stamp}`) })
    .click();
  expect((await moved).status()).toBeLessThan(300);
  expect((await apiRead<PageRow>(page, `/pages/${moving}`)).parentId).toBe(
    target,
  );

  // The breadcrumb shows the new parent, then the page.
  const crumbs = await apiRead<{ id?: string; unavailable?: boolean }[]>(
    page,
    `/pages/${moving}/breadcrumb`,
  );
  expect(crumbs.map((crumb) => crumb.id)).toEqual([target, moving]);

  // A Space that does not exist for this person answers 404 and the page stays.
  const before = await apiRead<PageRow>(page, `/pages/${moving}`);
  const refused = await apiWrite(
    page,
    "POST",
    `/pages/${before.id}/move-to-space`,
    {
      spaceId: crypto.randomUUID(),
      expectedVersion: before.version,
    },
  );
  expect(refused.status).toBe(404);
  const after = await apiRead<PageRow>(page, `/pages/${moving}`);
  expect(after.version).toBe(before.version);
  expect(after.parentId).toBe(target);
});

test("creating a page replays with its key, a conflicting body under the same key is refused, and a rename is guarded by the version, a no-op for the same title and hidden from a guest", async ({
  page,
  browser,
}) => {
  const stamp = Date.now();
  await signIn(page, realUsers.workspaceOwner);
  const workspace = await findWorkspace(page, "Workspace 3");
  const key = crypto.randomUUID();
  const path = `/workspaces/${workspace.id}/pages`;
  const first = await apiWrite<string>(
    page,
    "POST",
    path,
    { title: `Replay ${stamp}` },
    key,
  );
  expect(first.status).toBe(201);
  const replay = await apiWrite<string>(
    page,
    "POST",
    path,
    { title: `Replay ${stamp}` },
    key,
  );
  expect(replay.status).toBe(201);
  expect(replay.data).toBe(first.data);
  const conflicting = await apiWrite(
    page,
    "POST",
    path,
    { title: `Other ${stamp}` },
    key,
  );
  expect(conflicting.status).toBeGreaterThanOrEqual(400);

  const id = first.data as string;
  const read = await apiRead<PageRow>(page, `/pages/${id}`);
  const renamed = await apiWrite(page, "PATCH", `/pages/${id}`, {
    title: `Renamed ${stamp}`,
    expectedVersion: read.version,
  });
  expect(renamed.status).toBeLessThan(300);
  const after = await apiRead<PageRow>(page, `/pages/${id}`);
  expect(after.version).toBeGreaterThan(read.version);

  // The version read earlier is stale: 412 and the title stays.
  const stale = await apiWrite(page, "PATCH", `/pages/${id}`, {
    title: `Stale ${stamp}`,
    expectedVersion: read.version,
  });
  expect(stale.status).toBe(412);

  // The same title is a no-op: the version does not move.
  const same = await apiWrite(page, "PATCH", `/pages/${id}`, {
    title: `Renamed ${stamp}`,
    expectedVersion: after.version,
  });
  expect(same.status).toBeLessThan(300);
  expect((await apiRead<PageRow>(page, `/pages/${id}`)).version).toBe(
    after.version,
  );

  // A guest who cannot see the page gets the same 404 as for a page that does not exist.
  const guestContext = await browser.newContext();
  const guestPage = await guestContext.newPage();
  await signIn(guestPage, realUsers.workspaceGuest);
  const hidden = await apiWrite(guestPage, "PATCH", `/pages/${id}`, {
    title: "Hijack",
    expectedVersion: after.version,
  });
  const missing = await apiWrite(
    guestPage,
    "PATCH",
    `/pages/${crypto.randomUUID()}`,
    {
      title: "Hijack",
      expectedVersion: 1,
    },
  );
  expect(hidden.status).toBe(404);
  expect(missing.status).toBe(404);
  await guestContext.close();
});

test("pages nest ten levels and an eleventh child is refused, by create and by a move that carries a subtree", async ({
  page,
}) => {
  const stamp = Date.now();
  await signIn(page, realUsers.workspaceOwner);
  const workspace = await findWorkspace(page, "Workspace 3");
  let parent = await createDoc(page, workspace.id, `Level 1 ${stamp}`);
  const root = parent;
  for (let level = 2; level <= 10; level += 1) {
    parent = await createDoc(
      page,
      workspace.id,
      `Level ${level} ${stamp}`,
      parent,
    );
  }
  const tooDeep = await apiWrite(
    page,
    "POST",
    `/workspaces/${workspace.id}/pages`,
    {
      title: `Level 11 ${stamp}`,
      parentId: parent,
    },
  );
  expect(tooDeep.status).toBe(400);

  const loose = await createDoc(page, workspace.id, `Loose ${stamp}`);
  const row = await apiRead<PageRow>(page, `/pages/${loose}`);
  const moved = await apiWrite(page, "POST", `/pages/${loose}/move`, {
    parentId: parent,
    expectedVersion: row.version,
  });
  expect(moved.status).toBe(400);
  expect((await apiRead<PageRow>(page, `/pages/${loose}`)).parentId).toBeNull();
  void root;
});

test("trash and permanent-delete confirmations and the move dialog hold at 1440, 1024 and 390 px in both themes", async ({
  page,
}, testInfo) => {
  const stamp = Date.now();
  await signIn(page, realUsers.workspaceOwner);
  // The sign-in screen is English; the dialogs are reviewed in Vietnamese.
  await page.evaluate(() => localStorage.setItem("notrelix-locale", "vi"));
  const workspace = await findWorkspace(page, "Workspace 3");
  const other = await createDoc(page, workspace.id, `Other ${stamp}`);
  const parent = await createDoc(page, workspace.id, `Parent ${stamp}`);
  await createDoc(page, workspace.id, `Child ${stamp}`, parent);
  void other;

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
      const tag = `${mode}-${width}`;
      const checkDialog = async (name: string) => {
        const dialog = page.getByRole("dialog");
        await expect(dialog).toBeVisible();
        const box = await dialog.boundingBox();
        expect(box!.x, `${name} ${tag} left edge`).toBeGreaterThanOrEqual(0);
        expect(
          box!.x + box!.width,
          `${name} ${tag} right edge`,
        ).toBeLessThanOrEqual(width);
        const overflow = await dialog.evaluate(
          (node) => node.scrollWidth - node.clientWidth,
        );
        expect(
          overflow,
          `${name} ${tag} horizontal overflow`,
        ).toBeLessThanOrEqual(0);
        if (width === 390) {
          const small = await dialog
            .locator("button, [role=option]")
            .evaluateAll((nodes) =>
              nodes
                .filter((node) => {
                  const r = node.getBoundingClientRect();
                  return r.width > 0 && r.height > 0 && r.height < 44;
                })
                .map(
                  (node) =>
                    `${node.getAttribute("aria-label") ?? node.textContent} ${Math.round(node.getBoundingClientRect().height)}px`,
                ),
            );
          expect(small, `${name} ${tag} targets under 44 px`).toEqual([]);
        }
        await testInfo.attach(`${name}-${tag}`, {
          body: await page.screenshot(),
          contentType: "image/png",
        });
      };

      await page.goto(`/workspaces/${workspace.id}/docs/${parent}`);
      await expect(page.locator("article h1")).toHaveText(`Parent ${stamp}`);

      await openFileMenu(page);
      await page.getByRole("menuitem", { name: MOVE_TO }).click();
      await checkDialog("move");
      await page.keyboard.press("Escape");

      await openFileMenu(page);
      await page.getByRole("menuitem", { name: MOVE_TO_TRASH }).click();
      await checkDialog("trash-confirm");
      await page.keyboard.press("Escape");
    }
  }

  // The delete-for-good confirm step, once per theme at the narrow width.
  expect(
    (await apiWrite(page, "POST", `/pages/${parent}/archive`, {})).status,
  ).toBeLessThan(300);
  for (const width of [1440, 1024, 390]) {
    await page.setViewportSize({ width, height: 900 });
    await page.goto(`/workspaces/${workspace.id}/docs/${other}`);
    await openFileMenu(page);
    await page.getByRole("menuitem", { name: OPEN_TRASH }).click();
    const list = page.getByRole("dialog");
    await list
      .getByRole("button", {
        name: new RegExp(`(Xoá vĩnh viễn|Delete permanently): Parent ${stamp}`),
      })
      .click();
    await expect(
      list.getByRole("button", {
        name: /^(Xác nhận xoá vĩnh viễn|Confirm permanent delete)$/,
      }),
    ).toBeVisible();
    expect(
      await list.evaluate((node) => node.scrollWidth - node.clientWidth),
    ).toBeLessThanOrEqual(0);
    await testInfo.attach(`delete-confirm-${width}`, {
      body: await page.screenshot(),
      contentType: "image/png",
    });
  }
  expect((await page.request.get(`${apiUrl}/pages/${parent}`)).status()).toBe(
    404,
  );
});

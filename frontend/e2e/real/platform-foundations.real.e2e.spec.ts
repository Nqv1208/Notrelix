import { execFileSync } from "node:child_process";
import http from "node:http";
import { randomBytes } from "node:crypto";
import { expect, test, type Page } from "@playwright/test";
import { apiRead, apiWrite, realUsers, signIn } from "./support/real-session";
import { createOwnedWorkspace, joinAs } from "./support/real-workspace";

/**
 * The foundations every product capability stands on, proven on the real backend: the role matrix and the
 * identical NotFound (workspace and resource authorization), the retry and the stale write, and the realtime
 * gateway with its reconnect and gap recovery.
 */
test.setTimeout(300_000);
test.use({ viewport: { width: 1440, height: 900 } });

const apiUrl = process.env.VITE_API_URL ?? "http://127.0.0.1:58000/api/v1";
const wsUrl = apiUrl.replace(/^http/, "ws") + "/realtime";
const madeUp = "00000000-0000-4000-8000-0000000000bb";

async function probe(page: Page, path: string) {
  const response = await page.request.get(`${apiUrl}${path}`);
  return { status: response.status(), text: await response.text() };
}

/** A NotFound body with the ids and the trace removed: what is left must not tell hidden from missing. */
function shape(text: string, ...ids: string[]) {
  let out = text;
  for (const id of ids) out = out.split(id).join("<id>");
  const body = JSON.parse(out) as Record<string, unknown>;
  delete body.traceId;
  delete body.requestId;
  delete body.instance;
  return body;
}

/** A workspace of the admin with a Member, a Guest, a board holding one item and a person outside it. */
async function scene(browser: import("@playwright/test").Browser, page: Page) {
  await signIn(page, realUsers.admin);
  const workspace = await createOwnedWorkspace(page, "Foundations");
  const member = await joinAs(
    browser,
    page,
    workspace.id,
    realUsers.workspaceAdmin,
    "Member",
  );
  const guest = await joinAs(
    browser,
    page,
    workspace.id,
    realUsers.guest,
    "Guest",
  );
  const strangerContext = await browser.newContext();
  const stranger = await strangerContext.newPage();
  await signIn(stranger, realUsers.workspaceOwner);

  const unique = Date.now().toString(36);
  const created = await apiWrite(
    page,
    "POST",
    `/workspaces/${workspace.id}/boards`,
    { title: `Foundations ${unique}` },
  );
  expect(created.status, "create board").toBeLessThan(300);
  const board = (
    await apiRead<{ id: string; title: string }[]>(
      page,
      `/workspaces/${workspace.id}/boards`,
    )
  ).find((candidate) => candidate.title === `Foundations ${unique}`)!;
  expect(
    (
      await apiWrite(page, "POST", `/boards/${board.id}/groups`, {
        title: "Todo",
      })
    ).status,
  ).toBeLessThan(300);
  const groupId = (
    await apiRead<{ lists: { id: string }[] }>(page, `/boards/${board.id}/full`)
  ).lists[0]!.id;
  const item = await apiWrite<{ id: string }>(
    page,
    "POST",
    `/boards/${board.id}/items`,
    { groupId, title: `First ${unique}` },
  );
  expect(item.status).toBeLessThan(300);
  return {
    workspace,
    board,
    groupId,
    itemId: item.data!.id,
    unique,
    member,
    guest,
    stranger,
    close: async () => {
      await member.context.close();
      await guest.context.close();
      await strangerContext.close();
    },
  };
}

test("the role matrix decides a workspace action, and a hidden workspace, board, item and write answer exactly like a made-up id", async ({
  browser,
  page,
}) => {
  const s = await scene(browser, page);
  const roles = `/workspaces/${s.workspace.id}/roles`;

  // Role baseline: the Owner may list roles, a Member and a Guest see the workspace but lack the action.
  expect((await probe(page, roles)).status).toBe(200);
  expect((await probe(s.member.page, roles)).status).toBe(403);
  expect((await probe(s.guest.page, roles)).status).toBe(403);

  // A person outside the workspace gets the answer of a made-up workspace, on a read and on a role action.
  const hiddenWorkspace = await probe(s.stranger, roles);
  const missingWorkspace = await probe(
    s.stranger,
    `/workspaces/${madeUp}/roles`,
  );
  expect(hiddenWorkspace.status).toBe(404);
  expect(missingWorkspace.status).toBe(404);
  expect(shape(hiddenWorkspace.text, s.workspace.id)).toEqual(
    shape(missingWorkspace.text, madeUp),
  );

  // A Guest holds no grant on the board: the board, its item and its list answer as if they did not exist.
  const hiddenBoard = await probe(s.guest.page, `/boards/${s.board.id}`);
  const missingBoard = await probe(s.guest.page, `/boards/${madeUp}`);
  expect(hiddenBoard.status).toBe(404);
  expect(missingBoard.status).toBe(404);
  expect(shape(hiddenBoard.text, s.board.id)).toEqual(
    shape(missingBoard.text, madeUp),
  );
  const hiddenItem = await probe(s.guest.page, `/board-items/${s.itemId}`);
  const missingItem = await probe(s.guest.page, `/board-items/${madeUp}`);
  expect(hiddenItem.status).toBe(404);
  expect(missingItem.status).toBe(404);
  expect(shape(hiddenItem.text, s.itemId)).toEqual(
    shape(missingItem.text, madeUp),
  );
  // The collection returns what a single read would allow.
  expect(
    await apiRead(s.guest.page, `/workspaces/${s.workspace.id}/boards`),
  ).toEqual([]);
  expect(
    (
      await apiRead<{ id: string }[]>(
        s.member.page,
        `/workspaces/${s.workspace.id}/boards`,
      )
    ).map((board) => board.id),
  ).toContain(s.board.id);

  // A write to the hidden board is refused the same way and changes nothing.
  const before = await apiRead<{ lists: { items?: unknown[] }[] }>(
    page,
    `/boards/${s.board.id}/full`,
  );
  const refused = await apiWrite(
    s.guest.page,
    "POST",
    `/boards/${s.board.id}/items`,
    { groupId: s.groupId, title: "Guest write" },
  );
  const missing = await apiWrite(
    s.guest.page,
    "POST",
    `/boards/${madeUp}/items`,
    { groupId: s.groupId, title: "Guest write" },
  );
  expect(refused.status).toBe(404);
  expect(refused.status).toBe(missing.status);
  expect(await apiRead(page, `/boards/${s.board.id}/full`)).toEqual(before);

  await s.close();
});

test("a retry with the same key has no second effect, a write on an old version changes nothing, and a write without a key is refused", async ({
  browser,
  page,
}) => {
  const s = await scene(browser, page);
  const title = `Once ${s.unique}`;
  const key = crypto.randomUUID();
  const create = () =>
    apiWrite<{ id: string }>(
      page,
      "POST",
      `/boards/${s.board.id}/items`,
      { groupId: s.groupId, title },
      key,
    );
  const first = await create();
  const second = await create();
  expect(first.status).toBeLessThan(300);
  expect(second.status).toBe(first.status);
  expect(second.data?.id).toBe(first.data?.id);
  const titles = async () =>
    JSON.stringify(await apiRead(page, `/boards/${s.board.id}/full`));
  expect((await titles()).split(`"${title}"`).length - 1).toBe(1);

  // A write without a key is a typed refusal and creates nothing.
  const csrf = (await (
    await page.request.get(`${apiUrl}/auth/csrf`)
  ).json()) as { token: string };
  const keyless = await page.request.fetch(
    `${apiUrl}/boards/${s.board.id}/items`,
    {
      method: "POST",
      headers: {
        "X-CSRF-Token": csrf.token,
        "Content-Type": "application/json",
      },
      data: JSON.stringify({
        groupId: s.groupId,
        title: `Keyless ${s.unique}`,
      }),
    },
  );
  expect(keyless.status()).toBeGreaterThanOrEqual(400);
  expect(keyless.status()).toBeLessThan(500);
  expect(await titles()).not.toContain(`Keyless ${s.unique}`);

  // Stale write: the second writer names a version that is gone.
  const seen = await apiRead<{ version: number }>(
    page,
    `/board-items/${s.itemId}`,
  );
  const won = await apiWrite(page, "PATCH", `/board-items/${s.itemId}`, {
    title: `Won ${s.unique}`,
    expectedVersion: seen.version,
  });
  expect(won.status).toBeLessThan(300);
  const afterWin = await apiRead<{ version: number; title: string }>(
    page,
    `/board-items/${s.itemId}`,
  );
  const stale = await apiWrite(page, "PATCH", `/board-items/${s.itemId}`, {
    title: `Lost ${s.unique}`,
    expectedVersion: seen.version,
  });
  expect(stale.status).toBe(412);
  expect(await apiRead(page, `/board-items/${s.itemId}`)).toMatchObject({
    version: afterWin.version,
  });
  expect(await titles()).toContain(`Won ${s.unique}`);
  expect(await titles()).not.toContain(`Lost ${s.unique}`);

  await s.close();
});

/** Status of a raw WebSocket upgrade to the gateway: the browser's own refusal reasons are not visible to a page. */
function upgrade(origin: string | null, cookie: string | null) {
  const url = new URL(wsUrl.replace(/^ws/, "http"));
  return new Promise<number>((resolve, reject) => {
    const request = http.request({
      host: url.hostname,
      port: url.port,
      path: url.pathname,
      headers: {
        Connection: "Upgrade",
        Upgrade: "websocket",
        "Sec-WebSocket-Version": "13",
        "Sec-WebSocket-Key": randomBytes(16).toString("base64"),
        ...(origin ? { Origin: origin } : {}),
        ...(cookie ? { Cookie: cookie } : {}),
      },
    });
    request.on("upgrade", (_response, socket) => {
      socket.destroy();
      resolve(101);
    });
    request.on("response", (response) => {
      response.resume();
      resolve(response.statusCode ?? 0);
    });
    request.on("error", reject);
    request.end();
  });
}

test("the realtime gateway refuses a foreign origin and an unauthenticated upgrade, and a member sees a change live", async ({
  browser,
  page,
}) => {
  const s = await scene(browser, page);
  const cookies = (await s.member.context.cookies())
    .map((cookie) => `${cookie.name}=${cookie.value}`)
    .join("; ");
  const appOrigin = new URL(page.url()).origin;

  expect(await upgrade("http://evil.example", cookies)).toBe(403);
  expect(await upgrade(appOrigin, null)).toBe(401);
  expect(await upgrade(appOrigin, cookies)).toBe(101);

  // A change made by the owner reaches the member's open board without a reload.
  await s.member.page.goto(
    `/workspaces/${s.workspace.id}/boards/${s.board.id}`,
  );
  await expect(
    s.member.page.getByText(`First ${s.unique}`).first(),
  ).toBeVisible();
  const live = `Live ${s.unique}`;
  expect(
    (
      await apiWrite(page, "POST", `/boards/${s.board.id}/items`, {
        groupId: s.groupId,
        title: live,
      })
    ).status,
  ).toBeLessThan(300);
  await expect(s.member.page.getByText(live).first()).toBeVisible({
    timeout: 20_000,
  });

  await s.close();
});

test("after the connection drops the client reconnects, subscribes again and shows what changed meanwhile; after a lost event it recovers over REST", async ({
  browser,
  page,
}) => {
  const s = await scene(browser, page);
  const frames: string[] = [];
  const sockets: { close: () => Promise<void> }[] = [];
  let dropNextEvent = false;
  let dropped = 0;
  await s.member.page.routeWebSocket(/\/realtime/, (ws) => {
    const server = ws.connectToServer();
    sockets.push({
      close: async () => {
        await ws.close({ code: 1012, reason: "e2e connection drop" });
        await server.close();
      },
    });
    server.onMessage((message) => {
      const text = String(message);
      if (dropNextEvent && text.includes('"type":"event"')) {
        dropNextEvent = false;
        dropped += 1;
        return;
      }
      frames.push(text);
      ws.send(message);
    });
  });
  const subscribedCount = () =>
    frames.filter((frame) => frame.includes('"type":"subscribed"')).length;

  await s.member.page.goto(
    `/workspaces/${s.workspace.id}/boards/${s.board.id}`,
  );
  await expect(
    s.member.page.getByText(`First ${s.unique}`).first(),
  ).toBeVisible();
  // The page holds one subscription per scope it shows (the workspace and the board).
  await expect.poll(subscribedCount, { timeout: 20_000 }).toBeGreaterThan(0);
  await expect
    .poll(async () => {
      const seen = subscribedCount();
      await new Promise((resolve) => setTimeout(resolve, 2000));
      return subscribedCount() === seen ? seen : -1;
    })
    .toBeGreaterThan(0);
  const subscribedBefore = subscribedCount();
  const add = async (title: string) =>
    expect(
      (
        await apiWrite(page, "POST", `/boards/${s.board.id}/items`, {
          groupId: s.groupId,
          title,
        })
      ).status,
    ).toBeLessThan(300);

  // Drop: the socket closes, the owner writes while the member is offline, the member reconnects and sees it.
  await sockets.at(-1)!.close();
  const meanwhile = `Meanwhile ${s.unique}`;
  await add(meanwhile);
  await expect
    .poll(subscribedCount, { timeout: 30_000 })
    .toBeGreaterThanOrEqual(subscribedBefore * 2);
  await expect(s.member.page.getByText(meanwhile).first()).toBeVisible({
    timeout: 30_000,
  });

  // Gap: one event never arrives; the next one shows the gap and the board is read again over REST.
  const lost = `Lost event ${s.unique}`;
  const next = `After gap ${s.unique}`;
  dropNextEvent = true;
  await add(lost);
  await expect.poll(() => dropped, { timeout: 20_000 }).toBe(1);
  await add(next);
  await expect(s.member.page.getByText(lost).first()).toBeVisible({
    timeout: 30_000,
  });
  await expect(s.member.page.getByText(next).first()).toBeVisible();

  await s.close();
});

test("a manager shares a board and a page with a person, changes the level on the current version, and removing the access ends it at once", async ({
  browser,
  page,
}) => {
  const s = await scene(browser, page);
  const guestId = (await apiRead<{ id: string }>(s.guest.page, "/auth/me")).id;

  const created = await apiWrite<string>(
    page,
    "POST",
    `/workspaces/${s.workspace.id}/pages`,
    { title: `Shared page ${s.unique}` },
  );
  expect(created.status).toBe(201);
  const pageId = created.data as string;

  for (const [kind, id, read] of [
    ["work-management.board", s.board.id, `/boards/${s.board.id}`],
    ["documents.page", pageId, `/pages/${pageId}`],
  ] as const) {
    const permissions = `/resources/${kind}/${id}/permissions`;
    // Before: the Guest holds nothing and the resource answers like a missing one.
    expect((await probe(s.guest.page, read)).status).toBe(404);

    // Grant: the person gains the level on their next request.
    const granted = await apiWrite<{ id: string; version: number }>(
      page,
      "POST",
      permissions,
      { subjectType: "User", subjectId: guestId, level: "Viewer" },
    );
    expect(granted.status, kind).toBeLessThan(300);
    expect((await probe(s.guest.page, read)).status).toBe(200);

    // The same level again is a no-op: the same entry, no new version.
    const again = await apiWrite<{ id: string; version: number }>(
      page,
      "POST",
      permissions,
      { subjectType: "User", subjectId: guestId, level: "Viewer" },
    );
    expect(again.data).toMatchObject({
      id: granted.data!.id,
      version: granted.data!.version,
    });

    // A level change names the current version: a missing or old one is refused and changes nothing.
    const refused = await apiWrite(page, "POST", permissions, {
      subjectType: "User",
      subjectId: guestId,
      level: "Editor",
    });
    expect(refused.status).toBe(412);
    expect(JSON.stringify(refused.data)).toContain(
      "governance.permission.version-conflict",
    );
    const changed = await apiWrite<{ level: string; version: number }>(
      page,
      "POST",
      permissions,
      {
        subjectType: "User",
        subjectId: guestId,
        level: "Editor",
        expectedVersion: granted.data!.version,
      },
    );
    expect(changed.status).toBeLessThan(300);
    expect(changed.data?.level).toBe("Editor");
    const stale = await apiWrite(page, "POST", permissions, {
      subjectType: "User",
      subjectId: guestId,
      level: "Commenter",
      expectedVersion: granted.data!.version,
    });
    expect(stale.status).toBe(412);

    // Remove: access ends at once and the answer is the one of a missing resource; a repeat succeeds and changes nothing.
    const removal = `${permissions}/${granted.data!.id}`;
    expect((await apiWrite(page, "DELETE", removal)).status).toBe(204);
    expect((await probe(s.guest.page, read)).status).toBe(404);
    expect((await apiWrite(page, "DELETE", removal)).status).toBeLessThan(300);
    expect((await probe(s.guest.page, read)).status).toBe(404);
  }

  await s.close();
});

test("removing a person's access to a board ends it in their open tab without a reload and leaves nothing of the board in their cache", async ({
  browser,
  page,
}) => {
  const s = await scene(browser, page);
  const guestId = (await apiRead<{ id: string }>(s.guest.page, "/auth/me")).id;
  const permissions = `/resources/work-management.board/${s.board.id}/permissions`;
  const granted = await apiWrite<{ id: string }>(page, "POST", permissions, {
    subjectType: "User",
    subjectId: guestId,
    level: "Viewer",
  });
  expect(granted.status).toBeLessThan(300);

  // The guest has the board open: it shows the board.
  const boardUrl = `/workspaces/${s.workspace.id}/boards/${s.board.id}`;
  await s.guest.page.goto(`/workspaces/${s.workspace.id}`);
  await s.guest.page.goto(boardUrl);
  const title = s.guest.page.getByRole("heading", {
    level: 1,
    name: s.board.title,
  });
  await expect(title).toBeVisible({ timeout: 20_000 });

  // The manager removes the access. The open tab drops the board on its own (the access signal of the realtime
  // gateway revalidates the workspace), with no reload and no navigation.
  const reloads: string[] = [];
  s.guest.page.on("framenavigated", (frame) => {
    if (frame === s.guest.page.mainFrame()) reloads.push(frame.url());
  });
  const removed = await apiWrite(
    page,
    "DELETE",
    `${permissions}/${granted.data!.id}`,
  );
  expect(removed.status).toBeLessThan(300);
  await expect(title).toBeHidden({ timeout: 30_000 });
  expect(reloads, "the tab was not reloaded").toEqual([]);
  expect((await probe(s.guest.page, `/boards/${s.board.id}`)).status).toBe(404);

  // Coming back to the board inside the app (history, no reload) does not serve it from the cache.
  await s.guest.page.goBack();
  await s.guest.page.goForward();
  await expect(title).toBeHidden();

  await s.close();
});

/** The Redis of this e2e stack only; the stack's compose project owns it. */
const redisContainer = "notrelix-fe-real-e2e-redis";

test("with Redis down every authenticated request is refused with a retryable 503 that changes nothing, the app offers a retry, and all recovers when Redis is back", async ({
  browser,
  page,
}) => {
  const s = await scene(browser, page);
  await page.goto(`/workspaces/${s.workspace.id}/boards/${s.board.id}`);
  await expect(
    page.getByRole("heading", { level: 1, name: s.board.title }),
  ).toBeVisible();
  const csrf = (await (
    await page.request.get(`${apiUrl}/auth/csrf`)
  ).json()) as { token: string };
  const item = `/board-items/${s.itemId}`;
  const seen = await apiRead<{ version: number }>(page, item);
  const boardBefore = JSON.stringify(
    await apiRead(page, `/boards/${s.board.id}/full`),
  );

  execFileSync("docker", ["stop", redisContainer], { stdio: "pipe" });
  try {
    // Revocation cannot be checked, so the request is refused (never let through) and says to retry.
    const read = await page.request.get(`${apiUrl}${item}`);
    expect(read.status()).toBe(503);
    expect(read.headers()["retry-after"]).toBeTruthy();
    expect(await read.json()).toMatchObject({
      status: 503,
      errorCode: "auth.revocation_unavailable",
    });
    const write = await page.request.fetch(`${apiUrl}${item}`, {
      method: "PATCH",
      headers: {
        "X-CSRF-Token": csrf.token,
        "Idempotency-Key": crypto.randomUUID(),
        "Content-Type": "application/json",
      },
      data: JSON.stringify({
        title: `Down ${s.unique}`,
        expectedVersion: seen.version,
      }),
    });
    expect(write.status()).toBe(503);

    // The app keeps the person signed in and offers a retry instead of a sign-in page or a blank screen.
    await page.reload();
    // Each read waits out the 5 s Redis connect backlog before it answers 503.
    await expect(page.getByRole("alert")).toContainText(
      "Could not check your session",
      { timeout: 30_000 },
    );
    expect(new URL(page.url()).pathname).toContain(s.board.id);
  } finally {
    execFileSync("docker", ["start", redisContainer], { stdio: "pipe" });
  }

  await expect
    .poll(async () => (await page.request.get(`${apiUrl}${item}`)).status(), {
      timeout: 90_000,
      intervals: [1_000],
    })
    .toBe(200);
  await page.getByRole("button", { name: "Try again", exact: true }).click();
  await expect(
    page.getByRole("heading", { level: 1, name: s.board.title }),
  ).toBeVisible();

  // The refused write changed nothing.
  expect(await apiRead(page, item)).toMatchObject({ version: seen.version });
  expect(
    JSON.stringify(await apiRead(page, `/boards/${s.board.id}/full`)),
  ).toBe(boardBefore);

  await s.close();
});

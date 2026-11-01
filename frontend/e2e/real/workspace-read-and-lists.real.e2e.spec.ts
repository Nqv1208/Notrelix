import { expect, test, type Page } from "@playwright/test";
import { apiRead, apiWrite, realUsers, signIn } from "./support/real-session";
import {
  createOwnedWorkspace,
  joinAs,
  membersOf,
} from "./support/real-workspace";

test.setTimeout(240_000);

const apiUrl = process.env.VITE_API_URL ?? "http://127.0.0.1:58000/api/v1";
const wsUrl = apiUrl.replace(/^http/, "ws") + "/realtime";

interface Summary {
  readonly id: string;
  readonly name: string;
}
interface AccountRow extends Summary {
  readonly role: string;
  readonly memberCount: number | null;
  readonly departmentCount: number | null;
  readonly isDefault: boolean;
  readonly canLeave: boolean;
}

async function problemOf(page: Page, path: string) {
  const response = await page.request.get(`${apiUrl}${path}`);
  const text = await response.text();
  return { status: response.status(), body: text ? JSON.parse(text) : null };
}

/** Subscribes to one workspace on the realtime socket and reports what the server answers, then what it does next. */
async function subscribe(page: Page, workspaceId: string, waitMs: number) {
  return page.evaluate(
    ({ url, id, wait }) =>
      new Promise<{ frames: string[]; closed: number | null }>((resolve) => {
        const frames: string[] = [];
        const socket = new WebSocket(url);
        let closed: number | null = null;
        socket.onopen = () =>
          socket.send(
            JSON.stringify({
              type: "subscribe",
              subscriptionId: "e2e-1",
              scope: { kind: "workspace", id },
            }),
          );
        socket.onmessage = (event) => frames.push(String(event.data));
        socket.onclose = (event) => {
          closed = event.code;
        };
        setTimeout(() => {
          socket.close();
          resolve({ frames, closed });
        }, wait);
      }),
    { url: wsUrl, id: workspaceId, wait: waitMs },
  );
}

test("a member reads their workspace and sees it in both lists; a stranger and a removed member get the answer of a made-up id", async ({
  browser,
  page,
}) => {
  await signIn(page, realUsers.admin);
  const workspace = await createOwnedWorkspace(page, "Read lists");
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
    realUsers.workspaceGuest,
    "Guest",
  );
  const strangerContext = await browser.newContext();
  const stranger = await strangerContext.newPage();
  await signIn(stranger, realUsers.guest);

  // B01 main flow: an active member, a Guest too, reads the workspace with their role.
  const read = await apiRead<Summary & Record<string, unknown>>(
    member.page,
    `/workspaces/${workspace.id}`,
  );
  expect(read).toMatchObject({ id: workspace.id, name: workspace.name });
  expect(
    (await apiRead<Summary>(guest.page, `/workspaces/${workspace.id}`)).id,
  ).toBe(workspace.id);

  // B04 and I02 main flow: both lists carry the workspace for a member.
  const listed = await apiRead<Summary[]>(member.page, "/workspaces");
  expect(listed.map((row) => row.id)).toContain(workspace.id);
  const account = await apiRead<AccountRow[]>(
    member.page,
    "/account/workspaces",
  );
  expect(account.find((row) => row.id === workspace.id)).toMatchObject({
    role: "Member",
    canLeave: true,
  });
  // Counts: a Guest sees no member or department count; an Owner sees both.
  const asGuest = (
    await apiRead<AccountRow[]>(guest.page, "/account/workspaces")
  ).find((row) => row.id === workspace.id);
  expect(asGuest?.memberCount ?? null).toBeNull();
  expect(asGuest?.departmentCount ?? null).toBeNull();
  const asOwner = (
    await apiRead<AccountRow[]>(page, "/account/workspaces")
  ).find((row) => row.id === workspace.id);
  expect(asOwner?.memberCount).toBe(3);
  expect(asOwner?.departmentCount).not.toBeNull();

  // The person's own pages show it: the rail list and the account list.
  await member.page.goto(`/workspaces/${workspace.id}`);
  await member.page
    .getByRole("button", { name: /open the Notrelix system rail/ })
    .click();
  await expect(
    member.page.getByRole("button", { name: workspace.name, exact: true }),
  ).toBeVisible();
  await member.page.goto(`/workspaces/${workspace.id}/account/workspaces`);
  await expect(member.page.getByText(workspace.name).first()).toBeVisible();

  // B01 and I02 rejection: a signed-in stranger sees nothing of it, and cannot tell it from a made-up id.
  const madeUp = "00000000-0000-4000-8000-0000000000aa";
  const hidden = await problemOf(stranger, `/workspaces/${workspace.id}`);
  const missing = await problemOf(stranger, `/workspaces/${madeUp}`);
  expect(hidden.status).toBe(404);
  expect(hidden.status).toBe(missing.status);
  expect(Object.keys(hidden.body ?? {}).sort()).toEqual(
    Object.keys(missing.body ?? {}).sort(),
  );
  expect(
    (await apiRead<Summary[]>(stranger, "/workspaces")).map((row) => row.id),
  ).not.toContain(workspace.id);
  expect(
    (await apiRead<AccountRow[]>(stranger, "/account/workspaces")).map(
      (row) => row.id,
    ),
  ).not.toContain(workspace.id);

  // A removed member gets the same answer at once and the workspace leaves both of their lists.
  const mine = await apiRead<{ userId: string; version: number }>(
    member.page,
    `/workspaces/${workspace.id}/members/me`,
  );
  expect(
    (
      await apiWrite(
        page,
        "DELETE",
        `/workspaces/${workspace.id}/members/${mine.userId}?expectedVersion=${mine.version}`,
      )
    ).status,
  ).toBeLessThan(300);
  const afterRemoval = await problemOf(
    member.page,
    `/workspaces/${workspace.id}`,
  );
  expect(afterRemoval.status).toBe(404);
  expect(
    (await apiRead<Summary[]>(member.page, "/workspaces")).map((row) => row.id),
  ).not.toContain(workspace.id);
  expect(
    (await apiRead<AccountRow[]>(member.page, "/account/workspaces")).map(
      (row) => row.id,
    ),
  ).not.toContain(workspace.id);
  expect(
    (await membersOf(page, workspace.id)).map((row) => row.userId),
  ).not.toContain(mine.userId);

  await strangerContext.close();
  await member.context.close();
  await guest.context.close();
});

test("the realtime gateway refuses a stranger's subscription to a workspace and ends a removed member's within its check interval", async ({
  browser,
  page,
}) => {
  await signIn(page, realUsers.admin);
  const workspace = await createOwnedWorkspace(page, "Realtime scope");
  const member = await joinAs(
    browser,
    page,
    workspace.id,
    realUsers.workspaceAdmin,
    "Member",
  );
  const strangerContext = await browser.newContext();
  const stranger = await strangerContext.newPage();
  await signIn(stranger, realUsers.guest);
  await stranger.goto("/");
  await member.page.goto("/");

  // Rejection: a non-member's subscription is denied and no event frame is sent.
  const denied = await subscribe(stranger, workspace.id, 3000);
  expect(denied.frames.join("\n")).toContain("subscription_denied");
  expect(denied.frames.join("\n")).not.toContain('"subscribed"');

  // Main flow: a member's subscription is acknowledged and stays open.
  const mine = await apiRead<{ userId: string; version: number }>(
    member.page,
    `/workspaces/${workspace.id}/members/me`,
  );
  const open = member.page.evaluate(
    ({ url, id }) =>
      new Promise<string[]>((resolve) => {
        const frames: string[] = [];
        const socket = new WebSocket(url);
        socket.onopen = () =>
          socket.send(
            JSON.stringify({
              type: "subscribe",
              subscriptionId: "e2e-2",
              scope: { kind: "workspace", id },
            }),
          );
        socket.onmessage = (event) => {
          frames.push(String(event.data));
          // The server revokes it, or closes the socket, once the member is gone.
          if (String(event.data).includes("subscription_revoked")) {
            socket.close();
            resolve(frames);
          }
        };
        socket.onclose = () => resolve(frames);
        setTimeout(() => {
          socket.close();
          resolve(frames);
        }, 110_000);
      }),
    { url: wsUrl, id: workspace.id },
  );

  // The member is removed while the socket is open.
  await page.waitForTimeout(3000);
  expect(
    (
      await apiWrite(
        page,
        "DELETE",
        `/workspaces/${workspace.id}/members/${mine.userId}?expectedVersion=${mine.version}`,
      )
    ).status,
  ).toBeLessThan(300);
  const frames = (await open).join("\n");
  expect(frames).toContain('"subscribed"');
  expect(frames).toContain("subscription_revoked");

  await strangerContext.close();
  await member.context.close();
});

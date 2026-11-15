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

// Workspace 3: `admin` is a plain Member (not its Owner or Admin), `guest` (user5) a Member, and
// `workspaceGuest` (user6) a Guest. A plain member's only rights on a page come from the page itself.
const creator = realUsers.admin;
const viewerMember = realUsers.guest;
const guest = realUsers.workspaceGuest;

interface Grant {
  id: string;
  subjectType: string;
  subjectId: string;
  level: string;
  isRevoked: boolean;
}

async function as(
  browser: import("@playwright/test").Browser,
  user: (typeof realUsers)[keyof typeof realUsers],
): Promise<Page> {
  const page = await (await browser.newContext()).newPage();
  await signIn(page, user);
  return page;
}

async function userId(page: Page): Promise<string> {
  const me = await apiRead<{ id: string }>(page, "/auth/me");
  return me.id;
}

const read = async (page: Page, path: string) => {
  const response = await page.request.get(`${apiUrl}${path}`);
  return { status: response.status(), body: await response.text() };
};

test("a plain member who creates a page is its Manager and shares it; a person without access gets the answer of a made-up id, in the API and on screen; each level opens only what it allows", async ({
  browser,
}) => {
  const stamp = Date.now();
  const owner = await as(browser, creator);
  const workspace = await findWorkspace(owner, "Workspace 3");
  const created = await apiWrite<string>(
    owner,
    "POST",
    `/workspaces/${workspace.id}/pages`,
    { title: `Permissions ${stamp}` },
  );
  expect(created.status).toBe(201);
  const pageId = created.data as string;
  const ownerId = await userId(owner);
  const block = await apiWrite<{ id: string; version: number }>(
    owner,
    "POST",
    `/pages/${pageId}/blocks`,
    {
      type: "Text",
      content: { v: 1, runs: [{ text: `line ${stamp}`, marks: [] }] },
    },
  );
  expect(block.status).toBe(201);

  // The creator holds a stored Manager entry, and with it may share the page.
  const grants = await apiRead<Grant[]>(
    owner,
    `/resources/documents.page/${pageId}/permissions`,
  );
  expect(
    grants.some(
      (g) => g.subjectId === ownerId && g.level === "Manager" && !g.isRevoked,
    ),
  ).toBe(true);

  // A person with no access (a Guest, and a Member who is not the creator) gets the answer of a made-up id.
  const guestPage = await as(browser, guest);
  const memberPage = await as(browser, viewerMember);
  const madeUp = crypto.randomUUID();
  for (const outsider of [guestPage, memberPage]) {
    const none = await read(outsider, `/pages/${madeUp}`);
    const hidden = await read(outsider, `/pages/${pageId}`);
    expect(none.status).toBe(404);
    expect(hidden.status).toBe(404);
    expect(hidden.body.replaceAll(pageId, madeUp)).toBe(none.body);
  }
  // Nor may they share it: no one but a Manager grants.
  const outsiderGrant = await apiWrite(
    memberPage,
    "POST",
    `/resources/documents.page/${pageId}/permissions`,
    {
      subjectType: "User",
      subjectId: await userId(guestPage),
      level: "Viewer",
    },
  );
  expect(outsiderGrant.status).toBe(404);

  // On screen the same person reads that the page cannot be opened and finds a way out.
  await guestPage.goto(`/workspaces/${workspace.id}/docs/${pageId}`);
  await expect(
    guestPage.getByRole("heading", {
      name: /Document cannot be opened|This document cannot be opened|Không mở được tài liệu/,
    }),
  ).toBeVisible({ timeout: 20_000 });
  await expect(
    guestPage.getByRole("button", { name: /Back to home|Về trang chủ/ }),
  ).toBeVisible();

  // The creator shares it as a Viewer with the member and as an Editor with the guest.
  const share = (subject: string, level: string) =>
    apiWrite(owner, "POST", `/resources/documents.page/${pageId}/permissions`, {
      subjectType: "User",
      subjectId: subject,
      level,
    });
  expect((await share(await userId(memberPage), "Viewer")).status).toBe(201);
  expect((await share(await userId(guestPage), "Editor")).status).toBe(201);

  // The Viewer reads the page and its blocks; every write answers 404 and changes nothing.
  expect((await read(memberPage, `/pages/${pageId}`)).status).toBe(200);
  const blocksBefore = (await read(memberPage, `/pages/${pageId}/blocks`)).body;
  const body = {
    expectedVersion: block.data!.version,
    content: { v: 1, runs: [{ text: "viewer edit", marks: [] }] },
  };
  for (const attempt of [
    await apiWrite(
      memberPage,
      "PATCH",
      `/pages/${pageId}/blocks/${block.data!.id}`,
      body,
    ),
    await apiWrite(memberPage, "POST", `/pages/${pageId}/blocks`, {
      type: "Text",
    }),
    await apiWrite(memberPage, "PATCH", `/pages/${pageId}`, {
      title: "viewer rename",
    }),
  ]) {
    expect(attempt.status).toBe(404);
  }
  expect((await read(memberPage, `/pages/${pageId}/blocks`)).body).toBe(
    blocksBefore,
  );

  // The Editor changes the block, and the Viewer reads the change.
  const edited = await apiWrite<{ version: number }>(
    guestPage,
    "PATCH",
    `/pages/${pageId}/blocks/${block.data!.id}`,
    { ...body, content: { v: 1, runs: [{ text: "editor edit", marks: [] }] } },
  );
  expect(edited.status).toBe(200);
  expect(edited.data?.version).toBe(block.data!.version + 1);
  expect((await read(memberPage, `/pages/${pageId}/blocks`)).body).toContain(
    "editor edit",
  );
});

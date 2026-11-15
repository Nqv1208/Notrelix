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

// Workspace 3: `admin` is a plain Member (not its Owner or Admin); a Guest has no right to a page until given.
const creator = realUsers.admin;
// Two Guests of Workspace 3 (user7 is seeded like user6): a Guest has no right to a page until it is given.
const viewerMember = {
  email: "user7@notrelix.io",
  password: realUsers.workspaceGuest.password,
};
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

// The answer without what differs per request (trace and correlation ids) and with the asked id named.
const sameAnswer = (body: string, id: string) => {
  const { traceId, correlationId, ...rest } = JSON.parse(
    body.replaceAll(id, "<id>"),
  );
  void traceId;
  void correlationId;
  return rest;
};

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
  const editorPage = await as(browser, realUsers.guest);
  const memberPage = await as(browser, viewerMember);
  const madeUp = crypto.randomUUID();
  for (const outsider of [guestPage, memberPage]) {
    const none = await read(outsider, `/pages/${madeUp}`);
    const hidden = await read(outsider, `/pages/${pageId}`);
    expect(none.status).toBe(404);
    expect(hidden.status).toBe(404);
    expect(sameAnswer(hidden.body, pageId)).toEqual(
      sameAnswer(none.body, madeUp),
    );
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

  // The creator shares it as a Viewer with a Guest; a plain Member (user5) is an Editor by default.
  const share = (subject: string, level: string) =>
    apiWrite(owner, "POST", `/resources/documents.page/${pageId}/permissions`, {
      subjectType: "User",
      subjectId: subject,
      level,
    });
  expect((await share(await userId(memberPage), "Viewer")).status).toBe(201);

  // The Viewer reads the page and its blocks; every write answers 403 and changes nothing.
  // The page read carries the caller's own decision for a write: false for the Viewer, true for the
  // Editor and the creator; the Viewer's editor is read-only on screen.
  const canEdit = async (who: Page) =>
    (
      JSON.parse((await read(who, `/pages/${pageId}`)).body) as {
        canEdit: boolean;
      }
    ).canEdit;
  expect(await canEdit(memberPage)).toBe(false);
  await memberPage.goto(`/workspaces/${workspace.id}/docs/${pageId}`);
  await expect(
    memberPage.getByText(/You can only read this page|Bạn chỉ có quyền xem/),
  ).toBeVisible({ timeout: 20_000 });
  await expect(
    memberPage
      .getByRole("textbox", {
        name: /Type \/ to insert a block|Gõ \/ để chèn khối/,
      })
      .first(),
  ).toHaveAttribute("contenteditable", "false");
  expect((await read(memberPage, `/pages/${pageId}`)).status).toBe(200);
  const blocksBefore = (await read(memberPage, `/pages/${pageId}/blocks`)).body;
  const pageVersion = (
    JSON.parse((await read(memberPage, `/pages/${pageId}`)).body) as {
      version: number;
    }
  ).version;
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
      expectedVersion: pageVersion,
    }),
  ]) {
    // The Viewer can read the page, so its existence is no secret: the refusal is a plain 403.
    expect(attempt.status).toBe(403);
  }
  expect((await read(memberPage, `/pages/${pageId}/blocks`)).body).toBe(
    blocksBefore,
  );

  // The Editor changes the block, and the Viewer reads the change.
  const edited = await apiWrite<{ version: number }>(
    editorPage,
    "PATCH",
    `/pages/${pageId}/blocks/${block.data!.id}`,
    { ...body, content: { v: 1, runs: [{ text: "editor edit", marks: [] }] } },
  );
  expect(edited.status).toBe(200);
  expect(edited.data?.version).toBe(block.data!.version + 1);
  expect((await read(memberPage, `/pages/${pageId}/blocks`)).body).toContain(
    "editor edit",
  );

  // A plain Member holds Editor by the workspace default and edits on screen; a Guest is capped at Viewer.
  await editorPage.goto(`/workspaces/${workspace.id}/docs/${pageId}`);
  await expect(
    editorPage
      .getByRole("textbox", {
        name: /Type \/ to insert a block|Gõ \/ để chèn khối/,
      })
      .first(),
  ).not.toHaveAttribute("contenteditable", "false");

  // The Editor, who has just written, and the creator both read canEdit true.
  expect(await canEdit(editorPage)).toBe(true);
  expect(await canEdit(owner)).toBe(true);
});

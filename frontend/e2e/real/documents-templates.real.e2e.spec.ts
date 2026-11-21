import { expect, test } from "@playwright/test";
import {
  apiRead,
  apiWrite,
  findWorkspace,
  realUsers,
  signIn,
} from "./support/real-session";

test.setTimeout(240_000);

type Template = { id: string; name: string };
type PageRow = { id: string; title: string };

test("a template is saved and a page made from it replays by key, refuses a conflicting body, makes another page for a new key, and an outsider gets not found", async ({
  browser,
  page,
}) => {
  const stamp = Date.now();
  await signIn(page, realUsers.workspaceOwner);
  const workspace = await findWorkspace(page, "Workspace 3");
  const source = await apiWrite<string>(
    page,
    "POST",
    `/workspaces/${workspace.id}/pages`,
    { title: `Source ${stamp}` },
  );
  expect(source.status).toBe(201);

  // Save as a template: the same key and body replays, and an empty name is 400.
  const templateKey = crypto.randomUUID();
  const body = { pageId: source.data, name: `Template ${stamp}` };
  const saved = await apiWrite<string>(
    page,
    "POST",
    `/workspaces/${workspace.id}/page-templates`,
    body,
    templateKey,
  );
  expect(saved.status).toBeLessThan(300);
  const savedAgain = await apiWrite<string>(
    page,
    "POST",
    `/workspaces/${workspace.id}/page-templates`,
    body,
    templateKey,
  );
  expect(savedAgain.data).toEqual(saved.data);
  expect(
    (
      await apiWrite(
        page,
        "POST",
        `/workspaces/${workspace.id}/page-templates`,
        { pageId: source.data, name: "" },
      )
    ).status,
  ).toBe(400);
  const listed = await apiRead<Template[] | { items: Template[] }>(
    page,
    `/workspaces/${workspace.id}/page-templates`,
  );
  const templates = Array.isArray(listed) ? listed : listed.items;
  expect(templates.filter((t) => t.name === `Template ${stamp}`)).toHaveLength(
    1,
  );

  // Create from the template: one key, one page.
  const path = `/workspaces/${workspace.id}/page-templates/${(saved.data as unknown as Template).id}/pages`;
  const key = crypto.randomUUID();
  const first = await apiWrite<string>(
    page,
    "POST",
    path,
    { title: `From ${stamp}` },
    key,
  );
  expect(first.status).toBeLessThan(300);
  const replay = await apiWrite<string>(
    page,
    "POST",
    path,
    { title: `From ${stamp}` },
    key,
  );
  expect(replay.status).toBeLessThan(300);
  expect(replay.data).toEqual(first.data);
  const conflict = await apiWrite(
    page,
    "POST",
    path,
    { title: `Other ${stamp}` },
    key,
  );
  expect(conflict.status).toBeGreaterThanOrEqual(400);
  const another = await apiWrite<string>(
    page,
    "POST",
    path,
    { title: `From ${stamp}` },
    crypto.randomUUID(),
  );
  expect(another.data).not.toEqual(first.data);
  expect(
    (
      await apiRead<PageRow>(
        page,
        `/pages/${(first.data as unknown as { pageId: string }).pageId}`,
      )
    ).title,
  ).toBe(`From ${stamp}`);

  // An unknown template and an outsider both get not found.
  expect(
    (
      await apiWrite(
        page,
        "POST",
        `/workspaces/${workspace.id}/page-templates/${crypto.randomUUID()}/pages`,
        {},
      )
    ).status,
  ).toBe(404);
  const guestContext = await browser.newContext();
  const guest = await guestContext.newPage();
  await signIn(guest, realUsers.workspaceGuest);
  // A guest member without the right is refused the same way as for creating a page; a workspace that
  // is not theirs is the identical not found.
  const guestPage = await apiWrite(
    guest,
    "POST",
    `/workspaces/${workspace.id}/pages`,
    { title: "nope" },
  );
  const guestTemplate = await apiWrite(guest, "POST", path, { title: "nope" });
  expect(guestTemplate.status).toBe(guestPage.status);
  expect(guestTemplate.status).toBeGreaterThanOrEqual(403);
  expect(
    (
      await apiWrite(
        guest,
        "POST",
        `/workspaces/${crypto.randomUUID()}/page-templates/${crypto.randomUUID()}/pages`,
        { title: "nope" },
      )
    ).status,
  ).toBe(404);
  await guestContext.close();
});

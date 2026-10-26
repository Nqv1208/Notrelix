import { expect, test } from "@playwright/test";
import {
  apiRead,
  apiWrite,
  findWorkspace,
  realUsers,
  signIn,
} from "./support/real-session";

test.use({ viewport: { width: 1440, height: 900 } });
test.setTimeout(180_000);

test("a reply to a page mention in the Inbox posts a comment on the page", async ({
  browser,
  page,
}) => {
  const stamp = Date.now().toString(36);
  const context = await browser.newContext();
  const reader = await context.newPage();
  await signIn(reader, realUsers.admin);
  const readerWorkspace = await findWorkspace(reader, "Workspace 3");
  const me = await apiRead<{ userId: string }>(
    reader,
    `/workspaces/${readerWorkspace.id}/members/me`,
  );

  // The workspace owner mentions the admin in a comment on a page both can read.
  await signIn(page, realUsers.workspaceOwner);
  const workspace = await findWorkspace(page, "Workspace 3");
  const created = await apiWrite<string>(
    page,
    "POST",
    `/workspaces/${workspace.id}/pages`,
    { title: `Reply page ${stamp}` },
  );
  expect(created.status).toBe(201);
  const pageId = created.data as string;
  const mentioned = await apiWrite(page, "POST", `/pages/${pageId}/comments`, {
    contentMd: `Please read ${stamp}`,
    mentionedUserIds: [me.userId],
  });
  expect(mentioned.status).toBeLessThan(300);

  await expect(async () => {
    await reader.goto(`/workspaces/${readerWorkspace.id}/inbox`);
    await expect(reader.getByText(`Reply page ${stamp}`).first()).toBeVisible({
      timeout: 2_000,
    });
  }).toPass({ timeout: 60_000 });

  await reader.getByText(`Reply page ${stamp}`).first().click();
  await reader
    .getByRole("textbox", { name: /^Reply/ })
    .fill(`Done reading ${stamp}`);
  const posted = reader.waitForResponse(
    (response) =>
      response.request().method() === "POST" &&
      response.url().endsWith(`/pages/${pageId}/comments`),
  );
  await reader.getByRole("button", { name: /^Send/ }).click();
  expect((await posted).status()).toBeLessThan(300);

  const comments = await apiRead<{ contentMd?: string; content?: string }[]>(
    page,
    `/pages/${pageId}/comments`,
  );
  expect(
    comments.some((comment) =>
      (comment.contentMd ?? comment.content ?? "").includes(
        `Done reading ${stamp}`,
      ),
    ),
  ).toBe(true);
  await context.close();
});

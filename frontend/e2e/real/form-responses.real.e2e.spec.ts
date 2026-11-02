import { expect, test, type Page } from "@playwright/test";
import { apiWrite, realUsers, signIn } from "./support/real-session";
import {
  addQuestionViaApi,
  createFormViaApi,
  openFormsBoard,
  publishViaApi,
  signedInAs,
  submissionsOf,
} from "./support/real-forms";

test.use({ viewport: { width: 1440, height: 900 } });

function written(page: Page, route: RegExp) {
  return page.waitForResponse(
    (response) =>
      response.request().method() === "POST" && route.test(response.url()),
  );
}

test("a manager turns one response into an item, rejects one with a reason and marks one as spam; a handled response and a guest are refused", async ({
  page,
  browser,
}) => {
  await signIn(page, realUsers.workspaceAdmin);
  const { workspace, board } = await openFormsBoard(page);
  const form = await createFormViaApi(page, board.id, "Responses");
  await addQuestionViaApi(page, form.id, {
    key: "title",
    label: "What do you need?",
    required: true,
  });
  await publishViaApi(page, form.id);

  // Three responses sent by a member.
  const member = await signedInAs(browser, realUsers.workspaceOwner);
  const wanted = ["Process me", "Reject me", "Spam me"];
  for (const title of wanted) {
    const sent = await apiWrite(
      member.page,
      "POST",
      `/forms/${form.id}/submissions`,
      {
        answers: { title },
      },
    );
    expect(sent.status).toBe(201);
  }
  await member.context.close();

  await page.goto(
    `/workspaces/${workspace.id}/boards/${board.id}/forms/${form.id}/responses`,
  );
  await expect(page.getByRole("tab", { name: /New\s*3/ })).toBeVisible();

  // Process: the ordinary add-item dialog opens prefilled, and saving links the response to the item.
  await page.getByRole("option", { name: /Process me/ }).click();
  await page.getByRole("button", { name: "Create item from response" }).click();
  // The dialog is named by the item it will create, prefilled from the answer.
  const dialog = page.getByRole("dialog", { name: "Process me" });
  await expect(dialog.getByRole("textbox", { name: "Card title" })).toHaveValue(
    "Process me",
  );
  const processed = written(page, /\/form-submissions\/[^/]+\/process$/);
  await dialog.getByRole("button", { name: "Create item" }).click();
  expect((await processed).status()).toBeLessThan(300);
  await expect(page.getByRole("tab", { name: /Processed\s*1/ })).toBeVisible();

  // Reject: the reason is kept, no item is made.
  await page.getByRole("option", { name: /Reject me/ }).click();
  await page.getByRole("button", { name: "Reject…" }).click();
  await page
    .getByRole("textbox", { name: /Reason/ })
    .fill("Duplicate of ERP-168");
  const rejected = written(page, /\/form-submissions\/[^/]+\/reject$/);
  await page.getByRole("button", { name: "Reject", exact: true }).click();
  expect((await rejected).status()).toBeLessThan(300);

  // Spam: a soft state, the response stays and is listed.
  await page.getByRole("option", { name: /Spam me/ }).click();
  const spammed = written(page, /\/form-submissions\/[^/]+\/spam$/);
  await page.getByRole("button", { name: "Mark as spam" }).click();
  expect((await spammed).status()).toBeLessThan(300);

  const all = await submissionsOf(page, form.id);
  expect(all).toHaveLength(3);
  const byTitle = (title: string) =>
    all.find((s) => s.payloadJson.includes(title))!;
  const done = byTitle("Process me");
  expect(done.createdItemId).not.toBeNull();
  expect(byTitle("Reject me")).toMatchObject({
    status: "Rejected",
    reason: "Duplicate of ERP-168",
    createdItemId: null,
  });
  expect(byTitle("Spam me")).toMatchObject({
    status: "Spam",
    createdItemId: null,
  });
  await page.reload();
  await page.getByRole("tab", { name: /Spam\s*1/ }).click();
  await expect(page.getByRole("option", { name: /Spam me/ })).toBeVisible();

  // Rejection: a handled response cannot be handled again, and its first result stays.
  const again = await apiWrite(
    page,
    "POST",
    `/form-submissions/${byTitle("Reject me").id}/spam`,
  );
  expect([400, 409, 422]).toContain(again.status);
  const reprocess = await apiWrite(
    page,
    "POST",
    `/form-submissions/${done.id}/process`,
    {
      createdItemId: crypto.randomUUID(),
    },
  );
  expect(reprocess.status).toBeGreaterThanOrEqual(400);
  expect(
    (await submissionsOf(page, form.id)).find((s) => s.id === done.id)!
      .createdItemId,
  ).toBe(done.createdItemId);

  // Rejection: a guest cannot read or handle the responses.
  const guest = await signedInAs(browser, realUsers.workspaceGuest);
  const read = await guest.page.request.get(
    `${process.env.VITE_API_URL ?? "http://127.0.0.1:58000/api/v1"}/forms/${form.id}/submissions`,
  );
  expect([403, 404]).toContain(read.status());
  const handle = await apiWrite(
    guest.page,
    "POST",
    `/form-submissions/${byTitle("Spam me").id}/reject`,
    {
      reason: "no",
    },
  );
  expect([403, 404]).toContain(handle.status);
  await guest.context.close();
});

import { expect, test } from "@playwright/test";
import { apiRead, apiWrite, realUsers, signIn } from "./support/real-session";
import {
  addQuestionViaApi,
  createFormViaApi,
  openFormsBoard,
  publishViaApi,
  signedInAs,
} from "./support/real-forms";

test.use({ viewport: { width: 1440, height: 900 } });
// The submit limit is 10 a minute for one form: 56 responses take about six minutes.
test.setTimeout(900_000);

const SENT = 56;
const PAGE = 50;

test("the response tab counts are the server's totals over all responses, not the pages loaded so far", async ({
  page,
  browser,
}) => {
  await signIn(page, realUsers.workspaceAdmin);
  const { workspace, board } = await openFormsBoard(page);
  const form = await createFormViaApi(page, board.id, "Paged responses");
  await addQuestionViaApi(page, form.id, {
    key: "title",
    label: "What do you need?",
    required: true,
  });
  await publishViaApi(page, form.id);

  const member = await signedInAs(browser, realUsers.workspaceOwner);
  const submit = (n: number) =>
    apiWrite(member.page, "POST", `/forms/${form.id}/submissions`, {
      answers: { title: `Response ${n}` },
    });
  for (let sent = 0; sent < SENT; sent += 1) {
    let response = await submit(sent);
    if (response.status === 429) {
      await member.page.waitForTimeout(61_000);
      response = await submit(sent);
    }
    expect(response.status, `response ${sent}`).toBe(201);
  }
  await member.context.close();

  // The server: one page holds 50, the counts say 56.
  const first = await apiRead<{ items: unknown[]; nextCursor: string | null }>(
    page,
    `/forms/${form.id}/submissions/page`,
  );
  expect(first.items).toHaveLength(PAGE);
  expect(first.nextCursor).not.toBeNull();
  expect(await apiRead(page, `/forms/${form.id}/submissions/counts`)).toEqual({
    new: SENT,
    processed: 0,
    rejected: 0,
    spam: 0,
  });

  // The screen: 50 rows loaded, the tab already says 56; after "Show more responses" it still says 56.
  await page.goto(
    `/workspaces/${workspace.id}/boards/${board.id}/forms/${form.id}/responses`,
  );
  const tab = page.getByRole("tab", { name: new RegExp(`New\\s*${SENT}`) });
  await expect(tab).toBeVisible();
  await expect(page.getByRole("option")).toHaveCount(PAGE);
  await page.getByRole("button", { name: "Show more responses" }).click();
  await expect(page.getByRole("option")).toHaveCount(SENT);
  await expect(tab).toBeVisible();

  // A guest cannot read the counts.
  const guest = await signedInAs(browser, realUsers.workspaceGuest);
  const refused = await guest.page.request.get(
    `${process.env.VITE_API_URL ?? "http://127.0.0.1:58000/api/v1"}/forms/${form.id}/submissions/counts`,
  );
  expect([403, 404]).toContain(refused.status());
  await guest.context.close();
});

import { expect, test } from "@playwright/test";
import { apiWrite, realUsers, signIn } from "./support/real-session";
import {
  addQuestionViaApi,
  closeViaApi,
  createFormViaApi,
  openFormsBoard,
  publishViaApi,
  signedInAs,
  submissionsOf,
} from "./support/real-forms";

test.use({ viewport: { width: 1440, height: 900 } });

test("a member fills a published form and the answers reach the inbox once; an empty required answer, a closed form and a guest are refused", async ({
  page,
  browser,
}) => {
  await signIn(page, realUsers.workspaceAdmin);
  const { workspace, board } = await openFormsBoard(page);
  const form = await createFormViaApi(page, board.id, "Submit");
  await addQuestionViaApi(page, form.id, {
    key: "title",
    label: "What do you need?",
    required: true,
  });
  await publishViaApi(page, form.id);
  const fillUrl = `/workspaces/${workspace.id}/boards/${board.id}/forms/${form.id}`;

  const member = await signedInAs(browser, realUsers.workspaceOwner);
  const member$ = member.page;
  await member$.goto(fillUrl);
  await expect(
    member$.getByRole("heading", { name: form.title }),
  ).toBeVisible();

  // Rejection: an empty required answer shows where, and nothing is sent.
  let posted = 0;
  member$.on("request", (request) => {
    if (
      request.method() === "POST" &&
      /\/forms\/[^/]+\/submissions$/.test(request.url())
    )
      posted += 1;
  });
  await member$.getByRole("button", { name: "Send request" }).click();
  await expect(member$.getByText("Answer this question.")).toBeVisible();
  expect(posted).toBe(0);

  // Main flow: the answer is sent under an idempotency key and the page confirms.
  await member$
    .getByRole("textbox", { name: /What do you need/ })
    .fill("Wireless mouse for the new hire");
  const sent = member$.waitForResponse(
    (response) =>
      response.request().method() === "POST" &&
      /\/forms\/[^/]+\/submissions$/.test(response.url()),
  );
  await member$.getByRole("button", { name: "Send request" }).click();
  const response = await sent;
  expect(response.status()).toBe(201);
  expect(response.request().headers()["idempotency-key"]).toBeTruthy();
  await expect(
    member$.getByRole("heading", { name: "Request sent" }),
  ).toBeVisible();

  // The manager finds exactly one response, with the answer and who sent it.
  const inbox = await submissionsOf(page, form.id);
  expect(inbox).toHaveLength(1);
  expect(inbox[0]!.status).toBe("Accepted");
  expect(inbox[0]!.createdItemId).toBeNull();
  expect(inbox[0]!.payloadJson).toContain("Wireless mouse");

  // Retry: the same key replays the first answer and stores nothing more.
  const key = crypto.randomUUID();
  const body = { answers: { title: "Second request" } };
  const first = await apiWrite<string>(
    member$,
    "POST",
    `/forms/${form.id}/submissions`,
    body,
    key,
  );
  const replay = await apiWrite<string>(
    member$,
    "POST",
    `/forms/${form.id}/submissions`,
    body,
    key,
  );
  expect(first.status).toBe(201);
  expect(replay.data).toBe(first.data);
  expect(await submissionsOf(page, form.id)).toHaveLength(2);

  // Rejection: an answer the form does not accept is refused with the question named.
  const invalid = await apiWrite(
    member$,
    "POST",
    `/forms/${form.id}/submissions`,
    {
      answers: { other: "x" },
    },
  );
  expect(invalid.status).toBe(400);
  expect(await submissionsOf(page, form.id)).toHaveLength(2);

  // Rejection: a guest does not get the form.
  const guest = await signedInAs(browser, realUsers.workspaceGuest);
  await guest.page.goto(fillUrl);
  await expect(
    guest.page.getByRole("heading", {
      name: /Form not found|You cannot send this form/,
    }),
  ).toBeVisible();
  await guest.context.close();

  // Rejection: a closed form takes no more answers.
  await closeViaApi(page, form.id);
  const refused = await apiWrite(
    member$,
    "POST",
    `/forms/${form.id}/submissions`,
    {
      answers: { title: "Too late" },
    },
  );
  expect([400, 409, 422]).toContain(refused.status);
  await member$.reload();
  await expect(
    member$.getByRole("heading", { name: "This form is closed" }),
  ).toBeVisible();
  expect(await submissionsOf(page, form.id)).toHaveLength(2);
  await member.context.close();
});

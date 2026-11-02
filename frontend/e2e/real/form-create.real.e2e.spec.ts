import { expect, test } from "@playwright/test";
import { apiWrite, realUsers, signIn } from "./support/real-session";
import {
  formRead,
  formsUrl,
  openFormsBoard,
  signedInAs,
} from "./support/real-forms";

test.use({ viewport: { width: 1440, height: 900 } });

test("a board manager creates a form that starts as a draft and opens in the builder; an empty name, a guest and a lost answer are handled", async ({
  page,
  browser,
}) => {
  await signIn(page, realUsers.workspaceAdmin);
  const { workspace, board } = await openFormsBoard(page);
  await page.goto(formsUrl(workspace.id, board.id));
  await expect(
    page.getByRole("heading", { level: 2, name: "Forms" }),
  ).toBeVisible();

  // Rejection: a form needs a name, and nothing is sent without one.
  await page.getByRole("button", { name: "Create form" }).click();
  let posted = 0;
  page.on("request", (request) => {
    if (
      request.method() === "POST" &&
      /\/boards\/[^/]+\/forms$/.test(request.url())
    )
      posted += 1;
  });
  await page.getByRole("button", { name: "Create", exact: true }).click();
  await expect(page.getByText("Enter a form name.")).toBeVisible();
  expect(posted).toBe(0);

  // Main flow: the form is created as a draft and the builder opens on it.
  const title = `Access request ${Date.now().toString(36)}`;
  await page.getByRole("textbox", { name: "New form name" }).fill(title);
  const created = page.waitForResponse(
    (response) =>
      response.request().method() === "POST" &&
      /\/boards\/[^/]+\/forms$/.test(response.url()),
  );
  await page.getByRole("button", { name: "Create", exact: true }).click();
  const response = await created;
  expect(response.status()).toBe(201);
  expect(response.request().headers()["idempotency-key"]).toBeTruthy();
  await expect(page).toHaveURL(/\/forms\/[^/]+\/edit$/);
  await expect(page.getByRole("textbox", { name: "Form name" })).toHaveValue(
    title,
  );
  // A draft with no question cannot be published yet.
  await expect(page.getByRole("button", { name: "Publish" })).toBeDisabled();
  const formId = /\/forms\/([^/]+)\/edit$/.exec(page.url())![1]!;
  const form = await formRead(page, formId);
  expect(form.status).toBe("Draft");

  // The form is in the list after a reload, still a draft.
  await page.goto(formsUrl(workspace.id, board.id));
  const row = page.getByRole("listitem").filter({ hasText: title });
  await expect(row).toBeVisible();
  await expect(row.getByText("Draft", { exact: true })).toBeVisible();

  // Retry: the same key answers with the form already made, and no second one exists.
  const key = crypto.randomUUID();
  const first = await apiWrite<string>(
    page,
    "POST",
    `/boards/${board.id}/forms`,
    { title: `${title} retried` },
    key,
  );
  const replay = await apiWrite<string>(
    page,
    "POST",
    `/boards/${board.id}/forms`,
    { title: `${title} retried` },
    key,
  );
  expect(first.status).toBe(201);
  expect(replay.data).toBe(first.data);

  // Rejection: a guest of the workspace cannot create a form.
  const guest = await signedInAs(browser, realUsers.workspaceGuest);
  const refused = await apiWrite(
    guest.page,
    "POST",
    `/boards/${board.id}/forms`,
    {
      title: "Not allowed",
    },
  );
  expect([403, 404]).toContain(refused.status);
  await guest.context.close();
});

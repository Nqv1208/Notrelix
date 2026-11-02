import { expect, test } from "@playwright/test";
import { apiRead, apiWrite, realUsers, signIn } from "./support/real-session";
import {
  createFormViaApi,
  formRead,
  openFormsBoard,
} from "./support/real-forms";

test.use({ viewport: { width: 1440, height: 900 } });

test("a manager adds a question that fills the item name, renames it, and a stale version, a repeated key and a field that cannot hold the answer are refused", async ({
  page,
}) => {
  await signIn(page, realUsers.workspaceAdmin);
  const { workspace, board } = await openFormsBoard(page);
  const { id: formId } = await createFormViaApi(page, board.id, "Questions");
  await page.goto(
    `/workspaces/${workspace.id}/boards/${board.id}/forms/${formId}/edit`,
  );
  await expect(page.getByRole("textbox", { name: "Form name" })).toBeVisible();

  // Main flow: a question that goes into the item name, mapped to the board's title.
  await page.getByRole("button", { name: "Add question" }).click();
  const added = page.waitForResponse(
    (response) =>
      response.request().method() === "POST" &&
      /\/forms\/[^/]+\/questions$/.test(response.url()),
  );
  await page.getByRole("button", { name: "Item name" }).click();
  const addedResponse = await added;
  expect(addedResponse.status()).toBeLessThan(300);
  expect(addedResponse.request().postDataJSON()).toMatchObject({
    expectedVersion: expect.any(Number),
  });
  const list = page.getByRole("list", { name: "Questions" });
  await expect(list.getByRole("listitem")).toHaveCount(1);
  const mapped = await formRead(page, formId);
  expect(mapped.questions).toHaveLength(1);
  // "Goes into the item" is a built-in target: the question key says it, not a board field.
  expect(mapped.questions[0]!.questionKey).toBe("title");

  // The label is saved when the field is left, and carries the form version.
  await list.getByRole("listitem").first().getByRole("button").first().click();
  const label = page.getByRole("textbox", { name: "Question label" });
  const renamed = page.waitForResponse(
    (response) =>
      response.request().method() === "PATCH" &&
      /\/forms\/[^/]+\/questions\/[^/]+$/.test(response.url()),
  );
  await label.fill("What do you need?");
  await label.blur();
  const renameResponse = await renamed;
  expect(renameResponse.status()).toBeLessThan(300);
  expect(renameResponse.request().postDataJSON()).toMatchObject({
    label: "What do you need?",
    expectedVersion: expect.any(Number),
  });
  await page.reload();
  await expect(page.getByRole("list", { name: "Questions" })).toContainText(
    "What do you need?",
  );

  // Rejection: a stale form version changes nothing (412), the same key twice is refused, and a Short text
  // question cannot fill a field that cannot hold text.
  const before = await formRead(page, formId);
  const question = before.questions[0]!;
  const stale = await apiWrite(
    page,
    "PATCH",
    `/forms/${formId}/questions/${question.id}`,
    {
      label: "Lost update",
      isRequired: false,
      expectedVersion: before.version - 1,
    },
  );
  expect(stale.status).toBe(412);
  const duplicate = await apiWrite(page, "POST", `/forms/${formId}/questions`, {
    questionKey: question.questionKey,
    questionType: "ShortText",
    label: "Again",
    expectedVersion: before.version,
  });
  expect([400, 409, 422]).toContain(duplicate.status);

  const full = await apiRead<{
    columns: { id: string; fieldType: string }[];
  }>(page, `/boards/${board.id}/full?pageSize=1`);
  const unfit = full.columns.find(
    (field) => !["Text", "LongText", "Link", "Title"].includes(field.fieldType),
  );
  expect(unfit, "a board field that cannot hold text").toBeDefined();
  const mismatch = await apiWrite(
    page,
    "PATCH",
    `/forms/${formId}/questions/${question.id}`,
    {
      label: "What do you need?",
      isRequired: false,
      boardFieldId: unfit!.id,
      expectedVersion: before.version,
    },
  );
  expect([400, 409, 422]).toContain(mismatch.status);

  const after = await formRead(page, formId);
  expect(after.version).toBe(before.version);
  expect(after.questions[0]!.label).toBe("What do you need?");
  expect(after.questions[0]!.boardFieldId).toBe(question.boardFieldId);
});

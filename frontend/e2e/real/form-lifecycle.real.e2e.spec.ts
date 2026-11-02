import { expect, test } from "@playwright/test";
import { apiWrite, realUsers, signIn } from "./support/real-session";
import {
  addQuestionViaApi,
  createFormViaApi,
  formRead,
  formsUrl,
  openFormsBoard,
  signedInAs,
} from "./support/real-forms";

test.use({ viewport: { width: 1440, height: 900 } });

test("a manager publishes, closes, deletes and restores a form from the list; a stale version, an empty form and a person without manager rights are refused", async ({
  page,
  browser,
}) => {
  await signIn(page, realUsers.workspaceAdmin);
  const { workspace, board } = await openFormsBoard(page);
  const draft = await createFormViaApi(page, board.id, "Lifecycle");
  await addQuestionViaApi(page, draft.id, {
    key: "title",
    label: "What do you need?",
    required: true,
  });
  await page.goto(formsUrl(workspace.id, board.id));
  const row = page.getByRole("listitem").filter({ hasText: draft.title });
  await expect(row.getByText("Draft", { exact: true })).toBeVisible();

  // Publish: the row says Accepting once the server answers, and the version moved.
  const published = page.waitForResponse(
    (response) =>
      response.request().method() === "POST" &&
      /\/forms\/[^/]+\/publish/.test(response.url()),
  );
  await row.getByRole("button", { name: "Publish" }).click();
  const publishResponse = await published;
  expect(publishResponse.status()).toBeLessThan(300);
  expect(publishResponse.url()).toMatch(/expectedVersion=\d+/);
  await expect(row.getByText("Accepting", { exact: true })).toBeVisible();
  const afterPublish = await formRead(page, draft.id);
  expect(afterPublish.status).toBe("Published");

  // Publishing again with the current version changes nothing.
  const again = await apiWrite(
    page,
    "POST",
    `/forms/${draft.id}/publish?expectedVersion=${afterPublish.version}`,
  );
  expect(again.status).toBeLessThan(300);
  expect((await formRead(page, draft.id)).version).toBe(afterPublish.version);

  // Rejection: a stale version is refused (412) and changes nothing.
  const stale = await apiWrite(
    page,
    "POST",
    `/forms/${draft.id}/close?expectedVersion=${afterPublish.version - 1}`,
  );
  expect(stale.status).toBe(412);
  expect((await formRead(page, draft.id)).status).toBe("Published");

  // Close: submissions stop and the row says Closed.
  await row.getByRole("button", { name: "Stop accepting" }).click();
  await expect(row.getByText("Closed", { exact: true })).toBeVisible();
  expect((await formRead(page, draft.id)).status).toBe("Closed");

  // Delete asks first, then the form leaves the list; Undo brings it back.
  await row.getByRole("button", { name: `Delete form ${draft.title}` }).click();
  await row.getByRole("button", { name: "Delete form", exact: true }).click();
  const builderLink = page.getByRole("button", {
    name: `Open the builder of ${draft.title}`,
  });
  await expect(builderLink).toHaveCount(0);
  const restored = page.waitForResponse(
    (response) =>
      response.request().method() === "POST" &&
      /\/forms\/[^/]+\/restore$/.test(response.url()),
  );
  await page.getByRole("button", { name: "Undo" }).click();
  expect((await restored).status()).toBeLessThan(300);
  await expect(builderLink).toBeVisible();
  const back = await formRead(page, draft.id);
  expect(back.status).toBe("Closed");
  expect(back.questions).toHaveLength(1);

  // Rejection: a form with no question cannot be published, and a guest cannot manage a form.
  const empty = await createFormViaApi(page, board.id, "Empty");
  const emptyForm = await formRead(page, empty.id);
  const noQuestions = await apiWrite(
    page,
    "POST",
    `/forms/${empty.id}/publish?expectedVersion=${emptyForm.version}`,
  );
  expect([400, 409, 422]).toContain(noQuestions.status);
  expect((await formRead(page, empty.id)).status).toBe("Draft");

  const guest = await signedInAs(browser, realUsers.workspaceGuest);
  const refused = await apiWrite(
    guest.page,
    "POST",
    `/forms/${draft.id}/close?expectedVersion=${back.version}`,
  );
  expect([403, 404]).toContain(refused.status);
  await guest.context.close();
});

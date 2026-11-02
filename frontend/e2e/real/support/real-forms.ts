import { expect, type Browser, type Page } from "@playwright/test";
import {
  apiRead,
  apiWrite,
  openBoard,
  realUsers,
  signIn,
} from "./real-session";

/** The seeded board every forms journey works on: Workspace 3, where `workspaceAdmin` manages its boards. */
export async function openFormsBoard(page: Page) {
  return openBoard(page, "Workspace 3", "Engineering Tasks");
}

interface FormRead {
  readonly id: string;
  readonly name: string;
  readonly status: string;
  readonly version: number;
  readonly questions: readonly {
    readonly id: string;
    readonly questionKey: string;
    readonly label: string;
    readonly boardFieldId: string | null;
  }[];
}

export function formRead(page: Page, formId: string) {
  return apiRead<FormRead>(page, `/forms/${formId}`);
}

/** A form made through the API (the scene a journey starts from), with the unique name it was given. */
export async function createFormViaApi(
  page: Page,
  boardId: string,
  label: string,
) {
  const title = `${label} ${Date.now().toString(36)}${Math.floor(Math.random() * 1000)}`;
  const created = await apiWrite<string>(
    page,
    "POST",
    `/boards/${boardId}/forms`,
    { title },
  );
  expect(created.status, "create form").toBe(201);
  return { id: created.data as string, title };
}

export async function addQuestionViaApi(
  page: Page,
  formId: string,
  question: { key: string; label: string; type?: string; required?: boolean },
) {
  const form = await formRead(page, formId);
  const added = await apiWrite(page, "POST", `/forms/${formId}/questions`, {
    questionKey: question.key,
    questionType: question.type ?? "ShortText",
    label: question.label,
    isRequired: question.required ?? false,
    expectedVersion: form.version,
  });
  expect(added.status, `add question ${question.key}`).toBeLessThan(300);
}

export async function publishViaApi(page: Page, formId: string) {
  const form = await formRead(page, formId);
  const published = await apiWrite(
    page,
    "POST",
    `/forms/${formId}/publish?expectedVersion=${form.version}`,
  );
  expect(published.status, "publish form").toBeLessThan(300);
}

export async function closeViaApi(page: Page, formId: string) {
  const form = await formRead(page, formId);
  const closed = await apiWrite(
    page,
    "POST",
    `/forms/${formId}/close?expectedVersion=${form.version}`,
  );
  expect(closed.status, "close form").toBeLessThan(300);
}

export interface SubmissionRead {
  readonly id: string;
  readonly status: string;
  readonly payloadJson: string;
  readonly createdItemId: string | null;
  readonly reason: string | null;
}

export function submissionsOf(page: Page, formId: string) {
  return apiRead<SubmissionRead[]>(page, `/forms/${formId}/submissions`);
}

/** A second signed-in person in their own browser context. */
export async function signedInAs(
  browser: Browser,
  user: (typeof realUsers)[keyof typeof realUsers],
) {
  const context = await browser.newContext();
  const page = await context.newPage();
  await signIn(page, user);
  return { context, page };
}

export const formsUrl = (workspaceId: string, boardId: string) =>
  `/workspaces/${workspaceId}/boards/${boardId}/settings?section=forms`;

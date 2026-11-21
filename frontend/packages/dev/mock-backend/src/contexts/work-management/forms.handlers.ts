/**
 * Work Management — the forms of a board and their responses.
 *
 * Operations (the producer's WorkManagement.Forms.*):
 *   boards.forms.list / boards.forms.create        — /boards/:boardId/forms
 *   forms.get / forms.updateDetails / forms.delete — /forms/:formId
 *   forms.publish / .close / .restore              — POST /forms/:formId/<verb>
 *   forms.questions.add / .update / .remove        — /forms/:formId/questions[/:questionId]
 *   forms.submissions.list / forms.submit          — /forms/:formId/submissions
 *   forms.submissions.process / .reject / .spam / .reopen / .delete — /form-submissions/:submissionId[/<verb>]
 *   forms.publicAccess.get / .set                  — /forms/:formId/public-access (workspace Owner or Admin)
 *   publicForms.get / publicForms.submit           — /public/forms/:publicKey[/submissions] (no sign-in, no workspace)
 *
 * The rules mirror the producer: a form is read with the board, built by a
 * manager of the board, submitted by a member who can create items on it, and
 * only a published form takes a response. Answers are checked against the
 * questions and refused by question key.
 */

import { defineMockOperation } from "../../operations/types";
import {
  conflict,
  created,
  forbidden,
  noContent,
  notFound,
  ok,
  validationError,
} from "../../transport/create-response";
import type {
  MockForm,
  MockFormQuestion,
  MockFormSubmission,
} from "../../state/mock-board-forms";
import type { MockStore } from "../../state/mock-store";
import type { MockHttpResult } from "../../transport/create-response";
import {
  canEditBoard,
  canManageBoard,
  canReadBoard,
  canWriteBoardItems,
} from "./boards.handlers";

const QUESTION_TYPES = [
  "ShortText",
  "LongText",
  "Number",
  "Date",
  "Select",
  "MultiSelect",
  "Checkbox",
  "Email",
  "Phone",
  "FileUpload",
  "Signature",
] as const;

function formRow(form: MockForm) {
  return {
    id: form.id,
    name: form.name,
    slug: form.slug,
    boardId: form.boardId,
    status: form.status,
    visibility: "PublicLink",
    createdAt: form.createdAt,
  };
}

function listRow(form: MockForm, store: MockStore) {
  return {
    ...formRow(form),
    questionCount: form.questions.length,
    newSubmissionCount: store.forms
      .submissions(form.id)
      .filter((s) => s.status === "Accepted" && !s.createdItemId).length,
    updatedAt: form.createdAt,
  };
}

function detail(form: MockForm) {
  return {
    ...formRow(form),
    version: form.version,
    description: form.description,
    questions: [...form.questions]
      .sort((a, b) => (a.position < b.position ? -1 : 1))
      .map((q) => ({
        id: q.id,
        questionKey: q.questionKey,
        label: q.label,
        questionType: q.questionType,
        isRequired: q.isRequired,
        boardFieldId: q.boardFieldId,
        position: q.position,
        placeholder: q.placeholder,
        helpText: q.helpText,
        maxLength: q.maxLength,
        minValue: q.minValue,
        maxValue: q.maxValue,
        options: q.options,
      })),
  };
}

function submissionRow(s: MockFormSubmission) {
  return {
    id: s.id,
    formId: s.formId,
    boardId: s.boardId,
    createdItemId: s.createdItemId,
    submitterUserId: s.submitterUserId,
    submitterEmail: s.submitterEmail,
    status: s.status,
    submittedAt: s.submittedAt,
    processedAt: s.processedAt,
    payloadJson: s.payloadJson,
    reason: s.reason,
  };
}

/** The form behind the id, as the caller may see it, or the refusal. */
function readable(store: MockStore, formId: string, includeDeleted = false) {
  const form = store.forms.find(formId);
  if (
    !form ||
    (form.deleted && !includeDeleted) ||
    !canReadBoard(form.boardId, store)
  )
    return { form: null, refusal: notFound("Form not found") } as const;
  return { form, refusal: null } as const;
}

function manageable(store: MockStore, formId: string, includeDeleted = false) {
  const found = readable(store, formId, includeDeleted);
  if (!found.form) return found;
  if (!canManageBoard(found.form.boardId, store))
    return { form: null, refusal: forbidden() } as const;
  return found;
}

/** The producer refuses a write that names no version (400) and one that names a stale version (409). */
function versionRefusal(form: MockForm, expected: number | string | undefined) {
  const version = Number(expected);
  if (!Number.isFinite(version) || version <= 0)
    return validationError({
      expectedVersion: ["ExpectedVersion must be a positive value."],
    });
  if (version !== form.version) {
    // The producer names who saved first (changedByName), as a stale view or saved-filter write does.
    const refused = conflict("The form changed since it was read.");
    return {
      ...refused,
      body: { ...refused.body, changedByName: "Hà Nguyễn" },
    };
  }
  return null;
}

function submissionOf(store: MockStore, submissionId: string) {
  const submission = store.forms.findSubmission(submissionId);
  if (!submission)
    return {
      submission: null,
      refusal: notFound("Response not found"),
    } as const;
  if (!canManageBoard(submission.boardId, store))
    return { submission: null, refusal: forbidden() } as const;
  return { submission, refusal: null } as const;
}

/** The first problem of each answer, by question key (the producer's answer validator). */
function checkAnswers(
  form: MockForm,
  answers: Record<string, unknown>,
  store: MockStore,
): Record<string, string[]> {
  const errors: Record<string, string[]> = {};
  const known = new Set(form.questions.map((q) => q.questionKey.toLowerCase()));
  for (const key of Object.keys(answers))
    if (!known.has(key.toLowerCase())) errors[key] = ["Unknown question."];
  for (const q of form.questions) {
    const value = answers[q.questionKey];
    const blank =
      value === undefined ||
      value === null ||
      (typeof value === "string" && value.trim() === "") ||
      (Array.isArray(value) && value.length === 0);
    if (blank) {
      if (q.isRequired) errors[q.questionKey] = ["This question is required."];
      continue;
    }
    const problem = checkAnswer(q, value, form, store);
    if (problem) errors[q.questionKey] = [problem];
  }
  return errors;
}

function checkAnswer(
  q: MockFormQuestion,
  value: unknown,
  form: MockForm,
  store: MockStore,
): string | null {
  const optionIds = q.boardFieldId
    ? new Set(
        store
          .getColumns(form.boardId)
          .find((column) => column.id === q.boardFieldId)
          ?.options?.map((option) => option.id) ?? [],
      )
    : new Set((q.options ?? []).map((option) => option.key));
  switch (q.questionType) {
    case "ShortText":
    case "LongText":
    case "Email":
    case "Phone":
      if (typeof value !== "string") return "Expected text.";
      if (q.maxLength !== null && value.length > q.maxLength)
        return `At most ${q.maxLength} characters.`;
      if (q.questionType === "Email" && !value.includes("@"))
        return "Expected an email address.";
      return null;
    case "Number":
      if (typeof value !== "number") return "Expected a number.";
      if (q.minValue !== null && value < q.minValue)
        return `At least ${q.minValue}.`;
      if (q.maxValue !== null && value > q.maxValue)
        return `At most ${q.maxValue}.`;
      return null;
    case "Date":
      return typeof value === "string" && /^\d{4}-\d{2}-\d{2}/.test(value)
        ? null
        : "Expected a date.";
    case "Select":
      return typeof value === "string" && optionIds.has(value)
        ? null
        : "Unknown option.";
    case "MultiSelect":
      return Array.isArray(value) &&
        value.every((v) => typeof v === "string" && optionIds.has(v))
        ? null
        : "Unknown option.";
    case "Checkbox":
      return typeof value === "boolean" ? null : "Expected true or false.";
    default:
      return "This question type is not supported.";
  }
}

/** Cloudflare's always-pass Turnstile test site key: the real widget solves itself in the dev world. */
const MOCK_CAPTCHA_SITE_KEY = "1x00000000000000000000AA";
/** The tokens the mock's captcha accepts: the test one, and what Cloudflare's test key hands the real widget. */
const CAPTCHA_PASS_TOKENS = new Set([
  "captcha-test-pass",
  "XXXX.DUMMY.TOKEN.XXXX",
]);
const PUBLIC_SENDS_PER_MINUTE = 5;
const PUBLIC_WINDOW_SECONDS = 60;
const PUBLIC_SHARING_OFF = "WorkManagement_Form_PublicSharingDisabled";

function publicAccessRow(form: MockForm) {
  return { isPublic: form.publicKey !== null, publicKey: form.publicKey };
}

function workspaceOf(store: MockStore, form: MockForm): string {
  return store.getBoard(form.boardId)?.workspaceId ?? "";
}

/** Only a workspace Owner or Admin reads or switches the public state, whatever they hold on the board. */
function publicAdmin(store: MockStore, formId: string) {
  const { form, refusal } = readable(store, formId);
  if (!form) return { form: null, refusal } as const;
  const role = store.getCurrentMemberRole(workspaceOf(store, form));
  if (role !== "owner" && role !== "admin")
    return { form: null, refusal: forbidden() } as const;
  return { form, refusal: null } as const;
}

/**
 * The form a public key opens: public, published, on a live board of an active workspace that allows public
 * links. Anything else is the same null (a 404 that does not say why).
 */
function openForm(store: MockStore, publicKey: string): MockForm | null {
  const form = store.forms.findByPublicKey(publicKey);
  if (!form || form.deleted || form.status !== "Published") return null;
  const board = store.getBoard(form.boardId);
  if (!board || board.isArchived || store.isWorkspaceDeleted(board.workspaceId))
    return null;
  return store.getWorkspaceSettings(board.workspaceId).allowPublicSharing
    ? form
    : null;
}

/** The questions a visitor can answer: files and signatures have nowhere to go yet. */
function publicQuestions(form: MockForm, store: MockStore) {
  return [...form.questions]
    .filter(
      (q) => q.questionType !== "FileUpload" && q.questionType !== "Signature",
    )
    .sort((a, b) => (a.position < b.position ? -1 : 1))
    .map((q) => {
      const field = q.boardFieldId
        ? store.getColumns(form.boardId).find((c) => c.id === q.boardFieldId)
        : undefined;
      return {
        key: q.questionKey,
        label: q.label,
        type: q.questionType,
        required: q.isRequired,
        placeholder: q.placeholder,
        helpText: q.helpText,
        maxLength: q.maxLength,
        minValue: q.minValue,
        maxValue: q.maxValue,
        options: field?.options
          ? field.options.map((o) => ({ key: o.id, label: o.name }))
          : q.options,
      };
    });
}

function badRequest(errors: Record<string, string[]>): MockHttpResult<object> {
  return {
    status: 400,
    body: { message: "Validation failed", code: "VALIDATION_ERROR", errors },
  };
}

/**
 * One more anonymous send of this key; false once the last minute holds more than the limit.
 * shortcut: the minute is the chat's virtual time (one second per chat write, or advanceVirtualSeconds in a test),
 * never the wall clock; in a dev session the window opens again only as chat writes move it.
 */
function withinPublicLimit(store: MockStore, publicKey: string): boolean {
  const now = store.chat.virtualSeconds();
  const live = (store.forms.publicHits.get(publicKey) ?? []).filter(
    (at) => at > now - PUBLIC_WINDOW_SECONDS,
  );
  live.push(now);
  store.forms.publicHits.set(publicKey, live);
  return live.length <= PUBLIC_SENDS_PER_MINUTE;
}

type BoardParams = { boardId: string };
type FormParams = { formId: string };
type QuestionParams = { formId: string; questionId: string };
type SubmissionParams = { submissionId: string };

interface QuestionBody {
  questionKey?: string;
  questionType?: number;
  label?: string;
  isRequired?: boolean;
  configJson?: string | null;
  position?: string | null;
  boardFieldId?: string | null;
  expectedVersion?: number;
}

function applyConfig(
  q: MockFormQuestion,
  type: string,
  configJson: string | null | undefined,
): string | null {
  let config: Record<string, unknown> = {};
  try {
    if (configJson) config = JSON.parse(configJson) as Record<string, unknown>;
  } catch {
    return "Invalid config JSON.";
  }
  const num = (v: unknown) => (typeof v === "number" ? v : null);
  const str = (v: unknown) => (typeof v === "string" && v !== "" ? v : null);
  q.placeholder = str(config.placeholder);
  q.helpText = str(config.helpText);
  q.maxLength = num(config.maxLength);
  q.minValue = num(config.minValue);
  q.maxValue = num(config.maxValue);
  if (q.maxLength !== null && type !== "ShortText" && type !== "LongText")
    return "MaxLength is not supported for this field type.";
  const options = Array.isArray(config.options)
    ? (config.options as { key?: unknown; label?: unknown }[])
    : [];
  if (options.length > 0 && type !== "Select" && type !== "MultiSelect")
    return "Options are only supported for Select and MultiSelect questions.";
  q.options =
    options.length > 0
      ? options.map((o) => ({ key: String(o.key), label: String(o.label) }))
      : null;
  return null;
}

export const formsOperations = [
  defineMockOperation<BoardParams, never, unknown[]>({
    id: "boards.forms.list",
    contract: { kind: "openapi", operationId: "WorkManagement.Forms.List" },
    method: "GET",
    route: "/boards/:boardId/forms",
    async handle({ params, store }) {
      if (!canReadBoard(params.boardId, store))
        return notFound("Board not found");
      return ok(
        store.forms.list(params.boardId).map((form) => listRow(form, store)),
      );
    },
  }),

  defineMockOperation<BoardParams, { title?: string }, unknown>({
    id: "boards.forms.create",
    contract: { kind: "openapi", operationId: "WorkManagement.Forms.Create" },
    method: "POST",
    route: "/boards/:boardId/forms",
    async handle({ params, body, store }) {
      if (!canReadBoard(params.boardId, store))
        return notFound("Board not found");
      if (!canEditBoard(params.boardId, store)) return forbidden();
      const title = (body?.title ?? "").trim();
      if (title === "") return validationError({ title: ["Name the form."] });
      return created(store.forms.add(params.boardId, title).id);
    },
  }),

  defineMockOperation<FormParams, never, unknown>({
    id: "forms.get",
    contract: { kind: "openapi", operationId: "WorkManagement.Forms.Get" },
    method: "GET",
    route: "/forms/:formId",
    async handle({ params, store }) {
      const { form, refusal } = readable(store, params.formId);
      return form ? ok(detail(form)) : refusal;
    },
  }),

  defineMockOperation<
    FormParams,
    { title?: string; description?: string; expectedVersion?: number },
    void
  >({
    id: "forms.updateDetails",
    contract: {
      kind: "openapi",
      operationId: "WorkManagement.Forms.UpdateDetails",
    },
    method: "PATCH",
    route: "/forms/:formId",
    async handle({ params, body, store }) {
      const { form, refusal } = manageable(store, params.formId);
      if (!form) return refusal;
      const stale = versionRefusal(form, body?.expectedVersion);
      if (stale) return stale;
      const title = (body?.title ?? "").trim();
      if (title === "") return validationError({ title: ["Name the form."] });
      form.name = title;
      if (body?.description !== undefined) form.description = body.description;
      form.version += 1;
      return noContent();
    },
  }),

  defineMockOperation<FormParams, never, void>({
    id: "forms.delete",
    contract: {
      kind: "openapi",
      operationId: "WorkManagement.Forms.SoftDelete",
    },
    method: "DELETE",
    route: "/forms/:formId",
    async handle({ params, query, store }) {
      const { form, refusal } = manageable(store, params.formId);
      if (!form) return refusal;
      const stale = versionRefusal(form, query.expectedVersion);
      if (stale) return stale;
      form.deleted = true;
      form.version += 1;
      return noContent();
    },
  }),

  defineMockOperation<FormParams, never, void>({
    id: "forms.restore",
    contract: { kind: "openapi", operationId: "WorkManagement.Forms.Restore" },
    method: "POST",
    route: "/forms/:formId/restore",
    async handle({ params, store }) {
      const { form, refusal } = manageable(store, params.formId, true);
      if (!form) return refusal;
      form.deleted = false;
      form.version += 1;
      return noContent();
    },
  }),

  defineMockOperation<FormParams, never, void>({
    id: "forms.publish",
    contract: { kind: "openapi", operationId: "WorkManagement.Forms.Publish" },
    method: "POST",
    route: "/forms/:formId/publish",
    async handle({ params, query, store }) {
      const { form, refusal } = manageable(store, params.formId);
      if (!form) return refusal;
      const stale = versionRefusal(form, query.expectedVersion);
      if (stale) return stale;
      // A closed form is opened again by publishing it (Mở lại).
      if (form.status === "Published") return noContent();
      if (form.questions.length === 0)
        return conflict("Cannot publish a form with no questions.");
      form.status = "Published";
      form.version += 1;
      return noContent();
    },
  }),

  defineMockOperation<FormParams, never, void>({
    id: "forms.close",
    contract: { kind: "openapi", operationId: "WorkManagement.Forms.Close" },
    method: "POST",
    route: "/forms/:formId/close",
    async handle({ params, query, store }) {
      const { form, refusal } = manageable(store, params.formId);
      if (!form) return refusal;
      const stale = versionRefusal(form, query.expectedVersion);
      if (stale) return stale;
      if (form.status === "Closed") return noContent();
      form.status = "Closed";
      form.version += 1;
      return noContent();
    },
  }),

  defineMockOperation<FormParams, QuestionBody, void>({
    id: "forms.questions.add",
    contract: {
      kind: "openapi",
      operationId: "WorkManagement.Forms.AddQuestion",
    },
    method: "POST",
    route: "/forms/:formId/questions",
    async handle({ params, body, store }) {
      const { form, refusal } = manageable(store, params.formId);
      if (!form) return refusal;
      const stale = versionRefusal(form, body?.expectedVersion);
      if (stale) return stale;
      if (form.status === "Closed")
        return conflict("Cannot add a question to a closed form.");
      const key = (body?.questionKey ?? "").trim();
      const label = (body?.label ?? "").trim();
      if (key === "" || label === "")
        return validationError({ label: ["Name the question."] });
      if (
        form.questions.some(
          (q) => q.questionKey.toLowerCase() === key.toLowerCase(),
        )
      )
        return conflict(`A question with key '${key}' already exists.`);
      const type = QUESTION_TYPES[body?.questionType ?? 0] ?? "ShortText";
      const question: MockFormQuestion = {
        id: store.nextId("fq"),
        questionKey: key,
        label,
        questionType: type,
        isRequired: body?.isRequired === true,
        boardFieldId: body?.boardFieldId ?? null,
        position: body?.position ?? "a0",
        placeholder: null,
        helpText: null,
        maxLength: null,
        minValue: null,
        maxValue: null,
        options: null,
      };
      const problem = applyConfig(question, type, body?.configJson);
      if (problem) return validationError({ configJson: [problem] });
      form.questions.push(question);
      form.version += 1;
      return noContent();
    },
  }),

  defineMockOperation<QuestionParams, QuestionBody, void>({
    id: "forms.questions.update",
    contract: {
      kind: "openapi",
      operationId: "WorkManagement.Forms.UpdateQuestion",
    },
    method: "PATCH",
    route: "/forms/:formId/questions/:questionId",
    async handle({ params, body, store }) {
      const { form, refusal } = manageable(store, params.formId);
      if (!form) return refusal;
      const question = form.questions.find((q) => q.id === params.questionId);
      if (!question) return notFound("Question not found");
      const stale = versionRefusal(form, body?.expectedVersion);
      if (stale) return stale;
      if (form.status === "Closed")
        return conflict("Cannot change a question of a closed form.");
      const label = (body?.label ?? "").trim();
      if (label === "")
        return validationError({ label: ["Name the question."] });
      const problem = applyConfig(
        question,
        question.questionType,
        body?.configJson,
      );
      if (problem) return validationError({ configJson: [problem] });
      question.label = label;
      question.isRequired = body?.isRequired === true;
      question.boardFieldId = body?.boardFieldId ?? null;
      if (body?.position) question.position = body.position;
      form.version += 1;
      return noContent();
    },
  }),

  defineMockOperation<QuestionParams, never, void>({
    id: "forms.questions.remove",
    contract: {
      kind: "openapi",
      operationId: "WorkManagement.Forms.RemoveQuestion",
    },
    method: "DELETE",
    route: "/forms/:formId/questions/:questionId",
    async handle({ params, query, store }) {
      const { form, refusal } = manageable(store, params.formId);
      if (!form) return refusal;
      const stale = versionRefusal(form, query.expectedVersion);
      if (stale) return stale;
      if (form.status === "Closed")
        return conflict("Cannot remove a question from a closed form.");
      form.questions = form.questions.filter((q) => q.id !== params.questionId);
      form.version += 1;
      return noContent();
    },
  }),

  defineMockOperation<FormParams, never, unknown[]>({
    id: "forms.submissions.list",
    contract: {
      kind: "openapi",
      operationId: "WorkManagement.Forms.ListSubmissions",
    },
    method: "GET",
    route: "/forms/:formId/submissions",
    async handle({ params, store }) {
      const { form, refusal } = readable(store, params.formId);
      if (!form) return refusal;
      // Responses can hold respondent data: they need edit rights on the board.
      if (!canEditBoard(form.boardId, store)) return forbidden();
      return ok(store.forms.submissions(form.id).map(submissionRow));
    },
  }),

  defineMockOperation<
    FormParams,
    never,
    { items: unknown[]; nextCursor: null }
  >({
    id: "forms.submissions.page",
    contract: {
      kind: "openapi",
      operationId: "WorkManagement.Forms.ListSubmissionsPage",
    },
    method: "GET",
    route: "/forms/:formId/submissions/page",
    async handle({ params, store }) {
      const { form, refusal } = readable(store, params.formId);
      if (!form) return refusal;
      // Responses can hold respondent data: they need edit rights on the board.
      if (!canEditBoard(form.boardId, store)) return forbidden();
      // The mock's responses fit one page, so the cursor never goes on.
      return ok({
        items: store.forms.submissions(form.id).map(submissionRow),
        nextCursor: null,
      });
    },
  }),

  defineMockOperation<
    FormParams,
    never,
    { new: number; processed: number; rejected: number; spam: number }
  >({
    id: "forms.submissions.counts",
    contract: {
      kind: "openapi",
      operationId: "WorkManagement.Forms.CountSubmissions",
    },
    method: "GET",
    route: "/forms/:formId/submissions/counts",
    async handle({ params, store }) {
      const { form, refusal } = readable(store, params.formId);
      if (!form) return refusal;
      if (!canEditBoard(form.boardId, store)) return forbidden();
      const counts = { new: 0, processed: 0, rejected: 0, spam: 0 };
      for (const s of store.forms.submissions(form.id)) {
        if (s.status === "Rejected") counts.rejected += 1;
        else if (s.status === "Spam") counts.spam += 1;
        else if (s.status !== "Deleted")
          counts[s.createdItemId ? "processed" : "new"] += 1;
      }
      return ok(counts);
    },
  }),

  defineMockOperation<
    FormParams,
    { answers?: Record<string, unknown> },
    unknown
  >({
    id: "forms.submit",
    contract: { kind: "openapi", operationId: "WorkManagement.Forms.Submit" },
    method: "POST",
    route: "/forms/:formId/submissions",
    async handle({ params, body, store, request }) {
      const { form, refusal } = readable(store, params.formId);
      if (!form) return refusal;
      if (!canWriteBoardItems(form.boardId, store)) return forbidden();
      if (form.status !== "Published")
        return conflict(
          form.status === "Draft"
            ? "Cannot submit to a draft form."
            : "Cannot submit to a closed form.",
        );
      const answers = body?.answers ?? {};
      const errors = checkAnswers(form, answers, store);
      if (Object.keys(errors).length > 0) return validationError(errors);
      const key = request.headers.get("Idempotency-Key");
      const replay = key
        ? store.forms.submissions(form.id).find((s) => s.key === key)
        : undefined;
      if (replay) return created(replay.id);
      const me = store.getCurrentUser();
      return created(
        store.forms.addSubmission(form, {
          submitterUserId: me.id,
          submitterEmail: null,
          payloadJson: JSON.stringify(answers),
          key,
        }).id,
      );
    },
  }),

  defineMockOperation<SubmissionParams, { createdItemId?: string }, void>({
    id: "forms.submissions.process",
    contract: {
      kind: "openapi",
      operationId: "WorkManagement.Forms.ProcessSubmission",
    },
    method: "POST",
    route: "/form-submissions/:submissionId/process",
    async handle({ params, body, store }) {
      const { submission, refusal } = submissionOf(store, params.submissionId);
      if (!submission) return refusal;
      if (submission.status !== "Accepted" || submission.createdItemId)
        return conflict("Only accepted submissions can be processed.");
      submission.createdItemId = body?.createdItemId ?? null;
      submission.processedAt = store.now();
      return noContent();
    },
  }),

  defineMockOperation<SubmissionParams, { reason?: string | null }, void>({
    id: "forms.submissions.reject",
    contract: {
      kind: "openapi",
      operationId: "WorkManagement.Forms.RejectSubmission",
    },
    method: "POST",
    route: "/form-submissions/:submissionId/reject",
    async handle({ params, body, store }) {
      const { submission, refusal } = submissionOf(store, params.submissionId);
      if (!submission) return refusal;
      if (submission.status !== "Accepted" || submission.createdItemId)
        return conflict("Only accepted submissions can be rejected.");
      submission.status = "Rejected";
      submission.reason = body?.reason?.trim() ? body.reason.trim() : null;
      submission.processedAt = store.now();
      return noContent();
    },
  }),

  defineMockOperation<SubmissionParams, never, void>({
    id: "forms.submissions.spam",
    contract: {
      kind: "openapi",
      operationId: "WorkManagement.Forms.MarkAsSpam",
    },
    method: "POST",
    route: "/form-submissions/:submissionId/spam",
    async handle({ params, store }) {
      const { submission, refusal } = submissionOf(store, params.submissionId);
      if (!submission) return refusal;
      if (submission.status !== "Accepted" || submission.createdItemId)
        return conflict("Only accepted submissions can be marked as spam.");
      submission.status = "Spam";
      submission.processedAt = store.now();
      return noContent();
    },
  }),

  defineMockOperation<SubmissionParams, never, void>({
    id: "forms.submissions.reopen",
    contract: {
      kind: "openapi",
      operationId: "WorkManagement.Forms.ReopenSubmission",
    },
    method: "POST",
    route: "/form-submissions/:submissionId/reopen",
    async handle({ params, query, store }) {
      const { submission, refusal } = submissionOf(store, params.submissionId);
      if (!submission) return refusal;
      // The undo of "create item from response": the response forgets its item.
      if (query.clearItem === "true" && submission.createdItemId) {
        submission.createdItemId = null;
        submission.processedAt = null;
      }
      // The undo of a rejection, a spam mark or a delete: back to the New tab.
      if (submission.status === "Accepted") return noContent();
      submission.status = "Accepted";
      submission.reason = null;
      if (!submission.createdItemId) submission.processedAt = null;
      return noContent();
    },
  }),

  defineMockOperation<SubmissionParams, never, void>({
    id: "forms.submissions.delete",
    contract: {
      kind: "openapi",
      operationId: "WorkManagement.Forms.DeleteSubmission",
    },
    method: "DELETE",
    route: "/form-submissions/:submissionId",
    async handle({ params, store }) {
      const { submission, refusal } = submissionOf(store, params.submissionId);
      if (!submission) return refusal;
      submission.status = "Deleted";
      return noContent();
    },
  }),

  defineMockOperation<FormParams, never, unknown>({
    id: "forms.publicAccess.get",
    contract: {
      kind: "openapi",
      operationId: "WorkManagement.Forms.GetPublicAccess",
    },
    method: "GET",
    route: "/forms/:formId/public-access",
    async handle({ params, store }) {
      const { form, refusal } = publicAdmin(store, params.formId);
      return form ? ok(publicAccessRow(form)) : refusal;
    },
  }),

  defineMockOperation<FormParams, { enabled?: boolean }, unknown>({
    id: "forms.publicAccess.set",
    contract: {
      kind: "openapi",
      operationId: "WorkManagement.Forms.SetPublicAccess",
    },
    method: "PUT",
    route: "/forms/:formId/public-access",
    async handle({ params, body, store }) {
      const { form, refusal } = publicAdmin(store, params.formId);
      if (!form) return refusal;
      if (body?.enabled === true) {
        if (
          !store.getWorkspaceSettings(workspaceOf(store, form))
            .allowPublicSharing
        ) {
          const refused = conflict(
            "This workspace does not allow public links. An Owner or Admin can turn them on in the workspace settings.",
          );
          return {
            ...refused,
            body: { ...refused.body, ruleCode: PUBLIC_SHARING_OFF },
          };
        }
        // Already public keeps its key: every link out there stays good.
        form.publicKey ??= store.forms.mintPublicKey();
      } else form.publicKey = null;
      return ok(publicAccessRow(form));
    },
  }),

  defineMockOperation<{ publicKey: string }, never, unknown>({
    id: "publicForms.get",
    contract: {
      kind: "openapi",
      operationId: "WorkManagement.PublicForms.Get",
    },
    method: "GET",
    route: "/public/forms/:publicKey",
    async handle({ params, store }) {
      const form = openForm(store, params.publicKey);
      if (!form) return notFound("Form not found");
      return ok({
        name: form.name,
        description: form.description === "" ? null : form.description,
        questions: publicQuestions(form, store),
        captchaSiteKey: MOCK_CAPTCHA_SITE_KEY,
      });
    },
  }),

  defineMockOperation<
    { publicKey: string },
    {
      answers?: Record<string, unknown>;
      captchaToken?: string;
      website?: string;
    },
    void
  >({
    id: "publicForms.submit",
    contract: {
      kind: "openapi",
      operationId: "WorkManagement.PublicForms.Submit",
    },
    method: "POST",
    route: "/public/forms/:publicKey/submissions",
    async handle({ params, body, store }) {
      // The limit runs before anything else, per key, so a key that opens nothing is no oracle either.
      if (!withinPublicLimit(store, params.publicKey))
        return {
          status: 429,
          headers: { "Retry-After": String(PUBLIC_WINDOW_SECONDS) },
          body: {
            type: "https://notrelix.dev/problems/rate_limit-exceeded",
            title: "Too many requests",
            status: 429,
            detail: "Bạn đã gửi quá nhiều lần. Vui lòng thử lại sau ít phút.",
            errorCode: "rate_limit.exceeded",
          },
        };
      const form = openForm(store, params.publicKey);
      if (!form) return notFound("Form not found");
      if (!CAPTCHA_PASS_TOKENS.has(body?.captchaToken ?? ""))
        return badRequest({ captchaToken: ["Captcha verification failed."] });
      // A filled honeypot is a bot: a success that stores nothing.
      if ((body?.website ?? "").trim() !== "") return noContent();
      const answers = body?.answers ?? {};
      const errors = checkAnswers(form, answers, store);
      if (Object.keys(errors).length > 0) return badRequest(errors);
      store.forms.addSubmission(form, {
        submitterUserId: null,
        submitterEmail: null,
        payloadJson: JSON.stringify(answers),
        key: null,
      });
      return noContent();
    },
  }),
];

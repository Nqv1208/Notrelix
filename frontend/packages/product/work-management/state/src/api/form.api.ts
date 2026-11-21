import type { NotrelixClient, OperationRequestBody } from "@notrelix/contracts";
import { endpoints } from "@notrelix/contracts";
import {
  formQuestionTypeIndex,
  parseFormAnswers,
  parseFormQuestionType,
  parseFormStatus,
  parseFormSubmissionStatus,
  type Form,
  type FormQuestionChanges,
  type FormQuestionSettings,
  type NewFormQuestion,
  type FormDetail,
  type FormPublicAccess,
  type FormQuestion,
  type PublicForm,
  type FormSubmission,
  type FormSubmissionStatus,
} from "@notrelix/work-management-core";

// The producer's form reads have no typed body in the contract (it types them as void): the wire shapes are read here.
interface FormWire {
  id?: string;
  name?: string | null;
  boardId?: string;
  status?: string | null;
  createdAt?: string;
  questionCount?: number;
  newSubmissionCount?: number;
  updatedAt?: string | null;
  version?: number;
}
interface QuestionWire {
  id?: string;
  questionKey?: string | null;
  label?: string | null;
  questionType?: string | null;
  isRequired?: boolean;
  boardFieldId?: string | null;
  position?: string | null;
  placeholder?: string | null;
  helpText?: string | null;
  maxLength?: number | null;
  minValue?: number | null;
  maxValue?: number | null;
  options?: { key?: string; label?: string }[] | null;
}
interface FormDetailWire extends FormWire {
  version?: number;
  description?: string | null;
  questions?: QuestionWire[] | null;
}
interface SubmissionWire {
  id?: string;
  formId?: string;
  createdItemId?: string | null;
  submitterUserId?: string | null;
  submitterEmail?: string | null;
  status?: string | null;
  submittedAt?: string;
  processedAt?: string | null;
  payloadJson?: string | null;
  reason?: string | null;
}

interface PublicAccessWire {
  isPublic?: boolean;
  publicKey?: string | null;
}
interface PublicFormWire {
  name?: string | null;
  description?: string | null;
  captchaSiteKey?: string | null;
  questions?:
    | {
        key?: string | null;
        label?: string | null;
        type?: string | null;
        required?: boolean;
        placeholder?: string | null;
        helpText?: string | null;
        maxLength?: number | null;
        minValue?: number | null;
        maxValue?: number | null;
        options?: { key?: string; label?: string }[] | null;
      }[]
    | null;
}

/** Calls with no session: a 401 must not start a refresh or end a session that is not there. */
const ANONYMOUS = {
  skipAuthRefresh: true,
  skipGlobalErrorToast: true,
} as const;

type AddQuestionBody = OperationRequestBody<"WorkManagement.Forms.AddQuestion">;
type UpdateQuestionBody =
  OperationRequestBody<"WorkManagement.Forms.UpdateQuestion">;

/** The `configJson` of a question: only the settings it has. */
export function serializeFormQuestionConfig(
  required: boolean,
  settings: FormQuestionSettings,
): string {
  const config: Record<string, unknown> = { required };
  if (settings.placeholder) config.placeholder = settings.placeholder;
  if (settings.helpText) config.helpText = settings.helpText;
  if (typeof settings.maxLength === "number")
    config.maxLength = settings.maxLength;
  if (typeof settings.minValue === "number")
    config.minValue = settings.minValue;
  if (typeof settings.maxValue === "number")
    config.maxValue = settings.maxValue;
  if (settings.options && settings.options.length > 0)
    config.options = settings.options.map(({ key, label }) => ({ key, label }));
  return JSON.stringify(config);
}

function mapForm(row: FormWire, boardId: string): Form | null {
  if (!row.id) return null;
  return {
    id: row.id,
    boardId: row.boardId ?? boardId,
    title: row.name ?? "",
    status: parseFormStatus(row.status),
    createdAt: row.createdAt ?? "",
    ...(row.questionCount === undefined
      ? {}
      : { questionCount: row.questionCount }),
    ...(row.newSubmissionCount === undefined
      ? {}
      : { newSubmissionCount: row.newSubmissionCount }),
    ...(row.updatedAt ? { updatedAt: row.updatedAt } : {}),
    ...(row.version ? { version: row.version } : {}),
  };
}

function mapQuestion(row: QuestionWire): FormQuestion | null {
  if (!row.id || !row.questionKey) return null;
  return {
    id: row.id,
    key: row.questionKey,
    label: row.label ?? "",
    type: parseFormQuestionType(row.questionType),
    required: row.isRequired === true,
    boardFieldId: row.boardFieldId ?? null,
    position: row.position ?? "",
    placeholder: row.placeholder ?? null,
    helpText: row.helpText ?? null,
    maxLength: row.maxLength ?? null,
    minValue: row.minValue ?? null,
    maxValue: row.maxValue ?? null,
    options: (row.options ?? []).flatMap((option) =>
      option.key
        ? [{ key: option.key, label: option.label ?? option.key }]
        : [],
    ),
  };
}

type SetPublicAccessBody =
  OperationRequestBody<"WorkManagement.Forms.SetPublicAccess">;
type SubmitPublicBody =
  OperationRequestBody<"WorkManagement.PublicForms.Submit">;

function mapPublicAccess(row: PublicAccessWire | null): FormPublicAccess {
  const publicKey = row?.publicKey || null;
  return { isPublic: row?.isPublic === true && publicKey !== null, publicKey };
}

/** The version a body-less write names, as the producer's `expectedVersion` query parameter. */
function withVersion(path: string, expectedVersion: number): string {
  return `${path}?expectedVersion=${expectedVersion}`;
}

export function createFormApi(client: NotrelixClient) {
  const api = client.api;
  return {
    /** The forms of a board, newest first. */
    async list(boardId: string): Promise<Form[]> {
      const rows = await api.get<FormWire[]>(endpoints.forms.forBoard(boardId));
      return (rows ?? []).flatMap((row) => {
        const form = mapForm(row, boardId);
        return form ? [form] : [];
      });
    },

    /** A form with its questions in reading order. */
    async get(formId: string): Promise<FormDetail> {
      const row = await api.get<FormDetailWire>(endpoints.forms.detail(formId));
      const form = mapForm(row, row.boardId ?? "");
      if (!form) throw new Error("Form without an id");
      return {
        ...form,
        description: row.description ?? "",
        version: row.version ?? 0,
        questions: (row.questions ?? [])
          .flatMap((question) => {
            const mapped = mapQuestion(question);
            return mapped ? [mapped] : [];
          })
          .sort((a, b) => (a.position < b.position ? -1 : 1)),
      };
    },

    /** Creates a draft form; resolves with its id. */
    async create(boardId: string, title: string): Promise<string> {
      return api.post<string>(endpoints.forms.forBoard(boardId), { title });
    },

    /** Every write on a form names the version the caller last read (`expectedVersion`); a stale one is a 409. */
    async updateDetails(
      formId: string,
      title: string,
      description: string,
      expectedVersion: number,
    ): Promise<void> {
      await api.patch<void>(endpoints.forms.detail(formId), {
        title,
        description,
        expectedVersion,
      });
    },

    async publish(formId: string, expectedVersion: number): Promise<void> {
      await api.post<void>(
        withVersion(endpoints.forms.publish(formId), expectedVersion),
        {},
      );
    },

    async close(formId: string, expectedVersion: number): Promise<void> {
      await api.post<void>(
        withVersion(endpoints.forms.close(formId), expectedVersion),
        {},
      );
    },

    async remove(formId: string, expectedVersion: number): Promise<void> {
      await api.delete<void>(
        withVersion(endpoints.forms.detail(formId), expectedVersion),
      );
    },

    async restore(formId: string): Promise<void> {
      await api.post<void>(endpoints.forms.restore(formId), {});
    },

    async addQuestion(
      formId: string,
      question: NewFormQuestion,
      expectedVersion: number,
    ): Promise<void> {
      const body: AddQuestionBody = {
        questionKey: question.key,
        questionType: formQuestionTypeIndex(question.type) as NonNullable<
          AddQuestionBody["questionType"]
        >,
        label: question.label,
        isRequired: question.required,
        configJson: serializeFormQuestionConfig(question.required, question),
        position: question.position,
        boardFieldId: question.boardFieldId,
        expectedVersion,
      };
      await api.post<void>(endpoints.forms.questions(formId), body);
    },

    async updateQuestion(
      formId: string,
      questionId: string,
      changes: FormQuestionChanges,
      expectedVersion: number,
    ): Promise<void> {
      const body: UpdateQuestionBody = {
        label: changes.label,
        isRequired: changes.required,
        configJson: serializeFormQuestionConfig(changes.required, changes),
        position: changes.position,
        boardFieldId: changes.boardFieldId,
        expectedVersion,
      };
      await api.patch<void>(endpoints.forms.question(formId, questionId), body);
    },

    async removeQuestion(
      formId: string,
      questionId: string,
      expectedVersion: number,
    ): Promise<void> {
      await api.delete<void>(
        withVersion(
          endpoints.forms.question(formId, questionId),
          expectedVersion,
        ),
      );
    },

    /**
     * One page of the responses of a form, newest first (a deleted one is not listed); `nextCursor` goes back
     * as `cursor` for the next page and is null on the last.
     */
    async submissionsPage(
      formId: string,
      cursor?: string,
    ): Promise<{ items: FormSubmission[]; nextCursor: string | null }> {
      const page = await api.get<{
        items?: SubmissionWire[] | null;
        nextCursor?: string | null;
      }>(
        `${endpoints.forms.submissionsPage(formId)}${
          cursor ? `?cursor=${encodeURIComponent(cursor)}` : ""
        }`,
      );
      const items = (page?.items ?? []).flatMap((row) => {
        if (!row.id) return [];
        const status = parseFormSubmissionStatus(row.status, row.createdItemId);
        if (status === "deleted") return [];
        return [
          {
            id: row.id,
            formId: row.formId ?? formId,
            status,
            submitterUserId: row.submitterUserId ?? null,
            submitterEmail: row.submitterEmail ?? null,
            submittedAt: row.submittedAt ?? "",
            processedAt: row.processedAt ?? null,
            createdItemId: row.createdItemId ?? null,
            reason: row.reason ?? null,
            answers: parseFormAnswers(row.payloadJson),
          } satisfies FormSubmission,
        ];
      });
      return { items, nextCursor: page?.nextCursor ?? null };
    },

    /** Whether the form is public, with its key (workspace Owner or Admin; anyone else is refused 403/404). */
    async getFormPublicAccess(formId: string): Promise<FormPublicAccess> {
      return mapPublicAccess(
        await api.get<PublicAccessWire>(endpoints.forms.publicAccess(formId)),
      );
    },

    /** Switches the form public (a key is minted, or kept when it already is) or private (the key is cleared). */
    async setFormPublicAccess(
      formId: string,
      enabled: boolean,
    ): Promise<FormPublicAccess> {
      const body: SetPublicAccessBody = { enabled };
      return mapPublicAccess(
        await api.put<PublicAccessWire>(
          endpoints.forms.publicAccess(formId),
          body,
        ),
      );
    },

    /** The form an anonymous visitor fills; 404 for anything not open to the public, without saying why. */
    async getPublicForm(publicKey: string): Promise<PublicForm> {
      const row = await api.get<PublicFormWire>(
        endpoints.publicForms.detail(publicKey),
        ANONYMOUS,
      );
      return {
        name: row.name ?? "",
        description: row.description ?? "",
        captchaSiteKey: row.captchaSiteKey || null,
        questions: (row.questions ?? []).flatMap((question) =>
          question.key
            ? [
                {
                  id: question.key,
                  key: question.key,
                  label: question.label ?? "",
                  type: parseFormQuestionType(question.type),
                  required: question.required === true,
                  boardFieldId: null,
                  position: "",
                  placeholder: question.placeholder ?? null,
                  helpText: question.helpText ?? null,
                  maxLength: question.maxLength ?? null,
                  minValue: question.minValue ?? null,
                  maxValue: question.maxValue ?? null,
                  options: (question.options ?? []).flatMap((option) =>
                    option.key
                      ? [{ key: option.key, label: option.label ?? option.key }]
                      : [],
                  ),
                } satisfies FormQuestion,
              ]
            : [],
        ),
      };
    },

    /**
     * Sends an anonymous response. The captcha token is single use, so a retry needs a new one; the
     * honeypot (`website`) is empty for a person.
     */
    async submitPublicForm(
      publicKey: string,
      answers: Readonly<Record<string, unknown>>,
      captchaToken: string,
      honeypot = "",
    ): Promise<void> {
      const body: SubmitPublicBody = {
        answers: { ...answers },
        captchaToken,
        website: honeypot,
      };
      await api.post<void>(endpoints.publicForms.submissions(publicKey), body, {
        ...ANONYMOUS,
      });
    },

    /** The totals of all responses by tab, whatever pages are loaded. */
    async submissionCounts(
      formId: string,
    ): Promise<Record<FormSubmissionStatus, number>> {
      const wire = await api.get<{
        new?: number;
        processed?: number;
        rejected?: number;
        spam?: number;
      }>(endpoints.forms.submissionCounts(formId));
      return {
        new: wire?.new ?? 0,
        processed: wire?.processed ?? 0,
        rejected: wire?.rejected ?? 0,
        spam: wire?.spam ?? 0,
      };
    },

    /**
     * Submits answers by question key; resolves with the response id. The key
     * names the attempt: a retry with it replays the first, never a second response.
     */
    async submit(
      formId: string,
      answers: Readonly<Record<string, unknown>>,
      idempotencyKey: string,
      honeypot = "",
    ): Promise<string> {
      return api.post<string>(
        endpoints.forms.submissions(formId),
        honeypot ? { answers, website: honeypot } : { answers },
        { idempotencyKey },
      );
    },

    async process(submissionId: string, createdItemId: string): Promise<void> {
      await api.post<void>(endpoints.forms.process(submissionId), {
        createdItemId,
      });
    },

    async reject(submissionId: string, reason: string): Promise<void> {
      await api.post<void>(endpoints.forms.reject(submissionId), {
        reason: reason.trim() === "" ? null : reason.trim(),
      });
    },

    async markSpam(submissionId: string): Promise<void> {
      await api.post<void>(endpoints.forms.spam(submissionId), {});
    },

    /** Returns a rejected, spam or deleted response to the New tab: the undo of that decision. */
    async reopenSubmission(
      submissionId: string,
      clearItem = false,
    ): Promise<void> {
      await api.post<void>(
        `${endpoints.forms.reopen(submissionId)}${clearItem ? "?clearItem=true" : ""}`,
        {},
      );
    },

    async removeSubmission(submissionId: string): Promise<void> {
      await api.delete<void>(endpoints.forms.submission(submissionId));
    },
  };
}

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
  type FormQuestion,
  type FormSubmission,
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

    async updateDetails(
      formId: string,
      title: string,
      description: string,
    ): Promise<void> {
      await api.patch<void>(endpoints.forms.detail(formId), {
        title,
        description,
      });
    },

    async publish(formId: string): Promise<void> {
      await api.post<void>(endpoints.forms.publish(formId), {});
    },

    async close(formId: string): Promise<void> {
      await api.post<void>(endpoints.forms.close(formId), {});
    },

    async remove(formId: string): Promise<void> {
      await api.delete<void>(endpoints.forms.detail(formId));
    },

    async restore(formId: string): Promise<void> {
      await api.post<void>(endpoints.forms.restore(formId), {});
    },

    async addQuestion(
      formId: string,
      question: NewFormQuestion,
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
      };
      await api.post<void>(endpoints.forms.questions(formId), body);
    },

    async updateQuestion(
      formId: string,
      questionId: string,
      changes: FormQuestionChanges,
    ): Promise<void> {
      const body: UpdateQuestionBody = {
        label: changes.label,
        isRequired: changes.required,
        configJson: serializeFormQuestionConfig(changes.required, changes),
        position: changes.position,
        boardFieldId: changes.boardFieldId,
      };
      await api.patch<void>(endpoints.forms.question(formId, questionId), body);
    },

    async removeQuestion(formId: string, questionId: string): Promise<void> {
      await api.delete<void>(endpoints.forms.question(formId, questionId));
    },

    /** The responses of a form, newest first (a deleted one is not listed). */
    async submissions(formId: string): Promise<FormSubmission[]> {
      const rows = await api.get<SubmissionWire[]>(
        endpoints.forms.submissions(formId),
      );
      return (rows ?? []).flatMap((row) => {
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
    },

    /**
     * Submits answers by question key; resolves with the response id. The key
     * names the attempt: a retry with it replays the first, never a second response.
     */
    async submit(
      formId: string,
      answers: Readonly<Record<string, unknown>>,
      idempotencyKey: string,
    ): Promise<string> {
      return api.post<string>(
        endpoints.forms.submissions(formId),
        { answers },
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

    async removeSubmission(submissionId: string): Promise<void> {
      await api.delete<void>(endpoints.forms.submission(submissionId));
    },
  };
}

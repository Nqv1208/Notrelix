import { useCallback, useMemo } from "react";
import { useQueryClient } from "@tanstack/react-query";
import type { FormAnswerErrors } from "@notrelix/work-management-core";
import type { FormQuestionChanges, NewFormQuestion } from "../api/form.api";
import { wmQueryKeys } from "../queries/keys";
import { useWorkManagementServices } from "../services";

/** How a form write ended; the page says why a refusal did not apply. */
export type FormWriteResult =
  | { readonly ok: true; readonly id?: string }
  | {
      readonly ok: false;
      readonly reason:
        "forbidden" | "missing" | "conflict" | "validation" | "failed";
      /** Per question key, for a refused submit. */
      readonly errors?: FormAnswerErrors;
    };

function failure(error: unknown): FormWriteResult {
  const caught = (error ?? {}) as {
    status?: number;
    validationErrors?: Record<string, string[]>;
  };
  if (caught.status === 403) return { ok: false, reason: "forbidden" };
  if (caught.status === 404) return { ok: false, reason: "missing" };
  if (caught.status === 409 || caught.status === 412)
    return { ok: false, reason: "conflict" };
  if (
    (caught.status === 400 || caught.status === 422) &&
    caught.validationErrors
  ) {
    const errors: Record<string, string> = {};
    for (const [key, messages] of Object.entries(caught.validationErrors))
      if (messages[0]) errors[key] = messages[0];
    return { ok: false, reason: "validation", errors };
  }
  return { ok: false, reason: "failed" };
}

/**
 * Every write on the forms of a board. Not optimistic: a form's questions,
 * status and order are the server's, so what the write touched is read again
 * after every answer, also after a refusal (a form that vanished leaves the
 * list). The responses of a form are read again after each decision on one.
 */
export function useFormWrites(boardId: string, workspaceId: string) {
  const queryClient = useQueryClient();
  const { forms } = useWorkManagementServices();

  const refresh = useCallback(
    (formId?: string, submissions = false) =>
      Promise.all([
        queryClient.invalidateQueries({
          queryKey: wmQueryKeys.boardForms(workspaceId, boardId),
        }),
        formId
          ? queryClient.invalidateQueries({
              queryKey: wmQueryKeys.form(workspaceId, formId),
            })
          : undefined,
        formId && submissions
          ? queryClient.invalidateQueries({
              queryKey: wmQueryKeys.formSubmissions(workspaceId, formId),
            })
          : undefined,
      ]),
    [boardId, queryClient, workspaceId],
  );

  const write = useCallback(
    async (
      send: () => Promise<string | void>,
      formId?: string,
      submissions = false,
    ): Promise<FormWriteResult> => {
      try {
        const id = await send();
        await refresh(formId, submissions);
        return typeof id === "string" ? { ok: true, id } : { ok: true };
      } catch (caught) {
        await refresh(formId, submissions);
        return failure(caught);
      }
    },
    [refresh],
  );

  return useMemo(
    () => ({
      create: (title: string) => write(() => forms.create(boardId, title)),
      updateDetails: (formId: string, title: string, description: string) =>
        write(() => forms.updateDetails(formId, title, description), formId),
      publish: (formId: string) => write(() => forms.publish(formId), formId),
      close: (formId: string) => write(() => forms.close(formId), formId),
      remove: (formId: string) => write(() => forms.remove(formId), formId),
      restore: (formId: string) => write(() => forms.restore(formId), formId),
      addQuestion: (formId: string, question: NewFormQuestion) =>
        write(() => forms.addQuestion(formId, question), formId),
      updateQuestion: (
        formId: string,
        questionId: string,
        changes: FormQuestionChanges,
      ) =>
        write(() => forms.updateQuestion(formId, questionId, changes), formId),
      removeQuestion: (formId: string, questionId: string) =>
        write(() => forms.removeQuestion(formId, questionId), formId),
      process: (formId: string, submissionId: string, itemId: string) =>
        write(() => forms.process(submissionId, itemId), formId, true),
      reject: (formId: string, submissionId: string, reason: string) =>
        write(() => forms.reject(submissionId, reason), formId, true),
      markSpam: (formId: string, submissionId: string) =>
        write(() => forms.markSpam(submissionId), formId, true),
      removeSubmission: (formId: string, submissionId: string) =>
        write(() => forms.removeSubmission(submissionId), formId, true),
    }),
    [boardId, forms, write],
  );
}

/** Submits answers as the signed-in member; the key makes a retry replay the first attempt. */
export function useSubmitForm(formId: string, workspaceId: string) {
  const queryClient = useQueryClient();
  const { forms } = useWorkManagementServices();
  return useCallback(
    async (
      answers: Readonly<Record<string, unknown>>,
      idempotencyKey: string,
    ): Promise<FormWriteResult> => {
      try {
        const id = await forms.submit(formId, answers, idempotencyKey);
        await queryClient.invalidateQueries({
          queryKey: wmQueryKeys.formSubmissions(workspaceId, formId),
        });
        return { ok: true, id };
      } catch (caught) {
        return failure(caught);
      }
    },
    [formId, forms, queryClient, workspaceId],
  );
}

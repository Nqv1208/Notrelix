import { useCallback, useMemo, useRef } from "react";
import { useQueryClient } from "@tanstack/react-query";
import type {
  FormDetail,
  FormQuestionChanges,
  FormWriteResult,
  NewFormQuestion,
} from "@notrelix/work-management-core";
import { wmQueryKeys } from "../queries/keys";
import { useWorkManagementServices } from "../services";

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

  // The version of the form the screen last read. A form that is not open (the
  // list) has none in the cache, so it is read first.
  const versionOf = useCallback(
    async (formId: string): Promise<number> => {
      const key = wmQueryKeys.form(workspaceId, formId);
      const cached = queryClient.getQueryData<FormDetail>(key);
      if (cached) return cached.version;
      return (
        await queryClient.fetchQuery({
          queryKey: key,
          queryFn: () => forms.get(formId),
          staleTime: 0,
        })
      ).version;
    },
    [forms, queryClient, workspaceId],
  );

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

  // One write at a time: the next one reads the version the last one made.
  const queue = useRef<Promise<unknown>>(Promise.resolve());
  const write = useCallback(
    (
      send: () => Promise<string | void>,
      formId?: string,
      submissions = false,
    ): Promise<FormWriteResult> => {
      const attempt = async (): Promise<FormWriteResult> => {
        try {
          const id = await send();
          await refresh(formId, submissions);
          return typeof id === "string" ? { ok: true, id } : { ok: true };
        } catch (caught) {
          await refresh(formId, submissions);
          return failure(caught);
        }
      };
      const run = queue.current.then(attempt, attempt);
      queue.current = run;
      return run;
    },
    [refresh],
  );

  return useMemo(
    () => ({
      create: (title: string) => write(() => forms.create(boardId, title)),
      updateDetails: (formId: string, title: string, description: string) =>
        write(
          async () =>
            forms.updateDetails(
              formId,
              title,
              description,
              await versionOf(formId),
            ),
          formId,
        ),
      publish: (formId: string) =>
        write(
          async () => forms.publish(formId, await versionOf(formId)),
          formId,
        ),
      close: (formId: string) =>
        write(async () => forms.close(formId, await versionOf(formId)), formId),
      remove: (formId: string) =>
        write(
          async () => forms.remove(formId, await versionOf(formId)),
          formId,
        ),
      restore: (formId: string) => write(() => forms.restore(formId), formId),
      addQuestion: (formId: string, question: NewFormQuestion) =>
        write(
          async () =>
            forms.addQuestion(formId, question, await versionOf(formId)),
          formId,
        ),
      updateQuestion: (
        formId: string,
        questionId: string,
        changes: FormQuestionChanges,
      ) =>
        write(
          async () =>
            forms.updateQuestion(
              formId,
              questionId,
              changes,
              await versionOf(formId),
            ),
          formId,
        ),
      removeQuestion: (formId: string, questionId: string) =>
        write(
          async () =>
            forms.removeQuestion(formId, questionId, await versionOf(formId)),
          formId,
        ),
      process: (formId: string, submissionId: string, itemId: string) =>
        write(() => forms.process(submissionId, itemId), formId, true),
      reject: (formId: string, submissionId: string, reason: string) =>
        write(() => forms.reject(submissionId, reason), formId, true),
      markSpam: (formId: string, submissionId: string) =>
        write(() => forms.markSpam(submissionId), formId, true),
      removeSubmission: (formId: string, submissionId: string) =>
        write(() => forms.removeSubmission(submissionId), formId, true),
    }),
    [boardId, forms, versionOf, write],
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

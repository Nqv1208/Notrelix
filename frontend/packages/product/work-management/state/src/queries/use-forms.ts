import { useQueries, useQuery } from "@tanstack/react-query";
import type {
  Form,
  FormDetail,
  FormSubmission,
} from "@notrelix/work-management-core";
import { useWorkManagementServices } from "../services";
import { wmQueryKeys } from "./keys";

/** The forms of a board (a manager of the board reads them; a member opens one by its link). */
export function useBoardForms(
  boardId: string,
  workspaceId: string,
  enabled = true,
) {
  const { forms } = useWorkManagementServices();
  return useQuery<Form[]>({
    queryKey: wmQueryKeys.boardForms(workspaceId, boardId),
    queryFn: () => forms.list(boardId),
    enabled: enabled && Boolean(boardId && workspaceId),
    staleTime: 30_000,
  });
}

/** One form with its questions. */
export function useForm(formId: string, workspaceId: string, enabled = true) {
  const { forms } = useWorkManagementServices();
  return useQuery<FormDetail>({
    queryKey: wmQueryKeys.form(workspaceId, formId),
    queryFn: () => forms.get(formId),
    enabled: enabled && Boolean(formId && workspaceId),
    staleTime: 15_000,
    // A form the member cannot see answers 403/404: not worth three tries.
    retry: (count, error) => {
      const status = (error as { status?: number }).status;
      return status === 403 || status === 404 ? false : count < 2;
    },
  });
}

/** The responses of a form. */
export function useFormSubmissions(
  formId: string,
  workspaceId: string,
  enabled = true,
) {
  const { forms } = useWorkManagementServices();
  return useQuery<FormSubmission[]>({
    queryKey: wmQueryKeys.formSubmissions(workspaceId, formId),
    queryFn: () => forms.submissions(formId),
    enabled: enabled && Boolean(formId && workspaceId),
    staleTime: 10_000,
  });
}

/** What a list row shows besides the form: its question count and its responses (read apart, so each may still be loading). */
export function useBoardFormFacts(
  forms: readonly Form[],
  workspaceId: string,
  enabled = true,
) {
  const { forms: api } = useWorkManagementServices();
  const details = useQueries({
    queries: forms.map((form) => ({
      queryKey: wmQueryKeys.form(workspaceId, form.id),
      queryFn: () => api.get(form.id),
      enabled: enabled && Boolean(workspaceId),
      staleTime: 15_000,
    })),
  });
  const responses = useQueries({
    queries: forms.map((form) => ({
      queryKey: wmQueryKeys.formSubmissions(workspaceId, form.id),
      queryFn: () => api.submissions(form.id),
      enabled: enabled && Boolean(workspaceId),
      staleTime: 10_000,
    })),
  });
  return forms.map((form, index) => {
    const detail = details[index]?.data;
    const list = responses[index]?.data;
    return {
      formId: form.id,
      questionCount: detail?.questions.length,
      newCount: list?.filter((s) => s.status === "new").length,
      totalCount: list?.length,
    };
  });
}

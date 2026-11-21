import { useInfiniteQuery, useQuery } from "@tanstack/react-query";
import type {
  Form,
  FormDetail,
  FormPublicAccess,
  FormSubmission,
  PublicForm,
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

/** The tab totals of a form's responses from the server, over all pages; writes invalidate it with the pages. */
export function useFormSubmissionCounts(
  formId: string,
  workspaceId: string,
  enabled = true,
) {
  const { forms } = useWorkManagementServices();
  return useQuery({
    queryKey: [...wmQueryKeys.formSubmissions(workspaceId, formId), "counts"],
    queryFn: () => forms.submissionCounts(formId),
    enabled: enabled && Boolean(formId && workspaceId),
    staleTime: 10_000,
  });
}

/**
 * The responses of a form, a page at a time (newest first); `data` stays the flat list. Use `hasNextPage` and
 * `fetchNextPage` for "Xem thêm phản hồi". Writes invalidate the key, so the pages are read again from the first.
 */
export function useFormSubmissions(
  formId: string,
  workspaceId: string,
  enabled = true,
) {
  const { forms } = useWorkManagementServices();
  return useInfiniteQuery({
    queryKey: wmQueryKeys.formSubmissions(workspaceId, formId),
    queryFn: ({ pageParam }) =>
      forms.submissionsPage(formId, pageParam ?? undefined),
    initialPageParam: null as string | null,
    getNextPageParam: (last) => last.nextCursor,
    select: (data): FormSubmission[] =>
      data.pages.flatMap((page) => page.items),
    enabled: enabled && Boolean(formId && workspaceId),
    staleTime: 10_000,
  });
}

/** Whether a form is public, and its key. Only a workspace Owner or Admin reads it: anyone else gets 403/404. */
export function useFormPublicAccess(
  formId: string,
  workspaceId: string,
  enabled = true,
) {
  const { forms } = useWorkManagementServices();
  return useQuery<FormPublicAccess>({
    queryKey: wmQueryKeys.formPublicAccess(workspaceId, formId),
    queryFn: () => forms.getFormPublicAccess(formId),
    enabled: enabled && Boolean(formId && workspaceId),
    staleTime: 15_000,
    retry: (count, error) => {
      const status = (error as { status?: number }).status;
      return status === 403 || status === 404 ? false : count < 2;
    },
  });
}

/** The form behind a public link, read with no session. A 404 is final: the link opens nothing. */
export function usePublicForm(publicKey: string) {
  const { forms } = useWorkManagementServices();
  return useQuery<PublicForm>({
    queryKey: wmQueryKeys.publicForm(publicKey),
    queryFn: () => forms.getPublicForm(publicKey),
    enabled: publicKey !== "",
    staleTime: 60_000,
    retry: (count, error) => {
      const status = (error as { status?: number }).status;
      return status === 404 || status === 429 ? false : count < 2;
    },
  });
}

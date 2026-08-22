import { useMutation, useQueryClient } from "@tanstack/react-query";
import { wmQueryKeys } from "../queries/keys";
import type { CreateChecklistItemInput } from "../api/checklist.api";
import { useWorkManagementServices } from "../services";

export function useCreateChecklistItem(
  cardId: string,
  checklistId: string,
  workspaceId?: string,
) {
  const queryClient = useQueryClient();
  const { checklists } = useWorkManagementServices();
  const queryKey = wmQueryKeys.cardChecklists(workspaceId!, cardId);

  return useMutation<
    void,
    Error,
    Omit<CreateChecklistItemInput, "checklistId">
  >({
    mutationFn: (input) =>
      checklists.createChecklistItem({ ...input, checklistId }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey });
    },
  });
}

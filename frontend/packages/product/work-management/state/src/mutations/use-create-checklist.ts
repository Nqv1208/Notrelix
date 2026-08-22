import { useMutation, useQueryClient } from "@tanstack/react-query";
import { wmQueryKeys } from "../queries/keys";
import type { CreateChecklistInput } from "../api/checklist.api";
import { useWorkManagementServices } from "../services";

export function useCreateChecklist(cardId: string, workspaceId?: string) {
  const queryClient = useQueryClient();
  const { checklists } = useWorkManagementServices();
  const queryKey = wmQueryKeys.cardChecklists(workspaceId!, cardId);

  return useMutation<void, Error, Omit<CreateChecklistInput, "cardId">>({
    mutationFn: (input) => checklists.createChecklist({ ...input, cardId }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey });
    },
  });
}

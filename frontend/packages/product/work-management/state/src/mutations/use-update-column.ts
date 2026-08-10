import { useMutation, useQueryClient } from "@tanstack/react-query";
import { wmQueryKeys } from "../queries/keys";
import { useWorkManagementServices } from "../services";
import type { UpdateColumnInput } from "../api/field.api";

export function useUpdateColumn(boardId: string, workspaceId?: string) {
  const queryClient = useQueryClient();
  const { columns } = useWorkManagementServices();
  const queryKey = wmQueryKeys.fullBoard(workspaceId!, boardId);

  return useMutation<void, Error, Omit<UpdateColumnInput, "boardId">>({
    mutationFn: (input) => columns.updateColumn({ ...input, boardId }),
    onSuccess: () => queryClient.invalidateQueries({ queryKey }),
  });
}

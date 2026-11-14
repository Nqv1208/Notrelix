import { useMutation, useQueryClient } from "@tanstack/react-query";
import { wmQueryKeys } from "../queries/keys";
import { useWorkManagementServices } from "../services";

/**
 * Resolves or reopens a comment thread, then reads the list again whether the write worked or was refused
 * (a refusal leaves the list as the server holds it; the caller shows the words).
 */
export function useSetCardUpdateResolved(cardId: string, workspaceId: string) {
  const queryClient = useQueryClient();
  const { comments } = useWorkManagementServices();

  return useMutation({
    mutationFn: (input: { updateId: string; resolved: boolean }) =>
      comments.setCardUpdateResolved(input.updateId, input.resolved),
    onSettled: () =>
      queryClient.invalidateQueries({
        queryKey: wmQueryKeys.cardUpdates(workspaceId, cardId),
      }),
  });
}

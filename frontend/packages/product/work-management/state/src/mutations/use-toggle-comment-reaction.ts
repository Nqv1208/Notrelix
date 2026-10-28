import { useMutation, useQueryClient } from "@tanstack/react-query";
import { wmQueryKeys } from "../queries/keys";
import { useWorkManagementServices } from "../services";

/** Adds or withdraws the viewer's emoji on a comment, then reads the list again (no realtime push yet). */
export function useToggleCommentReaction(cardId: string, workspaceId: string) {
  const queryClient = useQueryClient();
  const { comments } = useWorkManagementServices();

  return useMutation({
    mutationFn: (input: { commentId: string; emoji: string; on: boolean }) =>
      comments.setCommentReaction(
        cardId,
        input.commentId,
        input.emoji,
        input.on,
      ),
    onSettled: () =>
      queryClient.invalidateQueries({
        queryKey: wmQueryKeys.cardUpdates(workspaceId, cardId),
      }),
  });
}

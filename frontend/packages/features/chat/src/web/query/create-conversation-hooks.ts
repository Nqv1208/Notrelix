import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import type { ChatService } from "../../core/api/chat.service";
import { upsertConversation } from "../../core/model/conversation-model";
import type {
  ChatConversation,
  ChatStartConversationInput,
} from "../../core/types/chat";
import { chatQueryKeys } from "../../query/keys";

const CONVERSATION_STALE_MS = 30_000;
/** The list refreshes on this interval until the realtime stream pushes previews. */
const CONVERSATION_REFRESH_MS = 60_000;

function newKey(): string {
  return crypto.randomUUID();
}

/**
 * The hooks of direct and group conversations and of person blocks (Chat.Conversations.*, Chat.Blocks.Set).
 * Hiding, showing and blocking are optimistic and roll back when the server refuses.
 */
export function createConversationHooks(service: ChatService) {
  /** The caller's conversations, newest activity first; hidden ones are not in the server list. */
  function useConversations(workspaceId: string) {
    return useQuery({
      queryKey: chatQueryKeys.conversations(workspaceId),
      queryFn: () => service.listConversations(workspaceId),
      staleTime: CONVERSATION_STALE_MS,
      refetchInterval: CONVERSATION_REFRESH_MS,
      enabled: workspaceId.length > 0,
    });
  }

  /** Starts (or returns the existing) direct conversation, or a group. Each call carries a fresh Idempotency-Key. */
  function useStartConversation(workspaceId: string) {
    const queryClient = useQueryClient();
    const key = chatQueryKeys.conversations(workspaceId);
    return useMutation({
      mutationFn: (input: ChatStartConversationInput) =>
        service.startConversation(workspaceId, input, newKey()),
      onSuccess: (result) => {
        // Starting a direct conversation again shows it in the list again.
        queryClient.setQueryData<ChatConversation[]>(key, (list) =>
          upsertConversation(list, { ...result.conversation, isHidden: false }),
        );
        void queryClient.invalidateQueries({ queryKey: key });
        void queryClient.invalidateQueries({
          queryKey: chatQueryKeys.unread(workspaceId),
        });
      },
    });
  }

  /** Adds people to a group; the list refetches to get the new participants and the system message preview. */
  function useAddParticipants(workspaceId: string, conversationId: string) {
    const queryClient = useQueryClient();
    return useMutation({
      mutationFn: (userIds: readonly string[]) =>
        service.addConversationParticipants(
          workspaceId,
          conversationId,
          userIds,
        ),
      onSuccess: () => {
        void queryClient.invalidateQueries({
          queryKey: chatQueryKeys.conversations(workspaceId),
        });
        void queryClient.invalidateQueries({
          queryKey: chatQueryKeys.messages(workspaceId, conversationId),
        });
      },
    });
  }

  /** Leaves a group: it leaves the list at once and its cached messages are dropped. */
  function useLeaveConversation(workspaceId: string) {
    const queryClient = useQueryClient();
    const key = chatQueryKeys.conversations(workspaceId);
    return useMutation({
      mutationFn: (conversationId: string) =>
        service.leaveConversation(workspaceId, conversationId),
      onSuccess: (_result, conversationId) => {
        queryClient.setQueryData<ChatConversation[]>(key, (list) =>
          list?.filter((entry) => entry.id !== conversationId),
        );
        queryClient.removeQueries({
          queryKey: chatQueryKeys.messages(workspaceId, conversationId),
        });
        void queryClient.invalidateQueries({ queryKey: key });
        void queryClient.invalidateQueries({
          queryKey: chatQueryKeys.unread(workspaceId),
        });
      },
    });
  }

  /**
   * Hide or show a conversation in the caller's own list. Optimistic: the flag flips in the cache at once
   * and goes back to what it was when the server refuses.
   */
  function useSetConversationVisibility(workspaceId: string) {
    const queryClient = useQueryClient();
    const key = chatQueryKeys.conversations(workspaceId);
    return useMutation({
      mutationFn: (input: { conversationId: string; hidden: boolean }) =>
        service.setConversationVisibility(
          workspaceId,
          input.conversationId,
          input.hidden,
        ),
      onMutate: async (input) => {
        await queryClient.cancelQueries({ queryKey: key });
        const previous = queryClient.getQueryData<ChatConversation[]>(key);
        queryClient.setQueryData<ChatConversation[]>(key, (list) =>
          list?.map((entry) =>
            entry.id === input.conversationId
              ? { ...entry, isHidden: input.hidden }
              : entry,
          ),
        );
        return { previous };
      },
      onError: (_error, _input, context) => {
        if (context) queryClient.setQueryData(key, context.previous);
      },
      onSettled: () => {
        void queryClient.invalidateQueries({ queryKey: key });
      },
    });
  }

  /** The people the caller blocked in this session (client-only: the API has no read of blocks). */
  function useBlockedPeople(workspaceId: string) {
    return useQuery<readonly string[]>({
      queryKey: chatQueryKeys.blocks(workspaceId),
      queryFn: () => [],
      initialData: [],
      staleTime: Infinity,
      gcTime: Infinity,
      enabled: false,
    });
  }

  /** Block or unblock a person. Optimistic; rolls back when the server refuses. */
  function useBlockPerson(workspaceId: string) {
    const queryClient = useQueryClient();
    const key = chatQueryKeys.blocks(workspaceId);
    return useMutation({
      mutationFn: (input: { userId: string; blocked: boolean }) =>
        service.setPersonBlock(workspaceId, input.userId, input.blocked),
      onMutate: async (input) => {
        await queryClient.cancelQueries({ queryKey: key });
        const previous = queryClient.getQueryData<readonly string[]>(key) ?? [];
        const rest = previous.filter((id) => id !== input.userId);
        queryClient.setQueryData<readonly string[]>(
          key,
          input.blocked ? [...rest, input.userId] : rest,
        );
        return { previous };
      },
      onError: (_error, _input, context) => {
        if (context) queryClient.setQueryData(key, context.previous);
      },
    });
  }

  return {
    useConversations,
    useStartConversation,
    useAddParticipants,
    useLeaveConversation,
    useSetConversationVisibility,
    useBlockedPeople,
    useBlockPerson,
  };
}

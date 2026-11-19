import { useMutation, useQueryClient } from "@tanstack/react-query";
import type { ChatService } from "../../core/api/chat.service";
import {
  applyGroupMoves,
  applyPlacements,
} from "../../core/model/sidebar-order-model";
import type {
  ChatChannel,
  ChatChannelGroup,
  ChatChannelPlacement,
  ChatCreateChannelGroupInput,
} from "../../core/types/chat";
import { chatQueryKeys } from "../../query/keys";

function newKey(): string {
  return crypto.randomUUID();
}

/** One category written back with a new name, a new position, or both. */
export interface ChannelGroupChange {
  readonly group: ChatChannelGroup;
  readonly name: string;
  readonly position: number;
}

/**
 * The hooks that manage and order the sidebar categories and channels (Chat.ChannelGroups.Create, Update, Delete and
 * Chat.Channels.Reorder). Rename, move, delete and reorder are optimistic and go back when the server refuses; every
 * settle reads the server again, so a stale version (412) ends on the current order.
 */
export function createChannelGroupHooks(service: ChatService) {
  /** Create a category; the Idempotency-Key is fresh per call. The new heading shows once the server answers. */
  function useCreateChannelGroup(workspaceId: string) {
    const queryClient = useQueryClient();
    return useMutation({
      mutationFn: (input: ChatCreateChannelGroupInput) =>
        service.createChannelGroup(workspaceId, input, newKey()),
      onSuccess: (group) => {
        queryClient.setQueryData<ChatChannelGroup[]>(
          chatQueryKeys.channelGroups(workspaceId),
          (list) => (list ? [...list, group] : [group]),
        );
        void queryClient.invalidateQueries({
          queryKey: chatQueryKeys.channelGroups(workspaceId),
        });
      },
    });
  }

  /** Rename or move one or more categories (several only when a renumber is needed). */
  function useUpdateChannelGroups(workspaceId: string) {
    const queryClient = useQueryClient();
    const key = chatQueryKeys.channelGroups(workspaceId);
    return useMutation({
      mutationFn: (changes: readonly ChannelGroupChange[]) =>
        Promise.all(
          changes.map((change) =>
            service.updateChannelGroup(workspaceId, change.group.id, {
              name: change.name,
              position: change.position,
              expectedVersion: change.group.version,
            }),
          ),
        ),
      onMutate: async (changes) => {
        await queryClient.cancelQueries({ queryKey: key });
        const previous = queryClient.getQueryData<ChatChannelGroup[]>(key);
        const names = new Map(changes.map((c) => [c.group.id, c.name]));
        queryClient.setQueryData<ChatChannelGroup[]>(key, (list) =>
          list
            ? applyGroupMoves(
                list.map((group) => ({
                  ...group,
                  name: names.get(group.id) ?? group.name,
                })),
                changes.map((c) => ({ group: c.group, position: c.position })),
              )
            : list,
        );
        return { previous };
      },
      onError: (_error, _changes, context) => {
        if (context) queryClient.setQueryData(key, context.previous);
      },
      onSettled: () => void queryClient.invalidateQueries({ queryKey: key }),
    });
  }

  /** Delete a category; its channels stay and show as ungrouped at once. */
  function useDeleteChannelGroup(workspaceId: string) {
    const queryClient = useQueryClient();
    const groupsKey = chatQueryKeys.channelGroups(workspaceId);
    const channelsKey = chatQueryKeys.channels(workspaceId);
    return useMutation({
      mutationFn: (group: ChatChannelGroup) =>
        service.deleteChannelGroup(workspaceId, group.id, group.version),
      onMutate: async (group) => {
        await queryClient.cancelQueries({ queryKey: groupsKey });
        await queryClient.cancelQueries({ queryKey: channelsKey, exact: true });
        const groups = queryClient.getQueryData<ChatChannelGroup[]>(groupsKey);
        const channels = queryClient.getQueryData<ChatChannel[]>(channelsKey);
        queryClient.setQueryData<ChatChannelGroup[]>(groupsKey, (list) =>
          list?.filter((entry) => entry.id !== group.id),
        );
        queryClient.setQueryData<ChatChannel[]>(channelsKey, (list) =>
          list?.map((channel) =>
            channel.groupId === group.id
              ? { ...channel, groupId: null }
              : channel,
          ),
        );
        return { groups, channels };
      },
      onError: (_error, _group, context) => {
        if (!context) return;
        queryClient.setQueryData(groupsKey, context.groups);
        queryClient.setQueryData(channelsKey, context.channels);
      },
      onSettled: () => {
        void queryClient.invalidateQueries({ queryKey: groupsKey });
        void queryClient.invalidateQueries({ queryKey: channelsKey });
      },
    });
  }

  /** Move channels between categories and positions (`Chat.Channels.Reorder`); the sidebar follows at once. */
  function useReorderChannels(workspaceId: string) {
    const queryClient = useQueryClient();
    const key = chatQueryKeys.channels(workspaceId);
    return useMutation({
      mutationFn: (placements: readonly ChatChannelPlacement[]) =>
        service.reorderChannels(workspaceId, placements),
      onMutate: async (placements) => {
        await queryClient.cancelQueries({ queryKey: key, exact: true });
        const previous = queryClient.getQueryData<ChatChannel[]>(key);
        queryClient.setQueryData<ChatChannel[]>(key, (list) =>
          list ? applyPlacements(list, placements) : list,
        );
        return { previous };
      },
      onError: (_error, _placements, context) => {
        if (context) queryClient.setQueryData(key, context.previous);
      },
      onSettled: () => void queryClient.invalidateQueries({ queryKey: key }),
    });
  }

  return {
    useCreateChannelGroup,
    useUpdateChannelGroups,
    useDeleteChannelGroup,
    useReorderChannels,
  };
}

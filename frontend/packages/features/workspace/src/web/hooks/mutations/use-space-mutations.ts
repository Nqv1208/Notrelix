import { useMutation, useQueryClient } from "@tanstack/react-query";
import {
  createSpacesService,
  type CreateSpaceInput,
  type MoveSpaceInput,
  type SpaceDetailsInput,
  type SpacesApiClient,
  type SpacesEndpoints,
} from "../../../core/api/spaces.service";
import {
  applySpaceMove,
  type SpaceNode,
  type SpaceRole,
  type SpaceSubjectType,
} from "../../../core/model/spaces";
import { workspaceQueryKeys } from "../../../query/keys";

interface SpaceMutationDeps {
  api: SpacesApiClient;
  endpoints: SpacesEndpoints;
}

export interface SpaceMemberWrite {
  readonly spaceId: string;
  readonly subjectType: SpaceSubjectType;
  readonly subjectId: string;
  /** The Space version read from the tree (`expectedVersion`). */
  readonly expectedVersion: number;
}

/**
 * Space writes (OT-5). Only a move is optimistic: it is a reversible local
 * reorder of the cached tree, rolled back when the producer rejects it.
 * Everything else waits for the producer. No write is retried automatically;
 * a stale version (409/412) surfaces as a conflict and every write refreshes
 * the tree, so the next attempt carries the current version.
 */
export function createUseSpaceMutations({ api, endpoints }: SpaceMutationDeps) {
  const service = createSpacesService(api, endpoints);

  return function useSpaceMutations(workspaceId: string) {
    const queryClient = useQueryClient();
    const treeKey = workspaceQueryKeys.spaceTree(workspaceId);
    const refreshTree = () =>
      queryClient.invalidateQueries({ queryKey: treeKey });
    const refreshMembers = async (spaceId: string) => {
      await queryClient.invalidateQueries({
        queryKey: workspaceQueryKeys.spaceMembers(workspaceId, spaceId),
      });
      await refreshTree();
    };

    const create = useMutation({
      mutationFn: (input: CreateSpaceInput) =>
        service.create(workspaceId, input),
      retry: false,
      onSettled: refreshTree,
    });

    const move = useMutation({
      mutationFn: (input: MoveSpaceInput) => service.move(workspaceId, input),
      retry: false,
      onMutate: async (input: MoveSpaceInput) => {
        await queryClient.cancelQueries({ queryKey: treeKey });
        const previous = queryClient.getQueryData<SpaceNode[]>(treeKey);
        if (previous) {
          queryClient.setQueryData<SpaceNode[]>(
            treeKey,
            applySpaceMove(previous, input.spaceId, input.parentId),
          );
        }
        return { previous };
      },
      onError: (_error, _input, context) => {
        if (context?.previous) {
          queryClient.setQueryData(treeKey, context.previous);
        }
      },
      onSettled: refreshTree,
    });

    const updateDetails = useMutation({
      mutationFn: ({
        spaceId,
        ...input
      }: SpaceDetailsInput & { readonly spaceId: string }) =>
        service.updateDetails(workspaceId, spaceId, input),
      retry: false,
      onSettled: refreshTree,
    });

    const archive = useMutation({
      mutationFn: (spaceId: string) => service.archive(workspaceId, spaceId),
      retry: false,
      onSettled: refreshTree,
    });

    const unarchive = useMutation({
      mutationFn: (spaceId: string) => service.unarchive(workspaceId, spaceId),
      retry: false,
      onSettled: refreshTree,
    });

    const addMember = useMutation({
      mutationFn: ({
        spaceId,
        ...input
      }: SpaceMemberWrite & { role: SpaceRole }) =>
        service.addMember(workspaceId, spaceId, input),
      retry: false,
      onSettled: (_result, _error, input) => refreshMembers(input.spaceId),
    });

    const changeMemberRole = useMutation({
      mutationFn: ({
        spaceId,
        ...input
      }: SpaceMemberWrite & { role: SpaceRole }) =>
        service.changeMemberRole(workspaceId, spaceId, input),
      retry: false,
      onSettled: (_result, _error, input) => refreshMembers(input.spaceId),
    });

    const removeMember = useMutation({
      mutationFn: ({ spaceId, ...input }: SpaceMemberWrite) =>
        service.removeMember(workspaceId, spaceId, input),
      retry: false,
      onSettled: (_result, _error, input) => refreshMembers(input.spaceId),
    });

    return {
      create,
      move,
      updateDetails,
      archive,
      unarchive,
      addMember,
      changeMemberRole,
      removeMember,
    };
  };
}

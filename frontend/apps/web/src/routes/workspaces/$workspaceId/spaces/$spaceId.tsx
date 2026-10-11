import { useCallback, useEffect, useMemo, useState } from "react";
import { useNavigate, useParams, useSearch } from "@tanstack/react-router";
import { toast } from "sonner";
import { createUsePageList } from "@notrelix/docs-state";
import {
  buildSpaceIndex,
  childrenOf,
  describeWorkspaceFailure,
  spaceMemberKey,
  spacePath,
  type SpaceMemberEntry,
  type SpaceRole,
  type SpaceTab,
} from "@notrelix/features-workspace/core";
import {
  formatCopy,
  useWorkspaceHomeCopy,
  useWorkspaceSpacesCopy,
  WorkspaceSpaceSurface,
  type AddSpaceMemberInput,
  type SectionState,
  type SpaceContent,
  type SpaceDetailsValues,
  type SpaceMemberCandidate,
  type SpaceResource,
  type SpaceSaveStatus,
} from "@notrelix/features-workspace/web";
import { useFormatters } from "@notrelix/i18n";
import { useFeatureRuntimeDependencies } from "@notrelix/runtime-web";
import { useWorkspaceContext } from "@/providers/workspace-provider";
import { useBoardPlacements } from "@/shell/workspace/board-placement-bridge";
import { toastFailure } from "@/shell/workspace/failure-toast";
import { sectionState } from "@/shell/workspace/section-state";
import { useCreateResource } from "@/shell/workspace/use-create-resource";
import { useCreateSpace } from "@/shell/workspace/use-create-space";
import { useMoveToSpace } from "@/shell/workspace/use-move-to-space";
import { useShellNavigation } from "@/shell/workspace/use-shell-navigation";
import { useWorkspaceResources } from "@/shell/workspace/use-workspace-resources";
import { useWorkspaceViewer } from "@/shell/workspace/use-workspace-viewer";
import {
  spaceRecoveryHint,
  useSpaceHooks,
} from "@/shell/workspace/use-workspace-spaces";

const TABS: readonly SpaceTab[] = ["overview", "members", "settings"];

/**
 * A Space page (OT-5): composes the Workspaces Space producer (tree,
 * members, teams, writes) with the Board placements (Work Management) and
 * the Doc list (Documents) into the pure surface. The member's access on
 * the Space shapes the actions; every write is authorized by the server and
 * a conflict reloads the latest Space.
 */
export function SpacePage() {
  const { workspaceId, workspace } = useWorkspaceContext();
  const { spaceId } = useParams({ strict: false }) as { spaceId: string };
  const search = useSearch({ strict: false }) as { tab?: SpaceTab };
  const tab: SpaceTab = TABS.includes(search.tab as SpaceTab)
    ? (search.tab as SpaceTab)
    : "overview";
  const navigate = useNavigate();
  const copy = useWorkspaceSpacesCopy();
  const homeCopy = useWorkspaceHomeCopy();
  const formatters = useFormatters();
  const hint = useMemo(() => spaceRecoveryHint(copy), [copy]);
  const navigation = useShellNavigation();
  const hooks = useSpaceHooks();
  const { api, endpoints } = useFeatureRuntimeDependencies();
  const usePageList = useMemo(
    () => createUsePageList(api, endpoints),
    [api, endpoints],
  );

  const viewer = useWorkspaceViewer(workspaceId);
  const tree = hooks.useSpaceTree(workspaceId);
  const index = useMemo(() => buildSpaceIndex(tree.data ?? []), [tree.data]);
  const node = index.byId.get(spaceId) ?? null;
  const canManage = node?.access === "Manager";
  const members = hooks.useSpaceMembers(workspaceId, node ? spaceId : null);
  const teams = hooks.useWorkspaceTeams(workspaceId, canManage);
  const resources = useWorkspaceResources(workspaceId);
  const placements = useBoardPlacements(workspaceId);
  const pages = usePageList(workspaceId);
  const mutations = hooks.useSpaceMutations(workspaceId);
  const mover = useMoveToSpace();

  const createResource = useCreateResource(
    workspaceId,
    homeCopy,
    node ? { id: node.id, name: node.name } : null,
  );
  const createSpace = useCreateSpace(workspaceId, workspace?.name ?? null);

  const [pendingMemberKeys, setPendingMemberKeys] = useState<
    ReadonlySet<string>
  >(() => new Set());
  const [addError, setAddError] = useState<string | null>(null);
  const [addResetKey, setAddResetKey] = useState(0);
  const [saveStatus, setSaveStatus] = useState<SpaceSaveStatus>("idle");
  const [archiveRefused, setArchiveRefused] = useState(false);

  // A different Space is a new page: drop its transient write state.
  useEffect(() => {
    setSaveStatus("idle");
    setArchiveRefused(false);
    setAddError(null);
  }, [spaceId]);

  const space: SectionState<typeof node> = tree.isError
    ? { status: "error" }
    : !tree.isSuccess
      ? { status: "loading" }
      : { status: "ready", data: node };

  const content = useMemo<SectionState<SpaceContent>>(() => {
    if (resources.failed || placements.isError || pages.isError)
      return { status: "error" };
    if (!resources.ready || !placements.data || !pages.data)
      return { status: "loading" };
    const inSpace = new Set(
      placements.data
        .filter((placement) => placement.spaceId === spaceId)
        .map((placement) => placement.id),
    );
    const boards: SpaceResource[] = resources.boards
      .filter((board) => inSpace.has(board.id))
      .map((board) => ({
        id: board.id,
        name: board.name,
        updatedAt: board.updatedAt,
      }));
    const docs: SpaceResource[] = pages.data
      .filter((page) => page.spaceId === spaceId && !page.parentId)
      .map((page) => ({
        id: page.id,
        name: page.title,
        updatedAt: page.updatedAt ?? null,
      }));
    return { status: "ready", data: { boards, docs } };
  }, [
    pages.data,
    pages.isError,
    placements.data,
    placements.isError,
    resources,
    spaceId,
  ]);

  const candidateUsers = useMemo<
    SectionState<readonly SpaceMemberCandidate[]>
  >(() => {
    const state = sectionState(viewer.members, viewer.members.data);
    if (state.status !== "ready") return state;
    return {
      status: "ready",
      data: state.data
        .filter((member) => member.membership !== "suspended")
        .map((member) => ({
          id: member.userId,
          name: member.name,
          avatarUrl: member.avatarUrl ?? null,
        })),
    };
  }, [viewer.members]);

  const setPending = useCallback((key: string, pending: boolean) => {
    setPendingMemberKeys((current) => {
      const next = new Set(current);
      if (pending) next.add(key);
      else next.delete(key);
      return next;
    });
  }, []);

  const onTabChange = useCallback(
    (next: SpaceTab) =>
      void navigate({
        to: "/workspaces/$workspaceId/spaces/$spaceId",
        params: { workspaceId, spaceId },
        search: next === "overview" ? {} : { tab: next },
        replace: true,
      } as never),
    [navigate, spaceId, workspaceId],
  );

  const memberWrite = useCallback(
    (
      entry: SpaceMemberEntry,
      run: (expectedVersion: number) => Promise<unknown>,
      messages: { success: string; failure: string },
    ) => {
      if (!node) return;
      const key = spaceMemberKey(entry);
      setPending(key, true);
      run(node.version)
        .then(() =>
          toast.success(
            formatCopy(messages.success, { name: entry.displayName }),
          ),
        )
        .catch((error: unknown) =>
          toastFailure(
            formatCopy(messages.failure, { name: entry.displayName }),
            error,
            hint,
          ),
        )
        .finally(() => setPending(key, false));
    },
    [hint, node, setPending],
  );

  const onChangeMemberRole = useCallback(
    (entry: SpaceMemberEntry, role: SpaceRole) =>
      memberWrite(
        entry,
        (expectedVersion) =>
          mutations.changeMemberRole.mutateAsync({
            spaceId,
            subjectType: entry.subjectType,
            subjectId: entry.subjectId,
            role,
            expectedVersion,
          }),
        {
          success: formatCopy(copy.toasts.roleChanged, {
            name: "{{name}}",
            role: copy.roles[role],
          }),
          failure: copy.toasts.roleFailed,
        },
      ),
    [copy, memberWrite, mutations.changeMemberRole, spaceId],
  );

  const onRemoveMember = useCallback(
    (entry: SpaceMemberEntry) =>
      memberWrite(
        entry,
        (expectedVersion) =>
          mutations.removeMember.mutateAsync({
            spaceId,
            subjectType: entry.subjectType,
            subjectId: entry.subjectId,
            expectedVersion,
          }),
        {
          success: copy.toasts.memberRemoved,
          failure: copy.toasts.memberRemoveFailed,
        },
      ),
    [copy, memberWrite, mutations.removeMember, spaceId],
  );

  const onAddMember = useCallback(
    (input: AddSpaceMemberInput) => {
      if (!node) return;
      setAddError(null);
      const name =
        input.subjectType === "Team"
          ? (teams.data?.find((team) => team.id === input.subjectId)?.name ??
            "")
          : ((candidateUsers.status === "ready"
              ? candidateUsers.data.find((user) => user.id === input.subjectId)
                  ?.name
              : undefined) ?? "");
      mutations.addMember.mutate(
        { spaceId, ...input, expectedVersion: node.version },
        {
          onSuccess: () => {
            setAddResetKey((key) => key + 1);
            toast.success(formatCopy(copy.toasts.memberAdded, { name }));
          },
          onError: (error) => {
            const failure = describeWorkspaceFailure(error);
            if (failure.kind === "rule" || failure.kind === "validation") {
              setAddError(
                failure.detail ??
                  Object.values(failure.fieldErrors).flat()[0] ??
                  copy.toasts.unknown,
              );
              return;
            }
            toastFailure(
              formatCopy(copy.toasts.memberAddFailed, { name }),
              error,
              hint,
            );
          },
        },
      );
    },
    [
      candidateUsers,
      copy,
      hint,
      mutations.addMember,
      node,
      spaceId,
      teams.data,
    ],
  );

  const onSave = useCallback(
    (changes: Partial<SpaceDetailsValues>) => {
      setSaveStatus("saving");
      mutations.updateDetails.mutate(
        { spaceId, ...changes },
        {
          onSuccess: () => {
            setSaveStatus("saved");
            toast.success(copy.toasts.saved);
          },
          onError: (error) => {
            setSaveStatus("idle");
            toastFailure(copy.toasts.saveFailed, error, hint);
          },
        },
      );
    },
    [copy, hint, mutations.updateDetails, spaceId],
  );

  const onArchive = useCallback(() => {
    if (!node) return;
    setArchiveRefused(false);
    mutations.archive.mutate(spaceId, {
      onSuccess: () =>
        toast.success(formatCopy(copy.toasts.archived, { name: node.name })),
      onError: (error) => {
        const failure = toastFailure(
          formatCopy(copy.toasts.archiveFailed, { name: node.name }),
          error,
          hint,
        );
        // WSP-022: content the member cannot see can still block it.
        if (failure.kind === "rule") setArchiveRefused(true);
      },
    });
  }, [copy, hint, mutations.archive, node, spaceId]);

  const onUnarchive = useCallback(() => {
    if (!node) return;
    mutations.unarchive.mutate(spaceId, {
      onSuccess: () =>
        toast.success(formatCopy(copy.toasts.unarchived, { name: node.name })),
      onError: (error) =>
        toastFailure(
          formatCopy(copy.toasts.unarchiveFailed, { name: node.name }),
          error,
          hint,
        ),
    });
  }, [copy, hint, mutations.unarchive, node, spaceId]);

  return (
    <>
      <WorkspaceSpaceSurface
        copy={copy}
        navigation={navigation}
        formatters={formatters}
        space={space}
        path={node ? spacePath(index, spaceId) : []}
        childSpaces={node ? childrenOf(index, spaceId) : []}
        content={content}
        members={sectionState(members, members.data)}
        candidateUsers={candidateUsers}
        candidateTeams={sectionState(teams, teams.data)}
        tab={tab}
        onTabChange={onTabChange}
        canMoveBoards={viewer.role !== null && viewer.role !== "guest"}
        pendingMemberKeys={pendingMemberKeys}
        addingMember={mutations.addMember.isPending}
        addMemberError={addError}
        addMemberResetKey={addResetKey}
        saveStatus={saveStatus}
        archiving={mutations.archive.isPending}
        archiveRefused={archiveRefused}
        unarchiving={mutations.unarchive.isPending}
        onCreateChild={() =>
          node && createSpace.open({ id: node.id, name: node.name })
        }
        onCreateBoard={() => createResource.open("board")}
        onCreateDoc={() => createResource.open("doc")}
        onMoveSpace={() => node && mover.openForSpace(node)}
        onMoveBoard={(board) => mover.openForBoard(board)}
        onUnarchive={onUnarchive}
        onSave={onSave}
        onArchive={onArchive}
        onAddMember={onAddMember}
        onChangeMemberRole={onChangeMemberRole}
        onRemoveMember={onRemoveMember}
        onRetry={() => void tree.refetch()}
        onRetryContent={() => {
          resources.retry();
          if (placements.isError) void placements.refetch();
          if (pages.isError) void pages.refetch();
        }}
        onRetryMembers={() => void members.refetch()}
      />
      {mover.dialog}
      {createResource.dialog}
      {createSpace.dialog}
    </>
  );
}

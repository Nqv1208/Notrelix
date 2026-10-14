import { useCallback, useEffect, useMemo, useState } from "react";
import { useParams, useSearch } from "@tanstack/react-router";
import { toast } from "sonner";
import { createUsePageList } from "@notrelix/docs-state";
import {
  buildSpaceIndex,
  childrenOf,
  describeWorkspaceFailure,
  identityToneFor,
  markToneFor,
  spaceMemberKey,
  spacePath,
  type SpaceMemberEntry,
  type SpaceRole,
  type SpaceTab,
} from "@notrelix/features-workspace/core";
import {
  buildAccessView,
  buildHeadPeople,
  formatCopy,
  useWorkspaceHomeCopy,
  useWorkspaceSpacesCopy,
  WorkspaceSpaceSurface,
  type AddSpaceMemberInput,
  type SectionState,
  type SpaceBoardRowView,
  type SpaceAddPreviewBase,
  type SpaceChildView,
  type SpaceContentView,
  type SpaceDetailsValues,
  type SpaceDocRowView,
  type SpaceDirectoryPerson,
  type SpaceMemberCandidate,
  type TeamPersonView,
  type SpaceSaveStatus,
} from "@notrelix/features-workspace/web";
import { useFeatureRuntimeDependencies } from "@notrelix/runtime-web";
import { useWorkspaceContext } from "@/providers/workspace-provider";
import { useBoardPlacements } from "@/shell/workspace/board-placement-bridge";
import { toastFailure } from "@/shell/workspace/failure-toast";
import { sectionState } from "@/shell/workspace/section-state";
import { useCreateResource } from "@/shell/workspace/use-create-resource";
import { useCreateSpace } from "@/shell/workspace/use-create-space";
import { useMoveToSpace } from "@/shell/workspace/use-move-to-space";
import { useNow } from "@/shell/workspace/use-now";
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
  const copy = useWorkspaceSpacesCopy();
  const homeCopy = useWorkspaceHomeCopy();
  const now = useNow();
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
  const [expandedTeams, setExpandedTeams] = useState<readonly string[]>([]);
  const teamMembers = hooks.useTeamsMembers(workspaceId, expandedTeams);
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
  const [addDialogKey, setAddDialogKey] = useState(0);
  const [addProgress, setAddProgress] = useState<{
    done: number;
    total: number;
  } | null>(null);
  const [saveStatus, setSaveStatus] = useState<SpaceSaveStatus>("idle");
  const [archiveRefused, setArchiveRefused] = useState(false);

  // A different Space is a new page: drop its transient write state.
  useEffect(() => {
    setSaveStatus("idle");
    setArchiveRefused(false);
    setAddError(null);
    setExpandedTeams([]);
  }, [spaceId]);

  const space: SectionState<typeof node> = tree.isError
    ? { status: "error" }
    : !tree.isSuccess
      ? { status: "loading" }
      : { status: "ready", data: node };

  const content = useMemo<SectionState<SpaceContentView>>(() => {
    if (resources.failed || placements.isError || pages.isError)
      return { status: "error" };
    if (!resources.ready || !placements.data || !pages.data)
      return { status: "loading" };
    const placementOf = new Map(
      placements.data.map((placement) => [placement.id, placement]),
    );
    const boards: SpaceBoardRowView[] = resources.boards
      .filter((board) => placementOf.get(board.id)?.spaceId === spaceId)
      .map((board) => {
        const placement = placementOf.get(board.id);
        return {
          id: board.id,
          name: board.name,
          icon: "kanban",
          meta: null,
          isPrivate: placement?.isPrivate ?? false,
          addedCount: placement?.memberCount ?? null,
          progress: null,
          attention: null,
          people: null,
          updatedAt: board.updatedAt,
        };
      });
    const docs: SpaceDocRowView[] = pages.data
      .filter((page) => page.spaceId === spaceId && !page.parentId)
      .map((page) => ({
        id: page.id,
        name: page.title,
        meta: null,
        editor: null,
        updatedAt: page.updatedAt ?? null,
      }))
      .sort((a, b) => (b.updatedAt ?? "").localeCompare(a.updatedAt ?? ""));
    return {
      status: "ready",
      data: {
        boards,
        boardTotal: boards.length,
        docs: docs.slice(0, 5),
        docTotal: docs.length,
      },
    };
  }, [
    pages.data,
    pages.isError,
    placements.data,
    placements.isError,
    resources,
    spaceId,
  ]);

  const childSpaces = useMemo<SpaceChildView[]>(() => {
    const visibleBoards = new Set(resources.boards.map((board) => board.id));
    return (node ? childrenOf(index, spaceId) : [])
      .filter((child) => !child.isArchived)
      .map((child) => ({
        id: child.id,
        name: child.name,
        markTone: markToneFor(child.id),
        isPrivate: child.visibility === "Private",
        boards: placements.data
          ? placements.data.filter(
              (placement) =>
                placement.spaceId === child.id &&
                visibleBoards.has(placement.id),
            ).length
          : null,
        people: null,
      }));
  }, [index, node, placements.data, resources.boards, spaceId]);

  const memberEntries = members.data ?? null;
  const people = useMemo(
    () =>
      memberEntries
        ? buildHeadPeople(copy, memberEntries, identityToneFor)
        : null,
    [copy, memberEntries],
  );
  const access = useMemo(
    () =>
      node
        ? buildAccessView(copy, node, memberEntries, teams.data ?? null, {
            meId: viewer.userId,
          })
        : null,
    [copy, memberEntries, node, teams.data, viewer.userId],
  );
  const firstSteps =
    canManage &&
    content.status === "ready" &&
    content.data.boardTotal === 0 &&
    content.data.docTotal === 0 &&
    childSpaces.length === 0 &&
    memberEntries !== null &&
    memberEntries.length <= 1;
  const ancestors = useMemo(
    () =>
      node
        ? spacePath(index, spaceId)
            .slice(0, -1)
            .map((item) => item.name)
        : [],
    [index, node, spaceId],
  );

  const directory = useMemo(() => {
    const map = new Map<string, SpaceDirectoryPerson>();
    for (const member of viewer.members.data ?? []) {
      map.set(member.userId, {
        email: null,
        workspaceRole: member.role,
      });
    }
    return map;
  }, [viewer.members.data]);

  const teamPeople = useMemo(() => {
    const map = new Map<string, SectionState<readonly TeamPersonView[]>>();
    expandedTeams.forEach((teamId, index) => {
      const query = teamMembers[index];
      if (!query) return;
      map.set(
        teamId,
        query.isError
          ? { status: "error" }
          : !query.data
            ? { status: "loading" }
            : {
                status: "ready",
                data: query.data.map((member) => ({
                  id: member.userId,
                  name: member.displayName,
                  tone: identityToneFor(member.userId),
                  note: null,
                })),
              },
      );
    });
    return map;
  }, [expandedTeams, teamMembers]);

  const previewBase = useMemo<SpaceAddPreviewBase | null>(() => {
    if (content.status !== "ready") return null;
    const boards = content.data.boards;
    return {
      openBoards: boards.filter((board) => !board.isPrivate).length,
      docs: content.data.docTotal,
      openChildren: childSpaces
        .filter((child) => !child.isPrivate)
        .map((child) => child.name),
      hidden: [
        ...childSpaces
          .filter((child) => child.isPrivate)
          .map((child) => child.name),
        ...boards.filter((board) => board.isPrivate).map((board) => board.name),
      ],
    };
  }, [childSpaces, content]);

  const formatDay = useCallback((iso: string) => {
    const date = new Date(iso);
    const pad = (value: number) => String(value).padStart(2, "0");
    return `${pad(date.getDate())}/${pad(date.getMonth() + 1)}`;
  }, []);

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
          email: null,
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

  const onAddMembers = useCallback(
    async (inputs: readonly AddSpaceMemberInput[]) => {
      if (!node || inputs.length === 0) return;
      setAddError(null);
      setAddProgress({ done: 0, total: inputs.length });
      let version = node.version;
      let done = 0;
      for (const input of inputs) {
        const name =
          input.subjectType === "Team"
            ? (teams.data?.find((team) => team.id === input.subjectId)?.name ??
              "")
            : ((candidateUsers.status === "ready"
                ? candidateUsers.data.find(
                    (user) => user.id === input.subjectId,
                  )?.name
                : undefined) ?? "");
        try {
          await mutations.addMember.mutateAsync({
            spaceId,
            ...input,
            expectedVersion: version,
          });
          done += 1;
          setAddProgress({ done, total: inputs.length });
          // Every write moves the Space version: read it before the next one.
          const fresh = await tree.refetch();
          version =
            fresh.data?.find((candidate) => candidate.id === spaceId)
              ?.version ?? version + 1;
        } catch (error) {
          const failure = describeWorkspaceFailure(error);
          if (failure.kind === "rule" || failure.kind === "validation") {
            setAddError(
              failure.detail ??
                Object.values(failure.fieldErrors).flat()[0] ??
                copy.toasts.unknown,
            );
          } else {
            toastFailure(
              formatCopy(copy.toasts.memberAddFailed, { name }),
              error,
              hint,
            );
          }
          break;
        }
      }
      setAddProgress(null);
      if (done === inputs.length) {
        toast.success(
          inputs.length === 1
            ? formatCopy(copy.toasts.memberAdded, {
                name:
                  inputs[0]?.subjectType === "Team"
                    ? (teams.data?.find((t) => t.id === inputs[0]?.subjectId)
                        ?.name ?? "")
                    : ((candidateUsers.status === "ready"
                        ? candidateUsers.data.find(
                            (user) => user.id === inputs[0]?.subjectId,
                          )?.name
                        : undefined) ?? ""),
              })
            : formatCopy(copy.toasts.membersAdded, { count: done }),
        );
        setAddDialogKey((key) => key + 1);
      }
    },
    [
      candidateUsers,
      copy,
      hint,
      mutations.addMember,
      node,
      spaceId,
      teams.data,
      tree,
    ],
  );

  const onExpandTeam = useCallback((teamId: string) => {
    setExpandedTeams((current) =>
      current.includes(teamId) ? current : [...current, teamId],
    );
  }, []);

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
        now={now}
        space={space}
        workspaceName={workspace?.name ?? ""}
        ancestors={ancestors}
        tab={tab}
        people={people}
        peopleTotal={null}
        contentCount={
          content.status === "ready"
            ? content.data.boardTotal + content.data.docTotal
            : null
        }
        membersCount={memberEntries ? memberEntries.length : null}
        content={content}
        childSpaces={childSpaces}
        access={access}
        attention={null}
        workload={null}
        templates={[]}
        firstSteps={firstSteps}
        viewAs={null}
        members={sectionState(members, members.data)}
        candidateUsers={candidateUsers}
        candidateTeams={sectionState(teams, teams.data)}
        pendingMemberKeys={pendingMemberKeys}
        addingMember={mutations.addMember.isPending}
        addMemberError={addError}
        addProgress={addProgress}
        addDialogKey={addDialogKey}
        removingMember={mutations.removeMember.isPending}
        directory={directory}
        meId={viewer.userId}
        teamPeople={teamPeople}
        previewBase={previewBase}
        formatDay={formatDay}
        onExpandTeam={onExpandTeam}
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
        onUnarchive={onUnarchive}
        onSave={onSave}
        onArchive={onArchive}
        onDelete={null}
        onAddMembers={onAddMembers}
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

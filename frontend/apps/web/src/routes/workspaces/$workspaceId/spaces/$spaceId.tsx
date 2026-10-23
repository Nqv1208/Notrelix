import { useCallback, useEffect, useMemo, useState } from "react";
import { useNavigate, useParams, useSearch } from "@tanstack/react-router";
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
  buildAccessViewFromSummary,
  buildHeadPeople,
  buildHeadPeopleOfTotal,
  formatCopy,
  useWorkspaceHomeCopy,
  useWorkspaceSpacesCopy,
  WorkspaceSpaceSurface,
  type AddSpaceMemberInput,
  type SectionState,
  type SpaceAttentionRowView,
  type SpaceBoardRowView,
  type SpaceAddPreviewBase,
  type SpaceChildView,
  type SpaceContentView,
  type SpaceDetailsValues,
  type SpaceDocRowView,
  type SpaceDirectoryPerson,
  type SpaceGuestView,
  type SpacePendingInviteView,
  spaceWhenLabel,
  type SpaceTemplateView,
  type SpaceWorkloadRowView,
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
import { useSpaceMembersExtra } from "@/shell/workspace/use-space-members-extra";
import { useSpaceOverview } from "@/shell/workspace/use-space-overview";
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
  const search = useSearch({ strict: false }) as {
    tab?: SpaceTab;
    as?: "member";
  };
  const navigate = useNavigate();
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
  const asMember = search.as === "member" && canManage;
  const members = hooks.useSpaceMembers(workspaceId, node ? spaceId : null);
  const teams = hooks.useWorkspaceTeams(workspaceId, canManage);
  const [expandedTeams, setExpandedTeams] = useState<readonly string[]>([]);
  const teamMembers = hooks.useTeamsMembers(workspaceId, expandedTeams);
  const resources = useWorkspaceResources(workspaceId);
  const placements = useBoardPlacements(workspaceId);
  const pages = usePageList(workspaceId);
  const mutations = hooks.useSpaceMutations(workspaceId);
  const overview = useSpaceOverview(
    workspaceId,
    spaceId,
    asMember,
    [
      placements.data?.filter((placement) => placement.spaceId === spaceId)
        .length ?? 0,
      pages.data?.filter((page) => page.spaceId === spaceId).length ?? 0,
      node?.version ?? 0,
      members.data?.length ?? 0,
    ].join(":"),
  );
  const mover = useMoveToSpace();

  const extra = useSpaceMembersExtra(
    workspaceId,
    spaceId,
    tab === "members",
    [node?.version ?? 0, members.data?.length ?? 0].join(":"),
  );

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

  const content = useMemo<SectionState<SpaceContentView>>(() => {
    if (resources.failed || placements.isError || pages.isError)
      return { status: "error" };
    if (!resources.ready || !placements.data || !pages.data)
      return { status: "loading" };
    const placementOf = new Map(
      placements.data.map((placement) => [placement.id, placement]),
    );
    const fallbackBoards: SpaceBoardRowView[] = resources.boards
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
    // The summary (when the producer offers it) knows the figures of each
    // Board, which Boards run, and counts only what the member may see.
    const boards: SpaceBoardRowView[] = overview
      ? overview.boards.map((row) => ({
          id: row.id,
          name: row.name,
          icon: row.kind,
          meta: row.meta,
          isPrivate: row.isPrivate,
          addedCount: row.addedCount,
          progress: row.progress,
          attention: row.attention,
          people: row.people.map((person) => ({
            id: person.id,
            name: person.name,
            tone: identityToneFor(person.id),
          })),
          updatedAt: row.updatedAt,
        }))
      : fallbackBoards;
    const titleOf = new Map(pages.data.map((page) => [page.id, page.title]));
    const rootDocs: SpaceDocRowView[] = pages.data
      .filter((page) => page.spaceId === spaceId && !page.parentId)
      .map((page) => ({
        id: page.id,
        name: page.title,
        meta: null,
        editor: null,
        updatedAt: page.updatedAt ?? null,
      }))
      .sort((a, b) => (b.updatedAt ?? "").localeCompare(a.updatedAt ?? ""));
    const docs: SpaceDocRowView[] = overview
      ? overview.docs.map((row) => ({
          id: row.id,
          name: titleOf.get(row.id) ?? "",
          meta: row.meta,
          editor: row.editorName
            ? formatCopy(copy.overview.editedBy, { name: row.editorName })
            : null,
          updatedAt: row.updatedAt,
        }))
      : rootDocs;
    return {
      status: "ready",
      data: {
        boards,
        boardTotal: overview ? overview.boardTotal : boards.length,
        docs: docs.slice(0, 5),
        docTotal: overview ? overview.docTotal : rootDocs.length,
      },
    };
  }, [
    copy,
    overview,
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
        boards:
          overview?.children.find((c) => c.id === child.id)?.boards ??
          (placements.data
            ? placements.data.filter(
                (placement) =>
                  placement.spaceId === child.id &&
                  visibleBoards.has(placement.id),
              ).length
            : null),
        people:
          overview?.children.find((c) => c.id === child.id)?.people ?? null,
      }))
      .filter((child) => !asMember || !child.isPrivate);
  }, [
    asMember,
    index,
    node,
    overview,
    placements.data,
    resources.boards,
    spaceId,
  ]);

  const memberEntries = members.data ?? null;
  const people = useMemo(
    () =>
      overview
        ? buildHeadPeopleOfTotal(
            copy,
            overview.people.slice(0, tab === "members" ? 3 : 4),
            overview.peopleTotal,
            identityToneFor,
          )
        : memberEntries
          ? buildHeadPeople(copy, memberEntries, identityToneFor)
          : null,
    [copy, memberEntries, overview, tab],
  );
  const access = useMemo(
    () =>
      !node
        ? null
        : overview
          ? buildAccessViewFromSummary(copy, node, {
              peopleTotal: overview.peopleTotal,
              ...overview.access,
            })
          : buildAccessView(copy, node, memberEntries, teams.data ?? null, {
              meId: viewer.userId,
            }),
    [copy, memberEntries, node, overview, teams.data, viewer.userId],
  );
  const attention = useMemo<readonly SpaceAttentionRowView[] | null>(() => {
    if (!overview || overview.attention.length === 0) return null;
    const labels = {
      overdue: copy.aside.attentionOverdue,
      unassigned: copy.aside.attentionUnassigned,
      review: copy.aside.attentionReview,
      stale: copy.aside.attentionStale,
    } as const;
    return overview.attention.map((row) => ({
      id: row.id,
      tone:
        row.id === "overdue" ? "over" : row.id === "stale" ? "mute" : "warn",
      count: row.count,
      label: labels[row.id],
      href: row.boardId
        ? navigation.getHref({ section: "board", boardId: row.boardId })
        : null,
    }));
  }, [copy, navigation, overview]);
  const workload = useMemo<readonly SpaceWorkloadRowView[] | null>(() => {
    if (!overview || overview.workload.length === 0) return null;
    // One item is 12.5% of the bar; a busier person stretches the scale.
    const scale = Math.max(8, ...overview.workload.map((row) => row.active));
    return overview.workload.map((row) => ({
      id: row.person.id,
      person: {
        id: row.person.id,
        name: row.person.name,
        tone: identityToneFor(row.person.id),
      },
      active: row.active,
      overdue: row.overdue,
      activePct: Math.round(((row.active - row.overdue) / scale) * 100),
      overduePct: Math.round((row.overdue / scale) * 100),
    }));
  }, [overview]);
  const templates = useMemo<readonly SpaceTemplateView[]>(
    () => overview?.templates ?? [],
    [overview],
  );
  // The page names a department "Phòng X" where the tree keeps the short name.
  const displayNode = useMemo(
    () =>
      node && node.type === "Department"
        ? {
            ...node,
            name: formatCopy(copy.page.departmentName, { name: node.name }),
          }
        : node,
    [copy, node],
  );
  const space: SectionState<typeof displayNode> = tree.isError
    ? { status: "error" }
    : !tree.isSuccess
      ? { status: "loading" }
      : { status: "ready", data: displayNode };
  const firstSteps =
    canManage &&
    content.status === "ready" &&
    content.data.boardTotal === 0 &&
    content.data.docTotal === 0 &&
    childSpaces.length === 0 &&
    memberEntries !== null &&
    memberEntries.length <= 1;
  // A department just made says who made it and leaves the rest for later.
  const accessView = useMemo(() => {
    if (!access || !firstSteps) return access;
    const creator =
      overview?.access.admins.length === 1 &&
      overview.access.admins[0]?.id === viewer.userId;
    return {
      ...access,
      summary: null,
      rows: access.rows
        .filter((row) => row.key !== "Viewer")
        .map((row) =>
          row.key === "admins" && creator
            ? { ...row, note: copy.aside.youCreator }
            : row,
        ),
    };
  }, [access, copy, firstSteps, overview, viewer.userId]);
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
    // The producer's extras: email, job line, last activity, the creator.
    for (const person of extra?.people ?? []) {
      const known = map.get(person.userId);
      const name =
        memberEntries?.find((entry) => entry.subjectId === person.userId)
          ?.displayName ?? "";
      map.set(person.userId, {
        email: person.email,
        workspaceRole: known?.workspaceRole ?? null,
        title: person.title,
        shortName: name ? (name.split(/\s+/).pop() ?? null) : null,
        sourceNote: person.creator ? copy.members.creator : null,
        activity: person.activeNow
          ? copy.members.activeNow
          : person.lastActiveAt
            ? spaceWhenLabel(copy, person.lastActiveAt, now)
            : null,
      });
    }
    return map;
  }, [copy, extra, memberEntries, now, viewer.members.data]);

  // Where a person also joins through a team of the department.
  const also = useMemo(() => {
    const map = new Map<string, { team: string; role: SpaceRole }>();
    for (const person of extra?.people ?? []) {
      const team = person.viaTeams[0];
      if (!team) continue;
      const role =
        memberEntries?.find(
          (entry) => entry.subjectType === "Team" && entry.displayName === team,
        )?.role ?? "Member";
      map.set(person.userId, { team, role });
    }
    return map;
  }, [extra, memberEntries]);

  // Waiting invitations are for the people who manage the department.
  const invites = useMemo<readonly SpacePendingInviteView[]>(
    () =>
      (canManage ? (extra?.invitations ?? []) : []).map((invite) => ({
        id: invite.id,
        email: invite.email,
        name: invite.name,
        sent: formatCopy(copy.members.inviteSent, {
          when: spaceWhenLabel(copy, invite.sentAt, now),
        }),
        role: invite.role,
      })),
    [canManage, copy, extra, now],
  );

  const guests = useMemo<readonly SpaceGuestView[]>(() => {
    const pad = (value: number) => String(value).padStart(2, "0");
    return (extra?.guests ?? []).map((guest) => {
      const until = guest.expiresAt ? new Date(guest.expiresAt) : null;
      return {
        id: guest.userId,
        name: guest.name,
        tone: identityToneFor(guest.userId),
        detail:
          formatCopy(copy.members.aside.guestLine, {
            company: guest.outside
              ? copy.members.aside.guestOutside
              : copy.members.aside.guestInside,
            level: copy.members.aside.guestLevel[guest.level],
            board: guest.boardName,
          }) +
          (until
            ? formatCopy(copy.members.aside.guestUntil, {
                date: `${pad(until.getDate())}/${pad(until.getMonth() + 1)}/${until.getFullYear()}`,
              })
            : ""),
        destination: { section: "board", boardId: guest.boardId },
      };
    });
  }, [copy, extra]);

  const guestsByEmail = useMemo(
    () =>
      new Map(
        (extra?.guests ?? [])
          .filter((guest) => guest.email)
          .map((guest) => [
            (guest.email ?? "").toLowerCase(),
            { name: guest.name, board: guest.boardName },
          ]),
      ),
    [extra],
  );

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
                  note: directory.get(member.userId)?.title ?? null,
                })),
              },
      );
    });
    return map;
  }, [directory, expandedTeams, teamMembers]);

  const previewBase = useMemo<SpaceAddPreviewBase | null>(() => {
    if (content.status !== "ready") return null;
    const boards = content.data.boards;
    return {
      openBoards: overview
        ? overview.boardTotal -
          overview.boards.filter((board) => board.isPrivate).length
        : boards.filter((board) => !board.isPrivate).length,
      docs: content.data.docTotal,
      openChildren: childSpaces
        .filter((child) => !child.isPrivate)
        .map((child) => child.name),
      hidden: [
        ...childSpaces
          .filter((child) => child.isPrivate)
          .map((child) => child.name),
        ...boards
          .filter((board) => board.isPrivate)
          .map((board) =>
            formatCopy(copy.members.add.hiddenBoard, { name: board.name }),
          ),
      ],
    };
  }, [childSpaces, content, copy, overview]);

  const formatDay = useCallback((iso: string) => {
    const date = new Date(iso);
    const pad = (value: number) => String(value).padStart(2, "0");
    return `${pad(date.getDate())}/${pad(date.getMonth() + 1)}`;
  }, []);

  const candidateUsers = useMemo<
    SectionState<readonly SpaceMemberCandidate[]>
  >(() => {
    // A Guest cannot list people (and cannot add any), so the picker is empty.
    if (viewer.role === "guest") return { status: "ready", data: [] };
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
  }, [viewer.members, viewer.role]);

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
        peopleTotal={overview?.peopleTotal ?? null}
        contentCount={
          content.status === "ready"
            ? content.data.boardTotal + content.data.docTotal
            : null
        }
        membersCount={
          overview?.peopleTotal ?? (memberEntries ? memberEntries.length : null)
        }
        content={content}
        childSpaces={childSpaces}
        access={accessView}
        attention={attention}
        workload={workload}
        templates={templates}
        firstSteps={firstSteps}
        viewAs={
          asMember
            ? {
                onExit: () =>
                  void navigate({
                    to: ".",
                    search: (previous: Record<string, unknown>) => ({
                      ...previous,
                      as: undefined,
                    }),
                  }),
              }
            : null
        }
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
        also={also}
        invites={invites}
        guests={guests}
        expandTeams
        guestsByEmail={guestsByEmail}
        membersPageSize={7}
        membersSummary={extra?.summary ?? null}
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

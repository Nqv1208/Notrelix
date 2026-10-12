import {
  useCallback,
  useEffect,
  useMemo,
  useRef,
  useState,
  type ReactNode,
} from "react";
import { useMatches, useNavigate } from "@tanstack/react-router";
import { createUsePageList } from "@notrelix/docs-state";
import { createUseLogout, useCurrentUser } from "@notrelix/features-auth";
import { createNotificationBell } from "@notrelix/features-notifications";
import {
  buildSpaceIndex,
  buildWorkspaceBreadcrumb,
  canCreateRootSpace,
  identityToneFor,
  markToneFor,
  spaceAncestors,
  spacePath,
  NAVIGATION_RESOURCE_KIND,
  resolveNavigationEntries,
  type NavigationEntry,
  type NavigationReference,
  type WorkspaceDestination,
} from "@notrelix/features-workspace/core";
import {
  commandShortcutLabel,
  createUseRecordRecent,
  createUseTogglePin,
  createUseWorkspaceList,
  createUseWorkspaceMembers,
  createUseWorkspacePins,
  createUseWorkspaceRecents,
  useShellShortcuts,
  useSpaceTreeExpansion,
  useWorkspaceShellCopy,
  useWorkspaceSpacesCopy,
  WorkspaceCommandPaletteSurface,
  WorkspaceShortcutSheetSurface,
  WorkspaceSidebarSurface,
  WorkspaceTopbarSurface,
  workspaceBreadcrumbLabels,
  type NavigationListState,
  type ShellNavigation,
  type ShellPaletteSelection,
  type ShellShortcutId,
  type SpaceTreeState,
} from "@notrelix/features-workspace/web";
import {
  useAppRuntime,
  useFeatureRuntimeDependencies,
} from "@notrelix/runtime-web";
import { SidebarProvider, useTheme } from "@notrelix/ui-web";
import { useWorkspaceBoards } from "@notrelix/work-management-state";
import { useWorkspaceContext } from "@/providers/workspace-provider";
import { CreateWorkspaceDialog } from "./create-workspace-dialog";
import { useCreateSpace } from "./use-create-space";
import { useSpaceHooks } from "./use-workspace-spaces";
import {
  destinationHref,
  resolveShellLocation,
  routerTarget,
} from "./workspace-destinations";

const RECENT_LIMIT = 8;

function listState(
  references: {
    data?: NavigationReference[];
    isLoading: boolean;
    isError: boolean;
  },
  catalog: {
    ready: boolean;
    failed: boolean;
    boards: { id: string; name: string }[];
    docs: { id: string; name: string }[];
  },
  pinned: readonly NavigationReference[],
  select: (entries: NavigationEntry[]) => NavigationEntry[] = (entries) =>
    entries,
): NavigationListState {
  if (references.isError || catalog.failed) return { status: "error" };
  if (references.isLoading || !catalog.ready || !references.data)
    return { status: "loading" };
  return {
    status: "ready",
    entries: select(resolveNavigationEntries(references.data, catalog, pinned)),
  };
}

/**
 * The workspace shell (WX-SHELL-001…005): one sidebar, the topbar, the
 * command palette, the shortcut sheet and one scroll container. The app
 * composes Work Management and Docs data into the workspace surfaces.
 */
export function WorkspaceShell({ children }: { children: ReactNode }) {
  const { workspaceId, workspace } = useWorkspaceContext();
  const copy = useWorkspaceShellCopy();
  const navigate = useNavigate();
  const matches = useMatches();
  const user = useCurrentUser();
  const theme = useTheme();
  const { api: runtimeClient } = useAppRuntime();
  const featureDeps = useFeatureRuntimeDependencies();

  const hooks = useMemo(() => {
    const deps = { api: runtimeClient.api, endpoints: runtimeClient.endpoints };
    return {
      useWorkspaceList: createUseWorkspaceList(deps),
      useMembers: createUseWorkspaceMembers({ api: runtimeClient.api }),
      useRecents: createUseWorkspaceRecents(deps),
      usePins: createUseWorkspacePins(deps),
      useTogglePin: createUseTogglePin(deps),
      useRecordRecent: createUseRecordRecent(deps),
      usePageList: createUsePageList(featureDeps.api, featureDeps.endpoints),
      NotificationBell: createNotificationBell(deps),
      useLogout: createUseLogout({
        api: runtimeClient.api,
        endpoints: runtimeClient.endpoints,
        navigate: (options) =>
          navigate({ to: options.to, replace: options.replace }),
        getSearchParams: () => new URLSearchParams(window.location.search),
      }),
    };
  }, [runtimeClient, featureDeps, navigate]);

  const workspaces = hooks.useWorkspaceList();
  const members = hooks.useMembers(workspaceId);
  const recents = hooks.useRecents(workspaceId);
  const pins = hooks.usePins(workspaceId);
  const boards = useWorkspaceBoards(workspaceId);
  const pages = hooks.usePageList(workspaceId);
  const togglePin = hooks.useTogglePin(workspaceId);
  const recordRecent = hooks.useRecordRecent();
  const logout = hooks.useLogout();

  const location = useMemo(
    () =>
      resolveShellLocation(
        matches.map((match) => ({
          routeId: match.routeId,
          params: match.params as Record<string, string | undefined>,
          search: match.search,
        })),
      ),
    [matches],
  );

  // ── Navigation port ─────────────────────────────────────────────
  const goTo = useCallback(
    (destination: WorkspaceDestination) => {
      const target = routerTarget(workspaceId, destination);
      void navigate({
        to: target.to,
        params: target.params,
        search: target.search,
      } as never);
    },
    [navigate, workspaceId],
  );
  const navigation = useMemo<ShellNavigation>(
    () => ({
      getHref: (destination) => destinationHref(workspaceId, destination),
      onNavigate: goTo,
    }),
    [goTo, workspaceId],
  );

  // ── Record a recent when a Board or Doc opens (fire-and-forget) ─
  const lastRecorded = useRef<string | null>(null);
  useEffect(() => {
    if (location?.section !== "board" && location?.section !== "doc") {
      lastRecorded.current = null;
      return;
    }
    const kind =
      location.section === "board"
        ? NAVIGATION_RESOURCE_KIND.board
        : NAVIGATION_RESOURCE_KIND.doc;
    const resourceId =
      location.section === "board" ? location.boardId : location.docId;
    const key = `${workspaceId}:${kind}:${resourceId}`;
    if (lastRecorded.current === key) return;
    lastRecorded.current = key;
    recordRecent({ workspaceId, kind, resourceId });
  }, [location, recordRecent, workspaceId]);

  // ── Authorized catalog for names (WX-DEC-011) ───────────────────
  const catalog = useMemo(
    () => ({
      ready: boards.isSuccess && pages.isSuccess,
      failed: boards.isError || pages.isError,
      boards: (boards.data ?? []).map((board) => ({
        id: board.id,
        name: board.title,
      })),
      docs: (pages.data ?? []).map((page) => ({
        id: page.id,
        name: page.title,
      })),
    }),
    [
      boards.data,
      boards.isError,
      boards.isSuccess,
      pages.data,
      pages.isError,
      pages.isSuccess,
    ],
  );

  const pinnedReferences = useMemo(() => pins.data ?? [], [pins.data]);
  const pinnedState = useMemo(
    () => listState(pins, catalog, pinnedReferences),
    [pins, catalog, pinnedReferences],
  );
  const recentState = useMemo(
    () =>
      listState(recents, catalog, pinnedReferences, (entries) =>
        entries.slice(0, RECENT_LIMIT),
      ),
    [recents, catalog, pinnedReferences],
  );

  const retryNavigation = useCallback(() => {
    void recents.refetch();
    void pins.refetch();
    if (boards.isError) void boards.refetch();
    if (pages.isError) void pages.refetch();
  }, [recents, pins, boards, pages]);

  // ── Space org tree (OT-5) ───────────────────────────────────────
  const spacesCopy = useWorkspaceSpacesCopy();
  const spaceHooks = useSpaceHooks();
  const spaceTree = spaceHooks.useSpaceTree(workspaceId);
  const expansion = useSpaceTreeExpansion(user?.id ?? null, workspaceId);
  const createSpace = useCreateSpace(workspaceId, workspace?.name ?? null);
  const activeSpaceId = location?.section === "space" ? location.spaceId : null;
  const spaceIndex = useMemo(
    () => buildSpaceIndex(spaceTree.data ?? []),
    [spaceTree.data],
  );
  const { reveal } = expansion;
  useEffect(() => {
    if (!activeSpaceId) return;
    reveal(spaceAncestors(spaceIndex, activeSpaceId).map((node) => node.id));
  }, [activeSpaceId, reveal, spaceIndex]);
  // Archived Spaces leave the sidebar; their pages stay reachable by link.
  // An archived Space never has active children (WSP-022).
  const sidebarSpaces = useMemo<SpaceTreeState>(() => {
    if (spaceTree.isError) return { status: "error" };
    if (!spaceTree.data) return { status: "loading" };
    return {
      status: "ready",
      nodes: spaceTree.data.filter((node) => !node.isArchived),
    };
  }, [spaceTree.data, spaceTree.isError]);

  // ── Identity ────────────────────────────────────────────────────
  const role =
    members.data?.find((member) => member.userId === user?.id)?.role ?? null;
  const currentWorkspace = workspace
    ? {
        id: workspace.id,
        name: workspace.name,
        mark: markToneFor(workspace.id),
        role,
      }
    : null;
  const workspaceOptions = useMemo(
    () =>
      (workspaces.data ?? []).map((item) => ({
        id: item.id,
        name: item.name,
        mark: markToneFor(item.id),
      })),
    [workspaces.data],
  );
  const shellUser = user
    ? {
        id: user.id,
        name: user.name,
        email: user.email,
        avatarUrl: user.avatarUrl,
        identity: identityToneFor(user.id),
      }
    : null;

  // ── Overlays and shortcuts ──────────────────────────────────────
  const [paletteOpen, setPaletteOpen] = useState(false);
  const [shortcutsOpen, setShortcutsOpen] = useState(false);
  const [createOpen, setCreateOpen] = useState(false);
  const appearance = theme.appearance === "dark" ? "dark" : "light";
  const toggleTheme = useCallback(
    () => theme.setMode(appearance === "dark" ? "light" : "dark"),
    [appearance, theme],
  );
  const commandShortcut = commandShortcutLabel(
    typeof navigator === "undefined" ? undefined : navigator.platform,
  );

  const onShortcut = useCallback(
    (id: ShellShortcutId) => {
      switch (id) {
        case "go-home":
          return goTo({ section: "home" });
        case "go-my-work":
          return goTo({ section: "my-work" });
        case "go-inbox":
          return goTo({ section: "inbox" });
        case "open-palette":
          return setPaletteOpen((open) => !open);
        case "show-shortcuts":
          return setShortcutsOpen(true);
      }
    },
    [goTo],
  );
  useShellShortcuts(onShortcut, { enabled: !createOpen });

  const onPaletteSelect = useCallback(
    (selection: ShellPaletteSelection) => {
      if (selection.type === "navigate") return goTo(selection.destination);
      if (selection.type === "search") {
        void navigate({
          to: "/workspaces/$workspaceId/search",
          params: { workspaceId },
          search: { q: selection.query },
        } as never);
        return;
      }
      if (selection.action === "toggle-theme") return toggleTheme();
      if (selection.action === "show-shortcuts") return setShortcutsOpen(true);
      setCreateOpen(true);
    },
    [goTo, navigate, toggleTheme, workspaceId],
  );

  const breadcrumb = useMemo(
    () =>
      buildWorkspaceBreadcrumb({
        workspaceName: workspace?.name ?? null,
        location,
        boards: catalog.ready || catalog.failed ? catalog.boards : null,
        docs: catalog.ready || catalog.failed ? catalog.docs : null,
        spacePath:
          location?.section === "space"
            ? spaceTree.isSuccess || spaceTree.isError
              ? spacePath(spaceIndex, location.spaceId)
              : null
            : null,
        labels: workspaceBreadcrumbLabels(copy),
      }),
    [
      catalog,
      copy,
      location,
      spaceIndex,
      spaceTree.isError,
      spaceTree.isSuccess,
      workspace?.name,
    ],
  );

  const paletteMembers = useMemo(
    () =>
      (members.data ?? []).map((member) => ({
        id: member.userId,
        name: member.name,
        identity: identityToneFor(member.userId),
        roleLabel: copy.switcher.roles[member.role],
      })),
    [copy, members.data],
  );

  const NotificationBell = hooks.NotificationBell;

  return (
    <SidebarProvider className="h-svh overflow-hidden bg-surface-canvas">
      <WorkspaceSidebarSurface
        copy={copy}
        navigation={navigation}
        workspace={currentWorkspace}
        workspaces={workspaceOptions}
        location={location}
        pinned={pinnedState}
        recent={recentState}
        spaces={{
          copy: spacesCopy,
          state: sidebarSpaces,
          expandedIds: expansion.expandedIds,
          activeSpaceId,
          canCreateRoot: canCreateRootSpace(role),
          onExpandedChange: expansion.setExpanded,
          onCreateSpace: () => createSpace.open(null),
          onRetry: () => void spaceTree.refetch(),
        }}
        user={shellUser}
        signingOut={logout.isPending}
        onSwitchWorkspace={(id) =>
          void navigate({
            to: "/workspaces/$workspaceId",
            params: { workspaceId: id },
          })
        }
        onCreateWorkspace={() => setCreateOpen(true)}
        onTogglePin={(entry, pinned) =>
          togglePin.mutate({
            kind: entry.kind,
            resourceId: entry.resourceId,
            pinned,
          })
        }
        onRetryNavigation={retryNavigation}
        onOpenShortcuts={() => setShortcutsOpen(true)}
        onSignOut={() => logout.mutate()}
      />
      <div className="relative flex min-h-0 min-w-0 flex-1 flex-col">
        <WorkspaceTopbarSurface
          copy={copy}
          navigation={navigation}
          breadcrumb={breadcrumb}
          appearance={appearance}
          commandShortcut={commandShortcut}
          onOpenCommand={() => setPaletteOpen(true)}
          onToggleTheme={toggleTheme}
          notifications={<NotificationBell />}
        />
        {/* The one scroll container of the workspace shell (WX-SHELL-004). */}
        <main id="workspace-main" className="min-h-0 flex-1 overflow-y-auto">
          {children}
        </main>
      </div>

      <WorkspaceCommandPaletteSurface
        copy={copy}
        open={paletteOpen}
        onOpenChange={setPaletteOpen}
        boards={catalog.boards}
        docs={catalog.docs}
        members={paletteMembers}
        appearance={appearance}
        canCreateWorkspace
        onSelect={onPaletteSelect}
      />
      <WorkspaceShortcutSheetSurface
        copy={copy}
        open={shortcutsOpen}
        onOpenChange={setShortcutsOpen}
        commandShortcut={commandShortcut}
      />
      {createSpace.dialog}
      <CreateWorkspaceDialog
        copy={copy}
        open={createOpen}
        onOpenChange={setCreateOpen}
        onCreated={(created) =>
          void navigate({
            to: "/workspaces/$workspaceId",
            params: { workspaceId: created.id },
          })
        }
      />
    </SidebarProvider>
  );
}

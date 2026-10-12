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
  initialsFor,
  markToneFor,
  spaceAncestors,
  spacePath,
  NAVIGATION_RESOURCE_KIND,
  type WorkspaceDestination,
} from "@notrelix/features-workspace/core";
import {
  commandShortcutLabel,
  createUseRecordRecent,
  createUseWorkspaceList,
  createUseWorkspaceMembers,
  useShellShortcuts,
  useSpaceTreeExpansion,
  useWorkspaceShellCopy,
  useWorkspaceSpacesCopy,
  NotrelixSystemRailSurface,
  useNotrelixSystemRailCopy,
  WorkspaceCommandPaletteSurface,
  WorkspaceShortcutSheetSurface,
  WorkspaceSidebarSurface,
  WorkspaceTopbarSurface,
  workspaceBreadcrumbLabels,
  type ShellNavigation,
  type ShellPaletteSelection,
  type ShellShortcutId,
  type SpaceBoardLink,
  type SpaceTreeState,
  type SystemRailWorkspace,
} from "@notrelix/features-workspace/web";
import {
  useAppRuntime,
  useFeatureRuntimeDependencies,
} from "@notrelix/runtime-web";
import { SidebarProvider, cn, useIsMobile, useTheme } from "@notrelix/ui-web";
import { useWorkspaceBoards } from "@notrelix/work-management-state";
import { useWorkspaceContext } from "@/providers/workspace-provider";
import { useBoardPlacements } from "./board-placement-bridge";
import { CreateWorkspaceDialog } from "./create-workspace-dialog";
import { useSystemRailPreference } from "./use-system-rail-preference";
import { flattenMyWork, useMyWorkItems } from "./work-management-bridge";
import { useCreateSpace } from "./use-create-space";
import { useSpaceHooks } from "./use-workspace-spaces";
import {
  destinationHref,
  resolveShellLocation,
  routerTarget,
} from "./workspace-destinations";

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
  const { api: runtimeClient, storage } = useAppRuntime();
  const featureDeps = useFeatureRuntimeDependencies();

  const hooks = useMemo(() => {
    const deps = { api: runtimeClient.api, endpoints: runtimeClient.endpoints };
    return {
      useWorkspaceList: createUseWorkspaceList(deps),
      useMembers: createUseWorkspaceMembers({ api: runtimeClient.api }),
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
  const boards = useWorkspaceBoards(workspaceId);
  const placements = useBoardPlacements(workspaceId);
  const myWork = useMyWorkItems(workspaceId);
  const pages = hooks.usePageList(workspaceId);
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

  // Boards under their Space (placement from the Board list, OT-5).
  const boardsBySpace = useMemo(() => {
    const bySpace = new Map<string, SpaceBoardLink[]>();
    if (!boards.data || !placements.data) return bySpace;
    const spaceOf = new Map(
      placements.data.map((placement) => [placement.id, placement.spaceId]),
    );
    for (const board of boards.data) {
      const spaceId = spaceOf.get(board.id);
      if (!spaceId) continue;
      const links = bySpace.get(spaceId) ?? [];
      links.push({
        id: board.id,
        name: board.title,
        isPrivate: board.visibility === "private",
      });
      bySpace.set(spaceId, links);
    }
    return bySpace;
  }, [boards.data, placements.data]);

  // My work count: the open Items assigned to the member. Unknown (no badge)
  // while loading, on error, or when more pages exist than are loaded.
  const myWorkCount =
    myWork.isSuccess && !myWork.hasNextPage
      ? flattenMyWork(myWork.data).length
      : null;

  // ── Identity ────────────────────────────────────────────────────
  const role =
    members.data?.find((member) => member.userId === user?.id)?.role ?? null;
  const currentWorkspace = workspace
    ? {
        id: workspace.id,
        name: workspace.name,
        mark: markToneFor(workspace.id),
        role,
        departmentCount: spaceTree.data
          ? spaceTree.data.filter(
              (node) =>
                node.type === "Department" &&
                node.parentId === null &&
                !node.isArchived,
            ).length
          : null,
        memberCount: members.data ? members.data.length : null,
      }
    : null;
  const shellUser = user
    ? {
        id: user.id,
        name: user.name,
        email: user.email,
        avatarUrl: user.avatarUrl,
        identity: identityToneFor(user.id),
      }
    : null;

  // ── Notrelix system rail (Final-Space `nav`) ────────────────────
  const railCopy = useNotrelixSystemRailCopy();
  const rail = useSystemRailPreference(storage);
  const isMobile = useIsMobile();
  const railOpener = useRef<HTMLElement | null>(null);
  const railWorkspaces = useMemo<SystemRailWorkspace[]>(() => {
    const list = workspaces.data ?? (workspace ? [workspace] : []);
    return list.map((item) => ({
      id: item.id,
      name: item.name,
      shortName: item.name,
      initials: initialsFor(item.name),
      tone: markToneFor(item.id),
      // The workspace list carries no role; only the current role is known.
      isGuest: item.id === workspaceId && role === "guest",
      // No per-workspace unread producer yet (gap register).
      unread: null,
      current: item.id === workspaceId,
    }));
  }, [role, workspace, workspaceId, workspaces.data]);

  const openSystemRail = useCallback(() => {
    if (isMobile) {
      // The drawer already lists the workspaces at its top.
      document
        .querySelector<HTMLElement>(
          '[data-presentation="drawer"] [aria-current="page"]',
        )
        ?.focus();
      return;
    }
    const active = document.activeElement;
    railOpener.current =
      active instanceof HTMLElement && active !== document.body ? active : null;
    rail.open();
  }, [isMobile, rail]);

  const closeSystemRail = useCallback(() => {
    rail.close();
    // Escape and the scrim return focus to the chip; when focus already
    // moved on (Tab out of the rail), leave it there.
    requestAnimationFrame(() => {
      const active = document.activeElement;
      if (active && active !== document.body) return;
      railOpener.current?.focus();
    });
  }, [rail]);

  const switchWorkspace = useCallback(
    (id: string) => {
      if (rail.presentation === "overlay") rail.close();
      // The workspace route runs prepareWorkspaceTransition (base.routes).
      void navigate({
        to: "/workspaces/$workspaceId",
        params: { workspaceId: id },
      });
    },
    [navigate, rail],
  );

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

  const railSurface = (presentation: typeof rail.presentation) => (
    <NotrelixSystemRailSurface
      copy={railCopy}
      presentation={presentation}
      workspaces={railWorkspaces}
      user={shellUser}
      // No all-workspace inbox producer yet: the entry shows why.
      allInboxAvailable={false}
      allInboxUnread={null}
      onSwitchWorkspace={switchWorkspace}
      onCreateWorkspace={() => {
        if (rail.presentation === "overlay") rail.close();
        setCreateOpen(true);
      }}
      onOpenAllInbox={() => goTo({ section: "inbox" })}
      // Search runs in the current workspace until a cross-workspace search exists.
      onOpenGlobalSearch={() => {
        if (rail.presentation === "overlay") rail.close();
        goTo({ section: "search" });
      }}
      onOpenAccount={() => {
        if (rail.presentation === "overlay") rail.close();
        goTo({ section: "account" });
      }}
      onTogglePinned={rail.togglePinned}
      onClose={closeSystemRail}
    />
  );
  const desktopRail = isMobile ? "hidden" : rail.presentation;

  return (
    <SidebarProvider
      data-system-rail={desktopRail}
      className={cn(
        "h-svh overflow-hidden bg-surface-canvas",
        // A pinned rail sits in flow before the sidebar; the sidebar's fixed
        // container moves right by the rail width (w-19).
        desktopRail === "pinned" && "[&_[data-slot=sidebar-container]]:left-19",
      )}
    >
      {desktopRail === "hidden" ? null : railSurface(desktopRail)}
      <WorkspaceSidebarSurface
        copy={copy}
        navigation={navigation}
        workspace={currentWorkspace}
        location={location}
        // No inbox producer yet (gap register): no badge.
        counts={{ myWork: myWorkCount, inbox: null }}
        spaces={{
          copy: spacesCopy,
          state: sidebarSpaces,
          expandedIds: expansion.expandedIds,
          activeSpaceId,
          canCreateRoot: canCreateRootSpace(role),
          onExpandedChange: expansion.setExpanded,
          onCreateSpace: () => createSpace.open(null),
          onRetry: () => void spaceTree.refetch(),
          boardsBySpace,
        }}
        user={shellUser}
        signingOut={logout.isPending}
        commandShortcut={commandShortcut}
        onOpenCommand={() => setPaletteOpen(true)}
        systemRailOpen={desktopRail !== "hidden"}
        onOpenSystemRail={openSystemRail}
        mobileSystemRail={railSurface("drawer")}
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

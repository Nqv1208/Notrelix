import {
  useCallback,
  useEffect,
  useMemo,
  useRef,
  useState,
  type CSSProperties,
  type ReactNode,
} from "react";
import { useMatches, useNavigate } from "@tanstack/react-router";
import { resolveApiAssetUrl } from "@notrelix/contracts";
import { createUsePageList } from "@notrelix/docs-state";
import { createUseLogout, useCurrentUser } from "@notrelix/features-auth";
import { createNotificationBell } from "@notrelix/features-notifications";
import {
  buildSpaceIndex,
  buildWorkspaceBreadcrumb,
  canCreateRootSpace,
  identityToneFor,
  MARK_TONE_IDS,
  markInitialsFor,
  markToneFor,
  spaceAncestors,
  spacePath,
  NAVIGATION_RESOURCE_KIND,
  resolveNavigationEntries,
  type WorkspaceDestination,
  type MarkToneId,
} from "@notrelix/features-workspace/core";
import {
  commandShortcutLabel,
  createUseRecordRecent,
  createUseTogglePin,
  createUseWorkspaceList,
  createUseWorkspacePins,
  useShellShortcuts,
  useSpaceTreeExpansion,
  useWorkspaceCommandCopy,
  useWorkspaceHomeCopy,
  useWorkspaceShellCopy,
  useWorkspaceSpacesCopy,
  NotrelixSystemRailSurface,
  useNotrelixSystemRailCopy,
  WorkspaceCommandPaletteSurface,
  WorkspaceShortcutSheetSurface,
  WorkspaceSidebarSurface,
  WorkspaceTopbarSurface,
  workspaceBreadcrumbLabels,
  type NavigationListState,
  type ShellNavigation,
  type ShellPaletteSelection,
  type ShellShortcutId,
  type SidebarDocsTree,
  type SpaceBoardLink,
  type SpaceTreeState,
  type SystemRailWorkspace,
  formatCopy,
} from "@notrelix/features-workspace/web";
import {
  useAppRuntime,
  useFeatureRuntimeDependencies,
} from "@notrelix/runtime-web";
import { SidebarProvider, cn, useIsMobile, useTheme } from "@notrelix/ui-web";
import { useWorkspaceBoards } from "@notrelix/work-management-state";
import { useWorkspaceContext } from "@/providers/workspace-provider";
import { useBoardPlacements } from "./board-placement-bridge";
import { buildSidebarDocs } from "./docs-sidebar-tree";
import { CreateWorkspaceDialog } from "./create-workspace-dialog";
import { useSystemRailPreference } from "./use-system-rail-preference";
import { useTabletSidebarPreference } from "./use-tablet-sidebar-preference";
import { useInboxSummary } from "./work-feed-bridge";
import { flattenMyWork, useMyWorkItems } from "./work-management-bridge";
import { useCommandPaletteData } from "./use-command-palette-data";
import { useCommandSearch } from "./use-command-search";
import { useWorkFeedWrites } from "./work-feed-bridge";
import { useCreateResource } from "./use-create-resource";
import { useCreateSpace } from "./use-create-space";
import { useWorkspaceResources } from "./use-workspace-resources";
import { useWorkspaceViewer } from "./use-workspace-viewer";
import { useSpaceHooks } from "./use-workspace-spaces";
import {
  destinationHref,
  resolveShellLocation,
  routerTarget,
} from "./workspace-destinations";

/**
 * The workspace shell (WX-SHELL-001…005): one sidebar, the topbar (narrow
 * screens only), the command palette, the shortcut sheet and one scroll
 * container. The app composes Work Management and Docs data into the
 * workspace surfaces.
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
      useRecordRecent: createUseRecordRecent(deps),
      usePins: createUseWorkspacePins(deps),
      useTogglePin: createUseTogglePin(deps),
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
  const viewer = useWorkspaceViewer(workspaceId);
  const members = viewer.members;
  const boards = useWorkspaceBoards(workspaceId);
  const placements = useBoardPlacements(workspaceId);
  const myWork = useMyWorkItems(workspaceId);
  const pages = hooks.usePageList(workspaceId);
  const recordRecent = hooks.useRecordRecent();
  const pins = hooks.usePins(workspaceId);
  const togglePin = hooks.useTogglePin(workspaceId);
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

  // ── Pinned Boards and Docs (WX-DEC-011, ruling R6) ──────────────
  const pinnedState = useMemo<NavigationListState>(() => {
    if (pins.isError || catalog.failed) return { status: "error" };
    if (pins.isLoading || !catalog.ready || !pins.data)
      return { status: "loading" };
    return {
      status: "ready",
      entries: resolveNavigationEntries(pins.data, catalog, pins.data),
    };
  }, [pins.data, pins.isError, pins.isLoading, catalog]);
  const retryPins = useCallback(() => {
    void pins.refetch();
    if (boards.isError) void boards.refetch();
    if (pages.isError) void pages.refetch();
  }, [pins, boards, pages]);

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
  // A Board page opens the Space that holds it (and its ancestors), so the
  // current Board shows in the tree as in Final-Board.
  const activeBoardId = location?.section === "board" ? location.boardId : null;
  const activeBoardSpaceId = useMemo(
    () =>
      activeBoardId
        ? (placements.data?.find((placement) => placement.id === activeBoardId)
            ?.spaceId ?? null)
        : null,
    [activeBoardId, placements.data],
  );
  useEffect(() => {
    if (!activeBoardSpaceId) return;
    reveal([
      activeBoardSpaceId,
      ...spaceAncestors(spaceIndex, activeBoardSpaceId).map((node) => node.id),
    ]);
  }, [activeBoardSpaceId, reveal, spaceIndex]);
  // An open document shows its department's document tree under the Docs row
  // (Final-Docs): the pages placed in the department's Spaces, nested by parent.
  const activeDocId = location?.section === "doc" ? location.docId : null;
  const [docsExpanded, setDocsExpanded] = useState<ReadonlySet<string>>(
    () => new Set(),
  );
  const docsTreeModel = useMemo(
    () =>
      buildSidebarDocs({
        pages: pages.data ?? [],
        spaces: spaceTree.data ?? [],
        spaceIndex,
        activeDocId,
      }),
    [activeDocId, pages.data, spaceIndex, spaceTree.data],
  );
  const docsRevealKey = docsTreeModel?.reveal.join("|") ?? "";
  useEffect(() => {
    if (!docsTreeModel) return;
    setDocsExpanded((current) => {
      const next = new Set(current);
      for (const id of docsTreeModel.reveal) next.add(id);
      return next.size === current.size ? current : next;
    });
    // `docsRevealKey` covers the reveal list.
  }, [docsRevealKey]);
  const docsTree = useMemo<SidebarDocsTree | null>(
    () =>
      docsTreeModel
        ? {
            departments: docsTreeModel.departments,
            activeDocId,
            expandedIds: docsExpanded,
            onExpandedChange: (id, expanded) =>
              setDocsExpanded((current) => {
                const next = new Set(current);
                if (expanded) next.add(id);
                else next.delete(id);
                return next;
              }),
          }
        : null,
    [activeDocId, docsExpanded, docsTreeModel],
  );

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

  // Boards the member opens without seeing their department: "Được chia sẻ
  // với bạn" (the placement read names the department either way).
  const sharedBoards = useMemo(() => {
    if (!boards.data || !placements.data) return [];
    const byId = new Map(placements.data.map((entry) => [entry.id, entry]));
    return boards.data.flatMap((board) => {
      const placement = byId.get(board.id);
      if (!placement || placement.spaceId || !placement.departmentName)
        return [];
      return [
        {
          id: board.id,
          name: board.title,
          departmentName: placement.departmentName,
          markTone: (MARK_TONE_IDS as readonly string[]).includes(
            placement.departmentColor ?? "",
          )
            ? (placement.departmentColor as MarkToneId)
            : markToneFor(board.id),
        },
      ];
    });
  }, [boards.data, placements.data]);

  // My work count: the open Items assigned to the member. Unknown (no badge)
  // while loading, on error, or when more pages exist than are loaded.
  const myWorkCount =
    myWork.isSuccess && !myWork.hasNextPage
      ? flattenMyWork(myWork.data).length
      : null;

  // ── Identity ────────────────────────────────────────────────────
  const role = viewer.role;
  const currentWorkspace = workspace
    ? {
        id: workspace.id,
        name: workspace.name,
        mark: markToneFor(workspace.id),
        role,
        // The workspace's own count when the producer sends one (a member
        // does not see every department in the tree).
        departmentCount:
          (workspace as { departmentCount?: number }).departmentCount ??
          (spaceTree.data
            ? spaceTree.data.filter(
                (node) =>
                  node.type === "Department" &&
                  node.parentId === null &&
                  !node.isArchived,
              ).length
            : null),
        // `WorkspaceDto.memberCount` is the total; the member list may be shorter
        // (and a Guest does not read it at all).
        roleLabel:
          role === "member"
            ? (() => {
                const admin = spaceTree.data?.find(
                  (node) =>
                    node.type === "Department" &&
                    node.parentId === null &&
                    node.access === "Manager",
                );
                return admin
                  ? formatCopy(copy.sidebar.departmentAdmin, {
                      name: admin.name,
                    })
                  : null;
              })()
            : null,
        memberCount:
          workspace.memberCount ?? (members.data ? members.data.length : null),
      }
    : null;
  const shellUser = user
    ? {
        id: user.id,
        name: user.name,
        email: user.email,
        avatarUrl: resolveApiAssetUrl(user.avatarUrl),
        identity: identityToneFor(user.id),
      }
    : null;

  // ── Notrelix system rail (Final-Space `nav`) ────────────────────
  const railCopy = useNotrelixSystemRailCopy();
  const rail = useSystemRailPreference(storage);
  const tabletSidebar = useTabletSidebarPreference(storage);
  const isMobile = useIsMobile();
  const railOpener = useRef<HTMLElement | null>(null);
  const inboxSummary = useInboxSummary();
  const railWorkspaces = useMemo<SystemRailWorkspace[]>(() => {
    const list = workspaces.data ?? (workspace ? [workspace] : []);
    return list.map((item) => ({
      id: item.id,
      name: item.name,
      // The label under the mark: the first two words ("Minh An").
      shortName: item.name.trim().split(/\s+/).slice(0, 2).join(" "),
      initials: markInitialsFor(item.name),
      tone: markToneFor(item.id),
      // The workspace list carries no role; only the current role is known.
      isGuest: item.id === workspaceId && role === "guest",
      // The current workspace's count is the sidebar's; the rail shows the others.
      unread:
        item.id === workspaceId
          ? null
          : (inboxSummary.data?.perWorkspace.find(
              (entry) => entry.workspaceId === item.id,
            )?.unread ?? null),
      current: item.id === workspaceId,
    }));
  }, [inboxSummary.data, role, workspace, workspaceId, workspaces.data]);

  const openSystemRail = useCallback(() => {
    if (isMobile || rail.presentation === "pinned") {
      // The drawer (mobile) or the pinned rail already shows the
      // workspaces: move focus to the current one instead of opening.
      const shown = isMobile ? "drawer" : "pinned";
      document
        .querySelector<HTMLElement>(
          `[data-presentation="${shown}"] [aria-current="page"]`,
        )
        ?.focus();
      return;
    }
    // WebKit does not focus a clicked button: fall back to the chip so
    // Escape and the scrim can still return focus to it.
    const active = document.activeElement;
    railOpener.current =
      active instanceof HTMLElement && active !== document.body
        ? active
        : document.querySelector<HTMLElement>("[data-workspace-chip]");
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
  const homeCopy = useWorkspaceHomeCopy();
  const createResource = useCreateResource(workspaceId, homeCopy);
  const openCreateResource = createResource.open;
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

  const workFeedWrites = useWorkFeedWrites(workspaceId);
  const onPaletteSelect = useCallback(
    (selection: ShellPaletteSelection) => {
      if (selection.type === "navigate") return goTo(selection.destination);
      if (selection.type === "create-item") {
        void workFeedWrites
          .createItem({ boardId: selection.boardId, name: selection.name })
          .then((created) =>
            goTo({
              section: "board",
              boardId: selection.boardId,
              item: created.id,
            }),
          )
          .catch(() => undefined);
        return;
      }
      switch (selection.action) {
        case "toggle-theme":
          return toggleTheme();
        case "show-shortcuts":
          return setShortcutsOpen(true);
        case "create-doc":
          return openCreateResource("doc");
        case "switch-workspace":
          // After the palette has closed, so its focus trap lets go first.
          return void requestAnimationFrame(() => openSystemRail());
        case "create-workspace":
          return setCreateOpen(true);
      }
    },
    [goTo, openCreateResource, openSystemRail, toggleTheme, workFeedWrites],
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

  const commandCopy = useWorkspaceCommandCopy();
  const resources = useWorkspaceResources(workspaceId);
  const myWorkItems = useMemo(() => flattenMyWork(myWork.data), [myWork.data]);
  const otherWorkspaceNames = useMemo(
    () =>
      railWorkspaces.filter((item) => !item.current).map((item) => item.name),
    [railWorkspaces],
  );
  const [paletteQuery, setPaletteQuery] = useState("");
  const paletteSearch = useCommandSearch(
    workspaceId,
    paletteQuery,
    paletteOpen,
  );
  const paletteData = useCommandPaletteData({
    search: paletteSearch,
    workspaceId,
    workspaceName: workspace?.name ?? "",
    otherWorkspaceNames,
    boards: resources.boards,
    docs: resources.docs,
    myWork: myWorkItems,
    spaceIndex,
  });

  const NotificationBell = hooks.NotificationBell;

  const railSurface = (presentation: typeof rail.presentation) => (
    <NotrelixSystemRailSurface
      copy={railCopy}
      presentation={presentation}
      workspaces={railWorkspaces}
      user={shellUser}
      allInboxAvailable
      allInboxUnread={inboxSummary.data?.total ?? null}
      onSwitchWorkspace={switchWorkspace}
      onCreateWorkspace={() => {
        if (rail.presentation === "overlay") rail.close();
        setCreateOpen(true);
      }}
      onOpenAllInbox={() => goTo({ section: "inbox" })}
      // No cross-workspace search producer yet: omitting the handler disables
      // "Search" with its reason (current-workspace search stays on ⌘K).
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
      tabletOpen={tabletSidebar.open}
      onTabletOpenChange={tabletSidebar.setOpen}
      data-system-rail={desktopRail}
      // The collapsed sidebar keeps a 64 px icon column on desktop as on
      // tablet, so its edge arrow stays reachable.
      style={{ "--sidebar-width-icon": "4rem" } as CSSProperties}
      className={cn(
        "h-svh overflow-hidden bg-surface-canvas",
        // A pinned rail sits in flow before the sidebar; the sidebar's fixed
        // container (open or icon column) moves right by the rail width (w-19).
        desktopRail === "pinned" &&
          "[&_[data-state=expanded]_[data-slot=sidebar-container]]:left-19 [&_[data-collapsible=icon]_[data-slot=sidebar-container]]:left-19",
      )}
    >
      {desktopRail === "hidden" ? null : railSurface(desktopRail)}
      <WorkspaceSidebarSurface
        copy={copy}
        navigation={navigation}
        workspace={currentWorkspace}
        location={location}
        counts={{
          myWork: myWorkCount,
          inbox:
            inboxSummary.data?.perWorkspace.find(
              (entry) => entry.workspaceId === workspaceId,
            )?.unread ?? null,
        }}
        sharedBoards={sharedBoards}
        pinned={pinnedState}
        onTogglePin={(entry, pinned) =>
          togglePin.mutate({
            kind: entry.kind,
            resourceId: entry.resourceId,
            pinned,
          })
        }
        onRetryNavigation={retryPins}
        docsTree={docsTree}
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
        // On mobile the chip focuses the drawer row; it opens no rail.
        systemRailOpen={isMobile ? undefined : desktopRail !== "hidden"}
        onOpenSystemRail={openSystemRail}
        mobileSystemRail={railSurface("drawer")}
        onOpenShortcuts={() => setShortcutsOpen(true)}
        onSignOut={() => logout.mutate()}
      />
      <div className="relative flex min-h-0 min-w-0 flex-1 flex-col">
        {/* Desktop and tablet have no top bar: the workspace starts at the
            top edge, the sidebar collapses with the arrow on its edge, and
            search, notifications and appearance live in the sidebar, the rail
            and the account pages. Phones keep the bar, which owns the
            navigation drawer trigger. */}
        {isMobile ? (
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
        ) : null}
        {/* The one scroll container of the workspace shell (WX-SHELL-004). */}
        <main id="workspace-main" className="min-h-0 flex-1 overflow-y-auto">
          {children}
        </main>
      </div>

      <WorkspaceCommandPaletteSurface
        copy={commandCopy}
        open={paletteOpen}
        onOpenChange={setPaletteOpen}
        onQueryChange={setPaletteQuery}
        workspace={paletteData.workspace}
        work={paletteData.work}
        docs={paletteData.docs}
        boards={paletteData.boards}
        people={paletteMembers}
        recents={paletteData.recents}
        appearance={appearance}
        canCreateWorkspace
        canCreateDoc
        canSwitchWorkspace={!isMobile}
        onSelect={onPaletteSelect}
      />
      {createResource.dialog}
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

import * as React from "react";
import {
  ArrowRightLeft,
  ChevronRight,
  FolderPlus,
  Lock,
  MoreHorizontal,
  Plus,
} from "lucide-react";
import {
  Badge,
  Button,
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
  Empty,
  EmptyContent,
  EmptyDescription,
  EmptyHeader,
  EmptyTitle,
  Skeleton,
  Tabs,
  TabsContent,
  TabsList,
  TabsTrigger,
} from "@notrelix/ui-web";
import {
  NAVIGATION_RESOURCE_KIND,
  type SpaceTab,
  type WorkspaceDestination,
} from "../../core/model/navigation";
import {
  spaceCapabilities,
  type SpaceContentCounts,
  type SpaceMemberEntry,
  type SpaceNode,
  type SpacePathItem,
  type SpaceRole,
  type SpaceTeam,
} from "../../core/model/spaces";
import type { SectionState, SurfaceFormatters } from "../home/home-models";
import { ResourceMark, ShellLink } from "../shell/shell-link";
import { formatCopy, type ShellNavigation } from "../shell/shell-models";
import {
  SpaceMembersPanel,
  type AddSpaceMemberInput,
  type SpaceMemberCandidate,
} from "./space-members-panel";
import {
  SpaceSettingsPanel,
  type SpaceDetailsValues,
  type SpaceSaveStatus,
} from "./space-settings-panel";
import { SPACE_TYPE_ICON } from "./space-tree-nav";
import type { WorkspaceSpacesCopy } from "./workspace-spaces.copy";

/** A Board or Doc placed in the Space, from its owning context's list. */
export interface SpaceResource {
  readonly id: string;
  readonly name: string;
  readonly updatedAt: string | null;
}

export interface SpaceContent {
  readonly boards: readonly SpaceResource[];
  readonly docs: readonly SpaceResource[];
}

export interface WorkspaceSpaceSurfaceProps {
  readonly copy: WorkspaceSpacesCopy;
  readonly navigation: ShellNavigation;
  readonly formatters: SurfaceFormatters;
  /** `ready` with `null` = the Space is not visible to the member (NotFound). */
  readonly space: SectionState<SpaceNode | null>;
  /** Visible path, root first, ending with the Space. */
  readonly path: readonly SpacePathItem[];
  /** Visible child Spaces, in sibling order. */
  readonly childSpaces: readonly SpaceNode[];
  readonly content: SectionState<SpaceContent>;
  readonly members: SectionState<readonly SpaceMemberEntry[]>;
  readonly candidateUsers: SectionState<readonly SpaceMemberCandidate[]>;
  readonly candidateTeams: SectionState<readonly SpaceTeam[]>;
  readonly tab: SpaceTab;
  readonly onTabChange: (tab: SpaceTab) => void;
  /** Boards in this Space the member may move (ManageBoard; GOV-028). */
  readonly canMoveBoards: boolean;
  /** Docs in this Space the member may move (UpdatePage; GOV-028). */
  readonly canMoveDocs: boolean;
  readonly pendingMemberKeys: ReadonlySet<string>;
  readonly addingMember: boolean;
  readonly addMemberError: string | null;
  readonly addMemberResetKey: number;
  readonly saveStatus: SpaceSaveStatus;
  readonly archiving: boolean;
  readonly archiveRefused: boolean;
  readonly unarchiving: boolean;
  readonly onCreateChild: () => void;
  readonly onCreateBoard: () => void;
  readonly onCreateDoc: () => void;
  readonly onMoveSpace: () => void;
  readonly onMoveBoard: (board: SpaceResource) => void;
  readonly onMoveDoc: (doc: SpaceResource) => void;
  readonly onUnarchive: () => void;
  readonly onSave: (changes: Partial<SpaceDetailsValues>) => void;
  readonly onArchive: () => void;
  readonly onAddMember: (input: AddSpaceMemberInput) => void;
  readonly onChangeMemberRole: (
    entry: SpaceMemberEntry,
    role: SpaceRole,
  ) => void;
  readonly onRemoveMember: (entry: SpaceMemberEntry) => void;
  readonly onRetry: () => void;
  readonly onRetryContent: () => void;
  readonly onRetryMembers: () => void;
}

const DATE_FORMAT: Intl.DateTimeFormatOptions = { dateStyle: "medium" };

function SectionList({
  title,
  empty,
  children,
  count,
}: {
  title: string;
  empty: string;
  count: number;
  children: React.ReactNode;
}) {
  const titleId = React.useId();
  return (
    <section aria-labelledby={titleId} className="flex flex-col gap-2">
      <h2
        id={titleId}
        className="flex items-baseline gap-2 text-subheading text-foreground"
      >
        {title}
        <span className="text-metadata tabular-nums text-muted-foreground">
          {count}
        </span>
      </h2>
      {count === 0 ? (
        <p className="rounded-lg border border-dashed border-border bg-surface-panel px-4 py-4 text-metadata text-muted-foreground">
          {empty}
        </p>
      ) : (
        <ul className="overflow-hidden rounded-lg border border-border bg-surface-panel">
          {children}
        </ul>
      )}
    </section>
  );
}

function Overview({
  node: space,
  ...props
}: WorkspaceSpaceSurfaceProps & { node: SpaceNode }) {
  const { copy, navigation, formatters, content, childSpaces } = props;
  const caps = spaceCapabilities(space);
  const moveBoards = props.canMoveBoards && caps.canCreateResources;
  const moveDocs = props.canMoveDocs && caps.canCreateResources;

  const resourceRow = (resource: SpaceResource, kind: "board" | "doc") => {
    const destination: WorkspaceDestination =
      kind === "board"
        ? { section: "board", boardId: resource.id }
        : { section: "doc", docId: resource.id };
    return (
      <li
        key={resource.id}
        className="flex h-11 items-center gap-3 border-t border-border px-3 first:border-t-0 hover:bg-state-hover"
      >
        <ResourceMark
          kind={
            kind === "board"
              ? NAVIGATION_RESOURCE_KIND.board
              : NAVIGATION_RESOURCE_KIND.doc
          }
          id={resource.id}
        />
        <ShellLink
          destination={destination}
          navigation={navigation}
          className="min-w-0 flex-1 truncate rounded-sm text-label text-foreground outline-none hover:underline focus-visible:ring-2 focus-visible:ring-focus-ring"
        >
          {resource.name}
        </ShellLink>
        {resource.updatedAt ? (
          <span className="hidden shrink-0 text-metadata tabular-nums text-muted-foreground sm:inline">
            {formatters.date(resource.updatedAt, DATE_FORMAT)}
          </span>
        ) : null}
        {(kind === "board" && moveBoards) || (kind === "doc" && moveDocs) ? (
          <Button
            variant="ghost"
            size="icon-sm"
            aria-label={formatCopy(copy.overview.move, { name: resource.name })}
            title={formatCopy(copy.overview.move, { name: resource.name })}
            onClick={() =>
              kind === "board"
                ? props.onMoveBoard(resource)
                : props.onMoveDoc(resource)
            }
          >
            <ArrowRightLeft aria-hidden="true" />
          </Button>
        ) : null}
      </li>
    );
  };

  return (
    <div className="flex flex-col gap-6">
      <SectionList
        title={copy.overview.spaces}
        empty={copy.overview.spacesEmpty}
        count={childSpaces.length}
      >
        {childSpaces.map((child) => {
          const Icon = SPACE_TYPE_ICON[child.type];
          return (
            <li
              key={child.id}
              className="flex h-11 items-center gap-3 border-t border-border px-3 first:border-t-0 hover:bg-state-hover"
            >
              <span
                aria-hidden="true"
                className="flex size-7 shrink-0 items-center justify-center rounded-md border border-border bg-surface-raised text-icon"
              >
                <Icon className="size-4" />
              </span>
              <ShellLink
                destination={{ section: "space", spaceId: child.id }}
                navigation={navigation}
                className="min-w-0 flex-1 truncate rounded-sm text-label text-foreground outline-none hover:underline focus-visible:ring-2 focus-visible:ring-focus-ring"
              >
                {child.name}
              </ShellLink>
              <span className="hidden shrink-0 text-metadata text-muted-foreground sm:inline">
                {copy.types[child.type]}
              </span>
              {child.visibility === "Private" ? (
                <Badge variant="outline" className="shrink-0 gap-1 font-normal">
                  <Lock aria-hidden="true" className="size-3" />
                  {copy.tree.private}
                </Badge>
              ) : null}
            </li>
          );
        })}
      </SectionList>
      {content.status === "loading" ? (
        <div
          role="status"
          aria-label={copy.overview.loading}
          className="flex flex-col gap-2"
        >
          <Skeleton className="h-5 w-24" />
          <Skeleton className="h-11 w-full rounded-lg" />
          <Skeleton className="h-11 w-full rounded-lg" />
        </div>
      ) : content.status === "error" ? (
        <div
          role="alert"
          className="flex flex-wrap items-center justify-between gap-3 rounded-lg border border-border bg-surface-panel px-4 py-3"
        >
          <p className="text-body text-muted-foreground">
            {copy.overview.error}
          </p>
          <Button variant="outline" size="sm" onClick={props.onRetryContent}>
            {copy.page.retry}
          </Button>
        </div>
      ) : (
        <>
          <SectionList
            title={copy.overview.boards}
            empty={copy.overview.boardsEmpty}
            count={content.data.boards.length}
          >
            {content.data.boards.map((board) => resourceRow(board, "board"))}
          </SectionList>
          <SectionList
            title={copy.overview.docs}
            empty={copy.overview.docsEmpty}
            count={content.data.docs.length}
          >
            {content.data.docs.map((doc) => resourceRow(doc, "doc"))}
          </SectionList>
        </>
      )}
    </div>
  );
}

/**
 * A Space page (OT-5): where it sits in the org tree, what is inside it
 * (child Spaces, Boards, Docs), its members and its settings. The member's
 * effective access on the Space (GOV-027) decides which actions are offered;
 * the producer authorizes each one.
 */
export function WorkspaceSpaceSurface(props: WorkspaceSpaceSurfaceProps) {
  const { copy, navigation, space, path, tab, onTabChange } = props;

  if (space.status === "loading") {
    return (
      <div
        role="status"
        aria-label={copy.page.loading}
        className="mx-auto flex w-full max-w-5xl flex-col gap-4 px-4 py-6 sm:px-6"
      >
        <Skeleton className="h-4 w-48" />
        <Skeleton className="h-8 w-64" />
        <Skeleton className="h-9 w-80" />
        <Skeleton className="h-40 w-full rounded-lg" />
      </div>
    );
  }
  if (space.status === "error" || space.data === null) {
    const notFound = space.status === "ready";
    return (
      <div className="mx-auto flex w-full max-w-3xl flex-col px-4 py-10 sm:px-6">
        <Empty className="border border-border bg-surface-panel">
          <EmptyHeader>
            <EmptyTitle className="text-subheading">
              {notFound ? copy.page.notFound : copy.page.error}
            </EmptyTitle>
            {notFound ? (
              <EmptyDescription className="text-body">
                {copy.page.notFoundDescription}
              </EmptyDescription>
            ) : null}
          </EmptyHeader>
          <EmptyContent className="flex-row justify-center gap-2">
            {notFound ? null : (
              <Button size="sm" variant="outline" onClick={props.onRetry}>
                {copy.page.retry}
              </Button>
            )}
            <Button size="sm" variant={notFound ? "outline" : "ghost"} asChild>
              <ShellLink
                destination={{ section: "home" }}
                navigation={navigation}
              >
                {copy.page.backHome}
              </ShellLink>
            </Button>
          </EmptyContent>
        </Empty>
      </div>
    );
  }

  const node = space.data;
  const caps = spaceCapabilities(node);
  const Icon = SPACE_TYPE_ICON[node.type];
  const ancestors = path.slice(0, -1);
  const hasMore = caps.canCreateChild || caps.canMove;
  const counts: SpaceContentCounts | null =
    props.content.status === "ready"
      ? {
          childSpaces: props.childSpaces.filter((child) => !child.isArchived)
            .length,
          boards: props.content.data.boards.length,
          docs: props.content.data.docs.length,
        }
      : null;

  return (
    <div className="mx-auto flex w-full max-w-5xl flex-col gap-5 px-4 py-6 sm:px-6">
      <header className="flex flex-col gap-3">
        {ancestors.length > 0 ? (
          <nav aria-label={copy.page.breadcrumb}>
            <ol className="flex flex-wrap items-center gap-1 text-metadata text-muted-foreground">
              {ancestors.map((item) => (
                <li key={item.id} className="flex min-w-0 items-center gap-1">
                  <ShellLink
                    destination={{ section: "space", spaceId: item.id }}
                    navigation={navigation}
                    className="truncate rounded-sm text-link outline-none hover:underline focus-visible:ring-2 focus-visible:ring-focus-ring"
                  >
                    {item.name}
                  </ShellLink>
                  <ChevronRight
                    aria-hidden="true"
                    className="size-3.5 shrink-0"
                  />
                </li>
              ))}
              <li aria-current="page" className="truncate text-foreground">
                {node.name}
              </li>
            </ol>
          </nav>
        ) : null}
        <div className="flex flex-col gap-3 sm:flex-row sm:items-start sm:justify-between">
          <div className="flex min-w-0 items-start gap-3">
            <span
              aria-hidden="true"
              className="flex size-10 shrink-0 items-center justify-center rounded-lg border border-border bg-surface-raised text-icon"
            >
              <Icon className="size-5" />
            </span>
            <div className="flex min-w-0 flex-col gap-1">
              <h1 className="flex flex-wrap items-center gap-2 text-page-title text-foreground">
                <span className="min-w-0 break-words">{node.name}</span>
                {node.visibility === "Private" ? (
                  <Badge variant="outline" className="gap-1 font-normal">
                    <Lock aria-hidden="true" className="size-3" />
                    {copy.tree.private}
                  </Badge>
                ) : null}
                {node.isArchived ? (
                  <Badge variant="secondary" className="font-normal">
                    {copy.tree.archived}
                  </Badge>
                ) : null}
              </h1>
              <p className="text-metadata text-muted-foreground">
                {copy.types[node.type]} · {copy.access[node.access]}
              </p>
              {node.description ? (
                <p className="max-w-prose text-body text-muted-foreground">
                  {node.description}
                </p>
              ) : null}
            </div>
          </div>
          <div className="flex shrink-0 flex-wrap gap-2">
            {caps.canCreateResources ? (
              <>
                <Button size="sm" onClick={props.onCreateBoard}>
                  <Plus aria-hidden="true" />
                  {copy.page.newBoard}
                </Button>
                <Button size="sm" variant="outline" onClick={props.onCreateDoc}>
                  <Plus aria-hidden="true" />
                  {copy.page.newDoc}
                </Button>
              </>
            ) : null}
            {hasMore ? (
              <DropdownMenu>
                <DropdownMenuTrigger asChild>
                  <Button
                    size="icon-sm"
                    variant="outline"
                    aria-label={formatCopy(copy.page.moreActions, {
                      name: node.name,
                    })}
                  >
                    <MoreHorizontal aria-hidden="true" />
                  </Button>
                </DropdownMenuTrigger>
                <DropdownMenuContent align="end" className="w-56">
                  {caps.canCreateChild ? (
                    <DropdownMenuItem onSelect={props.onCreateChild}>
                      <FolderPlus aria-hidden="true" />
                      {copy.page.newChild}
                    </DropdownMenuItem>
                  ) : null}
                  {caps.canMove ? (
                    <DropdownMenuItem onSelect={props.onMoveSpace}>
                      <ArrowRightLeft aria-hidden="true" />
                      {copy.page.move}
                    </DropdownMenuItem>
                  ) : null}
                </DropdownMenuContent>
              </DropdownMenu>
            ) : null}
          </div>
        </div>
        {node.isArchived ? (
          <div
            role="status"
            className="flex flex-wrap items-center justify-between gap-3 rounded-md bg-warning-subtle px-3 py-2 text-metadata text-warning-subtle-foreground"
          >
            <span>{copy.page.archivedNotice}</span>
            {caps.canManage ? (
              <Button
                size="xs"
                variant="outline"
                disabled={props.unarchiving}
                onClick={props.onUnarchive}
              >
                {props.unarchiving
                  ? copy.page.unarchiving
                  : copy.page.unarchive}
              </Button>
            ) : null}
          </div>
        ) : null}
      </header>

      <Tabs
        value={tab}
        onValueChange={(value) => onTabChange(value as SpaceTab)}
        className="gap-4"
      >
        <TabsList aria-label={copy.page.tabs.label}>
          <TabsTrigger value="overview">{copy.page.tabs.overview}</TabsTrigger>
          <TabsTrigger value="members">{copy.page.tabs.members}</TabsTrigger>
          <TabsTrigger value="settings">{copy.page.tabs.settings}</TabsTrigger>
        </TabsList>
        <TabsContent value="overview">
          <Overview {...props} node={node} />
        </TabsContent>
        <TabsContent value="members">
          <SpaceMembersPanel
            copy={copy}
            spaceName={node.name}
            canManage={caps.canManage && !node.isArchived}
            members={props.members}
            users={props.candidateUsers}
            teams={props.candidateTeams}
            pendingKeys={props.pendingMemberKeys}
            adding={props.addingMember}
            addError={props.addMemberError}
            addResetKey={props.addMemberResetKey}
            onAdd={props.onAddMember}
            onChangeRole={props.onChangeMemberRole}
            onRemove={props.onRemoveMember}
            onRetry={props.onRetryMembers}
          />
        </TabsContent>
        <TabsContent value="settings">
          <SpaceSettingsPanel
            copy={copy}
            space={node}
            canManage={caps.canManage}
            saveStatus={props.saveStatus}
            content={counts}
            archiving={props.archiving}
            archiveRefused={props.archiveRefused}
            onSave={props.onSave}
            onArchive={props.onArchive}
          />
        </TabsContent>
      </Tabs>
    </div>
  );
}

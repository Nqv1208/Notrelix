import * as React from "react";
import {
  Button,
  Empty,
  EmptyContent,
  EmptyDescription,
  EmptyHeader,
  EmptyTitle,
  Skeleton,
  cn,
} from "@notrelix/ui-web";
import {
  markToneFor,
  type SpaceTab,
  type WorkspaceDestination,
} from "../../core/model/navigation";
import {
  spaceCapabilities,
  type SpaceContentCounts,
  type SpaceMemberEntry,
  type SpaceNode,
  type SpaceRole,
  type SpaceTeam,
} from "../../core/model/spaces";
import type { SectionState } from "../home/home-models";
import { formatCopy, type ShellNavigation } from "../shell/shell-models";
import { ShellLink } from "../shell/shell-link";
import {
  AccessBlock,
  AttentionBlock,
  BoardsSection,
  ChildrenSection,
  DocsSection,
  SetupSection,
  WorkloadBlock,
} from "./space-overview";
import { FOCUS, Glyph, BTN_SM, SpaceHead, SpaceTabs } from "./space-page-parts";
import type {
  SpaceAccessView,
  SpaceAttentionRowView,
  SpaceChildView,
  SpaceContentView,
  SpaceHeadPeople,
  SpaceTemplateView,
  SpaceWorkloadRowView,
} from "./space-page-models";
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
import type { WorkspaceSpacesCopy } from "./workspace-spaces.copy";

export interface WorkspaceSpaceSurfaceProps {
  readonly copy: WorkspaceSpacesCopy;
  readonly navigation: ShellNavigation;
  /** Reference time for the "updated" labels (epoch ms). */
  readonly now: number;
  /** `ready` with `null` = the Space is not visible to the member (NotFound). */
  readonly space: SectionState<SpaceNode | null>;
  readonly workspaceName: string;
  /** Names of the visible ancestors, root first, without the Space itself. */
  readonly ancestors: readonly string[];
  readonly tab: SpaceTab;
  readonly people: SpaceHeadPeople | null;
  readonly peopleTotal: number | null;
  readonly contentCount: number | null;
  readonly membersCount: number | null;
  readonly content: SectionState<SpaceContentView>;
  readonly childSpaces: readonly SpaceChildView[];
  readonly access: SpaceAccessView | null;
  /** The summary blocks the producers cannot fill stay `null`. */
  readonly attention: readonly SpaceAttentionRowView[] | null;
  readonly workload: readonly SpaceWorkloadRowView[] | null;
  readonly templates: readonly SpaceTemplateView[];
  /** A department with nothing in it yet: the first-steps state. */
  readonly firstSteps: boolean;
  /** "Viewing as a member" banner; `null` when not previewing. */
  readonly viewAs: { readonly onExit: () => void } | null;
  /** Members tab (the members panel). */
  readonly members: SectionState<readonly SpaceMemberEntry[]>;
  readonly candidateUsers: SectionState<readonly SpaceMemberCandidate[]>;
  readonly candidateTeams: SectionState<readonly SpaceTeam[]>;
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
  readonly onUnarchive: () => void;
  readonly onSave: (changes: Partial<SpaceDetailsValues>) => void;
  readonly onArchive: () => void;
  /** `null`: no delete write exists, the button is drawn inert. */
  readonly onDelete: (() => void) | null;
  readonly onAddMember: (input: AddSpaceMemberInput) => void;
  readonly onChangeMemberRole: (
    entry: SpaceMemberEntry,
    role: SpaceRole,
  ) => void;
  readonly onRemoveMember: (entry: SpaceMemberEntry) => void;
  readonly onRetry: () => void;
  readonly onRetryContent: () => void;
  readonly onRetryMembers: () => void;
  /** Starts with the Create menu open (stories, tests). */
  readonly initialMenuOpen?: boolean;
  readonly initialDeleteOpen?: boolean;
}

const LIBRARY: WorkspaceDestination = { section: "library" };

function ContentSkeleton({ label }: { label: string }) {
  return (
    <div role="status" aria-label={label} className="flex flex-col gap-2">
      <Skeleton className="h-5 w-24" />
      <Skeleton className="h-[58px] w-full rounded-[12px]" />
      <Skeleton className="h-[58px] w-full rounded-[12px]" />
    </div>
  );
}

/**
 * A department (Space) page (Final-Space): the head with its actions and the
 * tabs, then the overview, the members or the settings. The member's access
 * on the Space (GOV-027) decides which actions are offered; the producer
 * authorizes each one.
 */
export function WorkspaceSpaceSurface(props: WorkspaceSpaceSurfaceProps) {
  const { copy, navigation, space, tab } = props;
  const [menuOpen, setMenuOpen] = React.useState(
    props.initialMenuOpen ?? false,
  );

  if (space.status === "loading") {
    return (
      <div
        role="status"
        aria-label={copy.page.loading}
        className="flex w-full max-w-5xl flex-col gap-4 px-7 py-6"
      >
        <Skeleton className="h-4 w-48" />
        <Skeleton className="h-8 w-64" />
        <Skeleton className="h-9 w-80" />
        <Skeleton className="h-40 w-full rounded-[12px]" />
      </div>
    );
  }
  if (space.status === "error" || space.data === null) {
    const notFound = space.status === "ready";
    return (
      <div className="mx-auto flex w-full max-w-3xl flex-col px-4 py-10 sm:px-6">
        <Empty className="border border-line bg-popover dark:bg-card">
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
  const counts: SpaceContentCounts | null =
    props.content.status === "ready"
      ? {
          childSpaces: props.childSpaces.length,
          boards: props.content.data.boardTotal,
          docs: props.content.data.docTotal,
        }
      : null;
  const asMember = props.viewAs !== null;
  const manage = caps.canManage && !asMember;
  const gotoTab = (target: SpaceTab) =>
    navigation.onNavigate({
      section: "space",
      spaceId: node.id,
      tab: target === "overview" ? undefined : target,
    });
  const banner = (children: React.ReactNode, action: React.ReactNode) => (
    <div
      data-f="viewas"
      role="status"
      className="flex items-center gap-2.5 border-b border-line bg-warning-subtle px-7 py-2 text-crumb leading-[1.45] text-warning-text"
    >
      <Glyph name="eye" />
      <span>{children}</span>
      <span className="ml-auto">{action}</span>
    </div>
  );

  return (
    <div data-f="page" className="flex h-full min-h-0 flex-col">
      {props.viewAs
        ? banner(
            <>
              <b className="font-semibold">{copy.page.viewAs.lead}</b>
              {copy.page.viewAs.text}
            </>,
            <button
              data-f="btn-sm"
              type="button"
              onClick={props.viewAs.onExit}
              className={cn(BTN_SM, FOCUS)}
            >
              {copy.page.viewAs.exit}
            </button>,
          )
        : node.isArchived
          ? banner(
              copy.page.archivedNotice,
              caps.canManage ? (
                <button
                  data-f="btn-sm"
                  type="button"
                  disabled={props.unarchiving}
                  onClick={props.onUnarchive}
                  className={cn(BTN_SM, FOCUS)}
                >
                  {props.unarchiving
                    ? copy.page.unarchiving
                    : copy.page.unarchive}
                </button>
              ) : null,
            )
          : null}
      <SpaceHead
        copy={copy}
        navigation={navigation}
        name={node.name}
        workspaceName={props.workspaceName}
        markTone={markToneFor(node.id)}
        isPrivate={node.visibility === "Private"}
        archived={node.isArchived}
        description={node.description}
        people={props.people}
        peopleTotal={props.peopleTotal}
        canManage={manage}
        canCreate={caps.canCreateResources && !asMember}
        tab={tab}
        spaceId={node.id}
        menuOpen={menuOpen}
        onMenuOpenChange={setMenuOpen}
        onToggleSettings={() =>
          gotoTab(tab === "settings" ? "overview" : "settings")
        }
        onCreateBoard={props.onCreateBoard}
        onCreateDoc={props.onCreateDoc}
        onCreateChild={caps.canCreateChild ? props.onCreateChild : null}
        primaryAdd={props.firstSteps && tab === "overview"}
      />
      <SpaceTabs
        copy={copy}
        navigation={navigation}
        name={node.name}
        spaceId={node.id}
        tab={tab}
        contentCount={props.contentCount}
        membersCount={props.membersCount}
        showSettings={manage || tab === "settings"}
      />
      <div data-f="body" className="flex min-h-0 flex-1 overflow-hidden">
        {tab === "overview" ? (
          <>
            <div
              data-f="col"
              className={cn(
                "flex min-w-0 flex-1 flex-col gap-7 overflow-auto px-7 pt-[22px] pb-8",
                props.firstSteps && "box-content max-w-[820px]",
              )}
            >
              {props.content.status === "loading" ? (
                <ContentSkeleton label={copy.overview.loading} />
              ) : props.content.status === "error" ? (
                <div
                  role="alert"
                  className="flex flex-wrap items-center justify-between gap-3 rounded-[12px] border border-line bg-popover px-4 py-3 dark:bg-card"
                >
                  <p className="m-0 text-body text-muted-foreground">
                    {copy.overview.error}
                  </p>
                  <button
                    type="button"
                    onClick={props.onRetryContent}
                    className={cn(BTN_SM, FOCUS)}
                  >
                    {copy.page.retry}
                  </button>
                </div>
              ) : props.firstSteps ? (
                <SetupSection
                  copy={copy}
                  name={node.name}
                  isPrivate={node.visibility === "Private"}
                  templates={props.templates}
                  onAddMembers={() => gotoTab("members")}
                  onCreateBoard={
                    caps.canCreateResources ? props.onCreateBoard : null
                  }
                />
              ) : (
                <>
                  <BoardsSection
                    copy={copy}
                    navigation={navigation}
                    now={props.now}
                    rows={props.content.data.boards}
                    total={props.content.data.boardTotal}
                    libraryHref={LIBRARY}
                    note={
                      asMember
                        ? formatCopy(copy.overview.viewAsNote, {
                            count: props.content.data.boardTotal,
                          })
                        : props.content.data.boards.some((b) => b.isPrivate)
                          ? copy.overview.boardNote
                          : null
                    }
                  />
                  {props.childSpaces.length > 0 || caps.canCreateChild ? (
                    <ChildrenSection
                      copy={copy}
                      navigation={navigation}
                      spaceName={node.name}
                      children={props.childSpaces}
                      canCreate={caps.canCreateChild && !asMember}
                      onCreate={props.onCreateChild}
                    />
                  ) : null}
                  <DocsSection
                    copy={copy}
                    navigation={navigation}
                    now={props.now}
                    rows={props.content.data.docs}
                    total={props.content.data.docTotal}
                    libraryHref={LIBRARY}
                  />
                </>
              )}
            </div>
            {props.attention || props.workload || props.access ? (
              <aside
                data-f="aside"
                aria-label={copy.aside.label}
                className="box-border flex w-[340px] shrink-0 flex-col gap-[22px] overflow-auto border-l border-line bg-popover px-5 pt-5 pb-7 dark:bg-card"
              >
                {!props.firstSteps && props.attention ? (
                  <AttentionBlock copy={copy} rows={props.attention} />
                ) : null}
                {!props.firstSteps && props.workload ? (
                  <WorkloadBlock copy={copy} rows={props.workload} />
                ) : null}
                {props.access ? (
                  <AccessBlock
                    copy={copy}
                    navigation={navigation}
                    spaceId={node.id}
                    access={props.access}
                    canManage={manage}
                    hint={
                      props.firstSteps && node.visibility === "Private"
                        ? copy.setup.accessHint
                        : undefined
                    }
                    showLink={!props.firstSteps}
                  />
                ) : null}
              </aside>
            ) : null}
          </>
        ) : tab === "members" ? (
          <div
            data-f="col"
            className="min-w-0 flex-1 overflow-auto px-7 pt-[22px] pb-8"
          >
            <SpaceMembersPanel
              copy={copy}
              spaceName={node.name}
              canManage={manage && !node.isArchived}
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
          </div>
        ) : (
          <SpaceSettingsPanel
            copy={copy}
            space={node}
            workspaceName={props.workspaceName}
            ancestors={props.ancestors}
            childNames={props.childSpaces.map((child) => child.name)}
            canManage={manage}
            saveStatus={props.saveStatus}
            content={counts}
            archiving={props.archiving}
            archiveRefused={props.archiveRefused}
            onSave={props.onSave}
            onMove={props.onMoveSpace}
            onArchive={props.onArchive}
            onDelete={props.onDelete}
            initialDeleteOpen={props.initialDeleteOpen}
          />
        )}
      </div>
    </div>
  );
}

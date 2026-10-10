import type { ReactNode } from "react";
import { ArrowRight } from "lucide-react";
import { Button, Skeleton, cn } from "@notrelix/ui-web";
import {
  destinationForEntry,
  identityToneFor,
  NAVIGATION_RESOURCE_KIND,
} from "../../core/model/navigation";
import { IdentityMark, ResourceMark, ShellLink } from "../shell/shell-link";
import { formatCopy, type ShellNavigation } from "../shell/shell-models";
import type {
  ContinueEntry,
  HomeActivityEntry,
  HomeInvitation,
  MyWorkGroupView,
  MyWorkItemView,
  SectionState,
  SurfaceFormatters,
} from "./home-models";
import { MyWorkList } from "./my-work-list";
import { MyWorkSkeleton } from "./workspace-my-work-surface";
import {
  WorkspaceOnboardingSurface,
  type WorkspaceOnboardingSurfaceProps,
} from "./workspace-onboarding-surface";
import type { WorkspaceHomeCopy } from "./workspace-home.copy";

export interface WorkspaceHomeSurfaceProps {
  readonly copy: WorkspaceHomeCopy;
  readonly navigation: ShellNavigation;
  readonly formatters: SurfaceFormatters;
  /** Reference time for relative labels (epoch ms). */
  readonly now: number;
  /** `null` while the workspace record loads. */
  readonly workspaceName: string | null;
  /** 5–8 recently opened Boards and Docs. */
  readonly continueState: SectionState<readonly ContinueEntry[]>;
  /** A preview of My work (the first items across due windows). */
  readonly myWork: SectionState<readonly MyWorkGroupView[]>;
  /** More assigned items exist than the preview shows. */
  readonly myWorkHasMore: boolean;
  readonly completingIds: ReadonlySet<string>;
  readonly onComplete: ((item: MyWorkItemView) => void) | null;
  readonly activity: SectionState<readonly HomeActivityEntry[]>;
  /** `null` hides the section (members who are not admins). */
  readonly invitations: SectionState<readonly HomeInvitation[]> | null;
  /**
   * A new workspace (no Boards and no Docs): the onboarding steps replace
   * the empty Continue and My work sections (WX-HOME-002).
   */
  readonly onboarding: Pick<
    WorkspaceOnboardingSurfaceProps,
    "canCreate" | "canInvite"
  > | null;
  readonly onCreateBoard: () => void;
  readonly onCreateDoc: () => void;
  readonly onInvite: () => void;
  readonly onRetryContinue: () => void;
  readonly onRetryMyWork: () => void;
  readonly onRetryActivity: () => void;
  readonly onRetryInvitations: () => void;
}

function Section({
  id,
  title,
  description,
  action,
  children,
}: {
  id: string;
  title: string;
  description?: string;
  action?: ReactNode;
  children: ReactNode;
}) {
  return (
    <section aria-labelledby={id} className="flex min-w-0 flex-col gap-3">
      <div className="flex flex-wrap items-end justify-between gap-x-3 gap-y-1">
        <div className="flex min-w-0 flex-col">
          <h2 id={id} className="text-subheading text-foreground">
            {title}
          </h2>
          {description ? (
            <p className="text-metadata text-muted-foreground">{description}</p>
          ) : null}
        </div>
        {action}
      </div>
      {children}
    </section>
  );
}

function SectionError({
  copy,
  onRetry,
}: {
  copy: WorkspaceHomeCopy;
  onRetry: () => void;
}) {
  return (
    <div
      role="alert"
      className="flex flex-wrap items-center justify-between gap-2 rounded-md border border-border px-3 py-2"
    >
      <p className="text-metadata text-muted-foreground">
        {copy.sections.error}
      </p>
      <Button variant="ghost" size="xs" onClick={onRetry}>
        {copy.sections.retry}
      </Button>
    </div>
  );
}

function ListSkeleton({ rows, label }: { rows: number; label: string }) {
  return (
    <div role="status" aria-label={label} className="flex flex-col gap-1">
      {Array.from({ length: rows }, (_, index) => (
        <div key={index} className="flex h-10 items-center gap-3 px-2">
          <Skeleton className="size-7 rounded-md" />
          <Skeleton
            className={index % 2 === 0 ? "h-3.5 w-2/5" : "h-3.5 w-1/3"}
          />
          <Skeleton className="ml-auto h-3 w-16" />
        </div>
      ))}
    </div>
  );
}

function SectionLink({
  navigation,
  destination,
  children,
}: {
  navigation: ShellNavigation;
  destination: Parameters<ShellNavigation["getHref"]>[0];
  children: ReactNode;
}) {
  return (
    <ShellLink
      destination={destination}
      navigation={navigation}
      className="inline-flex shrink-0 items-center gap-1 rounded-sm text-metadata font-medium text-link hover:underline focus-visible:ring-2 focus-visible:ring-focus-ring focus-visible:outline-none"
    >
      {children}
      <ArrowRight aria-hidden="true" className="size-3.5" />
    </ShellLink>
  );
}

function ContinueList({
  copy,
  navigation,
  formatters,
  now,
  entries,
}: {
  copy: WorkspaceHomeCopy;
  navigation: ShellNavigation;
  formatters: SurfaceFormatters;
  now: number;
  entries: readonly ContinueEntry[];
}) {
  return (
    <ul className="flex flex-col">
      {entries.map((entry) => {
        const isBoard = entry.kind === NAVIGATION_RESOURCE_KIND.board;
        const kindLabel = isBoard ? copy.continue.board : copy.continue.doc;
        return (
          <li key={`${entry.kind}:${entry.resourceId}`}>
            <ShellLink
              destination={destinationForEntry(entry)}
              navigation={navigation}
              className="flex min-h-11 items-center gap-3 rounded-md px-2 py-1.5 outline-none hover:bg-state-hover focus-visible:ring-2 focus-visible:ring-focus-ring active:bg-state-selected"
            >
              <ResourceMark kind={entry.kind} id={entry.resourceId} />
              <span className="min-w-0 flex-1">
                <span className="block truncate text-label text-foreground">
                  {entry.name}
                </span>
                <span className="block truncate text-metadata text-muted-foreground">
                  {kindLabel}
                  {entry.location ? ` · ${entry.location}` : ""}
                  {entry.at ? (
                    <span className="tabular-nums sm:hidden">
                      {" · "}
                      {formatters.relativeTime(entry.at, now)}
                    </span>
                  ) : null}
                </span>
              </span>
              {entry.at ? (
                <span className="hidden shrink-0 text-metadata tabular-nums text-muted-foreground sm:block">
                  {formatCopy(copy.continue.opened, {
                    time: formatters.relativeTime(entry.at, now),
                  })}
                </span>
              ) : null}
            </ShellLink>
          </li>
        );
      })}
    </ul>
  );
}

function ActivityList({
  copy,
  formatters,
  now,
  entries,
}: {
  copy: WorkspaceHomeCopy;
  formatters: SurfaceFormatters;
  now: number;
  entries: readonly HomeActivityEntry[];
}) {
  return (
    <ol className="flex flex-col gap-3">
      {entries.map((entry) => (
        <li key={entry.id} className="flex gap-2.5">
          <IdentityMark
            name={entry.actor}
            tone={identityToneFor(entry.actor)}
            size="sm"
            shape="round"
            className="mt-0.5 size-6"
          />
          <span className="min-w-0 flex-1">
            <span className="block text-metadata text-foreground">
              {formatCopy(copy.activity.entry, {
                actor: entry.actor,
                action: entry.action,
                target: entry.target,
              })}
            </span>
            <span className="block text-caption tabular-nums text-muted-foreground">
              {formatters.relativeTime(entry.createdAt, now)}
            </span>
          </span>
        </li>
      ))}
    </ol>
  );
}

function InvitationList({
  copy,
  formatters,
  now,
  invitations,
}: {
  copy: WorkspaceHomeCopy;
  formatters: SurfaceFormatters;
  now: number;
  invitations: readonly HomeInvitation[];
}) {
  return (
    <ul className="flex flex-col divide-y divide-border">
      {invitations.map((invitation) => (
        <li key={invitation.id} className="flex flex-col py-2 first:pt-0">
          <span className="truncate text-label text-foreground">
            {invitation.email}
          </span>
          <span className="text-metadata text-muted-foreground">
            {formatCopy(copy.invitations.role, {
              role: copy.roles[invitation.role],
            })}
            {" · "}
            <span className="tabular-nums">
              {formatCopy(copy.invitations.expires, {
                time: formatters.relativeTime(invitation.expiresAt, now),
              })}
            </span>
          </span>
        </li>
      ))}
    </ul>
  );
}

/**
 * Workspace Home (WX-HOME-001): "resume work". The main column lists recent
 * Boards/Docs and the member's assigned Items; the side column carries
 * workspace activity and, for admins, pending invitations. Each section
 * loads, fails and retries on its own, so partial data stays usable.
 */
export function WorkspaceHomeSurface(props: WorkspaceHomeSurfaceProps) {
  const {
    copy,
    navigation,
    formatters,
    now,
    workspaceName,
    continueState,
    myWork,
    activity,
    invitations,
    onboarding,
  } = props;

  return (
    <div className="mx-auto flex w-full max-w-6xl flex-col gap-8 px-4 py-6 md:px-8 md:py-8">
      <header className="flex flex-col gap-1">
        <h1 className="text-page-title text-foreground">{copy.page.title}</h1>
        <p className="text-body text-muted-foreground">
          {workspaceName
            ? formatCopy(copy.page.greeting, { workspace: workspaceName })
            : copy.page.greetingLoading}
        </p>
      </header>

      <div className="grid gap-10 lg:grid-cols-[minmax(0,1fr)_18rem] lg:gap-12">
        <div className="flex min-w-0 flex-col gap-10">
          {onboarding ? (
            <WorkspaceOnboardingSurface
              copy={copy}
              workspaceName={workspaceName ?? ""}
              canCreate={onboarding.canCreate}
              canInvite={onboarding.canInvite}
              onCreateBoard={props.onCreateBoard}
              onCreateDoc={props.onCreateDoc}
              onInvite={props.onInvite}
            />
          ) : (
            <Section
              id="home-continue"
              title={copy.continue.title}
              description={copy.continue.description}
              action={
                <SectionLink
                  navigation={navigation}
                  destination={{ section: "library" }}
                >
                  {copy.continue.viewAll}
                </SectionLink>
              }
            >
              {continueState.status === "loading" ? (
                <ListSkeleton rows={5} label={copy.continue.title} />
              ) : continueState.status === "error" ? (
                <SectionError copy={copy} onRetry={props.onRetryContinue} />
              ) : continueState.data.length === 0 ? (
                <p className="px-2 py-2 text-metadata text-muted-foreground">
                  {copy.continue.empty}
                </p>
              ) : (
                <ContinueList
                  copy={copy}
                  navigation={navigation}
                  formatters={formatters}
                  now={now}
                  entries={continueState.data}
                />
              )}
            </Section>
          )}

          <Section
            id="home-my-work"
            title={copy.myWork.title}
            description={copy.myWork.description}
            action={
              myWork.status === "ready" && props.myWorkHasMore ? (
                <SectionLink
                  navigation={navigation}
                  destination={{ section: "my-work" }}
                >
                  {copy.myWork.viewAll}
                </SectionLink>
              ) : undefined
            }
          >
            {myWork.status === "loading" ? (
              <div role="status" aria-label={copy.myWork.title}>
                <MyWorkSkeleton rows={4} />
              </div>
            ) : myWork.status === "error" ? (
              <SectionError copy={copy} onRetry={props.onRetryMyWork} />
            ) : myWork.data.length === 0 ? (
              <div className="flex flex-col gap-0.5 px-2 py-2">
                <p className="text-metadata text-foreground">
                  {copy.myWork.empty}
                </p>
                <p className="text-metadata text-muted-foreground">
                  {copy.myWork.emptyHint}
                </p>
              </div>
            ) : (
              <MyWorkList
                copy={copy}
                navigation={navigation}
                formatters={formatters}
                groups={myWork.data}
                completingIds={props.completingIds}
                onComplete={props.onComplete}
                showCounts={!props.myWorkHasMore}
              />
            )}
          </Section>
        </div>

        <aside
          aria-label={copy.page.sideLabel}
          className={cn(
            "flex min-w-0 flex-col gap-10",
            "border-t border-border pt-8 lg:border-t-0 lg:border-l lg:pt-0 lg:pl-8",
          )}
        >
          <Section id="home-activity" title={copy.activity.title}>
            {activity.status === "loading" ? (
              <ListSkeleton rows={3} label={copy.activity.title} />
            ) : activity.status === "error" ? (
              <SectionError copy={copy} onRetry={props.onRetryActivity} />
            ) : activity.data.length === 0 ? (
              <p className="text-metadata text-muted-foreground">
                {copy.activity.empty}
              </p>
            ) : (
              <ActivityList
                copy={copy}
                formatters={formatters}
                now={now}
                entries={activity.data}
              />
            )}
          </Section>

          {invitations ? (
            <Section
              id="home-invitations"
              title={copy.invitations.title}
              action={
                <SectionLink
                  navigation={navigation}
                  destination={{ section: "members", tab: "invitations" }}
                >
                  {copy.invitations.manage}
                </SectionLink>
              }
            >
              {invitations.status === "loading" ? (
                <ListSkeleton rows={2} label={copy.invitations.title} />
              ) : invitations.status === "error" ? (
                <SectionError copy={copy} onRetry={props.onRetryInvitations} />
              ) : invitations.data.length === 0 ? (
                <p className="text-metadata text-muted-foreground">
                  {copy.invitations.empty}
                </p>
              ) : (
                <InvitationList
                  copy={copy}
                  formatters={formatters}
                  now={now}
                  invitations={invitations.data}
                />
              )}
            </Section>
          ) : null}
        </aside>
      </div>
    </div>
  );
}

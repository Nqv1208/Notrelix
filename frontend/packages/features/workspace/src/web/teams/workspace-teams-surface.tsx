import { Info, Plus, Users } from "lucide-react";
import {
  Badge,
  Button,
  Empty,
  EmptyContent,
  EmptyDescription,
  EmptyHeader,
  EmptyTitle,
  Skeleton,
  ToggleGroup,
  ToggleGroupItem,
} from "@notrelix/ui-web";
import type { Team } from "../../core/model/teams";
import { pluralForm, type SectionState } from "../home/home-models";
import { isPlainClick } from "../shell/shell-models";
import type { WorkspaceTeamsCopy } from "./workspace-teams.copy";

export type TeamsFilter = "active" | "archived";

export interface WorkspaceTeamsSurfaceProps {
  readonly copy: WorkspaceTeamsCopy;
  /** Active and archived Teams; the surface shows the `filter` half. */
  readonly teams: SectionState<readonly Team[]>;
  readonly filter: TeamsFilter;
  readonly onFilterChange: (filter: TeamsFilter) => void;
  /** Workspace admins and owners manage Teams. */
  readonly canManage: boolean;
  /** Why the viewer cannot manage Teams; shown as text, `null` when they can. */
  readonly readOnlyReason: string | null;
  readonly navigation: {
    readonly getHref: (teamId: string) => string;
    readonly onNavigate: (teamId: string) => void;
  };
  readonly onCreate: () => void;
  readonly onRetry: () => void;
}

function TeamsSkeleton({ label }: { label: string }) {
  return (
    <div
      role="status"
      aria-label={label}
      className="overflow-hidden rounded-lg border border-border bg-surface-panel"
    >
      {[0, 1, 2, 3, 4].map((row) => (
        <div
          key={row}
          className="flex h-14 items-center gap-3 border-b border-border px-4 last:border-0"
        >
          <Skeleton className="size-8 rounded-md" />
          <Skeleton className={row % 2 ? "h-3.5 w-44" : "h-3.5 w-32"} />
          <Skeleton className="ml-auto h-3.5 w-20" />
        </div>
      ))}
    </div>
  );
}

/**
 * The workspace's Teams (WX-TEAM-001): a dense list with each Team's member
 * count, an Active / Archived filter and, for admins, New Team. Read-only
 * for everyone else, with the reason shown as text.
 */
export function WorkspaceTeamsSurface({
  copy,
  teams,
  filter,
  onFilterChange,
  canManage,
  readOnlyReason,
  navigation,
  onCreate,
  onRetry,
}: WorkspaceTeamsSurfaceProps) {
  const wanted = filter === "archived" ? "Archived" : "Active";
  const shown =
    teams.status === "ready"
      ? teams.data.filter((team) => team.status === wanted)
      : [];
  const emptyList = teams.status === "ready" && shown.length === 0;
  const offerCreateInEmpty = emptyList && canManage && filter === "active";

  return (
    <div className="mx-auto flex w-full max-w-5xl flex-col gap-6 px-4 py-6 md:px-8 md:py-8">
      <header className="flex flex-col gap-4 sm:flex-row sm:items-end sm:justify-between">
        <div className="flex min-w-0 flex-col gap-1">
          <h1 className="text-page-title text-foreground">{copy.list.title}</h1>
          <p className="text-body text-muted-foreground">
            {copy.list.description}
          </p>
        </div>
        {canManage && !offerCreateInEmpty ? (
          <Button size="sm" onClick={onCreate} className="self-start">
            <Plus aria-hidden="true" />
            {copy.list.new}
          </Button>
        ) : null}
      </header>

      {!canManage && readOnlyReason ? (
        <p className="flex items-center gap-2 rounded-md bg-info-subtle px-3 py-2 text-metadata text-info-subtle-foreground">
          <Info aria-hidden="true" className="size-4 shrink-0" />
          {readOnlyReason}
        </p>
      ) : null}

      <ToggleGroup
        type="single"
        variant="outline"
        size="sm"
        value={filter}
        onValueChange={(value) => {
          if (value === "active" || value === "archived") onFilterChange(value);
        }}
        aria-label={copy.list.filterLabel}
        className="self-start"
      >
        <ToggleGroupItem value="active">
          {copy.list.filter.active}
        </ToggleGroupItem>
        <ToggleGroupItem value="archived">
          {copy.list.filter.archived}
        </ToggleGroupItem>
      </ToggleGroup>

      {teams.status === "loading" ? (
        <TeamsSkeleton label={copy.list.loading} />
      ) : teams.status === "error" ? (
        <div
          role="alert"
          className="flex flex-wrap items-center justify-between gap-3 rounded-lg border border-border bg-surface-panel px-4 py-3"
        >
          <p className="text-body text-muted-foreground">{copy.list.error}</p>
          <Button variant="outline" size="sm" onClick={onRetry}>
            {copy.list.retry}
          </Button>
        </div>
      ) : emptyList ? (
        <Empty className="rounded-lg border border-dashed border-border bg-surface-panel">
          <EmptyHeader>
            <EmptyTitle>
              {filter === "archived"
                ? copy.list.empty.archivedTitle
                : copy.list.empty.title}
            </EmptyTitle>
            <EmptyDescription>
              {filter === "archived"
                ? copy.list.empty.archivedDescription
                : copy.list.empty.description}
            </EmptyDescription>
          </EmptyHeader>
          {offerCreateInEmpty ? (
            <EmptyContent>
              <Button size="sm" onClick={onCreate}>
                <Plus aria-hidden="true" />
                {copy.list.new}
              </Button>
            </EmptyContent>
          ) : null}
        </Empty>
      ) : (
        <ul
          aria-label={copy.list.title}
          className="overflow-hidden rounded-lg border border-border bg-surface-panel"
        >
          {shown.map((team) => (
            <li
              key={team.id}
              className="flex flex-wrap items-center gap-x-3 gap-y-1 border-t border-border px-4 py-2.5 first:border-t-0 hover:bg-state-hover sm:flex-nowrap"
            >
              <span
                aria-hidden="true"
                className="flex size-8 shrink-0 items-center justify-center rounded-md border border-border bg-surface-raised text-icon"
              >
                <Users className="size-4" />
              </span>
              <span className="flex min-w-0 flex-1 basis-40 flex-col">
                <span className="flex min-w-0 items-center gap-2">
                  <a
                    href={navigation.getHref(team.id)}
                    onClick={(event) => {
                      if (!isPlainClick(event)) return;
                      event.preventDefault();
                      navigation.onNavigate(team.id);
                    }}
                    className="truncate rounded-sm text-label text-link outline-none hover:underline focus-visible:ring-2 focus-visible:ring-focus-ring"
                  >
                    {team.name}
                  </a>
                  {team.status === "Archived" ? (
                    <Badge variant="secondary" className="shrink-0 font-normal">
                      {copy.list.archived}
                    </Badge>
                  ) : null}
                </span>
                {team.description ? (
                  <span className="truncate text-metadata text-muted-foreground">
                    {team.description}
                  </span>
                ) : null}
              </span>
              <span className="w-full shrink-0 pl-11 text-metadata tabular-nums text-muted-foreground sm:w-auto sm:pl-0">
                {pluralForm(
                  {
                    one: copy.list.members_one,
                    other: copy.list.members_other,
                  },
                  team.memberCount,
                )}
              </span>
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}

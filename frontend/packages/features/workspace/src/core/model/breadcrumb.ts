/**
 * Workspace breadcrumb (WX-SHELL-002).
 *
 * Built from the product hierarchy — Workspace › Board/Doc › View — using
 * the resolved location and loaded data. It never parses a URL: the host
 * resolves the location from its router's matched route and params, and
 * names come from the workspace record and the authorized Board/Doc lists.
 */

import type {
  NavigationResourceSummary,
  WorkspaceDestination,
  WorkspaceSection,
} from "./navigation";

export interface WorkspaceBreadcrumbLabels {
  readonly sections: Readonly<
    Record<Exclude<WorkspaceSection, "board" | "doc" | "space">, string>
  >;
  readonly views: Readonly<Record<string, string>>;
  /** Shown while a name is still loading. */
  readonly loading: string;
  /** Shown when the resource is not in the authorized list. */
  readonly unavailable: string;
  /** Current-page crumb of an open Team, until its name is a crumb (WX-TEAM-001). */
  readonly teamDetails: string;
}

export interface WorkspaceBreadcrumbItem {
  readonly id: string;
  readonly label: string;
  /** Present when the item links somewhere; the last item never links. */
  readonly destination?: WorkspaceDestination;
  /** The name is not known yet (render a placeholder). */
  readonly pending?: boolean;
}

export interface BuildBreadcrumbInput {
  readonly workspaceName: string | null;
  readonly location: WorkspaceDestination | null;
  readonly boards: readonly NavigationResourceSummary[] | null;
  readonly docs: readonly NavigationResourceSummary[] | null;
  /**
   * The visible path of the current Space, root first (OT-5); `null` while
   * the tree loads, `[]` when the Space is not visible to the member.
   */
  readonly spacePath?: readonly NavigationResourceSummary[] | null;
  readonly labels: WorkspaceBreadcrumbLabels;
}

function resourceName(
  list: readonly NavigationResourceSummary[] | null,
  id: string,
  labels: WorkspaceBreadcrumbLabels,
): { label: string; pending: boolean } {
  if (list === null) return { label: labels.loading, pending: true };
  const match = list.find((resource) => resource.id === id);
  return match
    ? { label: match.name, pending: false }
    : { label: labels.unavailable, pending: false };
}

export function buildWorkspaceBreadcrumb({
  workspaceName,
  location,
  boards,
  docs,
  spacePath = null,
  labels,
}: BuildBreadcrumbInput): WorkspaceBreadcrumbItem[] {
  const items: WorkspaceBreadcrumbItem[] = [
    {
      id: "workspace",
      label: workspaceName ?? labels.loading,
      pending: workspaceName === null,
      destination: { section: "home" },
    },
  ];

  if (!location || location.section === "home") {
    return withoutLastLink(items);
  }

  if (location.section === "board") {
    const name = resourceName(boards, location.boardId, labels);
    items.push({
      id: `board:${location.boardId}`,
      label: name.label,
      pending: name.pending,
      destination: { section: "board", boardId: location.boardId },
    });
    if (location.view) {
      items.push({
        id: `view:${location.view}`,
        label: labels.views[location.view] ?? location.view,
      });
    }
    return withoutLastLink(items);
  }

  if (location.section === "doc") {
    const name = resourceName(docs, location.docId, labels);
    items.push({
      id: `doc:${location.docId}`,
      label: name.label,
      pending: name.pending,
    });
    return withoutLastLink(items);
  }

  if (location.section === "space") {
    if (spacePath === null) {
      items.push({
        id: `space:${location.spaceId}`,
        label: labels.loading,
        pending: true,
      });
    } else if (spacePath.length === 0) {
      items.push({
        id: `space:${location.spaceId}`,
        label: labels.unavailable,
      });
    } else {
      for (const space of spacePath) {
        items.push({
          id: `space:${space.id}`,
          label: space.name,
          destination: { section: "space", spaceId: space.id },
        });
      }
    }
    return withoutLastLink(items);
  }

  if (location.section === "teams" && location.teamId) {
    // An open Team: the Teams crumb links back to the list, so it must not be
    // the last crumb (the last crumb is the current page and never links).
    items.push(
      {
        id: "section:teams",
        label: labels.sections.teams,
        destination: { section: "teams" },
      },
      { id: `team:${location.teamId}`, label: labels.teamDetails },
    );
    return withoutLastLink(items);
  }

  items.push({
    id: `section:${location.section}`,
    label: labels.sections[location.section],
  });
  return withoutLastLink(items);
}

function withoutLastLink(
  items: WorkspaceBreadcrumbItem[],
): WorkspaceBreadcrumbItem[] {
  return items.map((item, index) =>
    index === items.length - 1 ? { ...item, destination: undefined } : item,
  );
}

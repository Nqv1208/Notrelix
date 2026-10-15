import { useQuery } from "@tanstack/react-query";
import { workspaceQueryKey } from "@notrelix/query";
import { useAppRuntime } from "@notrelix/runtime-web";

/**
 * What a department's Boards, Docs and people add up to (gap
 * `CTR-GAP-WS-SPACE-OVERVIEW`, see docs/superpowers/be-blockers-2026-10.md,
 * "BE requirement from FE: Space"): the Board figures, the aside's counts,
 * the head count by role (teams expanded) and the Board guests. The server
 * decides what the member may see; `asMember` previews a department member.
 */
export interface SpaceOverviewDto {
  readonly peopleTotal: number;
  readonly people: readonly { readonly id: string; readonly name: string }[];
  readonly boardTotal: number;
  readonly boards: readonly {
    readonly id: string;
    readonly name: string;
    readonly kind: "kanban" | "table" | "inbox";
    readonly meta: string | null;
    readonly isPrivate: boolean;
    readonly addedCount: number | null;
    readonly progress:
      | { readonly kind: "bar"; readonly done: number; readonly total: number }
      | { readonly kind: "text"; readonly text: string }
      | null;
    readonly attention: {
      readonly tone: "over" | "warn" | "ok";
      readonly text: string;
    } | null;
    readonly people: readonly { readonly id: string; readonly name: string }[];
    readonly updatedAt: string | null;
  }[];
  readonly docTotal: number;
  readonly docs: readonly {
    readonly id: string;
    readonly meta: string | null;
    readonly editorName: string | null;
    readonly updatedAt: string | null;
  }[];
  readonly attention: readonly {
    readonly id: "overdue" | "unassigned" | "review" | "stale";
    readonly count: number;
    readonly boardId: string | null;
  }[];
  readonly workload: readonly {
    readonly person: { readonly id: string; readonly name: string };
    readonly active: number;
    readonly overdue: number;
  }[];
  readonly children: readonly {
    readonly id: string;
    readonly people: number | null;
    readonly boards: number | null;
  }[];
  readonly access: {
    readonly admins: readonly { readonly id: string; readonly name: string }[];
    readonly members: SpaceOverviewRole;
    readonly viewers: SpaceOverviewRole;
    readonly guests: readonly {
      readonly id: string;
      readonly name: string;
      readonly boards: readonly {
        readonly id: string;
        readonly name: string;
      }[];
    }[];
  };
  readonly templates: readonly {
    readonly id: string;
    readonly title: string;
    readonly hint: string;
    readonly icon: "contract" | "intake" | "calendar";
  }[];
}

interface SpaceOverviewRole {
  readonly people: number;
  readonly teams: readonly { readonly name: string; readonly count: number }[];
}

/** The summary of a department; `null` while loading or when the read is not offered. */
export function useSpaceOverview(
  workspaceId: string,
  spaceId: string,
  asMember: boolean,
  /** Changes when the Space's content or people change, to read again. */
  revision: string,
): SpaceOverviewDto | null {
  const { api } = useAppRuntime();
  const query = useQuery({
    queryKey: workspaceQueryKey(
      workspaceId,
      "space-overview",
      spaceId,
      asMember ? "member" : "self",
      revision,
    ),
    queryFn: () =>
      api.api.get<SpaceOverviewDto>(
        `/workspaces/${workspaceId}/spaces/${spaceId}/overview${
          asMember ? "?as=member" : ""
        }`,
        { skipGlobalErrorToast: true },
      ),
    staleTime: 15_000,
    retry: false,
  });
  return query.data ?? null;
}

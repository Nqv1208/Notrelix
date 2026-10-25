import { useQuery } from "@tanstack/react-query";
import { workspaceQueryKey } from "@notrelix/query";
import { useAppRuntime } from "@notrelix/runtime-web";

/**
 * What the department's Members page adds to the Space member list (gap
 * `CTR-GAP-WS-MEMBERS-EXT`, see docs/superpowers/be-blockers-2026-10.md,
 * "BE requirement from FE: Members"): the head count by role with the teams
 * expanded, each person's email, job line and last activity, the waiting
 * workspace invitations and the Board guests who are not in the department.
 */
export interface SpaceMembersExtraDto {
  readonly summary: {
    readonly admins: number;
    readonly members: number;
    readonly viewers: number;
    readonly invites: number;
  };
  readonly people: readonly {
    readonly userId: string;
    readonly email: string | null;
    readonly title: string | null;
    readonly lastActiveAt: string | null;
    readonly activeNow: boolean;
    readonly viaTeams: readonly string[];
    readonly creator: boolean;
  }[];
  readonly invitations: readonly {
    readonly id: string;
    readonly email: string;
    readonly name: string | null;
    readonly sentAt: string;
    readonly role: "Admin" | "Member" | "Viewer";
  }[];
  readonly guests: readonly {
    readonly userId: string;
    readonly name: string;
    readonly email: string | null;
    readonly outside: boolean;
    readonly boardId: string;
    readonly boardName: string;
    readonly level: "viewer" | "commenter" | "editor" | "manager";
  }[];
}

/** The extras of a department's members; `null` while loading or when not offered. */
export function useSpaceMembersExtra(
  workspaceId: string,
  spaceId: string,
  enabled: boolean,
  /** Changes when the Space's people change, to read again. */
  revision: string,
): SpaceMembersExtraDto | null {
  const { api } = useAppRuntime();
  const query = useQuery({
    queryKey: workspaceQueryKey(
      workspaceId,
      "space-members-extra",
      spaceId,
      revision,
    ),
    queryFn: () =>
      api.api.get<SpaceMembersExtraDto>(
        `/workspaces/${workspaceId}/spaces/${spaceId}/members-extra`,
        { skipGlobalErrorToast: true },
      ),
    enabled,
    staleTime: 15_000,
    retry: false,
  });
  return query.data ?? null;
}

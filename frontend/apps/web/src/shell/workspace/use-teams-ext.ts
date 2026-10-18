import { useQuery } from "@tanstack/react-query";
import { workspaceQueryKey } from "@notrelix/query";
import type { TeamsExtData } from "@notrelix/features-workspace/web";
import { useAppRuntime } from "@notrelix/runtime-web";

/**
 * What the Teams pages add to the Team producer (gap `CTR-GAP-WS-TEAMS-EXT`,
 * see docs/superpowers/be-blockers-2026-10.md, "BE requirement from FE:
 * Teams"): every Team's people with email and job line, who made and archived
 * it, where it is in use, the workspace admins and the people a Team may take.
 * `revision` changes when a Team is written, to read again.
 */
export function useTeamsExt(
  workspaceId: string,
  revision: string,
): TeamsExtData | null {
  const { api } = useAppRuntime();
  const query = useQuery({
    queryKey: workspaceQueryKey(workspaceId, "teams-ext", revision),
    queryFn: () =>
      api.api.get<TeamsExtData>(`/workspaces/${workspaceId}/teams-ext`, {
        skipGlobalErrorToast: true,
      }),
    staleTime: 10_000,
    retry: false,
  });
  return query.data ?? null;
}

/** Sharing and security extras (gap `CTR-GAP-WS-SECURITY-EXT`). */
export interface SecurityExtDto {
  readonly guestCount: number;
  readonly guestExpiryDays: number | null;
  readonly companyDomain: string;
  readonly linksOffSince: string | null;
}

export function useSecurityExt(
  workspaceId: string,
  revision: string,
): {
  readonly data: SecurityExtDto | null;
  readonly isError: boolean;
  readonly refetch: () => void;
} {
  const { api } = useAppRuntime();
  const query = useQuery({
    queryKey: workspaceQueryKey(workspaceId, "security-ext", revision),
    queryFn: () =>
      api.api.get<SecurityExtDto>(`/workspaces/${workspaceId}/security-ext`, {
        skipGlobalErrorToast: true,
      }),
    staleTime: 10_000,
    retry: false,
  });
  return {
    data: query.data ?? null,
    isError: query.isError,
    refetch: () => void query.refetch(),
  };
}

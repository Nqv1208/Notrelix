import { useQuery } from "@tanstack/react-query";
import { workspaceQueryKey } from "@notrelix/query";
import { useWorkManagementServices } from "../services";

/** The IANA zone the board's days are judged in: its workspace's (Hà Nội until it loads). */
export function useBoardTimeZone(workspaceId: string): string | undefined {
  const { boardSettings } = useWorkManagementServices();
  const query = useQuery({
    queryKey: workspaceQueryKey(workspaceId, "work-management", "locale"),
    queryFn: () => boardSettings.workspaceLocale(workspaceId),
    enabled: Boolean(workspaceId),
    staleTime: 5 * 60_000,
  });
  return query.data?.timeZone;
}

import { useQuery } from "@tanstack/react-query";
import { useWorkManagementServices } from "../services";
import type { BoardTemplate } from "../api/template.api";
import { wmQueryKeys } from "./keys";

/** The workspace's board templates (any status) with the built-in starters first. */
export function useBoardTemplates(workspaceId: string, enabled = true) {
  const { templates } = useWorkManagementServices();
  return useQuery<BoardTemplate[]>({
    queryKey: wmQueryKeys.boardTemplates(workspaceId),
    queryFn: () => templates.list(workspaceId),
    enabled: enabled && Boolean(workspaceId),
    staleTime: 30_000,
  });
}

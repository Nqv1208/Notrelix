import { useCallback } from "react";
import { useQueryClient } from "@tanstack/react-query";
import type { BoardSettingsWriteResult } from "@notrelix/work-management-core";
import { wmQueryKeys } from "../queries/keys";
import { useWorkManagementServices } from "../services";

/** How a failed template write is told to the page. */
function failure(error: unknown): BoardSettingsWriteResult {
  const kind =
    typeof error === "object" && error !== null && "kind" in error
      ? (error as { kind?: unknown }).kind
      : null;
  return {
    ok: false,
    reason: kind === "validation" ? "invalid" : "failed",
    ...(kind === "validation" ? { field: "name" } : {}),
  };
}

/**
 * Saves a board as a template and publishes, archives or deletes templates.
 * Not optimistic: a template is workspace-wide creation input, so the list
 * is read again once the server has answered.
 */
export function useBoardTemplateWrites(workspaceId: string) {
  const queryClient = useQueryClient();
  const { templates } = useWorkManagementServices();

  const write = useCallback(
    async (send: () => Promise<unknown>): Promise<BoardSettingsWriteResult> => {
      try {
        await send();
        await queryClient.invalidateQueries({
          queryKey: wmQueryKeys.boardTemplates(workspaceId),
        });
        return { ok: true };
      } catch (caught) {
        return failure(caught);
      }
    },
    [queryClient, workspaceId],
  );

  return {
    save: (
      boardId: string,
      input: { name: string; description?: string; includeItems: boolean },
    ) => write(() => templates.saveBoard({ boardId, ...input })),
    publish: (templateId: string) => write(() => templates.publish(templateId)),
    archive: (templateId: string) => write(() => templates.archive(templateId)),
    remove: (templateId: string) => write(() => templates.remove(templateId)),
  };
}

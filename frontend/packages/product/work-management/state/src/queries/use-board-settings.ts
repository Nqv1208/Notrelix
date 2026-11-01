import { useCallback } from "react";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { workspaceQueryKey } from "@notrelix/query";
import type {
  BoardSettingsDto,
  BoardSettingsWriteResult,
} from "@notrelix/work-management-core";
import type { BoardSettingsApi } from "../api/board-settings.api";
import { useWorkManagementServices } from "../services";
import { wmQueryKeys } from "./keys";

const settingsKey = (workspaceId: string, boardId: string) =>
  workspaceQueryKey(
    workspaceId,
    "work-management",
    "boards",
    "settings",
    boardId,
  );

/** The `kind` an API error carries (the app error of the client). */
function errorKind(error: unknown): string | null {
  return typeof error === "object" &&
    error !== null &&
    "kind" in error &&
    typeof error.kind === "string"
    ? error.kind
    : null;
}

/** The first field a validation error names. */
function ruleCodeOf(error: unknown): { code?: string } {
  const code = (error as { code?: unknown } | null)?.code;
  return typeof code === "string" && code !== "" ? { code } : {};
}

/** Who saved first, from a 412 the server answered with the last writer. */
export function conflictOf(error: unknown): {
  byName: string | null;
  at: string | null;
} {
  const body = (error as { details?: unknown } | null)?.details as
    Record<string, unknown> | null | undefined;
  return {
    byName: typeof body?.changedByName === "string" ? body.changedByName : null,
    at: typeof body?.changedAt === "string" ? body.changedAt : null,
  };
}

function invalidField(error: unknown): string | undefined {
  if (typeof error !== "object" || error === null) return undefined;
  const errors = (error as { validationErrors?: Record<string, string[]> })
    .validationErrors;
  return errors ? Object.keys(errors)[0] : undefined;
}

/**
 * The settings of a board (read from the board itself) and the writes on
 * them. `forbidden` is true when the caller is not a manager of the
 * board (the server answers 403). A write sends the versions of the read it
 * was made from and then refetches the settings and the board itself, since
 * fields, options and groups are the board's own data.
 */
export function useBoardSettings(boardId: string, workspaceId: string) {
  const { boardSettings } = useWorkManagementServices();
  const queryClient = useQueryClient();
  const query = useQuery({
    queryKey: settingsKey(workspaceId, boardId),
    queryFn: () => boardSettings.get(boardId),
    enabled: Boolean(boardId && workspaceId),
    staleTime: 0,
  });
  const error = query.error;
  const forbidden = errorKind(error) === "forbidden";
  const notFound = errorKind(error) === "not_found";

  const refresh = useCallback(async () => {
    await queryClient.invalidateQueries({
      queryKey: settingsKey(workspaceId, boardId),
    });
    void queryClient.invalidateQueries({
      queryKey: wmQueryKeys.fullBoard(workspaceId, boardId),
    });
    void queryClient.invalidateQueries({
      queryKey: wmQueryKeys.view(workspaceId, boardId),
    });
    void queryClient.invalidateQueries({
      queryKey: wmQueryKeys.columns(workspaceId, boardId),
    });
    void queryClient.invalidateQueries({
      queryKey: wmQueryKeys.groups(workspaceId, boardId),
    });
    void queryClient.invalidateQueries({
      queryKey: wmQueryKeys.workspaceList(workspaceId),
    });
    void queryClient.invalidateQueries({
      queryKey: wmQueryKeys.list(workspaceId),
    });
  }, [boardId, queryClient, workspaceId]);

  /** Runs a write with the current read, which carries the versions to send. */
  const write = useCallback(
    async (
      send: (
        api: BoardSettingsApi,
        current: BoardSettingsDto,
      ) => Promise<unknown>,
    ): Promise<BoardSettingsWriteResult> => {
      const current = queryClient.getQueryData<BoardSettingsDto>(
        settingsKey(workspaceId, boardId),
      );
      if (!current) return { ok: false, reason: "failed" };
      try {
        await send(boardSettings, current);
        await refresh();
        return { ok: true };
      } catch (caught) {
        if (errorKind(caught) === "conflict") {
          return {
            ok: false,
            reason: "conflict",
            conflict: conflictOf(caught),
          };
        }
        if (errorKind(caught) === "validation") {
          return {
            ok: false,
            reason: "invalid",
            field: invalidField(caught),
            ...ruleCodeOf(caught),
          };
        }
        return { ok: false, reason: "failed" };
      }
    },
    [boardId, boardSettings, queryClient, refresh, workspaceId],
  );

  /** The settings as saved now, read without replacing the ones the page shows. */
  const readLatest = useCallback(
    () => boardSettings.get(boardId).catch(() => null),
    [boardId, boardSettings],
  );

  return {
    readLatest,
    settings: query.data ?? null,
    isLoading: query.isLoading,
    isError: query.isError && !forbidden && !notFound,
    forbidden,
    notFound,
    refetch: refresh,
    write,
  };
}

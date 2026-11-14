import { useCallback } from "react";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { workspaceQueryKey } from "@notrelix/query";
import type {
  BoardSettingsDto,
  BoardSettingsWriteResult,
  FormulaPreviewDraft,
  FormulaRefusalDetail,
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
/**
 * The stable rule that refused the write (`ruleCode`, e.g. WorkManagement_Field_ComputedFieldCycle). `code` on the
 * error is only the category ("business_rule_violation"), so the rule is read from `ruleCode` first.
 */
function ruleCodeOf(error: unknown): { code?: string } {
  const rule = (error as { ruleCode?: unknown } | null)?.ruleCode;
  const code = (error as { code?: unknown } | null)?.code;
  const named = typeof rule === "string" && rule !== "" ? rule : code;
  return typeof named === "string" && named !== "" ? { code: named } : {};
}

/** What a formula refusal names, from the problem's extensions (the loop, the unknown field, the syntax position). */
function refusalDetailOf(error: unknown): { detail?: FormulaRefusalDetail } {
  const body = (error as { details?: unknown } | null)?.details as
    Record<string, unknown> | null | undefined;
  if (!body) return {};
  const detail: FormulaRefusalDetail = {};
  if (
    Array.isArray(body.cycle) &&
    body.cycle.every((name) => typeof name === "string")
  )
    detail.cycle = body.cycle as string[];
  if (typeof body.field === "string") detail.field = body.field;
  if (typeof body.position === "number") detail.position = body.position;
  if (typeof body.missing === "string") detail.missing = body.missing;
  return Object.keys(detail).length > 0 ? { detail } : {};
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

/** The published board limit a 409 named (D78.8): `work-management.limit.*` with limit and max. */
export function limitOf(
  error: unknown,
): { name: string; max: number | null } | null {
  const code = (error as { code?: unknown } | null)?.code;
  if (typeof code !== "string" || !code.startsWith("work-management.limit."))
    return null;
  const body = (error as { details?: unknown } | null)?.details as
    Record<string, unknown> | null | undefined;
  return {
    name: typeof body?.limit === "string" ? body.limit : "other",
    max: typeof body?.max === "number" ? body.max : null,
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
        // A limit is a 409 too, but nothing changed under the person: say the limit, do not reload.
        const limit = limitOf(caught);
        if (limit) {
          return { ok: false, reason: "limit", limit, ...ruleCodeOf(caught) };
        }
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
            ...refusalDetailOf(caught),
          };
        }
        // What was written to is gone (someone else deleted it): read the settings again so the
        // page shows that, instead of offering to retry a write that cannot land.
        if (errorKind(caught) === "not_found") void refresh();
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

  /** What a draft formula shows on `itemId`; null when the server could not be asked. */
  const previewFormula = useCallback(
    (itemId: string, draft: FormulaPreviewDraft) =>
      boardSettings.previewFormula(boardId, itemId, draft).catch(() => null),
    [boardId, boardSettings],
  );

  return {
    readLatest,
    previewFormula,
    settings: query.data ?? null,
    isLoading: query.isLoading,
    isError: query.isError && !forbidden && !notFound,
    forbidden,
    notFound,
    refetch: refresh,
    write,
  };
}

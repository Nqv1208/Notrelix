import { useCallback } from "react";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { workspaceQueryKey } from "@notrelix/query";
import type {
  AutomationRuleDetailDto,
  AutomationRuleDocumentDto,
  AutomationRunsDto,
  AutomationWriteResult,
} from "@notrelix/work-management-core";
import { useWorkManagementServices } from "../services";

/**
 * Under the key of the board's settings, so any settings write (a field
 * restored, a rule turned on) also refreshes the rules and the run log.
 */
const automationsKey = (
  workspaceId: string,
  boardId: string,
  ...rest: string[]
) =>
  workspaceQueryKey(
    workspaceId,
    "work-management",
    "boards",
    "settings",
    boardId,
    "automations",
    ...rest,
  );

function errorKind(error: unknown): string | null {
  return typeof error === "object" &&
    error !== null &&
    "kind" in error &&
    typeof error.kind === "string"
    ? error.kind
    : null;
}

/**
 * One automation rule of a board and its writes (gap
 * `CTR-GAP-WM-BOARD-AUTOMATIONS`). A write sends the revision of the read it
 * was made from; the rule, the run log and the settings list are read again
 * after it.
 */
export function useBoardAutomationRule(
  boardId: string,
  workspaceId: string,
  ruleId: string | null,
) {
  const { boardAutomations } = useWorkManagementServices();
  const queryClient = useQueryClient();
  const query = useQuery({
    queryKey: automationsKey(workspaceId, boardId, "rule", ruleId ?? "-"),
    queryFn: () => boardAutomations.get(boardId, ruleId!),
    enabled: Boolean(boardId && workspaceId && ruleId),
    staleTime: 0,
  });

  const refresh = useCallback(async () => {
    await queryClient.invalidateQueries({
      queryKey: workspaceQueryKey(
        workspaceId,
        "work-management",
        "boards",
        "settings",
        boardId,
      ),
    });
  }, [boardId, queryClient, workspaceId]);

  const send = useCallback(
    async (run: () => Promise<unknown>): Promise<AutomationWriteResult> => {
      try {
        await run();
        await refresh();
        return { ok: true };
      } catch (caught) {
        const kind = errorKind(caught);
        if (kind === "conflict") return { ok: false, reason: "conflict" };
        if (kind === "validation") return { ok: false, reason: "invalid" };
        return { ok: false, reason: "failed" };
      }
    },
    [refresh],
  );

  const create = useCallback(async (): Promise<AutomationWriteResult> => {
    try {
      const made = await boardAutomations.create(boardId);
      await refresh();
      return { ok: true, id: made.id };
    } catch {
      return { ok: false, reason: "failed" };
    }
  }, [boardAutomations, boardId, refresh]);

  const save = useCallback(
    (
      definition: AutomationRuleDocumentDto,
      mode: "draft" | "enable" | "version",
    ) =>
      send(() =>
        boardAutomations.save(
          boardId,
          ruleId!,
          query.data?.revision ?? 0,
          definition,
          mode,
        ),
      ),
    [boardAutomations, boardId, query.data?.revision, ruleId, send],
  );

  const changeRunner = useCallback(
    (personId: string) =>
      send(() => boardAutomations.changeRunner(boardId, ruleId!, personId)),
    [boardAutomations, boardId, ruleId, send],
  );

  return {
    rule: (query.data ?? null) as AutomationRuleDetailDto | null,
    isLoading: query.isLoading && Boolean(ruleId),
    isError: query.isError && errorKind(query.error) !== "not_found",
    notFound: errorKind(query.error) === "not_found",
    refetch: refresh,
    create,
    save,
    changeRunner,
  };
}

/** The run log of a board's automations and the run again of a failed step. */
export function useBoardAutomationRuns(
  boardId: string,
  workspaceId: string,
  enabled: boolean,
) {
  const { boardAutomations } = useWorkManagementServices();
  const queryClient = useQueryClient();
  const query = useQuery({
    queryKey: automationsKey(workspaceId, boardId, "runs"),
    queryFn: () => boardAutomations.runs(boardId),
    enabled: Boolean(boardId && workspaceId && enabled),
    staleTime: 0,
  });

  const refetch = useCallback(
    () =>
      queryClient.invalidateQueries({
        queryKey: automationsKey(workspaceId, boardId, "runs"),
      }),
    [boardId, queryClient, workspaceId],
  );

  const rerunStep = useCallback(
    async (
      runId: string,
      stepIndex: number,
    ): Promise<AutomationWriteResult> => {
      try {
        await boardAutomations.rerunStep(boardId, runId, stepIndex);
        await refetch();
        return { ok: true };
      } catch (caught) {
        return {
          ok: false,
          reason: errorKind(caught) === "validation" ? "invalid" : "failed",
        };
      }
    },
    [boardAutomations, boardId, refetch],
  );

  return {
    runs: (query.data ?? null) as AutomationRunsDto | null,
    isLoading: query.isLoading && enabled,
    isError: query.isError,
    refetch,
    rerunStep,
  };
}

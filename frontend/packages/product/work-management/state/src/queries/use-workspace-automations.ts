import { useCallback, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { workspaceQueryKey } from "@notrelix/query";
import type {
  CreateWorkspaceAutomationInput,
  WorkspaceAutomationDefinition,
  WorkspaceAutomationWriteResult,
} from "@notrelix/work-management-core";
import { useWorkManagementServices } from "../services";

const rulesKey = (workspaceId: string, boardId: string) =>
  workspaceQueryKey(workspaceId, "automation", "rules", boardId);
const runsKey = (workspaceId: string, ruleId: string) =>
  workspaceQueryKey(workspaceId, "automation", "runs", ruleId);

function errorKind(error: unknown): string | null {
  return typeof error === "object" &&
    error !== null &&
    "kind" in error &&
    typeof error.kind === "string"
    ? error.kind
    : null;
}

function failure(error: unknown): WorkspaceAutomationWriteResult {
  const kind = errorKind(error);
  if (kind === "forbidden") return { ok: false, reason: "forbidden" };
  if (kind === "validation") {
    // The one validation the page can word: the plan's rule limit.
    const text = String((error as { message?: unknown }).message ?? "");
    return { ok: false, reason: /limit/i.test(text) ? "limit" : "invalid" };
  }
  return { ok: false, reason: "failed" };
}

/**
 * The rules of the workspace, their writes and the runs of the rule that is
 * open. Only a person who manages the workspace reads them (`forbidden`).
 */
export function useWorkspaceAutomations(
  workspaceId: string,
  boardId: string,
  allowed = true,
) {
  const { workspaceAutomations } = useWorkManagementServices();
  const queryClient = useQueryClient();
  const [openRuleId, setOpenRuleId] = useState<string | null>(null);

  const rules = useQuery({
    queryKey: rulesKey(workspaceId, boardId),
    queryFn: () => workspaceAutomations.list(workspaceId, boardId),
    enabled: Boolean(workspaceId && boardId && allowed),
    staleTime: 0,
    retry: false,
  });
  const runs = useQuery({
    queryKey: runsKey(workspaceId, openRuleId ?? "-"),
    queryFn: () => workspaceAutomations.runs(boardId, openRuleId!),
    enabled: Boolean(workspaceId && openRuleId),
    staleTime: 0,
    retry: false,
  });

  const refresh = useCallback(
    () =>
      queryClient.invalidateQueries({
        queryKey: workspaceQueryKey(workspaceId, "automation"),
      }),
    [queryClient, workspaceId],
  );

  const createMutation = useMutation({
    mutationFn: (input: CreateWorkspaceAutomationInput) =>
      workspaceAutomations.create(workspaceId, input),
  });
  const enabledMutation = useMutation({
    mutationFn: (input: { ruleId: string; enabled: boolean }) =>
      workspaceAutomations.setEnabled(
        workspaceId,
        boardId,
        input.ruleId,
        input.enabled,
      ),
  });
  const updateMutation = useMutation({
    mutationFn: (input: {
      ruleId: string;
      definition: WorkspaceAutomationDefinition;
    }) =>
      workspaceAutomations.update(
        workspaceId,
        boardId,
        input.ruleId,
        input.definition,
      ),
  });
  const removeMutation = useMutation({
    mutationFn: (ruleId: string) =>
      workspaceAutomations.remove(workspaceId, boardId, ruleId),
  });

  const create = useCallback(
    async (
      input: CreateWorkspaceAutomationInput,
    ): Promise<WorkspaceAutomationWriteResult> => {
      try {
        await createMutation.mutateAsync(input);
        await refresh();
        return { ok: true };
      } catch (error) {
        return failure(error);
      }
    },
    [createMutation, refresh],
  );
  const setEnabled = useCallback(
    async (
      ruleId: string,
      enabled: boolean,
    ): Promise<WorkspaceAutomationWriteResult> => {
      try {
        await enabledMutation.mutateAsync({ ruleId, enabled });
        await refresh();
        return { ok: true };
      } catch (error) {
        return failure(error);
      }
    },
    [enabledMutation, refresh],
  );

  const update = useCallback(
    async (
      ruleId: string,
      definition: WorkspaceAutomationDefinition,
    ): Promise<WorkspaceAutomationWriteResult> => {
      try {
        await updateMutation.mutateAsync({ ruleId, definition });
        await refresh();
        return { ok: true };
      } catch (error) {
        return failure(error);
      }
    },
    [updateMutation, refresh],
  );
  const remove = useCallback(
    async (ruleId: string): Promise<WorkspaceAutomationWriteResult> => {
      try {
        await removeMutation.mutateAsync(ruleId);
        setOpenRuleId((open) => (open === ruleId ? null : open));
        await refresh();
        return { ok: true };
      } catch (error) {
        return failure(error);
      }
    },
    [removeMutation, refresh],
  );

  const forbidden = errorKind(rules.error) === "forbidden";
  return {
    rules: rules.data ?? [],
    isLoading: rules.isLoading,
    isError: rules.isError && !forbidden,
    forbidden,
    refetch: refresh,
    create,
    update,
    remove,
    setEnabled,
    openRuleId,
    openRuns: setOpenRuleId,
    runs: runs.data ?? [],
    runsLoading: runs.isLoading && Boolean(openRuleId),
    runsError: runs.isError,
  };
}

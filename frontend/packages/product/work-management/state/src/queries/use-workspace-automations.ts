import { useCallback, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { workspaceQueryKey } from "@notrelix/query";
import type {
  CreateWorkspaceAutomationInput,
  WorkspaceAutomationWriteResult,
} from "@notrelix/work-management-core";
import { useWorkManagementServices } from "../services";

const rulesKey = (workspaceId: string) =>
  workspaceQueryKey(workspaceId, "automation", "rules");
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
export function useWorkspaceAutomations(workspaceId: string) {
  const { workspaceAutomations } = useWorkManagementServices();
  const queryClient = useQueryClient();
  const [openRuleId, setOpenRuleId] = useState<string | null>(null);

  const rules = useQuery({
    queryKey: rulesKey(workspaceId),
    queryFn: () => workspaceAutomations.list(workspaceId),
    enabled: Boolean(workspaceId),
    staleTime: 0,
    retry: false,
  });
  const runs = useQuery({
    queryKey: runsKey(workspaceId, openRuleId ?? "-"),
    queryFn: () => workspaceAutomations.runs(openRuleId!),
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
      workspaceAutomations.setEnabled(workspaceId, input.ruleId, input.enabled),
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

  const forbidden = errorKind(rules.error) === "forbidden";
  return {
    rules: rules.data ?? [],
    isLoading: rules.isLoading,
    isError: rules.isError && !forbidden,
    forbidden,
    refetch: refresh,
    create,
    setEnabled,
    openRuleId,
    openRuns: setOpenRuleId,
    runs: runs.data ?? [],
    runsLoading: runs.isLoading && Boolean(openRuleId),
    runsError: runs.isError,
  };
}

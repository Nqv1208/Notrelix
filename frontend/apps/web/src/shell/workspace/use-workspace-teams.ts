import { useMemo } from "react";
import { toast } from "sonner";
import {
  createUseTeam,
  createUseTeamMembers,
  createUseTeamMutations,
  createUseTeams,
  type WorkspaceTeamsCopy,
} from "@notrelix/features-workspace/web";
import {
  describeWorkspaceFailure,
  type WorkspaceFailure,
} from "@notrelix/features-workspace/core";
import { useAppRuntime } from "@notrelix/runtime-web";

/** The Team producer hooks (Workspaces.Teams.*), built once per runtime. */
export function useTeamHooks() {
  const { api: runtimeClient } = useAppRuntime();
  return useMemo(() => {
    const deps = { api: runtimeClient.api, endpoints: runtimeClient.endpoints };
    return {
      useTeams: createUseTeams(deps),
      useTeam: createUseTeam(deps),
      useTeamMembers: createUseTeamMembers(deps),
      useTeamMutations: createUseTeamMutations(deps),
    };
  }, [runtimeClient]);
}

/** Recovery message for a failed Team write (DESIGN.md §34). */
export function teamRecoveryHint(copy: WorkspaceTeamsCopy) {
  return (failure: WorkspaceFailure): string => {
    switch (failure.kind) {
      case "conflict":
        return copy.errors.conflict;
      case "forbidden":
        return copy.errors.forbidden;
      case "not-found":
        return copy.errors.notFound;
      case "network":
        return copy.errors.network;
      default:
        return copy.errors.unknown;
    }
  };
}

/**
 * Reports a failed Team write in one toast: the producer's own rule or
 * validation message when it sent one, otherwise the recovery message for
 * the failure kind (a 409/412 is a conflict, a 403 is forbidden). The
 * mutation hooks already refresh the Team after every write, so a conflict
 * shows the current version.
 */
export function toastTeamFailure(
  error: unknown,
  copy: WorkspaceTeamsCopy,
): WorkspaceFailure {
  const failure = describeWorkspaceFailure(error);
  const producerMessage =
    failure.kind === "rule" || failure.kind === "validation"
      ? (failure.detail ?? Object.values(failure.fieldErrors).flat()[0] ?? null)
      : null;
  toast.error(producerMessage ?? teamRecoveryHint(copy)(failure));
  return failure;
}

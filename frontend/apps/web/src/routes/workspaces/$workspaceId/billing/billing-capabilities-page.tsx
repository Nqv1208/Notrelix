import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  BillingCapabilitiesSurface,
  WorkspaceSetupFrame,
  useFeatureAvailabilityCopy,
  useWorkspaceTeamsCopy,
  type BillingCapabilitiesState,
} from "@notrelix/features-workspace/web";
import { useWorkspaceContext } from "@/providers/workspace-provider";
import { useSetupFrame } from "@/shell/workspace/use-setup-frame";
import { billingKeys, useBillingApi } from "./use-billing";

function isForbidden(error: unknown): boolean {
  return (
    typeof error === "object" &&
    error !== null &&
    "kind" in error &&
    error.kind === "forbidden"
  );
}

/**
 * Plans and billing on a backend that only serves what the workspace's billing
 * grants: the capabilities with their limit and use. Everything else on the
 * page is said to be not available yet.
 */
export function BillingCapabilitiesPage() {
  const { workspaceId } = useWorkspaceContext();
  const copy = useFeatureAvailabilityCopy();
  const teamsCopy = useWorkspaceTeamsCopy();
  const frame = useSetupFrame("billing");
  const api = useBillingApi();
  const queryClient = useQueryClient();

  const capabilities = useQuery({
    queryKey: billingKeys.capabilities(workspaceId),
    queryFn: () => api.capabilities(workspaceId),
    retry: false,
  });
  const plan = useQuery({
    queryKey: billingKeys.plan(workspaceId),
    queryFn: () => api.plan(workspaceId),
    retry: false,
  });
  const changePlan = useMutation({
    mutationFn: (code: string) => api.changePlan(workspaceId, code),
    onSuccess: () =>
      queryClient.invalidateQueries({ queryKey: billingKeys.all(workspaceId) }),
  });
  const state: BillingCapabilitiesState = capabilities.data
    ? { status: "ready", data: capabilities.data }
    : capabilities.isError
      ? isForbidden(capabilities.error)
        ? { status: "forbidden" }
        : { status: "error" }
      : { status: "loading" };

  return (
    <WorkspaceSetupFrame
      copy={teamsCopy}
      frame={frame}
      hideNav={state.status === "forbidden"}
    >
      <BillingCapabilitiesSurface
        copy={copy}
        state={state}
        onRetry={() => void capabilities.refetch()}
        plan={
          plan.data
            ? {
                currentCode: plan.data.currentCode,
                plans: plan.data.plans,
                canChange: plan.data.canChange,
                changing: changePlan.isPending,
                failed: changePlan.isError,
              }
            : undefined
        }
        onChangePlan={(code) => changePlan.mutate(code)}
      />
    </WorkspaceSetupFrame>
  );
}

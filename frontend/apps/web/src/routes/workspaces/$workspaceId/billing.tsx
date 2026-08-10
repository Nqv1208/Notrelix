import { useParams } from "@tanstack/react-router";
import { useWorkspaceContext } from "@/providers/workspace-provider";
import {
  BillingPage as BillingPageComponent,
  type BillingPlanTier,
} from "@notrelix/features-billing";

export function BillingPage() {
  const { workspaceId } = useParams({ from: "/workspaces/$workspaceId" });
  const { workspace } = useWorkspaceContext();
  const currentPlan = (workspace?.plan as BillingPlanTier) ?? "free";

  return (
    <BillingPageComponent workspaceId={workspaceId} currentPlan={currentPlan} />
  );
}

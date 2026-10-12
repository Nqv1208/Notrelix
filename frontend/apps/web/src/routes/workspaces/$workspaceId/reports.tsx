import { BarChart3 } from "lucide-react";
import { useWorkspaceShellCopy } from "@notrelix/features-workspace/web";
import { WorkspaceSectionPlaceholder } from "@/shell/workspace/workspace-section-placeholder";

/**
 * Reports (Final-Space sidebar "Báo cáo"). A real destination with an
 * honest empty state; dashboards arrive with the Reports phase.
 */
export function ReportsPage() {
  const copy = useWorkspaceShellCopy();
  return (
    <WorkspaceSectionPlaceholder
      icon={BarChart3}
      title={copy.pages.reportsTitle}
      description={copy.pages.reportsDescription}
      emptyTitle={copy.pages.reportsEmptyTitle}
      emptyDescription={copy.pages.reportsEmptyDescription}
    />
  );
}

import { UserRoundCheck } from "lucide-react";
import { useWorkspaceShellCopy } from "@notrelix/features-workspace/web";
import { WorkspaceSectionPlaceholder } from "@/shell/workspace/workspace-section-placeholder";

/**
 * My work (WX-SHELL-001). A real destination with an honest empty state:
 * the assigned-items list is delivered by WX-HOME-001 on the My Work read.
 */
export function MyWorkPage() {
  const copy = useWorkspaceShellCopy();
  return (
    <WorkspaceSectionPlaceholder
      icon={UserRoundCheck}
      title={copy.pages.myWorkTitle}
      description={copy.pages.myWorkDescription}
      emptyTitle={copy.pages.myWorkEmptyTitle}
      emptyDescription={copy.pages.myWorkEmptyDescription}
    />
  );
}

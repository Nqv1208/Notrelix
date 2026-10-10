import { Inbox } from "lucide-react";
import { useWorkspaceShellCopy } from "@notrelix/features-workspace/web";
import { WorkspaceSectionPlaceholder } from "@/shell/workspace/workspace-section-placeholder";

/**
 * Inbox (WX-SHELL-001). A real destination with an honest empty state; no
 * inbox producer exists yet, and notifications stay in the topbar bell.
 */
export function InboxPage() {
  const copy = useWorkspaceShellCopy();
  return (
    <WorkspaceSectionPlaceholder
      icon={Inbox}
      title={copy.pages.inboxTitle}
      description={copy.pages.inboxDescription}
      emptyTitle={copy.pages.inboxEmptyTitle}
      emptyDescription={copy.pages.inboxEmptyDescription}
    />
  );
}

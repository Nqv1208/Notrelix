import { Outlet, useParams } from "@tanstack/react-router";
import { AccountSidebar } from "@/shell/account-sidebar";

export function AccountLayout() {
  const { workspaceId } = useParams({
    from: "/workspaces/$workspaceId/account",
  });

  return (
    // No inner scroll container: the workspace shell's <main> scrolls.
    <div className="flex min-h-full">
      <AccountSidebar workspaceId={workspaceId} />
      <div className="flex-1 min-w-0">
        <Outlet />
      </div>
    </div>
  );
}

import { Outlet, useParams } from '@tanstack/react-router';
import { AccountSidebar } from '@/shell/account-sidebar';

export function AccountLayout() {
  const { workspaceId } = useParams({ from: '/workspaces/$workspaceId/account' });

  return (
    <div className="flex h-full">
      <AccountSidebar workspaceId={workspaceId} />
      <div className="flex-1 min-w-0 overflow-y-auto">
        <Outlet />
      </div>
    </div>
  );
}

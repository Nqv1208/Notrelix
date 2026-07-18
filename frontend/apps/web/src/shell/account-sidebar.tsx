import { Link, useLocation } from '@tanstack/react-router';
import { ArrowLeft, Eye, Bell, Shield, User } from 'lucide-react';
import { cn } from '@notrelix/ui-web';

const navItems = [
  { label: 'Profile', icon: User, to: '/workspaces/$workspaceId/account/profile' as const },
  { label: 'Appearance', icon: Eye, to: '/workspaces/$workspaceId/account/appearance' as const },
  { label: 'Security', icon: Shield, to: '/workspaces/$workspaceId/account/security' as const },
  { label: 'Notifications', icon: Bell, to: '/workspaces/$workspaceId/account/notifications' as const },
];

export function AccountSidebar({ workspaceId }: { workspaceId: string }) {
  const location = useLocation();

  return (
    <aside className="hidden w-56 shrink-0 border-r border-border bg-card md:block">
      <div className="flex h-full flex-col p-4">
        <Link
          to="/workspaces/$workspaceId"
          params={{ workspaceId }}
          className="mb-4 flex items-center gap-2 text-sm text-muted-foreground hover:text-foreground transition-colors"
        >
          <ArrowLeft className="size-4" />
          Back to workspace
        </Link>
        <nav className="flex flex-col gap-1">
          {navItems.map((item) => {
            const isActive = location.pathname.includes(item.to.split('/').pop() ?? '');
            return (
              <Link
                key={item.to}
                to={item.to}
                params={{ workspaceId }}
                className={cn(
                  'flex items-center gap-2 rounded-lg px-3 py-2 text-sm font-medium transition-colors',
                  isActive
                    ? 'bg-primary/10 text-primary'
                    : 'text-muted-foreground hover:bg-muted hover:text-foreground',
                )}
              >
                <item.icon className="size-4" />
                {item.label}
              </Link>
            );
          })}
        </nav>
      </div>
    </aside>
  );
}

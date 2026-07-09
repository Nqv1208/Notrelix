import { useWorkspaceContext } from '../../providers/workspace-provider';
import { useTheme } from '../../providers/app-providers';
import { createNotificationBell } from '@notrelix/features-notifications';
import { api, endpoints } from '@notrelix/contracts';
import { Button } from '@notrelix/ui-web';
import { Search, Sun, Moon, ChevronRight } from 'lucide-react';
import { useLocation } from '@tanstack/react-router';

const NotificationBell = createNotificationBell({ api, endpoints });

export function WorkspaceTopbar() {
  const { workspace } = useWorkspaceContext();
  const { theme, setTheme } = useTheme();
  const location = useLocation();

  // Simple breadcrumb builder based on path
  const pathParts = location.pathname.split('/').filter(Boolean);
  const isBoard = pathParts.includes('boards');
  const isDoc = pathParts.includes('docs');
  const isDashboard = pathParts.includes('dashboard');
  const isSettings = pathParts.includes('settings');
  const isMembers = pathParts.includes('members');

  let currentPage = 'Home';
  if (isBoard) currentPage = 'Board';
  else if (isDoc) currentPage = 'Document';
  else if (isDashboard) currentPage = 'Dashboard';
  else if (isSettings) currentPage = 'Settings';
  else if (isMembers) currentPage = 'Members';

  return (
    <header className="h-14 border-b bg-card text-card-foreground px-6 flex items-center justify-between">
      {/* Breadcrumbs */}
      <div className="flex items-center gap-1.5 text-sm text-muted-foreground">
        <span className="font-semibold text-foreground truncate max-w-[120px]">
          {workspace?.name || 'Workspace'}
        </span>
        <ChevronRight className="h-3.5 w-3.5" />
        <span className="font-medium text-foreground/80 truncate">
          {currentPage}
        </span>
      </div>

      {/* Global Actions */}
      <div className="flex items-center gap-3">
        {/* Search Input Trigger */}
        <Button
          variant="outline"
          size="sm"
          className="h-9 w-60 justify-start text-muted-foreground gap-2 hidden md:flex font-normal"
        >
          <Search className="h-4 w-4" />
          <span className="text-xs">Search pages, boards...</span>
        </Button>

        {/* Theme Toggle */}
        <Button
          variant="ghost"
          size="icon"
          className="h-9 w-9 text-muted-foreground"
          onClick={() => setTheme(theme === 'dark' ? 'light' : 'dark')}
          title="Toggle theme"
        >
          {theme === 'dark' ? <Sun className="h-4 w-4" /> : <Moon className="h-4 w-4" />}
        </Button>

        {/* Notification Bell */}
        <NotificationBell />
      </div>
    </header>
  );
}

import { useState, useEffect } from 'react';
import { useWorkspaceContext } from '../../providers/workspace-provider';
import { useTheme } from '../../providers/app-providers';
import { createNotificationBell } from '@notrelix/features-notifications';
import { api, endpoints } from '@notrelix/contracts';
import { Button } from '@notrelix/ui-web';
import { Search, Sun, Moon, ChevronRight } from 'lucide-react';
import { useLocation } from '@tanstack/react-router';
import { GlobalSearch } from '../global-search';
import { env } from '@/config/env';

const NotificationBell = createNotificationBell({
  api,
  endpoints,
  options: { mockMode: env.mockApi },
});

export function WorkspaceTopbar() {
  const { workspace } = useWorkspaceContext();
  const { theme, setTheme } = useTheme();
  const location = useLocation();
  const [searchOpen, setSearchOpen] = useState(false);

  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      if ((e.metaKey || e.ctrlKey) && e.key === 'k') {
        e.preventDefault();
        setSearchOpen(true);
      }
    };
    window.addEventListener('keydown', handleKeyDown);
    return () => window.removeEventListener('keydown', handleKeyDown);
  }, []);

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
    <>
      <header className="h-14 border-b bg-card text-card-foreground px-6 flex items-center justify-between">
        <div className="flex items-center gap-1.5 text-sm text-muted-foreground">
          <span className="font-semibold text-foreground truncate max-w-[120px]">
            {workspace?.name || 'Workspace'}
          </span>
          <ChevronRight className="h-3.5 w-3.5" />
          <span className="font-medium text-foreground/80 truncate">
            {currentPage}
          </span>
        </div>

        <div className="flex items-center gap-3">
          <Button
            variant="outline"
            size="sm"
            className="h-9 w-60 justify-start text-muted-foreground gap-2 hidden md:flex font-normal"
            onClick={() => setSearchOpen(true)}
          >
            <Search className="h-4 w-4" />
            <span className="text-xs">Search pages, boards...</span>
            <kbd className="ml-auto text-[10px] bg-muted px-1.5 py-0.5 rounded font-mono">⌘K</kbd>
          </Button>

          <Button
            variant="ghost"
            size="icon"
            className="h-9 w-9 text-muted-foreground"
            onClick={() => setTheme(theme === 'dark' ? 'light' : 'dark')}
            title="Toggle theme"
          >
            {theme === 'dark' ? <Sun className="h-4 w-4" /> : <Moon className="h-4 w-4" />}
          </Button>

          <NotificationBell />
        </div>
      </header>

      <GlobalSearch open={searchOpen} onClose={() => setSearchOpen(false)} />
    </>
  );
}

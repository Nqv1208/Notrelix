import { Link, useParams } from '@tanstack/react-router';
import { useNavigate } from '@notrelix/platform/navigation';
import { useWorkspaceContext } from '../../providers/workspace-provider';
import { WorkspaceSwitcher } from './workspace-switcher';
import { useWorkspaceBoards } from '@notrelix/work-management-state';
import { useCurrentUser, createUseLogout } from '@notrelix/features-auth';
import { createUsePageList, createUseCreatePage } from '@notrelix/docs-core';
import { api, endpoints } from '@notrelix/contracts';
import { Avatar, AvatarFallback, AvatarImage, Button } from '@notrelix/ui-web';
import {
  LayoutDashboard,
  Users,
  Settings,
  LogOut,
  LayoutGrid,
  FileText,
  Plus,
  BookOpen,
} from 'lucide-react';

const useLogout = createUseLogout({ api, endpoints });
const usePageList = createUsePageList(api, endpoints);
const useCreatePage = createUseCreatePage(api, endpoints);

export function WorkspaceSidebar() {
  const navigate = useNavigate();
  const { workspaceId, workspace } = useWorkspaceContext();
  const { data: boards = [], isLoading: boardsLoading } = useWorkspaceBoards(workspaceId);
  const { data: pages = [], isLoading: pagesLoading } = usePageList(workspaceId);
  const createPageMutation = useCreatePage(workspaceId);
  const currentUser = useCurrentUser();
  const logoutMutation = useLogout();

  const handleLogout = () => {
    logoutMutation.mutate();
  };

  return (
    <aside className="w-64 border-r bg-card flex flex-col h-screen text-card-foreground">
      {/* Workspace Switcher Header */}
      <div className="p-3 border-b">
        <WorkspaceSwitcher />
      </div>

      {/* Main Navigation */}
      <div className="flex-1 overflow-y-auto p-3 space-y-6">
        {/* Workspace Operations */}
        <div className="space-y-1">
          <Link
            to="/workspaces/$workspaceId/dashboard"
            params={{ workspaceId }}
            activeProps={{ className: 'bg-accent text-accent-foreground font-medium' }}
            className="flex items-center gap-3 px-3 py-2 text-sm rounded-md hover:bg-accent/50 transition-colors"
          >
            <LayoutDashboard className="h-4 w-4 text-muted-foreground" />
            <span>Dashboard</span>
          </Link>
          <Link
            to="/workspaces/$workspaceId/members"
            params={{ workspaceId }}
            activeProps={{ className: 'bg-accent text-accent-foreground font-medium' }}
            className="flex items-center gap-3 px-3 py-2 text-sm rounded-md hover:bg-accent/50 transition-colors"
          >
            <Users className="h-4 w-4 text-muted-foreground" />
            <span>Members</span>
          </Link>
          <Link
            to="/workspaces/$workspaceId/settings"
            params={{ workspaceId }}
            activeProps={{ className: 'bg-accent text-accent-foreground font-medium' }}
            className="flex items-center gap-3 px-3 py-2 text-sm rounded-md hover:bg-accent/50 transition-colors"
          >
            <Settings className="h-4 w-4 text-muted-foreground" />
            <span>Settings</span>
          </Link>
        </div>

        {/* Boards Section */}
        <div className="space-y-2">
          <div className="flex items-center justify-between px-3 text-xs font-semibold tracking-wider text-muted-foreground uppercase">
            <span>Boards</span>
            <Button variant="ghost" size="icon" className="h-4 w-4 hover:bg-accent">
              <Plus className="h-3 w-3" />
            </Button>
          </div>
          <div className="space-y-0.5">
            {boardsLoading ? (
              <div className="px-3 py-2 text-xs text-muted-foreground">Loading boards...</div>
            ) : boards.length === 0 ? (
              <div className="px-3 py-2 text-xs text-muted-foreground italic">No boards found</div>
            ) : (
              boards.map((board) => (
                <Link
                  key={board.id}
                  to="/workspaces/$workspaceId/boards/$boardId"
                  params={{ workspaceId, boardId: board.id }}
                  activeProps={{ className: 'bg-accent text-accent-foreground font-medium' }}
                  className="flex items-center gap-3 px-3 py-1.5 text-sm rounded-md hover:bg-accent/50 transition-colors"
                >
                  <LayoutGrid className="h-4 w-4 text-muted-foreground shrink-0" />
                  <span className="truncate">{board.title}</span>
                </Link>
              ))
            )}
          </div>
        </div>

        {/* Documents Section */}
        <div className="space-y-2">
          <div className="flex items-center justify-between px-3 text-xs font-semibold tracking-wider text-muted-foreground uppercase">
            <span>Documents</span>
            <Button
              variant="ghost"
              size="icon"
              className="h-4 w-4 hover:bg-accent"
              onClick={() => {
                createPageMutation.mutate(
                  { workspaceId, title: 'Untitled Page' },
                  {
                    onSuccess: (newPage) => {
                      navigate({
                        to: `/workspaces/${workspaceId}/docs/${newPage.id}`,
                      });
                    },
                  }
                );
              }}
              disabled={createPageMutation.isPending}
            >
              <Plus className="h-3 w-3" />
            </Button>
          </div>
          <div className="space-y-0.5">
            {pagesLoading ? (
              <div className="px-3 py-2 text-xs text-muted-foreground">Loading docs...</div>
            ) : pages.length === 0 ? (
              <div className="px-3 py-2 text-xs text-muted-foreground italic">No documents found</div>
            ) : (
              pages.map((page) => (
                <Link
                  key={page.id}
                  to="/workspaces/$workspaceId/docs/$docId"
                  params={{ workspaceId, docId: page.id }}
                  activeProps={{ className: 'bg-accent text-accent-foreground font-medium' }}
                  className="flex items-center gap-3 px-3 py-1.5 text-sm rounded-md hover:bg-accent/50 transition-colors"
                >
                  <FileText className="h-4 w-4 text-muted-foreground shrink-0" />
                  <span className="truncate">{page.title}</span>
                </Link>
              ))
            )}
          </div>
        </div>
      </div>

      {/* User Footer */}
      <div className="p-3 border-t bg-muted/20 flex items-center justify-between gap-2 overflow-hidden">
        <div className="flex items-center gap-2.5 overflow-hidden">
          <Avatar className="h-9 w-9">
            <AvatarImage src={currentUser?.avatarUrl || undefined} alt={currentUser?.name} />
            <AvatarFallback className="bg-primary/10 text-primary font-semibold">
              {currentUser?.name?.substring(0, 2).toUpperCase() || 'US'}
            </AvatarFallback>
          </Avatar>
          <div className="flex flex-col text-left overflow-hidden">
            <span className="text-sm font-medium truncate w-32">{currentUser?.name || 'User'}</span>
            <span className="text-xs text-muted-foreground truncate w-32">{currentUser?.email}</span>
          </div>
        </div>
        <Button
          variant="ghost"
          size="icon"
          onClick={handleLogout}
          className="text-muted-foreground hover:text-destructive hover:bg-destructive/10 shrink-0"
          title="Sign out"
        >
          <LogOut className="h-4 w-4" />
        </Button>
      </div>
    </aside>
  );
}

import { useNavigate } from '@tanstack/react-router';
import { Plus } from 'lucide-react';
import { Badge, Button, DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuLabel, DropdownMenuSeparator, DropdownMenuTrigger } from '@notrelix/ui-web';
import { workspaceViewTemplates } from '../../core/constants/view-templates';
import type { WorkspaceViewType } from '../../core/types/workspace';
import { api } from '@notrelix/contracts';
import { createUseCreateWorkspaceView } from '../hooks/mutations/use-create-workspace-view';

const defaultCreateViewHook = createUseCreateWorkspaceView({ api });

export function WorkspaceAddViewMenu({
  workspaceId,
  createViewHook = defaultCreateViewHook,
  boards = [],
}: {
  workspaceId: string;
  createViewHook?: ReturnType<typeof createUseCreateWorkspaceView>;
  boards?: Array<{ id: string }>;
}) {
  const navigate = useNavigate();
  const createView = createViewHook(workspaceId);

  async function handleCreate(type: WorkspaceViewType, label: string, disabled?: boolean) {
    if (disabled || createView.isPending) return;

    const firstBoardId = boards[0]?.id;

    let target: { boardId?: string; pageId?: string; calendarId?: string; dashboardId?: string } = {};
    if (type === 'table' || type === 'kanban' || type === 'timeline') {
      target = { boardId: firstBoardId || 'board-product' };
    } else if (type === 'doc') {
      target = { pageId: 'docs-mvp-spec' };
    } else if (type === 'calendar') {
      target = { calendarId: 'workspace-calendar', boardId: firstBoardId };
    } else if (type === 'dashboard') {
      target = { dashboardId: 'workspace-health' };
    }

    const view = await createView.mutateAsync({
      workspaceId,
      name: label,
      type,
      target,
    });
    navigate({
      to: '/workspaces/$workspaceId',
      params: { workspaceId },
      search: { view: view.id },
    });
  }

  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <Button variant="ghost" size="sm" className="h-9 rounded-full px-2.5">
          <Plus className="size-4" />
          <span className="sr-only sm:not-sr-only">Add view</span>
        </Button>
      </DropdownMenuTrigger>
      <DropdownMenuContent align="start" className="w-[330px]">
        <DropdownMenuLabel>Add workspace view</DropdownMenuLabel>
        <DropdownMenuSeparator />
        {workspaceViewTemplates.map((template) => (
          <DropdownMenuItem
            key={template.type}
            disabled={Boolean(template.badge)}
            onClick={() => handleCreate(template.type, template.label, Boolean(template.badge))}
            className="items-start gap-3 py-3"
          >
            <span className="mt-0.5 flex size-8 shrink-0 items-center justify-center rounded-lg bg-muted text-sm text-foreground">
              {template.icon}
            </span>
            <span className="min-w-0 flex-1">
              <span className="flex items-center gap-2 text-sm font-medium text-foreground">
                {template.label}
                {template.badge ? (
                  <Badge variant="secondary" className="rounded-full">
                    {template.badge}
                  </Badge>
                ) : null}
              </span>
              <span className="mt-0.5 block text-xs leading-5 text-muted-foreground">
                {template.description}
              </span>
            </span>
          </DropdownMenuItem>
        ))}
      </DropdownMenuContent>
    </DropdownMenu>
  );
}

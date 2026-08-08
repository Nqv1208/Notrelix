import { useState, useMemo } from 'react';
import { useNavigate } from '@tanstack/react-router';
import {
  createUseWorkspaceList,
  createUseCreateWorkspace,
} from '@notrelix/features-workspace';
import { useAppRuntime } from '@notrelix/runtime-web';
import { useWorkspaceContext } from '../../providers/workspace-provider';
import {
  Avatar,
  AvatarImage,
  AvatarFallback,
  DropdownMenu,
  DropdownMenuTrigger,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  Button,
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogFooter,
  Input,
} from '@notrelix/ui-web';
import { ChevronsUpDown, Plus } from 'lucide-react';

export function WorkspaceSwitcher() {
  const navigate = useNavigate();
  const { api: runtimeClient, env: runtimeEnv } = useAppRuntime();
  const { workspaceId, workspace: activeWorkspace } = useWorkspaceContext();

  const useWorkspaceList = useMemo(
    () =>
      createUseWorkspaceList({
        api: runtimeClient.api,
        endpoints: runtimeClient.endpoints,
        options: { mockMode: runtimeEnv.nodeEnv === 'development' },
      }),
    [runtimeClient, runtimeEnv.nodeEnv],
  );

  const useCreateWorkspace = useMemo(
    () =>
      createUseCreateWorkspace({
        api: runtimeClient.api,
        endpoints: runtimeClient.endpoints,
      }),
    [runtimeClient],
  );
  
  const { data: workspaces = [] } = useWorkspaceList();
  const createWorkspaceMutation = useCreateWorkspace();
  
  const [isDialogOpen, setIsDialogOpen] = useState(false);
  const [newWorkspaceName, setNewWorkspaceName] = useState('');

  const handleCreateWorkspace = () => {
    if (!newWorkspaceName.trim()) return;
    
    // Generate simple slug
    const slug = newWorkspaceName.toLowerCase().replace(/[^a-z0-9]+/g, '-').replace(/(^-|-$)/g, '');
    
    createWorkspaceMutation.mutate(
      {
        name: newWorkspaceName,
        slug,
        isPersonal: false,
      },
      {
        onSuccess: (newWorkspace) => {
          setIsDialogOpen(false);
          setNewWorkspaceName('');
          navigate({ to: `/workspaces/${newWorkspace.id}` });
        },
      }
    );
  };

  return (
    <>
      <DropdownMenu>
        <DropdownMenuTrigger asChild>
          <Button variant="ghost" className="w-full flex items-center justify-between px-2 py-1.5 h-12 hover:bg-muted/50 border border-transparent hover:border-muted-foreground/10 rounded-lg">
            <div className="flex items-center gap-2.5 overflow-hidden">
              <Avatar className="h-8 w-8 rounded-lg">
                <AvatarImage src={activeWorkspace?.icon || undefined} alt={activeWorkspace?.name} />
                <AvatarFallback className="rounded-lg bg-primary/10 text-primary font-bold">
                  {activeWorkspace?.name?.substring(0, 2).toUpperCase() || 'WX'}
                </AvatarFallback>
              </Avatar>
              <div className="flex flex-col items-start overflow-hidden text-left">
                <span className="font-semibold text-sm truncate leading-tight w-36">
                  {activeWorkspace?.name || 'Loading...'}
                </span>
                <span className="text-xs text-muted-foreground truncate w-36">
                  {activeWorkspace?.plan ? `${activeWorkspace.plan.toUpperCase()} Plan` : ''}
                </span>
              </div>
            </div>
            <ChevronsUpDown className="h-4 w-4 text-muted-foreground shrink-0" />
          </Button>
        </DropdownMenuTrigger>
        <DropdownMenuContent className="w-64" align="start">
          <DropdownMenuLabel className="text-xs text-muted-foreground">Workspaces</DropdownMenuLabel>
          {workspaces.map((ws) => (
            <DropdownMenuItem
              key={ws.id}
              onClick={() => navigate({ to: `/workspaces/${ws.id}` })}
              className={`flex items-center gap-2 px-2 py-1.5 cursor-pointer ${ws.id === workspaceId ? 'bg-muted font-medium' : ''}`}
            >
              <Avatar className="h-6 w-6 rounded-md">
                <AvatarFallback className="rounded-md bg-muted text-xs">
                  {ws.name.substring(0, 2).toUpperCase()}
                </AvatarFallback>
              </Avatar>
              <span className="truncate text-sm">{ws.name}</span>
            </DropdownMenuItem>
          ))}
          <DropdownMenuSeparator />
          <DropdownMenuItem
            onClick={() => setIsDialogOpen(true)}
            className="flex items-center gap-2 px-2 py-1.5 cursor-pointer text-primary hover:text-primary-foreground"
          >
            <Plus className="h-4 w-4" />
            <span className="text-sm">Create Workspace</span>
          </DropdownMenuItem>
        </DropdownMenuContent>
      </DropdownMenu>

      <Dialog open={isDialogOpen} onOpenChange={setIsDialogOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Create new workspace</DialogTitle>
          </DialogHeader>
          <div className="py-4 space-y-4">
            <div className="space-y-2">
              <label htmlFor="ws-name" className="text-sm font-medium">Workspace Name</label>
              <Input
                id="ws-name"
                placeholder="e.g. My Awesome Team"
                value={newWorkspaceName}
                onChange={(e) => setNewWorkspaceName(e.target.value)}
              />
            </div>
          </div>
          <DialogFooter>
            <Button variant="ghost" onClick={() => setIsDialogOpen(false)}>
              Cancel
            </Button>
            <Button
              onClick={handleCreateWorkspace}
              disabled={createWorkspaceMutation.isPending || !newWorkspaceName.trim()}
            >
              {createWorkspaceMutation.isPending ? 'Creating...' : 'Create'}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </>
  );
}

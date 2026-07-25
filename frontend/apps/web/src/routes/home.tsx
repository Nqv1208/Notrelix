import { useNavigate } from '@tanstack/react-router';
import { createUseWorkspaceList, type WorkspaceSummary } from '@notrelix/features-workspace';
import { api, endpoints } from '@notrelix/contracts';
import { Button } from '@notrelix/ui-web';

const useWorkspaceList = createUseWorkspaceList({ api, endpoints });

export function HomePage() {
  const navigate = useNavigate();
  const { data: workspaces, isLoading } = useWorkspaceList();

  if (isLoading) {
    return (
      <div className="min-h-screen flex items-center justify-center p-8">
        <div className="text-center">
          <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-primary mx-auto mb-4" />
          <p className="text-muted-foreground text-sm">Loading workspaces...</p>
        </div>
      </div>
    );
  }

  if (!workspaces || workspaces.length === 0) {
    return (
      <div className="min-h-screen flex items-center justify-center p-8">
        <div className="text-center max-w-md">
          <div className="flex items-center justify-center size-16 rounded-2xl bg-primary/10 mx-auto mb-6">
            <svg className="size-8 text-primary" fill="none" viewBox="0 0 24 24" strokeWidth={2} stroke="currentColor">
              <path strokeLinecap="round" strokeLinejoin="round" d="M12 9v6m3-3H9m12 0a9 9 0 1 1-18 0 9 9 0 0 1 18 0Z" />
            </svg>
          </div>
          <h1 className="text-2xl font-bold tracking-tight mb-2">Welcome to Notrelix</h1>
          <p className="text-muted-foreground mb-6">
            Create your first workspace to get started, or ask a teammate to invite you.
          </p>
          <Button onClick={() => navigate({ to: '/sign-in' })}>
            Sign in to a workspace
          </Button>
        </div>
      </div>
    );
  }

  return (
    <div className="min-h-screen p-8">
      <div className="max-w-2xl mx-auto">
        <h1 className="text-2xl font-bold tracking-tight mb-2">Your Workspaces</h1>
        <p className="text-muted-foreground mb-6">Select a workspace to continue.</p>
        <div className="flex flex-col gap-2">
          {workspaces.map((ws: WorkspaceSummary) => (
            <button
              key={ws.id}
              onClick={() => navigate({ to: `/workspaces/${ws.id}` })}
              className="flex items-center gap-3 p-3 rounded-lg border border-border hover:bg-muted/50 text-left transition-colors"
            >
              <div className="flex size-10 items-center justify-center rounded-lg bg-primary/10 text-sm font-semibold text-primary shrink-0">
                {(ws.name ?? ws.slug ?? 'W').charAt(0).toUpperCase()}
              </div>
              <div className="min-w-0 flex-1">
                <p className="font-medium text-foreground truncate">{ws.name}</p>
                <p className="text-xs text-muted-foreground truncate">{ws.slug}</p>
              </div>
              <svg className="size-4 text-muted-foreground shrink-0" fill="none" viewBox="0 0 24 24" strokeWidth={2} stroke="currentColor">
                <path strokeLinecap="round" strokeLinejoin="round" d="m8.25 4.5 7.5 7.5-7.5 7.5" />
              </svg>
            </button>
          ))}
        </div>
      </div>
    </div>
  );
}

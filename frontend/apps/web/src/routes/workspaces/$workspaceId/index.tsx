import { useParams, Link } from '@tanstack/react-router';
import { useWorkspaceContext } from '@/providers/workspace-provider';
import { FileText, LayoutGrid, ArrowRight } from 'lucide-react';

export function WorkspaceHomePage() {
  const { workspaceId } = useParams({ from: '/workspaces/$workspaceId' });
  const { workspace } = useWorkspaceContext();

  return (
    <div className="p-8 max-w-6xl">
      <div className="mb-8">
        <h1 className="text-2xl font-bold tracking-tight mb-1">{workspace?.name ?? 'Workspace'}</h1>
        {workspace?.description && (
          <p className="text-muted-foreground">{workspace.description}</p>
        )}
      </div>

      <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
        <Link
          to="/workspaces/$workspaceId/dashboard"
          params={{ workspaceId }}
          className="group rounded-xl border border-border p-5 hover:bg-muted/30 transition-colors"
        >
          <div className="flex items-center justify-between mb-3">
            <div className="flex items-center gap-2">
              <LayoutGrid className="size-4 text-muted-foreground" />
              <h2 className="font-semibold text-sm">Dashboard</h2>
            </div>
            <ArrowRight className="size-4 text-muted-foreground group-hover:text-foreground transition-colors" />
          </div>
          <p className="text-sm text-muted-foreground">
            Overview of your workspace activity, boards, and documents.
          </p>
        </Link>

        <Link
          to="/workspaces/$workspaceId/docs/$pageId"
          params={{ workspaceId, pageId: '' }}
          className="group rounded-xl border border-border p-5 hover:bg-muted/30 transition-colors"
        >
          <div className="flex items-center justify-between mb-3">
            <div className="flex items-center gap-2">
              <FileText className="size-4 text-muted-foreground" />
              <h2 className="font-semibold text-sm">Documents</h2>
            </div>
            <ArrowRight className="size-4 text-muted-foreground group-hover:text-foreground transition-colors" />
          </div>
          <p className="text-sm text-muted-foreground">
            Create and manage workspace documents, specs, and notes.
          </p>
        </Link>
      </div>
    </div>
  );
}

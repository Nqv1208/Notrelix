import { Link } from '@tanstack/react-router';
import { FileText, ArrowRight } from 'lucide-react';
import { Badge } from '@notrelix/ui-web';
import { cn } from '@notrelix/ui-web';

interface Doc {
  id: string;
  title: string;
  updatedAt?: string;
}

interface PinnedDocsProps {
  workspaceId: string;
  docs: Doc[];
  isLoading?: boolean;
}

export function PinnedDocs({ workspaceId, docs, isLoading }: PinnedDocsProps) {
  return (
    <div className="rounded-xl border border-border/60 bg-card/50 p-5">
      <div className="flex items-center justify-between mb-4">
        <div className="flex items-center gap-2">
          <FileText className="size-4 text-muted-foreground" />
          <h3 className="font-semibold text-sm">Recent Documents</h3>
        </div>
        {docs.length > 0 && (
          <Link
            to="/workspaces/$workspaceId"
            params={{ workspaceId }}
            className="text-xs text-muted-foreground hover:text-foreground transition-colors"
          >
            View all
          </Link>
        )}
      </div>

      {isLoading ? (
        <div className="space-y-2">
          {[1, 2, 3].map((i) => (
            <div key={i} className="h-10 bg-muted rounded animate-pulse" />
          ))}
        </div>
      ) : docs.length === 0 ? (
        <p className="text-sm text-muted-foreground py-4">No documents yet. Create your first document to get started.</p>
      ) : (
        <div className="space-y-1">
          {docs.slice(0, 5).map((doc) => (
            <Link
              key={doc.id}
              to="/workspaces/$workspaceId/docs/$docId"
              params={{ workspaceId, docId: doc.id }}
              className="group flex items-center justify-between p-2 rounded-md hover:bg-muted/50 text-sm transition-colors"
            >
              <div className="flex items-center gap-2 min-w-0">
                <FileText className="size-3.5 text-muted-foreground shrink-0" />
                <span className="truncate text-foreground">{doc.title}</span>
              </div>
              {doc.updatedAt && (
                <span className="text-[11px] text-muted-foreground shrink-0 ml-2">
                  {new Date(doc.updatedAt).toLocaleDateString()}
                </span>
              )}
            </Link>
          ))}
        </div>
      )}
    </div>
  );
}

import {
  createUsePageHistory,
  type DocsApiClient,
  type PageApiEndpoints,
  type PageActivity,
} from '@notrelix/docs-state';
import { Clock, FileText, MessageSquare, Share2, ArrowRight, Sparkles } from 'lucide-react';

interface DocHistoryProps {
  api: DocsApiClient;
  endpoints: PageApiEndpoints;
  pageId: string;
}

const ACTION_ICONS: Record<PageActivity['action'], React.ReactNode> = {
  created: <FileText className="h-3.5 w-3.5" />,
  edited: <FileText className="h-3.5 w-3.5" />,
  commented: <MessageSquare className="h-3.5 w-3.5" />,
  shared: <Share2 className="h-3.5 w-3.5" />,
  moved: <ArrowRight className="h-3.5 w-3.5" />,
  published: <Sparkles className="h-3.5 w-3.5" />,
};

const ACTION_LABELS: Record<PageActivity['action'], string> = {
  created: 'created this page',
  edited: 'edited this page',
  commented: 'commented on this page',
  shared: 'shared this page',
  moved: 'moved this page',
  published: 'published this page',
};

export function DocHistory({ api, endpoints, pageId }: DocHistoryProps) {
  const usePageHistory = createUsePageHistory(api, endpoints);
  const { data: history = [], isLoading } = usePageHistory(pageId);

  return (
    <div className="space-y-4">
      <div className="flex items-center gap-2 text-sm font-medium text-muted-foreground">
        <Clock className="h-4 w-4" />
        History ({history.length})
      </div>

      {isLoading ? (
        <div className="space-y-3">
          {[1, 2, 3].map((i) => (
            <div key={i} className="h-12 bg-muted rounded animate-pulse" />
          ))}
        </div>
      ) : history.length === 0 ? (
        <p className="text-sm text-muted-foreground italic">No history yet</p>
      ) : (
        <div className="space-y-1">
          {history.map((activity: PageActivity) => (
            <div
              key={activity.id}
              className="flex items-start gap-3 py-2.5 px-2 rounded-lg hover:bg-muted/50 transition-colors"
            >
              <div className="mt-0.5 text-muted-foreground">
                {ACTION_ICONS[activity.action as keyof typeof ACTION_ICONS]}
              </div>
              <div className="flex-1 min-w-0">
                <p className="text-sm">
                  <span className="font-medium">{activity.actorId}</span>{' '}
                  <span className="text-muted-foreground">{ACTION_LABELS[activity.action as keyof typeof ACTION_LABELS]}</span>
                </p>
                {activity.targetLabel && (
                  <p className="text-xs text-muted-foreground truncate mt-0.5">
                    {activity.targetLabel}
                  </p>
                )}
              </div>
              <span className="text-xs text-muted-foreground whitespace-nowrap">
                {new Date(activity.createdAt).toLocaleDateString()}
              </span>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}

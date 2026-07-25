import { Activity } from 'lucide-react';
import { Avatar, AvatarFallback } from '@notrelix/ui-web';
import type { WorkspaceActivityItem } from '../../../core';
import { cn } from '@notrelix/ui-web';

const avatarColors = [
  'bg-violet-100 text-violet-700 dark:bg-violet-900/40 dark:text-violet-300',
  'bg-sky-100 text-sky-700 dark:bg-sky-900/40 dark:text-sky-300',
  'bg-amber-100 text-amber-700 dark:bg-amber-900/40 dark:text-amber-300',
  'bg-emerald-100 text-emerald-700 dark:bg-emerald-900/40 dark:text-emerald-300',
  'bg-rose-100 text-rose-700 dark:bg-rose-900/40 dark:text-rose-300',
];

function getInitials(name: string) {
  return name.split(/\s+/).map((p) => p[0]).join('').toUpperCase().slice(0, 2);
}

function timeAgo(dateStr: string) {
  const date = new Date(dateStr);
  const now = new Date();
  const diffMs = now.getTime() - date.getTime();
  const diffMin = Math.floor(diffMs / 60000);
  if (diffMin < 1) return 'just now';
  if (diffMin < 60) return `${diffMin}m ago`;
  const diffHr = Math.floor(diffMin / 60);
  if (diffHr < 24) return `${diffHr}h ago`;
  const diffDay = Math.floor(diffHr / 24);
  if (diffDay < 7) return `${diffDay}d ago`;
  return date.toLocaleDateString();
}

interface ActivityFeedProps {
  activities: WorkspaceActivityItem[];
  isLoading?: boolean;
}

export function ActivityFeed({ activities, isLoading }: ActivityFeedProps) {
  return (
    <div className="rounded-xl border border-border/60 bg-card/50 p-5">
      <div className="flex items-center gap-2 mb-4">
        <Activity className="size-4 text-muted-foreground" />
        <h3 className="font-semibold text-sm">Recent Activity</h3>
      </div>

      {isLoading ? (
        <div className="space-y-3">
          {[1, 2, 3].map((i) => (
            <div key={i} className="h-10 bg-muted rounded animate-pulse" />
          ))}
        </div>
      ) : activities.length === 0 ? (
        <p className="text-sm text-muted-foreground py-4">No activity yet.</p>
      ) : (
        <div className="space-y-1">
          {activities.slice(0, 7).map((item, i) => (
            <div key={item.id} className="flex items-start gap-3 p-2 rounded-md hover:bg-muted/30 transition-colors">
              <Avatar className="size-7 mt-0.5">
                <AvatarFallback className={cn('text-[10px]', avatarColors[i % avatarColors.length])}>
                  {getInitials(item.actor)}
                </AvatarFallback>
              </Avatar>
              <div className="flex-1 min-w-0">
                <p className="text-sm leading-snug">
                  <span className="font-medium text-foreground">{item.actor}</span>{' '}
                  <span className="text-muted-foreground">{item.action}</span>{' '}
                  <span className="font-medium text-foreground">{item.target}</span>
                </p>
                <p className="text-[11px] text-muted-foreground mt-0.5">{timeAgo(item.createdAt)}</p>
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}

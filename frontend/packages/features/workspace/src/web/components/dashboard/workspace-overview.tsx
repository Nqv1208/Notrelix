import { FileText, LayoutGrid, Users, ClipboardList } from 'lucide-react';
import type { WorkspaceMember } from '../../../core';
import { cn } from '@notrelix/ui-web';

interface WorkspaceOverviewProps {
  workspaceName: string;
  pageCount: number;
  boardCount: number;
  memberCount: number;
  isLoading?: boolean;
}

function getGreeting() {
  const hour = new Date().getHours();
  if (hour < 12) return 'Good morning';
  if (hour < 18) return 'Good afternoon';
  return 'Good evening';
}

const stats = [
  { key: 'pages', label: 'Pages', icon: FileText, color: 'bg-blue-500/10 text-blue-600 dark:text-blue-400' },
  { key: 'boards', label: 'Active Boards', icon: LayoutGrid, color: 'bg-violet-500/10 text-violet-600 dark:text-violet-400' },
  { key: 'tasks', label: 'Team Members', icon: Users, color: 'bg-emerald-500/10 text-emerald-600 dark:text-emerald-400' },
];

export function WorkspaceOverview({ workspaceName, pageCount, boardCount, memberCount, isLoading }: WorkspaceOverviewProps) {
  const values: Record<string, number> = { pages: pageCount, boards: boardCount, tasks: memberCount };

  return (
    <div className="space-y-6">
      <div>
        <h2 className="text-2xl font-bold tracking-tight">{getGreeting()}, {workspaceName}</h2>
        <p className="text-sm text-muted-foreground mt-1">Here's what's happening in your workspace.</p>
      </div>

      <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
        {stats.map((stat) => {
          const Icon = stat.icon;
          return (
            <div key={stat.key} className="rounded-xl border border-border/60 bg-card/50 p-5 flex items-center gap-4">
              <div className={cn('size-10 rounded-xl flex items-center justify-center', stat.color)}>
                <Icon className="size-5" />
              </div>
              <div>
                {isLoading ? (
                  <div className="h-7 w-12 bg-muted rounded animate-pulse" />
                ) : (
                  <p className="text-2xl font-bold">{values[stat.key]}</p>
                )}
                <p className="text-xs text-muted-foreground">{stat.label}</p>
              </div>
            </div>
          );
        })}
      </div>
    </div>
  );
}

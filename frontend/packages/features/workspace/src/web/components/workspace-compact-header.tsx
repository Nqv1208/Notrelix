import { Link } from '@tanstack/react-router';
import {
  Bot,
  ChevronDown,
  Link2,
  MessageSquareText,
  MoreHorizontal,
  Plug,
  Settings,
  Share2,
  Sparkles,
  Star,
  UserPlus,
  Workflow,
} from 'lucide-react';
import {
  Avatar,
  AvatarFallback,
  Badge,
  Button,
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
  Separator,
  cn,
} from '@notrelix/ui-web';
import type { WorkspaceMember, WorkspaceSummary } from '~/core/types/workspace';
import { toast } from 'sonner';

const avatarColors = [
  'bg-violet-100 text-violet-700 dark:bg-violet-900/40 dark:text-violet-300',
  'bg-sky-100 text-sky-700 dark:bg-sky-900/40 dark:text-sky-300',
  'bg-amber-100 text-amber-700 dark:bg-amber-900/40 dark:text-amber-300',
  'bg-emerald-100 text-emerald-700 dark:bg-emerald-900/40 dark:text-emerald-300',
  'bg-rose-100 text-rose-700 dark:bg-rose-900/40 dark:text-rose-300',
  'bg-cyan-100 text-cyan-700 dark:bg-cyan-900/40 dark:text-cyan-300',
  'bg-indigo-100 text-indigo-700 dark:bg-indigo-900/40 dark:text-indigo-300',
];

export function WorkspaceCompactHeader({
  workspace,
  members,
}: {
  workspace: WorkspaceSummary;
  members: WorkspaceMember[];
}) {
  const handleCopyLink = () => {
    if (typeof window !== 'undefined') {
      const workspaceUrl = `${window.location.origin}/workspaces/${workspace.id}`;
      navigator.clipboard
        .writeText(workspaceUrl)
        .then(() => toast.success('Workspace link copied to clipboard'))
        .catch(() => toast.error('Failed to copy workspace link'));
    }
  };

  return (
    <header className="border-b border-border bg-card px-4 py-2.5 sm:px-6">
      <div className="flex min-h-10 flex-wrap items-center gap-2">
        <DropdownMenu>
          <DropdownMenuTrigger asChild>
            <Button variant="ghost" className="h-10 rounded-xl px-2 text-left">
              <span className="mr-2 flex size-8 items-center justify-center rounded-xl bg-primary text-sm font-semibold text-primary-foreground">
                {workspace.icon}
              </span>
              <span className="max-w-[220px] truncate text-lg font-semibold tracking-[-0.01em] text-foreground">
                {workspace.name}
              </span>
              <ChevronDown className="ml-1 size-4 text-muted-foreground" />
            </Button>
          </DropdownMenuTrigger>
          <DropdownMenuContent align="start" className="w-64">
            <DropdownMenuLabel>Workspace actions</DropdownMenuLabel>
            <DropdownMenuItem>
              <Star className="size-4 mr-2" />
              Add to favorites
            </DropdownMenuItem>
            <DropdownMenuItem onClick={handleCopyLink} className="cursor-pointer">
              <Link2 className="size-4 mr-2" />
              Copy workspace link
            </DropdownMenuItem>
            <DropdownMenuSeparator />
            <DropdownMenuItem asChild className="cursor-pointer">
              <Link to="/workspaces/$workspaceId/settings" params={{ workspaceId: workspace.id }}>
                <Settings className="size-4 mr-2" />
                Workspace Settings
              </Link>
            </DropdownMenuItem>
          </DropdownMenuContent>
        </DropdownMenu>

        <Button variant="ghost" size="icon" aria-label="Favorite workspace">
          <Star className="size-4" />
        </Button>
        <Badge variant="secondary" className="rounded-full capitalize">
          {workspace.plan}
        </Badge>
        <Separator orientation="vertical" className="mx-1 hidden h-6 md:block" />
        <Button variant="ghost" size="sm" className="rounded-full">
          <Sparkles className="size-4" />
          AI suggestions
        </Button>
        <Button variant="ghost" size="sm" className="rounded-full" asChild>
          <Link to="/workspaces/$workspaceId/settings" params={{ workspaceId: workspace.id }}>
            <Plug className="size-4" />
            Integrate
          </Link>
        </Button>
        <Button variant="ghost" size="sm" className="rounded-full" asChild>
          <Link to="/workspaces/$workspaceId/settings" params={{ workspaceId: workspace.id }}>
            <Workflow className="size-4" />
            Automate
          </Link>
        </Button>
        <Button variant="ghost" size="sm" className="rounded-full">
          <Bot className="size-4" />
          Agents
        </Button>
        <div className="ml-auto flex items-center gap-2">
          <div className="hidden -space-x-2 md:flex">
            {members.slice(0, 4).map((member, i) => (
              <Avatar key={member.id} className="size-8 border-2 border-card">
                <AvatarFallback className={cn('text-[10px]', avatarColors[i % avatarColors.length])}>
                  {member.initials}
                </AvatarFallback>
              </Avatar>
            ))}
          </div>
          <Button size="sm" className="rounded-full" asChild>
            <Link to="/workspaces/$workspaceId/members" params={{ workspaceId: workspace.id }}>
              <UserPlus className="size-4 mr-2" />
              Invite
            </Link>
          </Button>
          <Button variant="outline" size="sm" className="bg-card">
            <Share2 className="size-4" />
            Share
          </Button>
          <Button variant="ghost" size="icon" aria-label="Workspace comments and activity" asChild>
            <Link to="/workspaces/$workspaceId/settings" params={{ workspaceId: workspace.id }}>
              <MessageSquareText className="size-4" />
            </Link>
          </Button>
          <Button variant="ghost" size="icon" aria-label="More workspace actions">
            <MoreHorizontal className="size-4" />
          </Button>
        </div>
      </div>
    </header>
  );
}

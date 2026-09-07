import { useState } from "react";
import {
  Bot,
  ChevronDown,
  MessageSquareText,
  MoreHorizontal,
  Settings,
  Share2,
  Sparkles,
  Star,
  UserPlus,
  Workflow,
} from "lucide-react";
import {
  Avatar,
  AvatarFallback,
  Badge,
  Button,
  Separator,
  cn,
} from "@notrelix/ui-web";
import type {
  WorkspaceMember,
  WorkspaceSummary,
} from "../../core/types/workspace";
import { avatarColors } from "./workspace-ui-models";

export interface WorkspaceCompactHeaderSurfaceProps {
  workspace: WorkspaceSummary;
  members: WorkspaceMember[];
  onCopyLink?: () => void;
  onOpenSettings?: () => void;
  onInvite?: () => void;
  onShare?: () => void;
  onFavorite?: () => void;
}

export function WorkspaceCompactHeaderSurface({
  workspace,
  members,
  onCopyLink,
  onOpenSettings,
  onInvite,
  onShare,
  onFavorite,
}: WorkspaceCompactHeaderSurfaceProps) {
  const [open, setOpen] = useState(false);

  return (
    <header className="border-b border-border bg-card px-4 py-2.5 sm:px-6">
      <div className="flex min-h-10 flex-wrap items-center gap-2">
        <div className="relative">
          <Button
            variant="ghost"
            className="h-10 rounded-xl px-2 text-left"
            onClick={() => setOpen((value) => !value)}
          >
            <span className="mr-2 flex size-8 items-center justify-center rounded-xl bg-primary text-sm font-semibold text-primary-foreground">
              {workspace.icon}
            </span>
            <span className="max-w-[220px] truncate text-lg font-semibold tracking-[-0.01em] text-foreground">
              {workspace.name}
            </span>
            <ChevronDown className="ml-1 size-4 text-muted-foreground" />
          </Button>
          {open ? (
            <div className="absolute left-0 top-11 z-20 w-64 rounded-xl border border-border bg-popover p-1 shadow-lg">
              <p className="px-2 py-1.5 text-xs font-semibold text-muted-foreground">
                Workspace actions
              </p>
              <button
                type="button"
                onClick={onFavorite}
                className="flex w-full items-center gap-2 rounded-md px-2 py-1.5 text-left text-sm hover:bg-muted"
              >
                <Star className="size-4" />
                Add to favorites
              </button>
              <button
                type="button"
                onClick={onCopyLink}
                className="flex w-full items-center gap-2 rounded-md px-2 py-1.5 text-left text-sm hover:bg-muted"
              >
                <MessageSquareText className="size-4" />
                Copy workspace link
              </button>
              <Separator className="my-1" />
              <button
                type="button"
                onClick={onOpenSettings}
                className="flex w-full items-center gap-2 rounded-md px-2 py-1.5 text-left text-sm hover:bg-muted"
              >
                <Settings className="size-4" />
                Workspace Settings
              </button>
            </div>
          ) : null}
        </div>

        <Button
          variant="ghost"
          size="icon"
          aria-label="Favorite workspace"
          onClick={onFavorite}
        >
          <Star className="size-4" />
        </Button>
        <Badge variant="secondary" className="rounded-full capitalize">
          {workspace.plan}
        </Badge>
        <Separator
          orientation="vertical"
          className="mx-1 hidden h-6 md:block"
        />
        <Button variant="ghost" size="sm" className="rounded-full">
          <Sparkles className="size-4" />
          AI suggestions
        </Button>
        <Button variant="ghost" size="sm" className="rounded-full">
          <Workflow className="size-4" />
          Automate
        </Button>
        <Button variant="ghost" size="sm" className="rounded-full">
          <Bot className="size-4" />
          Agents
        </Button>
        <div className="ml-auto flex items-center gap-2">
          <div className="hidden -space-x-2 md:flex">
            {members.slice(0, 4).map((member, i) => (
              <Avatar key={member.id} className="size-8 border-2 border-card">
                <AvatarFallback
                  className={cn(
                    "text-[10px]",
                    avatarColors[i % avatarColors.length],
                  )}
                >
                  {member.initials}
                </AvatarFallback>
              </Avatar>
            ))}
          </div>
          <Button size="sm" className="rounded-full" onClick={onInvite}>
            <UserPlus className="size-4 mr-2" />
            Invite
          </Button>
          <Button
            variant="outline"
            size="sm"
            className="bg-card"
            onClick={onShare}
          >
            <Share2 className="size-4" />
            Share
          </Button>
          <Button
            variant="ghost"
            size="icon"
            aria-label="More workspace actions"
          >
            <MoreHorizontal className="size-4" />
          </Button>
        </div>
      </div>
    </header>
  );
}
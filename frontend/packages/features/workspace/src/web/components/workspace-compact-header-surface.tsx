import { useState } from "react";
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
  onAiSuggestions?: () => void;
  onIntegrate?: () => void;
  onAutomate?: () => void;
  onAgents?: () => void;
  onOpenActivity?: () => void;
  onMoreActions?: () => void;
}

export function WorkspaceCompactHeaderSurface({
  workspace,
  members,
  onCopyLink,
  onOpenSettings,
  onInvite,
  onShare,
  onFavorite,
  onAiSuggestions,
  onIntegrate,
  onAutomate,
  onAgents,
  onOpenActivity,
  onMoreActions,
}: WorkspaceCompactHeaderSurfaceProps) {
  const [menuOpen, setMenuOpen] = useState(false);
  const hasWorkspaceMenuActions = Boolean(
    onFavorite || onCopyLink || onOpenSettings,
  );

  const runMenuAction = (action?: () => void) => {
    setMenuOpen(false);
    action?.();
  };

  return (
    <header className="border-b border-border bg-card px-4 py-2.5 sm:px-6">
      <div className="flex min-h-10 flex-wrap items-center gap-2">
        <div className="relative">
          {hasWorkspaceMenuActions ? (
            <Button
              variant="ghost"
              className="h-10 rounded-xl px-2 text-left"
              onClick={() => setMenuOpen((current) => !current)}
            >
              <span className="mr-2 flex size-8 items-center justify-center rounded-xl bg-primary text-sm font-semibold text-primary-foreground">
                {workspace.icon}
              </span>
              <span className="max-w-[220px] truncate text-lg font-semibold tracking-[-0.01em] text-foreground">
                {workspace.name}
              </span>
              <ChevronDown className="ml-1 size-4 text-muted-foreground" />
            </Button>
          ) : (
            <div className="flex h-10 items-center px-2">
              <span className="mr-2 flex size-8 items-center justify-center rounded-xl bg-primary text-sm font-semibold text-primary-foreground">
                {workspace.icon}
              </span>
              <span className="max-w-[220px] truncate text-lg font-semibold tracking-[-0.01em] text-foreground">
                {workspace.name}
              </span>
            </div>
          )}
          {menuOpen && hasWorkspaceMenuActions ? (
            <div className="absolute left-0 top-11 z-20 w-64 rounded-xl border border-border bg-popover p-1 shadow-lg">
              <p className="px-2 py-1.5 text-xs font-semibold text-muted-foreground">
                Workspace actions
              </p>
              {onFavorite ? (
                <button
                  type="button"
                  onClick={() => runMenuAction(onFavorite)}
                  className="flex w-full items-center gap-2 rounded-md px-2 py-1.5 text-left text-sm hover:bg-muted"
                >
                  <Star className="size-4" />
                  Add to favorites
                </button>
              ) : null}
              {onCopyLink ? (
                <button
                  type="button"
                  onClick={() => runMenuAction(onCopyLink)}
                  className="flex w-full items-center gap-2 rounded-md px-2 py-1.5 text-left text-sm hover:bg-muted"
                >
                  <Link2 className="size-4" />
                  Copy workspace link
                </button>
              ) : null}
              {onOpenSettings ? (
                <>
                  {onFavorite || onCopyLink ? (
                    <Separator className="my-1" />
                  ) : null}
                  <button
                    type="button"
                    onClick={() => runMenuAction(onOpenSettings)}
                    className="flex w-full items-center gap-2 rounded-md px-2 py-1.5 text-left text-sm hover:bg-muted"
                  >
                    <Settings className="size-4" />
                    Workspace Settings
                  </button>
                </>
              ) : null}
            </div>
          ) : null}
        </div>

        {onFavorite ? (
          <Button
            variant="ghost"
            size="icon"
            aria-label="Favorite workspace"
            onClick={onFavorite}
          >
            <Star className="size-4" />
          </Button>
        ) : null}
        <Badge variant="secondary" className="rounded-full capitalize">
          {workspace.plan}
        </Badge>
        <Separator
          orientation="vertical"
          className="mx-1 hidden h-6 md:block"
        />
        {onAiSuggestions ? (
          <Button
            variant="ghost"
            size="sm"
            className="rounded-full"
            onClick={onAiSuggestions}
          >
            <Sparkles className="size-4" />
            AI suggestions
          </Button>
        ) : null}
        {onIntegrate ? (
          <Button
            variant="ghost"
            size="sm"
            className="rounded-full"
            onClick={onIntegrate}
          >
            <Plug className="size-4" />
            Integrate
          </Button>
        ) : null}
        {onAutomate ? (
          <Button
            variant="ghost"
            size="sm"
            className="rounded-full"
            onClick={onAutomate}
          >
            <Workflow className="size-4" />
            Automate
          </Button>
        ) : null}
        {onAgents ? (
          <Button
            variant="ghost"
            size="sm"
            className="rounded-full"
            onClick={onAgents}
          >
            <Bot className="size-4" />
            Agents
          </Button>
        ) : null}
        <div className="ml-auto flex items-center gap-2">
          <div className="hidden -space-x-2 md:flex">
            {members.slice(0, 4).map((member, index) => (
              <Avatar key={member.id} className="size-8 border-2 border-card">
                <AvatarFallback
                  className={cn(
                    "text-[10px]",
                    avatarColors[index % avatarColors.length],
                  )}
                >
                  {member.initials}
                </AvatarFallback>
              </Avatar>
            ))}
          </div>
          {onInvite ? (
            <Button size="sm" className="rounded-full" onClick={onInvite}>
              <UserPlus className="mr-2 size-4" />
              Invite
            </Button>
          ) : null}
          {onShare ? (
            <Button
              variant="outline"
              size="sm"
              className="bg-card"
              onClick={onShare}
            >
              <Share2 className="size-4" />
              Share
            </Button>
          ) : null}
          {onOpenActivity ? (
            <Button
              variant="ghost"
              size="icon"
              aria-label="Workspace comments and activity"
              onClick={onOpenActivity}
            >
              <MessageSquareText className="size-4" />
            </Button>
          ) : null}
          {onMoreActions ? (
            <Button
              variant="ghost"
              size="icon"
              aria-label="More workspace actions"
              onClick={onMoreActions}
            >
              <MoreHorizontal className="size-4" />
            </Button>
          ) : null}
        </div>
      </div>
    </header>
  );
}

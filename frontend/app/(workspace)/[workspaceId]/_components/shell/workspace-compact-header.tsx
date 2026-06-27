"use client"

import Link from "next/link"
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
} from "lucide-react"
import { Avatar, AvatarFallback } from "@/components/ui/avatar"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu"
import { Separator } from "@/components/ui/separator"
import type { WorkspaceMember, WorkspaceSummary } from "@/features/workspace"
import { toast } from "sonner"

export function WorkspaceCompactHeader({
  workspace,
  members,
}: {
  workspace: WorkspaceSummary
  members: WorkspaceMember[]
}) {
  const handleCopyLink = () => {
    if (typeof window !== "undefined") {
      const workspaceUrl = `${window.location.origin}/${workspace.id}`
      navigator.clipboard.writeText(workspaceUrl)
        .then(() => toast.success("Workspace link copied to clipboard"))
        .catch(() => toast.error("Failed to copy workspace link"))
    }
  }
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
              <Star className="size-4" />
              Add to favorites
            </DropdownMenuItem>
            <DropdownMenuItem onClick={handleCopyLink} className="cursor-pointer">
              <Link2 className="size-4" />
              Copy workspace link
            </DropdownMenuItem>
            <DropdownMenuSeparator />
            <DropdownMenuItem asChild className="cursor-pointer">
              <Link href={`/${workspace.id}?panel=settings&tab=general`}>
                <Settings className="size-4 mr-2" />
                Workspace Settings
              </Link>
            </DropdownMenuItem>
          </DropdownMenuContent>
        </DropdownMenu>

        <Button variant="ghost" size="icon-sm" aria-label="Favorite workspace">
          <Star className="size-4" />
        </Button>
        <Badge variant="secondary" className="rounded-full">{workspace.plan}</Badge>
        <Separator orientation="vertical" className="mx-1 hidden h-6 md:block" />
        <Button variant="ghost" size="sm" className="rounded-full">
          <Sparkles className="size-4" />
          AI suggestions
        </Button>
        <Button variant="ghost" size="sm" className="rounded-full" asChild>
          <Link href={`/${workspace.id}?panel=settings&tab=integrations`}>
            <Plug className="size-4" />
            Integrate
          </Link>
        </Button>
        <Button variant="ghost" size="sm" className="rounded-full" asChild>
          <Link href={`/${workspace.id}?panel=settings&tab=automations`}>
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
            {members.slice(0, 4).map((member) => (
              <Avatar key={member.id} className="size-8 border-2 border-card">
                <AvatarFallback className="text-[10px] text-primary-foreground" style={{ backgroundColor: member.color }}>
                  {member.initials}
                </AvatarFallback>
              </Avatar>
            ))}
          </div>
          <Button size="sm" className="rounded-full" asChild>
            <Link href={`/${workspace.id}?panel=settings&tab=members`}>
              <UserPlus className="size-4" />
              Invite
            </Link>
          </Button>
          <Button variant="outline" size="sm" className="bg-card">
            <Share2 className="size-4" />
            Share
          </Button>
          <Button variant="ghost" size="icon-sm" aria-label="Workspace comments and activity" asChild>
            <Link href={`/${workspace.id}?panel=settings&tab=activity`}>
              <MessageSquareText className="size-4" />
            </Link>
          </Button>
          <Button variant="ghost" size="icon-sm" aria-label="More workspace actions">
            <MoreHorizontal className="size-4" />
          </Button>
        </div>
      </div>
    </header>
  )
}

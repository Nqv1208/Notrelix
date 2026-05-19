"use client"

import { Bot, ChevronDown, Link2, MessageSquareText, MoreHorizontal, Plug, Share2, Sparkles, Star, UserPlus, Workflow } from "lucide-react"
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
import type { WorkspaceMember, WorkspaceSummary } from "@/features/workspace/types"

export function WorkspaceBoardHeader({
  workspace,
  members,
}: {
  workspace: WorkspaceSummary
  members: WorkspaceMember[]
}) {
  const onlineMembers = members.filter((member) => member.status === "active" || member.status === "in-call")

  return (
    <header className="border-b border-border bg-card px-4 py-4 sm:px-6">
      <div className="flex flex-col gap-4 xl:flex-row xl:items-end xl:justify-between">
        <div className="min-w-0">
          <div className="mb-2 flex flex-wrap items-center gap-2">
            <DropdownMenu>
              <DropdownMenuTrigger asChild>
                <Button variant="ghost" className="h-auto rounded-xl px-2 py-1 text-left">
                  <span className="mr-2 flex size-9 items-center justify-center rounded-xl bg-primary text-sm font-semibold text-primary-foreground">
                    {workspace.icon}
                  </span>
                  <span className="min-w-0">
                    <span className="block truncate text-2xl font-semibold tracking-[-0.015em] text-foreground sm:text-3xl">
                      {workspace.name}
                    </span>
                  </span>
                  <ChevronDown className="ml-2 size-4 text-muted-foreground" />
                </Button>
              </DropdownMenuTrigger>
              <DropdownMenuContent align="start" className="w-64">
                <DropdownMenuLabel>Workspace actions</DropdownMenuLabel>
                <DropdownMenuItem>
                  <Star className="size-4" />
                  Add to favorites
                </DropdownMenuItem>
                <DropdownMenuItem>
                  <Link2 className="size-4" />
                  Copy workspace link
                </DropdownMenuItem>
                <DropdownMenuSeparator />
                <DropdownMenuItem>
                  <MoreHorizontal className="size-4" />
                  More settings
                </DropdownMenuItem>
              </DropdownMenuContent>
            </DropdownMenu>
            <Button variant="ghost" size="icon-sm" aria-label="Favorite workspace">
              <Star className="size-4" />
            </Button>
            <Badge variant="secondary" className="rounded-full">{workspace.plan}</Badge>
          </div>
          <p className="max-w-3xl text-sm leading-6 text-muted-foreground">{workspace.description}</p>
        </div>

        <div className="flex flex-wrap items-center gap-2">
          <Button variant="outline" size="sm" className="bg-card">
            <Sparkles className="size-4" />
            AI suggestions
          </Button>
          <Button variant="outline" size="sm" className="bg-card">
            <Plug className="size-4" />
            Integrate
          </Button>
          <Button variant="outline" size="sm" className="bg-card">
            <Workflow className="size-4" />
            Automate
          </Button>
          <Button variant="outline" size="sm" className="bg-card">
            <Bot className="size-4" />
            Agents
          </Button>
          <Separator orientation="vertical" className="hidden h-7 sm:block" />
          <div className="flex -space-x-2">
            {members.slice(0, 4).map((member) => (
              <Avatar key={member.id} className="size-8 border-2 border-card">
                <AvatarFallback className="text-[10px] text-primary-foreground" style={{ backgroundColor: member.color }}>
                  {member.initials}
                </AvatarFallback>
              </Avatar>
            ))}
          </div>
          <Badge variant="secondary" className="rounded-full">{onlineMembers.length} online</Badge>
          <Button size="sm" className="rounded-full">
            <UserPlus className="size-4" />
            Invite
          </Button>
          <Button variant="outline" size="sm" className="bg-card">
            <Share2 className="size-4" />
            Share
          </Button>
          <Button variant="ghost" size="icon-sm" aria-label="Workspace comments and activity">
            <MessageSquareText className="size-4" />
          </Button>
          <Button variant="ghost" size="icon-sm" aria-label="More workspace actions">
            <MoreHorizontal className="size-4" />
          </Button>
        </div>
      </div>
    </header>
  )
}

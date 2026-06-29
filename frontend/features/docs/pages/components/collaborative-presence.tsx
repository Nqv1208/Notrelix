"use client"

import { Avatar, AvatarFallback, AvatarImage } from "@/components/ui/avatar"
import { Tooltip, TooltipContent, TooltipTrigger } from "@/components/ui/tooltip"
import type { DocsUser } from "../../shared/types/user.types"

interface CollaborativePresenceProps {
  collaborators: DocsUser[]
  activeUserIds: string[]
}

export function CollaborativePresence({ collaborators, activeUserIds }: CollaborativePresenceProps) {
  return (
    <div className="mb-4 flex items-center gap-3">
      <div className="-space-x-2">
        {collaborators.slice(0, 5).map((user) => {
          const active = activeUserIds.includes(user.id)
          return (
            <Tooltip key={user.id}>
              <TooltipTrigger asChild>
                <Avatar className="inline-flex size-8 border-2 border-white">
                  <AvatarImage src={user.avatarUrl ?? undefined} />
                  <AvatarFallback style={{ backgroundColor: user.color, color: "white" }}>
                    {user.name.slice(0, 2).toUpperCase()}
                  </AvatarFallback>
                  {active ? <span className="absolute bottom-0 right-0 size-2 rounded-full bg-emerald-500 ring-2 ring-card" /> : null}
                </Avatar>
              </TooltipTrigger>
              <TooltipContent>{user.name} · {active ? "editing now" : "recent collaborator"}</TooltipContent>
            </Tooltip>
          )
        })}
      </div>
      <p className="text-xs text-muted-foreground">{activeUserIds.length} active · changes saved</p>
    </div>
  )
}

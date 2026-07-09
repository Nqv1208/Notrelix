"use client"

import { useState } from "react"
import { AtSign, Bot, ImageIcon, Paperclip, Send, SmilePlus } from "lucide-react"
import { Avatar, AvatarFallback } from "@notrelix/ui-web"
import { Button } from "@notrelix/ui-web"
import {
  Command,
  CommandEmpty,
  CommandGroup,
  CommandInput,
  CommandItem,
  CommandList,
} from "@notrelix/ui-web"
import { Popover, PopoverContent, PopoverTrigger } from "@notrelix/ui-web"
import { Textarea } from "@notrelix/ui-web"
import { Tooltip, TooltipContent, TooltipTrigger } from "@notrelix/ui-web"
import { useCreateCardUpdate } from "@notrelix/work-management-state"
import type { CardMember } from "@notrelix/work-management-core"

export function UpdateComposer({ cardId, members }: { cardId: string; members: CardMember[] }) {
  const [body, setBody] = useState("")
  const [mentionUserIds, setMentionUserIds] = useState<string[]>([])
  const createUpdate = useCreateCardUpdate(cardId)
  const canSubmit = body.trim().length > 0 && !createUpdate.isPending

  function submit() {
    const nextBody = body.trim()
    if (!nextBody) return
    createUpdate.mutate(
      {
        cardId,
        body: nextBody,
        mentionUserIds,
        attachmentIds: [],
      },
      {
        onSuccess: () => {
          setBody("")
          setMentionUserIds([])
        },
      }
    )
  }

  function toggleMention(userId: string) {
    setMentionUserIds((current) =>
      current.includes(userId) ? current.filter((id) => id !== userId) : [...current, userId]
    )
  }

  return (
    <div className="rounded-lg border border-border bg-card overflow-hidden">
      <Textarea
        value={body}
        onChange={(event: any) => setBody(event.target.value)}
        placeholder="Write an update..."
        className="min-h-16 h-16 resize-none border-0 bg-card p-2.5 text-xs shadow-none focus-visible:ring-0"
        aria-label="Write an update"
      />
      <div className="flex flex-wrap items-center justify-between gap-1.5 border-t border-border px-2 py-1 bg-muted/10">
        <div className="flex items-center gap-0.5">
          <Popover>
            <Tooltip>
              <TooltipTrigger asChild>
                <PopoverTrigger asChild>
                  <Button type="button" variant="ghost" size="icon-sm" className="size-7" aria-label="Mention user">
                    <AtSign className="size-3.5" />
                  </Button>
                </PopoverTrigger>
              </TooltipTrigger>
              <TooltipContent className="text-[10px]">Mention user</TooltipContent>
            </Tooltip>
            <PopoverContent align="start" className="w-56 p-0">
              <Command className="text-xs">
                <CommandInput placeholder="Search people..." className="h-8 text-xs" />
                <CommandList>
                  <CommandEmpty className="text-xs p-2 text-center text-muted-foreground">No people found.</CommandEmpty>
                  <CommandGroup heading="People" className="text-[10px] text-muted-foreground p-1">
                    {members.map((member) => (
                      <CommandItem
                        key={member.userId}
                        value={member.name}
                        data-checked={mentionUserIds.includes(member.userId)}
                        onSelect={() => toggleMention(member.userId)}
                        className="text-xs py-1"
                      >
                        <Avatar className="size-5">
                          <AvatarFallback className="text-[8px] font-bold text-primary-foreground" style={{ backgroundColor: member.color }}>
                            {member.initials}
                          </AvatarFallback>
                        </Avatar>
                        <span>{member.name}</span>
                      </CommandItem>
                    ))}
                  </CommandGroup>
                </CommandList>
              </Command>
            </PopoverContent>
          </Popover>

          <ComposerIcon icon={Paperclip} label="Attach file" />
          <ComposerIcon icon={ImageIcon} label="Add GIF" />
          <ComposerIcon icon={SmilePlus} label="Add emoji" />
          <ComposerIcon icon={Bot} label="AI action" />
        </div>
        <Button type="button" size="sm" className="h-7 px-2.5 text-xs gap-1" disabled={!canSubmit} onClick={submit}>
          <Send className="size-3" />
          Update
        </Button>
      </div>
    </div>
  )
}

function ComposerIcon({ icon: Icon, label }: { icon: typeof Paperclip; label: string }) {
  return (
    <Tooltip>
      <TooltipTrigger asChild>
        <Button type="button" variant="ghost" size="icon-sm" className="size-7" aria-label={label}>
          <Icon className="size-3.5" />
        </Button>
      </TooltipTrigger>
      <TooltipContent className="text-[10px]">{label}</TooltipContent>
    </Tooltip>
  )
}

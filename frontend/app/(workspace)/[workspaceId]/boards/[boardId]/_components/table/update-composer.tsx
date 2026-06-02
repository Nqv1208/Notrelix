"use client"

import { useState } from "react"
import { AtSign, Bot, ImageIcon, Paperclip, Send, SmilePlus } from "lucide-react"
import { Avatar, AvatarFallback } from "@/components/ui/avatar"
import { Button } from "@/components/ui/button"
import {
  Command,
  CommandEmpty,
  CommandGroup,
  CommandInput,
  CommandItem,
  CommandList,
} from "@/components/ui/command"
import { Popover, PopoverContent, PopoverTrigger } from "@/components/ui/popover"
import { Textarea } from "@/components/ui/textarea"
import { Tooltip, TooltipContent, TooltipTrigger } from "@/components/ui/tooltip"
import { useCreateCardUpdate } from "@/features/boards/hooks"
import type { CardMember } from "@/features/boards/types"

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
    <div className="rounded-lg border border-border bg-card">
      <Textarea
        value={body}
        onChange={(event) => setBody(event.target.value)}
        placeholder="Write an update..."
        className="min-h-24 resize-none border-0 bg-card shadow-none focus-visible:ring-0"
        aria-label="Write an update"
      />
      <div className="flex flex-wrap items-center justify-between gap-2 border-t border-border px-3 py-2">
        <div className="flex items-center gap-1">
          <Popover>
            <Tooltip>
              <TooltipTrigger asChild>
                <PopoverTrigger asChild>
                  <Button type="button" variant="ghost" size="icon-sm" aria-label="Mention user">
                    <AtSign className="size-4" />
                  </Button>
                </PopoverTrigger>
              </TooltipTrigger>
              <TooltipContent>Mention user</TooltipContent>
            </Tooltip>
            <PopoverContent align="start" className="w-64 p-0">
              <Command>
                <CommandInput placeholder="Search people..." />
                <CommandList>
                  <CommandEmpty>No people found.</CommandEmpty>
                  <CommandGroup heading="People">
                    {members.map((member) => (
                      <CommandItem
                        key={member.userId}
                        value={member.name}
                        data-checked={mentionUserIds.includes(member.userId)}
                        onSelect={() => toggleMention(member.userId)}
                      >
                        <Avatar className="size-6">
                          <AvatarFallback className="text-[10px] font-semibold text-primary-foreground" style={{ backgroundColor: member.color }}>
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
        <Button type="button" size="sm" disabled={!canSubmit} onClick={submit}>
          <Send className="size-4" />
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
        <Button type="button" variant="ghost" size="icon-sm" aria-label={label}>
          <Icon className="size-4" />
        </Button>
      </TooltipTrigger>
      <TooltipContent>{label}</TooltipContent>
    </Tooltip>
  )
}

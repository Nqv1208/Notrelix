"use client"

import { AtSign } from "lucide-react"
import { Button } from "@/components/ui/button"
import { Command, CommandEmpty, CommandGroup, CommandInput, CommandItem, CommandList } from "@/components/ui/command"
import { Popover, PopoverContent, PopoverTrigger } from "@/components/ui/popover"
import { mockDocsWorkspace } from "@/features/docs/mock/mock-data"

export function MentionMenu({ onMention }: { onMention?: (userId: string) => void }) {
  return (
    <Popover>
      <PopoverTrigger asChild>
        <Button variant="ghost" size="sm" className="rounded-full">
          <AtSign className="size-4" />
          Mention
        </Button>
      </PopoverTrigger>
      <PopoverContent align="start" className="w-72 p-0">
        <Command>
          <CommandInput placeholder="Mention a teammate..." />
          <CommandList>
            <CommandEmpty>No teammate found.</CommandEmpty>
            <CommandGroup heading="People">
              {mockDocsWorkspace.users.map((user) => (
                <CommandItem key={user.id} value={user.name} onSelect={() => onMention?.(user.id)}>
                  <span className="flex size-6 items-center justify-center rounded-full bg-primary text-[10px] font-semibold text-primary-foreground">
                    {user.name.split(" ").map((part) => part[0]).join("").slice(0, 2)}
                  </span>
                  <span>{user.name}</span>
                </CommandItem>
              ))}
            </CommandGroup>
          </CommandList>
        </Command>
      </PopoverContent>
    </Popover>
  )
}

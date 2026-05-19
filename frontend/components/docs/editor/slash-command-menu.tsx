"use client"

import { Command, CommandEmpty, CommandGroup, CommandItem, CommandList } from "@/components/ui/command"
import type { BlockType, SlashCommandItem } from "@/features/docs/types"

export function SlashCommandMenu({
  open,
  items,
  onSelect,
}: {
  open: boolean
  items: SlashCommandItem[]
  onSelect: (type: BlockType) => void
}) {
  if (!open) return null

  return (
    <div className="absolute left-14 top-9 z-50 w-72 overflow-hidden rounded-xl border border-border bg-popover shadow-lg">
      <Command>
        <CommandList>
          <CommandEmpty>No block found.</CommandEmpty>
          <CommandGroup heading="Blocks">
            {items.map((item) => (
              <CommandItem key={item.id} value={item.label} onSelect={() => onSelect(item.type)}>
                <span className="flex size-7 items-center justify-center rounded-lg bg-muted text-xs text-foreground">
                  {item.label.slice(0, 1)}
                </span>
                <span className="min-w-0">
                  <span className="block text-sm font-medium text-foreground">{item.label}</span>
                  <span className="block truncate text-xs text-muted-foreground">{item.description}</span>
                </span>
              </CommandItem>
            ))}
          </CommandGroup>
        </CommandList>
      </Command>
    </div>
  )
}

"use client"

import type { ComponentType } from "react"
import { Code2, Heading1, Heading2, Heading3, List, ListChecks, Pilcrow, Quote, SquareKanban, Table2 } from "lucide-react"
import { Command, CommandGroup, CommandItem, CommandList } from "@/components/ui/command"
import { Popover, PopoverContent, PopoverTrigger } from "@/components/ui/popover"
import { BLOCK_LABELS, SLASH_COMMAND_BLOCKS } from "@/features/docs/constants"
import type { BlockType } from "@/features/docs/types"

const icons: Partial<Record<BlockType, ComponentType<{ className?: string }>>> = {
  paragraph: Pilcrow,
  heading_1: Heading1,
  heading_2: Heading2,
  heading_3: Heading3,
  bulleted_list: List,
  numbered_list: List,
  todo: ListChecks,
  quote: Quote,
  code: Code2,
  table: Table2,
  board_reference: SquareKanban,
}

interface SlashCommandProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  onSelect: (type: BlockType) => void
}

export function SlashCommand({ open, onOpenChange, onSelect }: SlashCommandProps) {
  return (
    <Popover open={open} onOpenChange={onOpenChange}>
      <PopoverTrigger asChild>
        <span className="absolute left-14 top-8" />
      </PopoverTrigger>
      <PopoverContent align="start" className="w-72 p-0">
        <Command>
          <CommandList>
            <CommandGroup heading="Blocks">
              {SLASH_COMMAND_BLOCKS.map((type) => {
                const Icon = icons[type] ?? Pilcrow
                return (
                  <CommandItem key={type} onSelect={() => onSelect(type)}>
                    <Icon className="size-4" />
                    {BLOCK_LABELS[type]}
                  </CommandItem>
                )
              })}
            </CommandGroup>
          </CommandList>
        </Command>
      </PopoverContent>
    </Popover>
  )
}

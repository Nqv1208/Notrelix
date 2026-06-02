"use client"

import type { ComponentType } from "react"
import { Code2, Heading1, Heading2, Heading3, List, ListChecks, Pilcrow, Quote, Rows3, SquareDashedBottomCode } from "lucide-react"
import { Button } from "@/components/ui/button"
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu"
import type { BlockType } from "@/features/docs/types"
import { blockTypeLabels } from "./formatting"

const options: Array<{ type: BlockType; icon: ComponentType<{ className?: string }> }> = [
  { type: "paragraph", icon: Pilcrow },
  { type: "heading_1", icon: Heading1 },
  { type: "heading_2", icon: Heading2 },
  { type: "heading_3", icon: Heading3 },
  { type: "bulleted_list", icon: List },
  { type: "numbered_list", icon: Rows3 },
  { type: "todo", icon: ListChecks },
  { type: "quote", icon: Quote },
  { type: "code", icon: Code2 },
  { type: "callout", icon: SquareDashedBottomCode },
]

export function BlockTypeMenu({
  value,
  onValueChange,
}: {
  value: BlockType
  onValueChange: (type: BlockType) => void
}) {
  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <Button variant="outline" size="sm" className="min-w-[132px] justify-start bg-card">
          {blockTypeLabels[value]}
        </Button>
      </DropdownMenuTrigger>
      <DropdownMenuContent align="start" className="w-56">
        {options.map((option) => (
          <DropdownMenuItem key={option.type} onClick={() => onValueChange(option.type)}>
            <option.icon className="size-4" />
            {blockTypeLabels[option.type]}
          </DropdownMenuItem>
        ))}
      </DropdownMenuContent>
    </DropdownMenu>
  )
}

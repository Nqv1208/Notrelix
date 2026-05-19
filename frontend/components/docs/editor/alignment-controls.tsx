"use client"

import { AlignCenter, AlignLeft, AlignRight } from "lucide-react"
import { ToggleGroup, ToggleGroupItem } from "@/components/ui/toggle-group"
import type { BlockProperties } from "@/features/docs/types"

export function AlignmentControls({
  value,
  onValueChange,
}: {
  value?: BlockProperties["align"]
  onValueChange: (align: NonNullable<BlockProperties["align"]>) => void
}) {
  return (
    <ToggleGroup
      type="single"
      value={value ?? "left"}
      onValueChange={(align) => align && onValueChange(align as NonNullable<BlockProperties["align"]>)}
    >
      <ToggleGroupItem value="left" size="sm" aria-label="Align left">
        <AlignLeft className="size-4" />
      </ToggleGroupItem>
      <ToggleGroupItem value="center" size="sm" aria-label="Align center">
        <AlignCenter className="size-4" />
      </ToggleGroupItem>
      <ToggleGroupItem value="right" size="sm" aria-label="Align right">
        <AlignRight className="size-4" />
      </ToggleGroupItem>
    </ToggleGroup>
  )
}

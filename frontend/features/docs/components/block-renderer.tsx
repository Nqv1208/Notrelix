"use client"

import type { Block } from "../types"

export function BlockRenderer({ block }: { block: Block }) {
  return (
    <div className="rounded-md px-2 py-1 text-sm text-muted-foreground">
      {block.properties.text ?? block.properties.title ?? ""}
    </div>
  )
}

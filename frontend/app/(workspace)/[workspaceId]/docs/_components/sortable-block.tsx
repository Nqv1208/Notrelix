"use client"

import type { Block } from "@/features/docs/types"
import { BlockRenderer } from "./block-renderer"

export function SortableBlock({ block }: { block: Block }) {
  return <BlockRenderer block={block} />
}

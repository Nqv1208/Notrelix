"use client"

import type { BlockType } from "../types"

export function SlashCommandMenu({ onSelect }: { onSelect?: (type: BlockType) => void }) {
  return (
    <button type="button" className="rounded-md border px-2 py-1 text-xs" onClick={() => onSelect?.("paragraph")}>
      Add text block
    </button>
  )
}

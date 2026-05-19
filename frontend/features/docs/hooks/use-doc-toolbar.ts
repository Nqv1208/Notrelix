"use client"

import { useMemo } from "react"
import { useDocsEditorStore } from "../store/editor-store"
import type { Block, BlockProperties, BlockType } from "../types"
import { useCreateBlock } from "./use-create-block"
import { useUpdateBlock } from "./use-update-block"

export function useDocToolbar(pageId: string, blocks: Block[]) {
  const focusedBlockId = useDocsEditorStore((state) => state.focusedBlockId)
  const setFocusedBlockId = useDocsEditorStore((state) => state.setFocusedBlockId)
  const activeBlock = useMemo(
    () => blocks.find((block) => block.id === focusedBlockId) ?? blocks[0],
    [blocks, focusedBlockId]
  )
  const updateBlock = useUpdateBlock(pageId)
  const createBlock = useCreateBlock(pageId)

  function updateProperties(properties: BlockProperties) {
    if (!activeBlock) return
    updateBlock.mutate({ blockId: activeBlock.id, payload: { properties } })
  }

  function setBlockType(type: BlockType) {
    if (!activeBlock) return
    updateBlock.mutate({ blockId: activeBlock.id, payload: { type } })
  }

  function toggleProperty(property: "bold" | "italic" | "underline" | "strike") {
    if (!activeBlock) return
    updateProperties({ [property]: !activeBlock.properties[property] })
  }

  function addBlock(type: BlockType = "paragraph") {
    const position = activeBlock ? activeBlock.position + 0.5 : blocks.length + 1
    createBlock.mutate(
      { type, position, properties: defaultPropertiesFor(type) },
      {
        onSuccess: (block) => setFocusedBlockId(block.id),
      }
    )
  }

  return {
    activeBlock,
    activeBlockId: activeBlock?.id ?? null,
    activeBlockType: activeBlock?.type ?? "paragraph",
    properties: activeBlock?.properties ?? {},
    isPending: updateBlock.isPending || createBlock.isPending,
    addBlock,
    setBlockType,
    updateProperties,
    toggleProperty,
  }
}

function defaultPropertiesFor(type: BlockType): BlockProperties {
  if (type === "todo") return { text: "", checked: false }
  if (type === "callout") return { text: "", icon: "i", highlight: "accent" }
  if (type === "code") return { text: "", language: "tsx", fontFamily: "mono" }
  return { text: "" }
}

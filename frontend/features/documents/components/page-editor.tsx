"use client"

import * as React from "react"
import {
  DndContext,
  closestCenter,
  KeyboardSensor,
  PointerSensor,
  useSensor,
  useSensors,
  type DragEndEvent,
  type DragStartEvent,
  DragOverlay,
} from "@dnd-kit/core"
import {
  SortableContext,
  sortableKeyboardCoordinates,
  verticalListSortingStrategy,
} from "@dnd-kit/sortable"
import { restrictToVerticalAxis } from "@dnd-kit/modifiers"
import { useEditorStore } from "../store/editor-store"
import { SortableBlock } from "./sortable-block"
import { BlockRenderer } from "./block-renderer"
import { SlashCommandMenu } from "./slash-command-menu"
import { PageTitle } from "./page-title"
import type { BlockType } from "../types/document.types"

type PageEditorProps = {
  workspaceId: string
  pageId: string
}

export function PageEditor({ workspaceId, pageId }: PageEditorProps) {
  const {
    blocks,
    setCurrentPage,
    moveBlock,
    addBlock,
    updateBlock,
    setIsDragging,
    focusedBlockId,
  } = useEditorStore()

  const [slashMenu, setSlashMenu] = React.useState<{
    blockId: string
    position: { top: number; left: number }
    query: string
  } | null>(null)

  const [activeId, setActiveId] = React.useState<string | null>(null)
  const editorRef = React.useRef<HTMLDivElement>(null)

  React.useEffect(() => {
    setCurrentPage(pageId)
  }, [pageId, setCurrentPage])

  const sensors = useSensors(
    useSensor(PointerSensor, {
      activationConstraint: { distance: 8 },
    }),
    useSensor(KeyboardSensor, {
      coordinateGetter: sortableKeyboardCoordinates,
    })
  )

  const handleDragStart = React.useCallback(
    (event: DragStartEvent) => {
      setActiveId(event.active.id as string)
      setIsDragging(true)
    },
    [setIsDragging]
  )

  const handleDragEnd = React.useCallback(
    (event: DragEndEvent) => {
      const { active, over } = event
      setActiveId(null)
      setIsDragging(false)

      if (over && active.id !== over.id) {
        moveBlock(active.id as string, over.id as string)
      }
    },
    [moveBlock, setIsDragging]
  )

  React.useEffect(() => {
    function handleInput(e: Event) {
      const target = e.target as HTMLElement
      if (!target.isContentEditable) return

      const text = target.textContent ?? ""
      const blockEl = target.closest("[data-block-id]")
      const blockId = blockEl?.getAttribute("data-block-id")

      if (!blockId) return

      if (text.startsWith("/")) {
        const rect = target.getBoundingClientRect()
        setSlashMenu({
          blockId,
          position: {
            top: rect.bottom + 4,
            left: rect.left,
          },
          query: text.slice(1),
        })
      } else if (slashMenu?.blockId === blockId) {
        setSlashMenu(null)
      }
    }

    const editor = editorRef.current
    if (editor) {
      editor.addEventListener("input", handleInput)
      return () => editor.removeEventListener("input", handleInput)
    }
  }, [slashMenu])

  const handleSlashSelect = React.useCallback(
    (type: BlockType) => {
      if (!slashMenu) return

      updateBlock(slashMenu.blockId, { content: "", type })

      if (type === "to_do") {
        updateBlock(slashMenu.blockId, {
          content: "",
          type,
          properties: { checked: false },
        })
      } else if (type === "callout") {
        updateBlock(slashMenu.blockId, {
          content: "",
          type,
          properties: { emoji: "💡" },
        })
      } else if (type === "code") {
        updateBlock(slashMenu.blockId, {
          content: "",
          type,
          properties: { language: "typescript" },
        })
      }

      setSlashMenu(null)
    },
    [slashMenu, updateBlock]
  )

  const handleEditorClick = React.useCallback(
    (e: React.MouseEvent) => {
      if (e.target === editorRef.current) {
        const lastBlock = blocks[blocks.length - 1]
        if (lastBlock && lastBlock.content === "" && lastBlock.type === "paragraph") {
          const { setFocusedBlock } = useEditorStore.getState()
          setFocusedBlock(lastBlock.id)
        } else {
          addBlock("paragraph")
        }
      }
    },
    [blocks, addBlock]
  )

  const activeBlock = activeId
    ? blocks.find((b) => b.id === activeId)
    : null

  return (
    <div className="flex-1 flex flex-col min-h-0">
      <div
        ref={editorRef}
        className="flex-1 overflow-y-auto px-4 py-8 cursor-text"
        onClick={handleEditorClick}
      >
        <div className="max-w-3xl mx-auto">
          <PageTitle pageId={pageId} />

          <DndContext
            sensors={sensors}
            collisionDetection={closestCenter}
            onDragStart={handleDragStart}
            onDragEnd={handleDragEnd}
            modifiers={[restrictToVerticalAxis]}
          >
            <SortableContext
              items={blocks.map((b) => b.id)}
              strategy={verticalListSortingStrategy}
            >
              <div className="space-y-0.5">
                {blocks.map((block) => (
                  <div key={block.id} data-block-id={block.id}>
                    <SortableBlock block={block} />
                  </div>
                ))}
              </div>
            </SortableContext>

            <DragOverlay>
              {activeBlock ? (
                <div className="bg-background/90 backdrop-blur-sm rounded-lg border shadow-xl px-4 py-2 max-w-3xl">
                  <BlockRenderer block={activeBlock} />
                </div>
              ) : null}
            </DragOverlay>
          </DndContext>

          <div className="h-[40vh]" />
        </div>
      </div>

      {slashMenu && (
        <SlashCommandMenu
          position={slashMenu.position}
          query={slashMenu.query}
          onSelect={handleSlashSelect}
          onClose={() => setSlashMenu(null)}
        />
      )}
    </div>
  )
}

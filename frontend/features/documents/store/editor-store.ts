import { create } from "zustand"
import type { Block, BlockType, Page, Workspace } from "../types/document.types"
import { sampleWorkspaces, getSampleBlocks } from "../data/sample-data"

function generateId() {
  return `b-${Date.now()}-${Math.random().toString(36).slice(2, 8)}`
}

function generatePageId() {
  return `p-${Date.now()}-${Math.random().toString(36).slice(2, 8)}`
}

function createEmptyBlock(type: BlockType, position: number): Block {
  const now = new Date().toISOString()
  return {
    id: generateId(),
    type,
    content: "",
    properties: type === "to_do" ? { checked: false } : type === "callout" ? { emoji: "💡" } : type === "code" ? { language: "typescript" } : undefined,
    position,
    createdAt: now,
    updatedAt: now,
  }
}

type EditorState = {
  workspaces: Workspace[]
  currentPageId: string | null
  blocks: Block[]
  focusedBlockId: string | null
  selectedBlockIds: Set<string>
  isDragging: boolean

  setCurrentPage: (pageId: string) => void
  setBlocks: (blocks: Block[]) => void
  addBlock: (type: BlockType, afterBlockId?: string) => string
  updateBlock: (blockId: string, updates: Partial<Block>) => void
  deleteBlock: (blockId: string) => void
  moveBlock: (activeId: string, overId: string) => void
  duplicateBlock: (blockId: string) => void
  setFocusedBlock: (blockId: string | null) => void
  setSelectedBlocks: (ids: Set<string>) => void
  setIsDragging: (dragging: boolean) => void
  toggleTodo: (blockId: string) => void
  turnInto: (blockId: string, type: BlockType) => void

  addPage: (workspaceId: string, parentId?: string | null) => string
  updatePage: (pageId: string, updates: Partial<Page>) => void
  deletePage: (pageId: string) => void
  toggleFavorite: (pageId: string) => void
  getPage: (pageId: string) => Page | undefined
  getFavoritePages: () => Page[]
}

function findPageInWorkspaces(
  workspaces: Workspace[],
  pageId: string
): Page | undefined {
  for (const ws of workspaces) {
    const found = findPageRecursive(ws.pages, pageId)
    if (found) return found
  }
  return undefined
}

function findPageRecursive(pages: Page[], pageId: string): Page | undefined {
  for (const page of pages) {
    if (page.id === pageId) return page
    if (page.children) {
      const found = findPageRecursive(page.children, pageId)
      if (found) return found
    }
  }
  return undefined
}

function updatePageRecursive(
  pages: Page[],
  pageId: string,
  updates: Partial<Page>
): Page[] {
  return pages.map((page) => {
    if (page.id === pageId) {
      return { ...page, ...updates, updatedAt: new Date().toISOString() }
    }
    if (page.children) {
      return {
        ...page,
        children: updatePageRecursive(page.children, pageId, updates),
      }
    }
    return page
  })
}

function deletePageRecursive(pages: Page[], pageId: string): Page[] {
  return pages
    .filter((page) => page.id !== pageId)
    .map((page) => ({
      ...page,
      children: page.children
        ? deletePageRecursive(page.children, pageId)
        : undefined,
    }))
}

function collectFavorites(pages: Page[]): Page[] {
  const result: Page[] = []
  for (const page of pages) {
    if (page.isFavorite && !page.isDeleted) result.push(page)
    if (page.children) result.push(...collectFavorites(page.children))
  }
  return result
}

export const useEditorStore = create<EditorState>((set, get) => ({
  workspaces: sampleWorkspaces,
  currentPageId: null,
  blocks: [],
  focusedBlockId: null,
  selectedBlockIds: new Set(),
  isDragging: false,

  setCurrentPage: (pageId) => {
    const blocks = getSampleBlocks(pageId)
    set({ currentPageId: pageId, blocks, focusedBlockId: null })
  },

  setBlocks: (blocks) => set({ blocks }),

  addBlock: (type, afterBlockId) => {
    const { blocks } = get()
    let insertIndex = blocks.length

    if (afterBlockId) {
      const idx = blocks.findIndex((b) => b.id === afterBlockId)
      if (idx !== -1) insertIndex = idx + 1
    }

    const newBlock = createEmptyBlock(type, insertIndex)
    const updatedBlocks = [...blocks]
    updatedBlocks.splice(insertIndex, 0, newBlock)

    const reindexed = updatedBlocks.map((b, i) => ({
      ...b,
      position: i + 1,
    }))

    set({ blocks: reindexed, focusedBlockId: newBlock.id })
    return newBlock.id
  },

  updateBlock: (blockId, updates) => {
    set((state) => ({
      blocks: state.blocks.map((b) =>
        b.id === blockId
          ? { ...b, ...updates, updatedAt: new Date().toISOString() }
          : b
      ),
    }))
  },

  deleteBlock: (blockId) => {
    const { blocks } = get()
    if (blocks.length <= 1) return

    const idx = blocks.findIndex((b) => b.id === blockId)
    const filtered = blocks.filter((b) => b.id !== blockId)
    const reindexed = filtered.map((b, i) => ({ ...b, position: i + 1 }))
    const focusIdx = Math.max(0, idx - 1)

    set({
      blocks: reindexed,
      focusedBlockId: reindexed[focusIdx]?.id ?? null,
    })
  },

  moveBlock: (activeId, overId) => {
    const { blocks } = get()
    const oldIndex = blocks.findIndex((b) => b.id === activeId)
    const newIndex = blocks.findIndex((b) => b.id === overId)

    if (oldIndex === -1 || newIndex === -1 || oldIndex === newIndex) return

    const reordered = [...blocks]
    const [moved] = reordered.splice(oldIndex, 1)
    reordered.splice(newIndex, 0, moved)

    const reindexed = reordered.map((b, i) => ({ ...b, position: i + 1 }))
    set({ blocks: reindexed })
  },

  duplicateBlock: (blockId) => {
    const { blocks } = get()
    const idx = blocks.findIndex((b) => b.id === blockId)
    if (idx === -1) return

    const original = blocks[idx]
    const duplicate: Block = {
      ...original,
      id: generateId(),
      createdAt: new Date().toISOString(),
      updatedAt: new Date().toISOString(),
    }

    const updated = [...blocks]
    updated.splice(idx + 1, 0, duplicate)
    const reindexed = updated.map((b, i) => ({ ...b, position: i + 1 }))

    set({ blocks: reindexed, focusedBlockId: duplicate.id })
  },

  setFocusedBlock: (blockId) => set({ focusedBlockId: blockId }),

  setSelectedBlocks: (ids) => set({ selectedBlockIds: ids }),

  setIsDragging: (dragging) => set({ isDragging: dragging }),

  toggleTodo: (blockId) => {
    set((state) => ({
      blocks: state.blocks.map((b) =>
        b.id === blockId
          ? {
              ...b,
              properties: {
                ...b.properties,
                checked: !b.properties?.checked,
              },
              updatedAt: new Date().toISOString(),
            }
          : b
      ),
    }))
  },

  turnInto: (blockId, type) => {
    set((state) => ({
      blocks: state.blocks.map((b) =>
        b.id === blockId
          ? {
              ...b,
              type,
              properties:
                type === "to_do"
                  ? { checked: false }
                  : type === "callout"
                    ? { emoji: "💡" }
                    : type === "code"
                      ? { language: "typescript" }
                      : undefined,
              updatedAt: new Date().toISOString(),
            }
          : b
      ),
    }))
  },

  addPage: (workspaceId, parentId = null) => {
    const newPageId = generatePageId()
    const now = new Date().toISOString()
    const newPage: Page = {
      id: newPageId,
      workspaceId,
      parentId,
      title: "Untitled",
      icon: "📄",
      position: 999,
      isDeleted: false,
      isFavorite: false,
      createdAt: now,
      updatedAt: now,
    }

    set((state) => ({
      workspaces: state.workspaces.map((ws) => {
        if (ws.id !== workspaceId) return ws

        if (parentId) {
          return {
            ...ws,
            pages: addChildPage(ws.pages, parentId, newPage),
          }
        }

        return { ...ws, pages: [...ws.pages, newPage] }
      }),
    }))

    return newPageId
  },

  updatePage: (pageId, updates) => {
    set((state) => ({
      workspaces: state.workspaces.map((ws) => ({
        ...ws,
        pages: updatePageRecursive(ws.pages, pageId, updates),
      })),
    }))
  },

  deletePage: (pageId) => {
    set((state) => ({
      workspaces: state.workspaces.map((ws) => ({
        ...ws,
        pages: deletePageRecursive(ws.pages, pageId),
      })),
    }))
  },

  toggleFavorite: (pageId) => {
    const page = findPageInWorkspaces(get().workspaces, pageId)
    if (!page) return
    get().updatePage(pageId, { isFavorite: !page.isFavorite })
  },

  getPage: (pageId) => findPageInWorkspaces(get().workspaces, pageId),

  getFavoritePages: () => {
    const { workspaces } = get()
    return workspaces.flatMap((ws) => collectFavorites(ws.pages))
  },
}))

function addChildPage(
  pages: Page[],
  parentId: string,
  newPage: Page
): Page[] {
  return pages.map((page) => {
    if (page.id === parentId) {
      return {
        ...page,
        children: [...(page.children ?? []), newPage],
      }
    }
    if (page.children) {
      return {
        ...page,
        children: addChildPage(page.children, parentId, newPage),
      }
    }
    return page
  })
}

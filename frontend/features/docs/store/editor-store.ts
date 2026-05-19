import { create } from "zustand"
import type { BlockType, ID } from "../types"
import { sampleWorkspaces } from "../data/sample-data"
import type { Page, Workspace } from "../types/document.types"

type EditorMode = "read" | "edit" | "comment"

interface DocsEditorState {
  sidebarOpen: boolean
  mobileSidebarOpen: boolean
  activePageId: ID | null
  focusedBlockId: ID | null
  selectedBlockIds: ID[]
  slashCommandOpen: boolean
  slashCommandQuery: string
  editorMode: EditorMode
  pendingBlockType: BlockType | null
  commentsOpen: boolean
  activityOpen: boolean
  searchOpen: boolean
  setSidebarOpen: (open: boolean) => void
  setMobileSidebarOpen: (open: boolean) => void
  setActivePageId: (pageId: ID | null) => void
  setFocusedBlockId: (blockId: ID | null) => void
  setSelectedBlockIds: (blockIds: ID[]) => void
  setSlashCommand: (open: boolean, query?: string) => void
  setEditorMode: (mode: EditorMode) => void
  setPendingBlockType: (type: BlockType | null) => void
  setCommentsOpen: (open: boolean) => void
  setActivityOpen: (open: boolean) => void
  setSearchOpen: (open: boolean) => void
}

export const useDocsEditorStore = create<DocsEditorState>((set) => ({
  sidebarOpen: true,
  mobileSidebarOpen: false,
  activePageId: null,
  focusedBlockId: null,
  selectedBlockIds: [],
  slashCommandOpen: false,
  slashCommandQuery: "",
  editorMode: "edit",
  pendingBlockType: null,
  commentsOpen: true,
  activityOpen: false,
  searchOpen: false,
  setSidebarOpen: (sidebarOpen) => set({ sidebarOpen }),
  setMobileSidebarOpen: (mobileSidebarOpen) => set({ mobileSidebarOpen }),
  setActivePageId: (activePageId) => set({ activePageId }),
  setFocusedBlockId: (focusedBlockId) => set({ focusedBlockId }),
  setSelectedBlockIds: (selectedBlockIds) => set({ selectedBlockIds }),
  setSlashCommand: (slashCommandOpen, slashCommandQuery = "") =>
    set({ slashCommandOpen, slashCommandQuery }),
  setEditorMode: (editorMode) => set({ editorMode }),
  setPendingBlockType: (pendingBlockType) => set({ pendingBlockType }),
  setCommentsOpen: (commentsOpen) => set({ commentsOpen }),
  setActivityOpen: (activityOpen) => set({ activityOpen }),
  setSearchOpen: (searchOpen) => set({ searchOpen }),
}))

interface LegacyEditorState {
  workspaces: Workspace[]
  addPage: (workspaceId: string, parentId?: string | null) => string
  getFavoritePages: () => Page[]
  getPage: (pageId: string) => Page | undefined
  toggleFavorite: (pageId: string) => void
}

function collectFavorites(pages: Page[]): Page[] {
  return pages.flatMap((page) => [
    ...(page.isFavorite && !page.isDeleted ? [page] : []),
    ...(page.children ? collectFavorites(page.children) : []),
  ])
}

function findPage(pages: Page[], pageId: string): Page | undefined {
  for (const page of pages) {
    if (page.id === pageId) return page
    const child = page.children ? findPage(page.children, pageId) : undefined
    if (child) return child
  }
  return undefined
}

function toggleFavoriteInPages(pages: Page[], pageId: string): Page[] {
  return pages.map((page) => ({
    ...page,
    isFavorite: page.id === pageId ? !page.isFavorite : page.isFavorite,
    children: page.children ? toggleFavoriteInPages(page.children, pageId) : page.children,
  }))
}

export const useEditorStore = create<LegacyEditorState>((set, get) => ({
  workspaces: sampleWorkspaces,
  addPage: (workspaceId, parentId = null) => {
    const newId = `page-${Date.now()}`
    const timestamp = new Date().toISOString()
    const newPage: Page = {
      id: newId,
      workspaceId,
      parentId,
      title: "Untitled",
      icon: "📄",
      position: 999,
      isDeleted: false,
      isFavorite: false,
      createdAt: timestamp,
      updatedAt: timestamp,
    }
    set((state) => ({
      workspaces: state.workspaces.map((workspace) =>
        workspace.id === workspaceId
          ? { ...workspace, pages: [newPage, ...workspace.pages] }
          : workspace
      ),
    }))
    return newId
  },
  getFavoritePages: () => get().workspaces.flatMap((workspace) => collectFavorites(workspace.pages)),
  getPage: (pageId) => {
    for (const workspace of get().workspaces) {
      const page = findPage(workspace.pages, pageId)
      if (page) return page
    }
    return undefined
  },
  toggleFavorite: (pageId) => {
    set((state) => ({
      workspaces: state.workspaces.map((workspace) => ({
        ...workspace,
        pages: toggleFavoriteInPages(workspace.pages, pageId),
      })),
    }))
  },
}))

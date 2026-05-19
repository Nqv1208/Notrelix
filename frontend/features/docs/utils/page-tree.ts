import type { Page, PageTreeNode } from "../types"

export function buildPageTree(pages: Page[], parentId: string | null = null, depth = 0): PageTreeNode[] {
  return pages
    .filter((page) => page.parentId === parentId)
    .sort((a, b) => a.position - b.position)
    .map((page) => ({
      ...page,
      depth,
      children: buildPageTree(pages, page.id, depth + 1),
    }))
}

export function flattenPageTree(nodes: PageTreeNode[]): PageTreeNode[] {
  return nodes.flatMap((node) => [node, ...flattenPageTree(node.children)])
}

export function getBreadcrumb(pages: Page[], pageId: string) {
  const byId = new Map(pages.map((page) => [page.id, page]))
  const result = []
  let current = byId.get(pageId)

  while (current) {
    result.unshift({ id: current.id, title: current.title, icon: current.icon })
    current = current.parentId ? byId.get(current.parentId) : undefined
  }

  return result
}

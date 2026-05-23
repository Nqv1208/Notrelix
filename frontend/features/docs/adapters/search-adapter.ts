import type { Block, LinkedBoard, LinkedTask, Page, SearchResult } from "../types"

function normalize(value: string) {
  return value.toLowerCase().trim()
}

export function createSearchResults(input: {
  query: string
  pages: Page[]
  blocks: Record<string, Block[]>
  tasks: LinkedTask[]
  boards: LinkedBoard[]
}): SearchResult[] {
  const query = normalize(input.query)
  if (!query) return []

  const results: SearchResult[] = []

  for (const page of input.pages) {
    const haystack = normalize(`${page.title} ${page.tags.join(" ")}`)
    if (haystack.includes(query)) {
      results.push({
        id: page.id,
        type: "page",
        title: page.title,
        excerpt: page.tags.length ? page.tags.join(" · ") : "Workspace page",
        icon: page.icon,
        score: page.title.toLowerCase().startsWith(query) ? 1 : 0.7,
        group: "Pages",
      })
    }
  }

  for (const [pageId, blocks] of Object.entries(input.blocks)) {
    for (const block of blocks) {
      const text = block.properties.text ?? block.properties.title ?? ""
      if (normalize(text).includes(query)) {
        results.push({
          id: block.id,
          type: "block",
          title: text.slice(0, 80) || "Untitled block",
          excerpt: `Block in ${input.pages.find((page) => page.id === pageId)?.title ?? "page"}`,
          icon: "¶",
          pageId,
          score: 0.55,
          group: "Blocks",
        })
      }
    }
  }

  for (const task of input.tasks) {
    if (normalize(task.title).includes(query)) {
      results.push({
        id: task.id,
        type: "task",
        title: task.title,
        excerpt: `${task.status.replace("_", " ")} · linked task`,
        icon: "☑",
        score: 0.5,
        group: "Tasks",
      })
    }
  }

  for (const board of input.boards) {
    if (normalize(board.name).includes(query)) {
      results.push({
        id: board.id,
        type: "board",
        title: board.name,
        excerpt: `${board.openTasks} open · ${board.doneTasks} done`,
        icon: "▦",
        score: 0.45,
        group: "Boards",
      })
    }
  }

  return results.sort((a, b) => b.score - a.score)
}

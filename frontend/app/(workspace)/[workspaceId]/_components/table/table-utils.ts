import type { BoardTableColumn, Card } from "@/features/boards/types"

export function getMainTableGridTemplate(columns: BoardTableColumn[]) {
  return `64px ${columns.map((column) => `${column.width}px`).join(" ")} 48px`
}

export function getOptionToneClass(id?: string) {
  if (!id) return "border-border bg-muted text-muted-foreground"
  if (id.includes("stuck") || id.includes("urgent")) return "border-destructive/30 bg-destructive/10 text-foreground"
  if (id.includes("working") || id.includes("high") || id.includes("completed")) return "border-primary/30 bg-primary/10 text-foreground"
  if (id.includes("done") || id.includes("medium")) return "border-accent bg-accent text-foreground"
  return "border-border bg-muted text-muted-foreground"
}

export function getGroupToneClass(title: string) {
  const normalized = title.toLowerCase()
  if (normalized.includes("stuck")) return "bg-destructive"
  if (normalized.includes("working") || normalized.includes("completed")) return "bg-primary"
  if (normalized.includes("done")) return "bg-accent"
  return "bg-muted"
}

export function getChecklistProgress(card: Card) {
  const total = card.checklists.reduce((count, checklist) => count + checklist.items.length, 0)
  const done = card.checklists.reduce((count, checklist) => count + checklist.items.filter((item) => item.isDone).length, 0)
  return total === 0 ? 0 : Math.round((done / total) * 100)
}

export function formatDate(value?: string) {
  const date = parseDateValue(value)
  if (!date) return "No date"
  return new Intl.DateTimeFormat("en", { month: "short", day: "numeric" }).format(date)
}

export function toDateInputValue(value?: string) {
  const date = parseDateValue(value)
  if (!date) return ""
  return date.toISOString().slice(0, 10)
}

function parseDateValue(value?: string) {
  if (!value) return null
  const date = new Date(value)
  return Number.isNaN(date.getTime()) ? null : date
}

import type { BoardTableColumn, Card } from "@/features/boards/types"

export function getMainTableGridTemplate(columns: BoardTableColumn[]) {
  return `64px ${columns.map((column) => `${column.width}px`).join(" ")} 48px`
}

export function getOptionToneClass(id?: string) {
  if (!id) return "bg-[var(--badge-default-bg)] text-[var(--badge-default-text)]"
  const lower = id.toLowerCase()
  if (lower.includes("done") || lower.includes("completed")) return "bg-[var(--badge-done-bg)] text-[var(--badge-done-text)]"
  if (lower.includes("stuck")) return "bg-[var(--badge-stuck-bg)] text-[var(--badge-stuck-text)]"
  if (lower.includes("working")) return "bg-[var(--badge-working-bg)] text-[var(--badge-working-text)]"
  if (lower.includes("urgent")) return "bg-[var(--badge-urgent-bg)] text-[var(--badge-urgent-text)]"
  if (lower.includes("high")) return "bg-[var(--badge-high-bg)] text-[var(--badge-high-text)]"
  if (lower.includes("medium")) return "bg-[var(--badge-medium-bg)] text-[var(--badge-medium-text)]"
  if (lower.includes("low")) return "bg-[var(--badge-low-bg)] text-[var(--badge-low-text)]"
  return "bg-[var(--badge-default-bg)] text-[var(--badge-default-text)]"
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

const dateFormatter = new Intl.DateTimeFormat("en", { month: "short", day: "numeric" })

export function formatDate(value?: string) {
  const date = parseDateValue(value)
  if (!date) return "No date"
  return dateFormatter.format(date)
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

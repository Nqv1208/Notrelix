"use client"

import { useMemo } from "react"
import { CalendarDays } from "lucide-react"
import { Badge } from "@/components/ui/badge"
import type { BoardGroup, Card } from "@/features/work-management/types"

type CalendarCard = Card & { groupTitle: string }

export function BoardCalendarView({ groups }: { groups: BoardGroup[] }) {
  const currentWeekDays = useMemo(() => {
    const today = new Date()
    const dayOfWeek = today.getDay()
    const mondayOffset = dayOfWeek === 0 ? -6 : 1 - dayOfWeek
    const monday = new Date(today)
    monday.setDate(today.getDate() + mondayOffset)

    return Array.from({ length: 5 }).map((_, index) => {
      const date = new Date(monday)
      date.setDate(monday.getDate() + index)
      return date
    })
  }, [])

  const cards = useMemo<CalendarCard[]>(() => {
    return groups.flatMap((group) => group.cards.map((card) => ({ ...card, groupTitle: group.title })))
  }, [groups])

  const cardsByDay = useMemo(() => {
    const result: Record<number, CalendarCard[]> = { 0: [], 1: [], 2: [], 3: [], 4: [] }
    let fallbackIndex = 0

    cards.forEach((card) => {
      let placed = false
      if (card.dueDate) {
        const cardDate = new Date(card.dueDate)
        if (!Number.isNaN(cardDate.getTime())) {
          currentWeekDays.forEach((day, index) => {
            if (cardDate.toDateString() === day.toDateString()) {
              result[index].push(card)
              placed = true
            }
          })
        }
      }

      if (!placed) {
        result[fallbackIndex % 5].push(card)
        fallbackIndex++
      }
    })

    return result
  }, [cards, currentWeekDays])

  return (
    <section className="overflow-hidden rounded-2xl border border-border bg-card shadow-sm">
      <div className="flex items-center justify-between border-b border-border px-4 py-3">
        <div>
          <h2 className="text-sm font-semibold text-foreground">Workspace calendar</h2>
          <p className="text-xs text-muted-foreground">Unified deadlines from board cards and linked docs.</p>
        </div>
        <Badge variant="secondary" className="rounded-full">{cards.length} scheduled</Badge>
      </div>
      <div className="grid min-w-[760px] grid-cols-5">
        {currentWeekDays.map((day, index) => {
          const dayLabel = day.toLocaleDateString("en-US", { weekday: "short", day: "numeric" })
          return (
            <div key={day.toISOString()} className="min-h-[520px] border-r border-border p-3 last:border-r-0">
              <div className="mb-3 flex items-center gap-2 text-xs font-semibold uppercase tracking-[0.06em] text-muted-foreground">
                <CalendarDays className="size-3.5" />
                {dayLabel}
              </div>
              <div className="space-y-2">
                {cardsByDay[index].map((card) => (
                  <BoardCalendarCard key={card.id} card={card} />
                ))}
              </div>
            </div>
          )
        })}
      </div>
    </section>
  )
}

function BoardCalendarCard({ card }: { card: CalendarCard }) {
  return (
    <div className="rounded-xl border border-border bg-muted p-3">
      <p className="line-clamp-2 text-sm font-medium text-foreground">{card.title}</p>
      <div className="mt-3 flex items-center justify-between gap-2">
        <Badge variant="secondary" className="rounded-full">{card.groupTitle}</Badge>
        <span className="text-xs text-muted-foreground">{card.dueDate?.slice(5, 10)}</span>
      </div>
    </div>
  )
}

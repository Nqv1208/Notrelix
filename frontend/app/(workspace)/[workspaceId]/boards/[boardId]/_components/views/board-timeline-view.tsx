"use client"

import { useCallback, useMemo } from "react"
import type { BoardGroup, Card } from "@/features/boards/types"

type TimelineCard = Card & { groupTitle: string }

export function BoardTimelineView({ groups }: { groups: BoardGroup[] }) {
  const { timelineStart, timelineEnd, weeks } = useMemo(() => {
    const today = new Date()
    const dayOfWeek = today.getDay()
    const mondayOffset = dayOfWeek === 0 ? -6 : 1 - dayOfWeek
    const timelineStart = new Date(today)
    timelineStart.setDate(today.getDate() + mondayOffset)
    timelineStart.setHours(0, 0, 0, 0)

    const timelineEnd = new Date(timelineStart)
    timelineEnd.setDate(timelineStart.getDate() + 42)

    const weeks = Array.from({ length: 6 }).map((_, index) => `Week ${index + 1}`)
    return { timelineStart, timelineEnd, weeks }
  }, [])

  const cards = useMemo<TimelineCard[]>(() => {
    return groups.flatMap((group) => group.cards.map((card) => ({ ...card, groupTitle: group.title }))).slice(0, 10)
  }, [groups])

  const getTimelineBarStyles = useCallback((card: TimelineCard, index: number) => {
    const start = new Date(card.startDate || new Date())
    const end = new Date(card.dueDate || new Date())

    if (Number.isNaN(start.getTime()) || Number.isNaN(end.getTime()) || start > end) {
      const width = 20 + (index % 4) * 15
      const margin = (index % 3) * 10
      return { width: `${width}%`, marginLeft: `${margin}%` }
    }

    const totalDuration = timelineEnd.getTime() - timelineStart.getTime()
    const cardStartOffset = start.getTime() - timelineStart.getTime()
    const cardDuration = end.getTime() - start.getTime()

    const marginLeftPct = Math.max(0, Math.min(100, (cardStartOffset / totalDuration) * 100))
    const widthPct = Math.max(5, Math.min(100 - marginLeftPct, (cardDuration / totalDuration) * 100))

    return {
      width: `${widthPct}%`,
      marginLeft: `${marginLeftPct}%`,
    }
  }, [timelineEnd, timelineStart])

  return (
    <section className="overflow-hidden rounded-2xl border border-border bg-card shadow-sm">
      <div className="grid min-w-[900px] grid-cols-[260px_repeat(6,minmax(96px,1fr))] border-b border-border bg-muted px-4 py-3 text-xs font-semibold uppercase tracking-[0.06em] text-muted-foreground">
        <div>Item</div>
        {weeks.map((week) => <div key={week}>{week}</div>)}
      </div>
      <div className="min-w-[900px]">
        {cards.map((card, index) => {
          const barStyle = getTimelineBarStyles(card, index)
          return (
            <div key={card.id} className="grid min-h-14 grid-cols-[260px_repeat(6,minmax(96px,1fr))] items-center border-b border-border px-4 last:border-b-0">
              <div className="min-w-0 pr-4">
                <p className="truncate text-sm font-medium text-foreground">{card.title}</p>
                <p className="text-xs text-muted-foreground">{card.groupTitle}</p>
              </div>
              <div className="col-span-6 h-3 rounded-full bg-muted">
                <div className="h-3 rounded-full bg-primary" style={barStyle} />
              </div>
            </div>
          )
        })}
      </div>
    </section>
  )
}

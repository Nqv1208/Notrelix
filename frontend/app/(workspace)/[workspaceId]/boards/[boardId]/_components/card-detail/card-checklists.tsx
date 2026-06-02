import { CheckCircle2, Circle } from "lucide-react"
import { Progress } from "@/components/ui/progress"
import type { Card } from "@/features/boards/types"

export function CardChecklists({ card }: { card: Card }) {
  return (
    <section className="rounded-2xl border border-border bg-card p-5">
      <h2 className="mb-4 text-sm font-semibold text-foreground">Checklists</h2>
      <div className="space-y-4">
        {card.checklists.map((checklist) => {
          const done = checklist.items.filter((item) => item.isDone).length
          const progress = checklist.items.length === 0 ? 0 : Math.round((done / checklist.items.length) * 100)
          return (
            <div key={checklist.id} className="rounded-xl border border-border bg-muted/40 p-4">
              <div className="mb-3 flex items-center justify-between gap-3">
                <h3 className="text-sm font-medium text-foreground">{checklist.title}</h3>
                <span className="text-xs text-muted-foreground">{progress}%</span>
              </div>
              <Progress value={progress} className="mb-3 h-2" />
              <div className="space-y-2">
                {checklist.items.map((item) => (
                  <div key={item.id} className="flex items-center gap-2 text-sm">
                    {item.isDone ? <CheckCircle2 className="size-4 text-emerald-500" /> : <Circle className="size-4 text-muted-foreground" />}
                    <span className={item.isDone ? "text-muted-foreground line-through" : "text-foreground"}>{item.title}</span>
                  </div>
                ))}
              </div>
            </div>
          )
        })}
      </div>
    </section>
  )
}

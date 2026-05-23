import { Filter, Plus, Search, SlidersHorizontal, Users } from "lucide-react"
import { Avatar, AvatarFallback } from "@/components/ui/avatar"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import type { Board } from "@/features/boards/types"

export function BoardToolbar({ board }: { board: Board }) {
  return (
    <section className="mb-5 rounded-2xl border border-border bg-card p-5 shadow-sm">
      <div className="flex flex-col gap-4 lg:flex-row lg:items-end lg:justify-between">
        <div className="min-w-0">
          <div className="mb-3 inline-flex items-center gap-2 rounded-full border border-border bg-muted px-3 py-1 text-xs font-medium text-muted-foreground">
            <SlidersHorizontal className="size-3.5 text-primary" />
            Board workspace
          </div>
          <h1 className="truncate text-2xl font-semibold tracking-[-0.015em] text-foreground sm:text-3xl">{board.title}</h1>
          <p className="mt-2 max-w-2xl text-sm leading-6 text-muted-foreground">{board.description}</p>
        </div>

        <div className="flex flex-wrap items-center gap-2">
          <div className="-space-x-2 pr-2">
            {board.members.map((member) => (
              <Avatar key={member.id} className="inline-flex size-8 border-2 border-card">
                <AvatarFallback className="text-[10px] font-semibold text-primary-foreground" style={{ backgroundColor: member.color }}>
                  {member.initials}
                </AvatarFallback>
              </Avatar>
            ))}
          </div>
          <Badge variant="secondary" className="hidden rounded-full sm:inline-flex">
            <Users className="size-3.5" />
            {board.members.length} members
          </Badge>
          <Button variant="outline" className="bg-card">
            <Search className="size-4" />
            Search
          </Button>
          <Button variant="outline" className="bg-card">
            <Filter className="size-4" />
            Filter
          </Button>
          <Button className="rounded-full">
            <Plus className="size-4" />
            New task
          </Button>
        </div>
      </div>
    </section>
  )
}

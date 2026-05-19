import Link from "next/link"
import { CalendarDays, FileText, Link2, Tag, UserPlus } from "lucide-react"
import { Avatar, AvatarFallback } from "@/components/ui/avatar"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import { Separator } from "@/components/ui/separator"
import type { Card } from "@/features/boards/types"

export function CardSidebar({ card, workspaceSlug, boardId }: { card: Card; workspaceSlug: string; boardId: string }) {
  return (
    <aside className="space-y-4 rounded-2xl border border-border bg-card p-4">
      <section>
        <p className="mb-3 text-xs font-semibold uppercase tracking-[0.08em] text-muted-foreground">People</p>
        <div className="space-y-2">
          {card.members.map((member) => (
            <div key={member.id} className="flex items-center gap-2">
              <Avatar className="size-8">
                <AvatarFallback className="text-[10px] font-semibold text-primary-foreground" style={{ backgroundColor: member.color }}>
                  {member.initials}
                </AvatarFallback>
              </Avatar>
              <span className="truncate text-sm text-foreground">{member.name}</span>
            </div>
          ))}
        </div>
        <Button variant="outline" size="sm" className="mt-3 w-full bg-card">
          <UserPlus className="size-4" />
          Assign member
        </Button>
      </section>

      <Separator />

      <section className="space-y-2">
        <p className="mb-3 text-xs font-semibold uppercase tracking-[0.08em] text-muted-foreground">Actions</p>
        <Button variant="ghost" size="sm" className="w-full justify-start">
          <Tag className="size-4" />
          Labels
        </Button>
        <Button variant="ghost" size="sm" className="w-full justify-start">
          <CalendarDays className="size-4" />
          Dates
        </Button>
        {card.linkedPageId ? (
          <Button variant="ghost" size="sm" className="w-full justify-start" asChild>
            <Link href={`/${workspaceSlug}/boards/${boardId}?doc=${card.linkedPageId}` as never}>
              <FileText className="size-4" />
              Open linked doc
            </Link>
          </Button>
        ) : (
          <Button variant="ghost" size="sm" className="w-full justify-start">
            <Link2 className="size-4" />
            Link doc
          </Button>
        )}
      </section>

      <Separator />

      <section>
        <p className="mb-3 text-xs font-semibold uppercase tracking-[0.08em] text-muted-foreground">Labels</p>
        <div className="flex flex-wrap gap-2">
          {card.labels.map((label) => (
            <Badge key={label.id} variant="secondary" className="rounded-full" style={{ color: label.color }}>
              {label.name}
            </Badge>
          ))}
        </div>
      </section>
    </aside>
  )
}

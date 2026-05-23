import { Avatar, AvatarFallback } from "@/components/ui/avatar"
import type { CardMember } from "@/features/boards/types"

export function CellPerson({ members }: { members: CardMember[] }) {
  if (members.length === 0) return <span className="text-sm text-muted-foreground">Unassigned</span>

  return (
    <div className="flex items-center gap-2">
      <div className="-space-x-2">
        {members.slice(0, 3).map((member) => (
          <Avatar key={member.id} className="inline-flex size-7 border-2 border-card">
            <AvatarFallback className="text-[10px] font-semibold text-primary-foreground" style={{ backgroundColor: member.color }}>
              {member.initials}
            </AvatarFallback>
          </Avatar>
        ))}
      </div>
      <span className="truncate text-sm text-muted-foreground">{members[0]?.name}</span>
    </div>
  )
}

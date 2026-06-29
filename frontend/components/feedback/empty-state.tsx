import * as React from "react"
import { Inbox } from "lucide-react"

import { cn } from "@/lib/utils"

interface EmptyStateProps {
  icon?: React.ReactNode
  title: string
  description: string
  action?: React.ReactNode
  className?: string
}

export function EmptyState({
  icon = <Inbox className="h-10 w-10 text-muted-foreground/60" />,
  title,
  description,
  action,
  className,
}: EmptyStateProps) {
  return (
    <div className={cn("flex flex-col items-center justify-center p-8 text-center min-h-[300px] border border-dashed rounded-lg bg-muted/20 space-y-4", className)}>
      <div className="flex items-center justify-center w-16 h-16 rounded-full bg-muted">
        {icon}
      </div>
      <div className="space-y-1 max-w-sm">
        <h3 className="text-lg font-medium tracking-tight">{title}</h3>
        <p className="text-sm text-muted-foreground">{description}</p>
      </div>
      {action && <div className="mt-2">{action}</div>}
    </div>
  )
}

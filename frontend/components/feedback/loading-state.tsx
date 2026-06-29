import * as React from "react"
import { Loader2 } from "lucide-react"

import { cn } from "@/lib/utils"

interface LoadingStateProps {
  title?: string
  description?: string
  className?: string
}

export function LoadingState({
  title = "Đang tải dữ liệu...",
  description = "Vui lòng chờ trong giây lát.",
  className,
}: LoadingStateProps) {
  return (
    <div className={cn("flex flex-col items-center justify-center p-8 text-center min-h-[300px] space-y-3", className)}>
      <Loader2 className="h-8 w-8 animate-spin text-primary" />
      <div className="space-y-1">
        <h3 className="text-lg font-medium tracking-tight">{title}</h3>
        <p className="text-sm text-muted-foreground max-w-xs">{description}</p>
      </div>
    </div>
  )
}

import * as React from "react"
import { FileQuestion } from "lucide-react"

import { cn } from "../../lib/cn"

interface NotFoundStateProps {
  title?: string
  description?: string
  action?: React.ReactNode
  className?: string
}

export function NotFoundState({
  title = "Không tìm thấy tài nguyên",
  description = "Tài nguyên bạn đang tìm kiếm không tồn tại hoặc đã bị xóa.",
  action,
  className,
}: NotFoundStateProps) {
  return (
    <div className={cn("flex flex-col items-center justify-center p-8 text-center min-h-[300px] space-y-4", className)}>
      <div className="flex items-center justify-center w-12 h-12 rounded-full bg-muted text-muted-foreground">
        <FileQuestion className="h-6 w-6" />
      </div>
      <div className="space-y-1 max-w-sm">
        <h3 className="text-lg font-medium tracking-tight">{title}</h3>
        <p className="text-sm text-muted-foreground">{description}</p>
      </div>
      {action && <div className="mt-2">{action}</div>}
    </div>
  )
}

import * as React from "react"
import { AlertTriangle } from "lucide-react"
// AppError will be available from @notrelix/kernel in future milestones

import { cn } from '../../lib/cn'

interface ErrorStateProps {
  error?: unknown
  title?: string
  description?: string
  action?: React.ReactNode
  className?: string
}

export function ErrorState({
  error,
  title = "Đã xảy ra lỗi",
  description,
  action,
  className,
}: ErrorStateProps) {
  const displayDescription = React.useMemo(() => {
    if (description) return description
    if (error instanceof Error) {
      return error.message
    }
    if (error instanceof Error) {
      return error.message
    }
    return "Không thể hoàn thành yêu cầu. Vui lòng thử lại sau."
  }, [description, error])

  return (
    <div className={cn("flex flex-col items-center justify-center p-8 text-center min-h-[300px] border border-red-100 dark:border-red-950/30 rounded-lg bg-red-50/50 dark:bg-red-950/10 space-y-4", className)}>
      <div className="flex items-center justify-center w-12 h-12 rounded-full bg-red-100 dark:bg-red-950 text-red-600 dark:text-red-400">
        <AlertTriangle className="h-6 w-6" />
      </div>
      <div className="space-y-1 max-w-sm">
        <h3 className="text-lg font-semibold tracking-tight text-red-900 dark:text-red-200">{title}</h3>
        <p className="text-sm text-red-700/85 dark:text-red-400/80">{displayDescription}</p>
      </div>
      {action && <div className="mt-2">{action}</div>}
    </div>
  )
}

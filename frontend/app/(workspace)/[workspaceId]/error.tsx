"use client"

import { AlertCircle, RefreshCw } from "lucide-react"
import { Button } from "@/components/ui/button"

export default function WorkspaceError({
  reset,
}: {
  error: Error & { digest?: string }
  reset: () => void
}) {
  return (
    <div className="min-h-screen bg-card flex items-center justify-center px-6">
      <div className="flex flex-col items-center text-center max-w-sm space-y-4">
        <div className="flex items-center justify-center size-14 rounded-2xl bg-red-50 dark:bg-red-950/30">
          <AlertCircle className="size-7 text-red-600 dark:text-red-400 stroke-[1.5]" />
        </div>
        <div className="space-y-1.5">
          <h2
            className="text-lg font-semibold text-foreground tracking-[-0.01em]"
            style={{ fontFamily: "var(--font-poppins)" }}
          >
            Something went wrong
          </h2>
          <p className="text-sm text-muted-foreground leading-relaxed">
            We couldn&apos;t load this workspace. This might be a temporary issue — try refreshing.
          </p>
        </div>
        <Button
          variant="outline"
          onClick={reset}
          className="gap-2 rounded-xl"
        >
          <RefreshCw className="size-3.5" />
          Try again
        </Button>
      </div>
    </div>
  )
}

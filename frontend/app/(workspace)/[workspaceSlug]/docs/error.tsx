"use client"

import { useEffect } from "react"
import { AlertCircle } from "lucide-react"
import { Button } from "@/components/ui/button"

export default function DocsError({ error, reset }: { error: Error & { digest?: string }; reset: () => void }) {
  useEffect(() => {
    console.error(error)
  }, [error])

  return (
    <div className="flex min-h-svh items-center justify-center bg-background p-8 text-center">
      <div className="max-w-md rounded-2xl border border-border bg-card p-8 shadow-[rgba(205,208,223,0.28)_0px_2px_28px]">
        <div className="mx-auto mb-4 flex size-12 items-center justify-center rounded-2xl bg-red-50 text-red-600">
          <AlertCircle className="size-6" />
        </div>
        <h1 className="text-lg font-semibold text-foreground">Docs could not load</h1>
        <p className="mt-2 text-sm text-muted-foreground">Refresh the workspace or try again.</p>
        <Button className="mt-5" onClick={reset}>Try again</Button>
      </div>
    </div>
  )
}

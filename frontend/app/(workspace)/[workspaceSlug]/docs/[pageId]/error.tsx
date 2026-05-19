"use client"

import { useEffect } from "react"
import { useRouter } from "next/navigation"
import { AlertCircle } from "lucide-react"
import { Button } from "@/components/ui/button"

export default function PageError({ error, reset }: { error: Error & { digest?: string }; reset: () => void }) {
  const router = useRouter()

  useEffect(() => {
    console.error(error)
  }, [error])

  return (
    <div className="flex min-h-svh items-center justify-center bg-card p-8 text-center">
      <div className="max-w-md rounded-2xl border border-border p-8">
        <div className="mx-auto mb-4 flex size-12 items-center justify-center rounded-2xl bg-red-50 text-red-600">
          <AlertCircle className="size-6" />
        </div>
        <h1 className="text-lg font-semibold text-foreground">Page not found</h1>
        <p className="mt-2 text-sm text-muted-foreground">This page may have been deleted or you do not have permission to view it.</p>
        <div className="mt-5 flex justify-center gap-2">
          <Button variant="outline" onClick={() => router.back()}>Go back</Button>
          <Button onClick={reset}>Try again</Button>
        </div>
      </div>
    </div>
  )
}

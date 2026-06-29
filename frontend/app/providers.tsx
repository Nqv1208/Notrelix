"use client"

import { useState, useEffect } from "react"
import { useRouter } from "next/navigation"
import { QueryClientProvider } from "@tanstack/react-query"
import { createQueryClient } from "@/lib/query/query-client"

export function QueryProvider({ children }: { children: React.ReactNode }) {
  const [queryClient] = useState(() => createQueryClient())
  const router = useRouter()

  useEffect(() => {
    const handleAuthFailure = () => {
      queryClient.clear()
      router.push("/sign-in")
    }

    if (typeof window !== "undefined") {
      window.addEventListener("auth:failure", handleAuthFailure)
    }
    return () => {
      if (typeof window !== "undefined") {
        window.removeEventListener("auth:failure", handleAuthFailure)
      }
    }
  }, [queryClient, router])

  return (
    <QueryClientProvider client={queryClient}>
      {children}
    </QueryClientProvider>
  )
}
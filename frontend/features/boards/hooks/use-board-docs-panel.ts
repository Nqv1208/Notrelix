"use client"

import { useCallback } from "react"
import { usePathname, useRouter, useSearchParams } from "next/navigation"

export function useBoardDocsPanel() {
  const router = useRouter()
  const pathname = usePathname()
  const searchParams = useSearchParams()
  const activeDocId = searchParams.get("doc") ?? undefined

  const setDoc = useCallback((pageId?: string) => {
    const params = new URLSearchParams(searchParams.toString())
    if (pageId) {
      params.set("doc", pageId)
    } else {
      params.delete("doc")
    }
    const query = params.toString()
    router.push((query ? `${pathname}?${query}` : pathname) as never, { scroll: false })
  }, [pathname, router, searchParams])

  return {
    activeDocId,
    isOpen: Boolean(activeDocId),
    openDoc: (pageId: string) => setDoc(pageId),
    closeDoc: () => setDoc(undefined),
    toggleDoc: (pageId: string) => setDoc(activeDocId === pageId ? undefined : pageId),
  }
}

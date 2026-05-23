"use client"

import { useCallback } from "react"
import type { Route } from "next"
import { usePathname, useRouter, useSearchParams } from "next/navigation"

const TASK_ID_PARAM = "taskId"

export function useSelectedCardPanel() {
  const router = useRouter()
  const pathname = usePathname()
  const searchParams = useSearchParams()
  const selectedCardId = searchParams.get(TASK_ID_PARAM)

  const setSelectedCardId = useCallback(
    (cardId: string | null) => {
      const params = new URLSearchParams(searchParams.toString())
      if (cardId) params.set(TASK_ID_PARAM, cardId)
      else params.delete(TASK_ID_PARAM)

      const query = params.toString()
      const href = (query ? `${pathname}?${query}` : pathname) as Route
      router.replace(href, { scroll: false })
    },
    [pathname, router, searchParams]
  )

  const openCard = useCallback((cardId: string) => setSelectedCardId(cardId), [setSelectedCardId])
  const closePanel = useCallback(() => setSelectedCardId(null), [setSelectedCardId])

  return {
    selectedCardId,
    isOpen: Boolean(selectedCardId),
    openCard,
    closePanel,
  }
}

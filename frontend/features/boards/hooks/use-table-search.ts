"use client"

import { useMemo, useState } from "react"
import { useDebounce } from "@/hooks/use-debounce"
import type { BoardGroup, Card } from "../types"

export function useTableSearch(groups: BoardGroup[], delay = 250) {
  const [query, setQuery] = useState("")
  const debouncedQuery = useDebounce(query, delay)

  const filteredGroups = useMemo(() => {
    const needle = debouncedQuery.trim().toLowerCase()
    if (!needle) return groups

    return groups
      .map((group) => ({
        ...group,
        cards: group.cards.filter((card) => cardMatchesSearch(card, needle)),
      }))
      .filter((group) => group.cards.length > 0)
  }, [debouncedQuery, groups])

  return { query, setQuery, debouncedQuery, filteredGroups }
}

function cardMatchesSearch(card: Card, needle: string) {
  const haystack = [
    card.title,
    card.descriptionMd,
    card.linkedPageId,
    ...card.members.map((member) => member.name),
    ...card.labels.map((label) => label.name),
  ]
    .filter(Boolean)
    .join(" ")
    .toLowerCase()

  return haystack.includes(needle)
}

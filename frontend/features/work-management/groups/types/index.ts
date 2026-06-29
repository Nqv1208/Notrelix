import type { Card, CardSummaryDtoApi } from "../../items/types"

export interface BoardGroup {
  id: string
  title: string
  color?: string
  position: number
  isCollapsed: boolean
  cards: Card[]
}

export interface ListDtoApi {
  id: string
  title: string
  color?: string | null
  position: number
  isArchived: boolean
  cards: CardSummaryDtoApi[]
}

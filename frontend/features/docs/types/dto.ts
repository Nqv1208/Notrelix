/**
 * DTO types for API responses from backend.
 * Internal use only - not exported outside features/docs.
 * These types describe the shape of data received from the backend,
 * before transformation to domain types.
 */

import type { Block, BlockProperties } from "../types"

export type PageDtoApi = {
  id: string
  workspaceId: string
  parentId?: string | null
  title: string
  iconType?: string | null
  iconValue?: string | null
  coverUrl?: string | null
  position: number
  depth: number
  isTemplate: boolean
  isArchived: boolean
  publishedAt?: string | null
  deadline?: string | null
  createdAt: string
  updatedAt?: string | null
}

export type BreadcrumbDtoApi = {
  id: string
  title: string
  iconType?: string | null
  iconValue?: string | null
}

export type CommentDtoApi = {
  id: string
  userId: string
  contentMd: string
  createdAt: string
  isEdited: boolean
}

export type HistoryDtoApi = {
  id: string
  actorId: string
  action: string
  resourceTitle?: string | null
  createdAt: string
}

export type BlockDtoApi = {
  id: string
  pageId: string
  parentBlockId?: string | null
  type: Block["type"]
  properties: string | BlockProperties
  position: number
  version: number
  createdByUserId: string
  createdAt: string
  updatedAt?: string | null
}

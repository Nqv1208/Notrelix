"use client"

import React from "react"
import { usePageList } from "../../tree/hooks/queries/use-page-tree"

interface DocsViewPickerDataProviderProps {
  workspaceId: string
  children: (data: { firstPageId?: string; isLoading: boolean }) => React.ReactNode
}

export function DocsViewPickerDataProvider({
  workspaceId,
  children,
}: DocsViewPickerDataProviderProps) {
  const { data: pages = [], isLoading } = usePageList(workspaceId)
  const firstPageId = pages[0]?.id
  return <>{children({ firstPageId, isLoading })}</>
}

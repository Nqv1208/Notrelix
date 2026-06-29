"use client"

import { use } from "react"
import { DocumentHistoryScreen } from "@/features/docs"

interface HistoryPageProps {
  params: Promise<{ workspaceId: string; pageId: string }>
}

export default function HistoryPage({ params }: HistoryPageProps) {
  const { workspaceId, pageId } = use(params)

  return (
    <DocumentHistoryScreen
      workspaceId={workspaceId}
      pageId={pageId}
    />
  )
}

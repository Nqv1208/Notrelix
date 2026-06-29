"use client"

import { use } from "react"
import { DocumentScreen } from "@/features/docs"

interface PageEditorProps {
  params: Promise<{ workspaceId: string; pageId: string }>
}

export default function PageEditorPage({ params }: PageEditorProps) {
  const { workspaceId, pageId } = use(params)

  return (
    <DocumentScreen
      workspaceId={workspaceId}
      pageId={pageId}
    />
  )
}

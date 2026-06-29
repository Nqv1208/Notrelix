"use client"

import { use } from "react"
import { DocsWorkspaceView } from "@/features/docs"

interface DocsPageProps {
  params: Promise<{ workspaceId: string }>
}

export default function DocsPage({ params }: DocsPageProps) {
  const { workspaceId } = use(params)

  return <DocsWorkspaceView workspaceId={workspaceId} />
}

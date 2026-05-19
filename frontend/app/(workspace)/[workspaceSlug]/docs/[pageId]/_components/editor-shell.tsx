"use client"

import { MondayDocEditor } from "@/components/docs/editor"

interface EditorShellProps {
  pageId: string
  workspaceSlug: string
}

export function EditorShell({ pageId, workspaceSlug }: EditorShellProps) {
  return <MondayDocEditor pageId={pageId} workspaceSlug={workspaceSlug} />
}

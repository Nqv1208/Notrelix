"use client"

import React from "react"
import { DocumentToolbar } from "../../editor/components/document-toolbar"

interface DocsViewToolbarProps {
  pageId: string
}

export function DocsViewToolbar({ pageId }: DocsViewToolbarProps) {
  return <DocumentToolbar pageId={pageId} compact />
}

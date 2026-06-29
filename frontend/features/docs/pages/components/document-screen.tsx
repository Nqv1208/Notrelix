"use client"

import { usePage } from "../hooks/queries/use-page"
import { usePageBlocks } from "../../blocks/hooks/queries/use-page-blocks"
import { DocumentEditor, EditorSkeleton } from "../../editor/components"

import { NotFoundState, ErrorState } from "@/components/feedback"

interface DocumentScreenProps {
  pageId: string
  workspaceId: string
  embedded?: boolean
  showToolbar?: boolean
  showOpenFullDoc?: boolean
}

export function DocumentScreen({
  pageId,
  workspaceId,
  embedded,
  showToolbar = true,
  showOpenFullDoc,
}: DocumentScreenProps) {
  const page = usePage(pageId)
  const blocks = usePageBlocks(pageId)
  const detail = page.data
  const pageBlocks = blocks.data ?? detail?.blocks ?? []
  const contained = embedded || !showToolbar

  if (page.isLoading || blocks.isLoading) {
    return <EditorSkeleton embedded={contained} />
  }

  if (page.isError) {
    return (
      <ErrorState
        error={page.error}
        title="Lỗi tải trang tài liệu"
      />
    )
  }

  if (!detail) {
    return (
      <NotFoundState
        title="Không tìm thấy trang tài liệu"
        description="Trang tài liệu này có thể đã bị di chuyển, lưu trữ hoặc xóa bỏ."
      />
    )
  }

  return (
    <DocumentEditor
      pageId={pageId}
      workspaceId={workspaceId}
      detail={detail}
      pageBlocks={pageBlocks}
      embedded={embedded}
      showToolbar={showToolbar}
      showOpenFullDoc={showOpenFullDoc}
    />
  )
}

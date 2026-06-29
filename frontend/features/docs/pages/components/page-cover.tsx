"use client"

import type { PageDetail } from "../types/page.types"

interface PageCoverProps {
  page: PageDetail
}

export function PageCover({ page }: PageCoverProps) {
  return (
    <div
      className="mt-4 h-36 overflow-hidden rounded-2xl border border-border sm:h-44"
      style={{
        background: page.coverUrl
          ? `url(${page.coverUrl}) center/cover`
          : `linear-gradient(135deg, ${page.coverColor}, #ffffff)`,
      }}
      aria-label="Page cover"
    />
  )
}

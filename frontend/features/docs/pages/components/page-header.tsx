"use client"

import { useState } from "react"
import { Input } from "@/components/ui/input"
import { useUpdatePage } from "../hooks/mutations/use-update-page"
import type { PageDetail } from "../types/page.types"

interface PageHeaderProps {
  page: PageDetail
}

export function PageHeader({ page }: PageHeaderProps) {
  const [title, setTitle] = useState(page.title)
  const updatePage = useUpdatePage(page.id)

  return (
    <header className="mb-4">
      <div className="mb-4 flex size-16 items-center justify-center rounded-2xl text-4xl shadow-[rgba(205,208,223,0.35)_0px_2px_24px]" style={{ backgroundColor: page.coverColor }}>
        {page.icon}
      </div>
      <Input
        value={title}
        onChange={(event) => setTitle(event.target.value)}
        onBlur={() => {
          if (title.trim() && title !== page.title) updatePage.mutate({ title: title.trim() })
        }}
        className="h-auto border-0 bg-transparent px-0 text-4xl font-semibold tracking-[-0.02em] text-foreground shadow-none focus-visible:ring-0"
        aria-label="Page title"
      />
      <p className="mt-2 text-sm text-muted-foreground">
        Last edited {new Date(page.lastEditedAt).toLocaleString()} · Version {page.metadata.version}
      </p>
    </header>
  )
}

"use client"

import { useState } from "react"
import { Input } from "@/components/ui/input"
import { useUpdatePage } from "../hooks/mutations/use-update-page"
import type { PageDetail } from "../types/page.types"

export function EditablePageTitle({ page }: { page: PageDetail }) {
  const [value, setValue] = useState(page.title)
  const updatePage = useUpdatePage(page.id)

  function commit() {
    const next = value.trim()
    if (!next || next === page.title) {
      setValue(page.title)
      return
    }
    updatePage.mutate({ title: next })
  }

  return (
    <Input
      value={value}
      onChange={(event) => setValue(event.target.value)}
      onBlur={commit}
      onKeyDown={(event) => {
        if (event.key === "Enter") event.currentTarget.blur()
        if (event.key === "Escape") {
          setValue(page.title)
          event.currentTarget.blur()
        }
      }}
      aria-label="Page title"
      className="h-auto border-0 bg-transparent px-0 py-1 text-3xl font-semibold tracking-[-0.015em] shadow-none focus-visible:ring-0 sm:text-4xl"
    />
  )
}

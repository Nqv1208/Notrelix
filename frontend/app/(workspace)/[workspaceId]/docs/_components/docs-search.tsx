"use client"

import { useEffect, useMemo, useState } from "react"
import { useRouter } from "next/navigation"
import { Command, FileText, History, Search, SquareKanban } from "lucide-react"
import { Button } from "@/components/ui/button"
import {
  CommandDialog,
  CommandEmpty,
  CommandGroup,
  CommandInput,
  CommandItem,
  CommandList,
  CommandSeparator,
} from "@/components/ui/command"
import { Kbd } from "@/components/ui/kbd"
import { useDocsSearch } from "@/features/docs/hooks/use-docs-search"
import { mockDocsWorkspace } from "@/features/docs/mock/mock-data"

interface DocsSearchProps {
  workspaceId: string
  mode?: "button" | "inline"
}

export function DocsSearch({ workspaceId, mode = "button" }: DocsSearchProps) {
  const router = useRouter()
  const [open, setOpen] = useState(false)
  const [query, setQuery] = useState("")
  const { data: results = [], isFetching } = useDocsSearch(workspaceId, query)

  useEffect(() => {
    function onKeyDown(event: KeyboardEvent) {
      if ((event.metaKey || event.ctrlKey) && event.key.toLowerCase() === "k") {
        event.preventDefault()
        setOpen((value) => !value)
      }
    }
    window.addEventListener("keydown", onKeyDown)
    return () => window.removeEventListener("keydown", onKeyDown)
  }, [])

  const grouped = useMemo(() => {
    return results.reduce<Record<string, typeof results>>((acc, result) => {
      acc[result.group] = [...(acc[result.group] ?? []), result]
      return acc
    }, {})
  }, [results])

  const trigger =
    mode === "inline" ? (
      <button
        type="button"
        onClick={() => setOpen(true)}
        className="flex h-9 w-full items-center gap-2 rounded-lg border border-border bg-muted px-3 text-left text-sm text-muted-foreground transition hover:bg-card"
      >
        <Search className="size-4" />
        <span className="min-w-0 flex-1 truncate">Search docs, blocks, tasks...</span>
        <Kbd>⌘K</Kbd>
      </button>
    ) : (
      <Button variant="outline" onClick={() => setOpen(true)} className="h-10 min-w-[220px] justify-between rounded-xl bg-card text-muted-foreground">
        <span className="flex items-center gap-2">
          <Search className="size-4" />
          Quick search
        </span>
        <Kbd>⌘K</Kbd>
      </Button>
    )

  function openResult(result: { type: string; id: string; pageId?: string }) {
    setOpen(false)
    const pageId = result.type === "page" ? result.id : result.pageId
    if (pageId) router.push(`/${workspaceId}/docs/${pageId}`)
  }

  return (
    <>
      {trigger}
      <CommandDialog open={open} onOpenChange={setOpen} title="Search docs" description="Search pages, blocks, tasks, and boards">
        <Command className="border-0">
          <CommandInput placeholder="Search docs, blocks, tasks..." value={query} onValueChange={setQuery} />
          <CommandList className="max-h-[420px]">
            <CommandEmpty>{isFetching ? "Searching..." : "No results found."}</CommandEmpty>
            {query.length < 2 ? (
              <CommandGroup heading="Recent searches">
                {mockDocsWorkspace.recentSearches.map((item) => (
                  <CommandItem key={item} onSelect={() => setQuery(item)}>
                    <History className="size-4" />
                    {item}
                  </CommandItem>
                ))}
              </CommandGroup>
            ) : null}
            {Object.entries(grouped).map(([group, items]) => (
              <CommandGroup key={group} heading={group}>
                {items.map((result) => (
                  <CommandItem key={`${result.type}-${result.id}`} onSelect={() => openResult(result)}>
                    {result.type === "board" ? <SquareKanban className="size-4" /> : result.type === "task" ? <Command className="size-4" /> : <FileText className="size-4" />}
                    <span className="min-w-0 flex-1">
                      <span className="block truncate">{result.title}</span>
                      <span className="block truncate text-xs text-muted-foreground">{result.excerpt}</span>
                    </span>
                  </CommandItem>
                ))}
              </CommandGroup>
            ))}
            {results.length ? <CommandSeparator /> : null}
          </CommandList>
        </Command>
      </CommandDialog>
    </>
  )
}

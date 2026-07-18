import { useMemo, useState } from "react"
import { Activity, Bot, Download, RefreshCw, Search, UserRound } from "lucide-react"
import { Button } from "@notrelix/ui-web"
import {
  Command,
  CommandEmpty,
  CommandGroup,
  CommandInput,
  CommandItem,
  CommandList,
} from "@notrelix/ui-web"
import { Input } from "@notrelix/ui-web"
import { Popover, PopoverContent, PopoverTrigger } from "@notrelix/ui-web"
import { Skeleton } from "@notrelix/ui-web"
import { useCardActivity } from "@notrelix/work-management-state"
import type { CardDetail, CardActivity } from "@notrelix/work-management-core"
import { TaskDetailEmptyState } from "./task-detail-empty-state"

export function TaskActivityTab({ card }: { card: CardDetail }) {
  const { data = [], isLoading, isFetching, refetch } = useCardActivity(card.id)
  const [query, setQuery] = useState("")
  const [person, setPerson] = useState<string | null>(null)
  const filteredActivity = useMemo(() => {
    const normalized = query.trim().toLowerCase()
    return data.filter((item) => {
      const matchesQuery = !normalized || `${item.actor} ${item.action}`.toLowerCase().includes(normalized)
      const matchesPerson = !person || item.actor === person
      return matchesQuery && matchesPerson
    })
  }, [data, person, query])
  const people = Array.from(new Set(data.map((item) => item.actor)))

  return (
    <div className="flex flex-col gap-4 p-4">
      <div className="flex flex-col gap-2">
        <div className="flex flex-wrap items-center gap-2">
          <div className="relative">
            <Search className="pointer-events-none absolute left-2.5 top-1/2 size-4 -translate-y-1/2 text-muted-foreground" />
            <Input
              value={query}
              onChange={(event) => setQuery(event.target.value)}
              className="h-8 w-56 bg-card pl-8"
              placeholder="Filter log"
              aria-label="Filter activity log"
            />
          </div>
          <Popover>
            <PopoverTrigger asChild>
              <Button variant="outline" size="sm" className="bg-card">
                <UserRound className="size-4" />
                {person ?? "Person"}
              </Button>
            </PopoverTrigger>
            <PopoverContent align="start" className="w-60 p-0">
              <Command>
                <CommandInput placeholder="Filter people..." />
                <CommandList>
                  <CommandEmpty>No people found.</CommandEmpty>
                  <CommandGroup heading="People">
                    <CommandItem data-checked={!person} onSelect={() => setPerson(null)}>All people</CommandItem>
                    {people.map((name: string) => (
                      <CommandItem key={String(name)} value={name} data-checked={person === name} onSelect={() => setPerson(name)}>
                        {name}
                      </CommandItem>
                    ))}
                  </CommandGroup>
                </CommandList>
              </Command>
            </PopoverContent>
          </Popover>
          <Button variant="outline" size="sm" className="bg-card">
            <Bot className="size-4" />
            AI filter
          </Button>
          <Button variant="ghost" size="icon-sm" aria-label="Refresh activity" onClick={() => refetch()} disabled={isFetching}>
            <RefreshCw className={isFetching ? "size-4 animate-spin" : "size-4"} />
          </Button>
          <Button variant="ghost" size="icon-sm" aria-label="Export activity log">
            <Download className="size-4" />
          </Button>
        </div>
      </div>

      {isLoading ? (
        <div className="flex flex-col gap-2">
          <Skeleton className="h-14 rounded-lg" />
          <Skeleton className="h-14 rounded-lg" />
          <Skeleton className="h-14 rounded-lg" />
        </div>
      ) : filteredActivity.length === 0 ? (
        <TaskDetailEmptyState
          icon={Activity}
          title="No activity found"
          description="Changes, comments, files, and automations will appear here."
        />
      ) : (
        <div className="flex flex-col gap-2">
          {filteredActivity.map((item: CardActivity) => (
            <div key={item.id} className="rounded-lg border border-border bg-card p-3">
              <p className="text-sm text-foreground"><span className="font-medium">{item.actor}</span> {item.action}</p>
              <p className="mt-1 text-xs text-muted-foreground">{new Date(item.createdAt).toLocaleString()}</p>
            </div>
          ))}
        </div>
      )}
    </div>
  )
}

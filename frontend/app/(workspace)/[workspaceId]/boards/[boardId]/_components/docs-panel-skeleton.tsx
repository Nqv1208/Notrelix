import { Skeleton } from "@/components/ui/skeleton"

export function DocsPanelSkeleton() {
  return (
    <aside className="h-full min-h-[620px] space-y-4 rounded-2xl border border-border bg-card p-4">
      <Skeleton className="h-8 w-32" />
      <Skeleton className="h-32 rounded-xl" />
      <Skeleton className="h-64 rounded-xl" />
    </aside>
  )
}

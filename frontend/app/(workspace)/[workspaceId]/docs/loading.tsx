import { Skeleton } from "@/components/ui/skeleton"

export default function DocsLoading() {
  return (
    <div className="min-h-svh bg-card p-6">
      <div className="mx-auto grid max-w-[1180px] gap-5 xl:grid-cols-[minmax(0,1fr)_340px]">
        <div className="space-y-5">
          <Skeleton className="h-40 rounded-2xl" />
          <Skeleton className="h-56 rounded-2xl" />
          <Skeleton className="h-48 rounded-2xl" />
        </div>
        <div className="space-y-5">
          <Skeleton className="h-72 rounded-2xl" />
          <Skeleton className="h-72 rounded-2xl" />
        </div>
      </div>
    </div>
  )
}

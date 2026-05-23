import { Skeleton } from "@/components/ui/skeleton"

export default function CardLoading() {
  return (
    <main className="mx-auto max-w-[1180px] space-y-5 px-4 py-6 sm:px-6 lg:px-8">
      <Skeleton className="h-16 rounded-2xl" />
      <div className="grid gap-5 lg:grid-cols-[minmax(0,1fr)_320px]">
        <Skeleton className="h-[520px] rounded-2xl" />
        <Skeleton className="h-[520px] rounded-2xl" />
      </div>
    </main>
  )
}

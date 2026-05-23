import { Skeleton } from "@/components/ui/skeleton"

export default function PageEditorLoading() {
  return (
    <div className="mx-auto max-w-[820px] space-y-5 p-8">
      <Skeleton className="h-40 rounded-2xl" />
      <Skeleton className="h-12 w-2/3" />
      <Skeleton className="h-8 w-1/2" />
      {Array.from({ length: 8 }).map((_, index) => (
        <Skeleton key={index} className="h-7 w-full" />
      ))}
    </div>
  )
}

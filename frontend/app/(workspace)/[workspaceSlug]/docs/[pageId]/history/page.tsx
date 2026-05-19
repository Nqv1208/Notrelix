import { BreadcrumbNav } from "../_components/breadcrumb-nav"
import { HistoryClient } from "./_components/history-client"
import { mockPageService } from "@/features/docs/mock/mock-page-service"

interface HistoryPageProps {
  params: Promise<{ workspaceSlug: string; pageId: string }>
}

export default async function HistoryPage({ params }: HistoryPageProps) {
  const { workspaceSlug, pageId } = await params
  const breadcrumb = await mockPageService.getBreadcrumb(pageId)

  return (
    <div className="min-h-svh bg-background">
      <div className="mx-auto max-w-[1180px] px-4 py-8 sm:px-6 lg:px-8">
        <BreadcrumbNav breadcrumb={breadcrumb} workspaceSlug={workspaceSlug} />
        <div className="mb-6 rounded-2xl border border-border bg-card p-5">
          <h1 className="text-2xl font-semibold tracking-[-0.015em] text-foreground">Version history</h1>
          <p className="mt-2 text-sm text-muted-foreground">Review page activity and restore points.</p>
        </div>
        <HistoryClient pageId={pageId} />
      </div>
    </div>
  )
}

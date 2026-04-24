import { TaskPage } from "@/app/(workspace)/_components/task-page"

interface PageProps {
  params: Promise<{
    workspaceId: string
    pageId: string
  }>
}

export default async function WorkspacePageView({ params }: PageProps) {
  const { workspaceId, pageId } = await params
  
  return <TaskPage workspaceId={workspaceId} pageId={pageId} />
}

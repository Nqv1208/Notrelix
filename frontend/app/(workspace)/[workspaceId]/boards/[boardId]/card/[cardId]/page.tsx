import { CardDetail } from "../../_components/card-detail"

export default async function CardPage({ params }: { params: Promise<{ workspaceId: string; boardId: string; cardId: string }> }) {
  const { workspaceId, boardId, cardId } = await params
  return <CardDetail workspaceId={workspaceId} boardId={boardId} cardId={cardId} mode="page" />
}

import { CardModal } from "../../../_components/card-detail/card-modal"

export default async function CardModalPage({ params }: { params: Promise<{ workspaceId: string; boardId: string; cardId: string }> }) {
  const { workspaceId, boardId, cardId } = await params
  return <CardModal workspaceId={workspaceId} boardId={boardId} cardId={cardId} />
}

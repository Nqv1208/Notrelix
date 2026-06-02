import { WorkspaceRoomChat } from "../_components/chat/workspace-room-chat"
import { workspaceChatMessages, workspaceMembers } from "../_components/dashboard/workspace-data"

export default async function WorkspaceChatPage({ params }: { params: Promise<{ workspaceId: string }> }) {
  await params

  return <WorkspaceRoomChat members={workspaceMembers} messages={workspaceChatMessages} />
}

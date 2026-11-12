import { useMemo } from "react";
import { useNavigate } from "@tanstack/react-router";
import { DiscussButton, createChatHooks } from "@notrelix/features-chat/web";
import { useAppRuntime } from "@notrelix/runtime-web";

/**
 * "Discuss" on a Board (UC-CHT-H10): opens the channel linked to this Board. It shows nothing when no channel the person can see is
 * linked (the server lists only those, and nothing to a person who cannot view the Board). The wrapper collapses with it.
 */
export function BoardDiscuss({
  workspaceId,
  boardId,
}: {
  readonly workspaceId: string;
  readonly boardId: string;
}) {
  const { api: client } = useAppRuntime();
  const navigate = useNavigate();
  const hooks = useMemo(
    () => createChatHooks({ api: client.api, endpoints: client.endpoints }),
    [client],
  );
  const resource = useMemo(
    () => ({ kind: "work-management.board" as const, resourceId: boardId }),
    [boardId],
  );
  return (
    <div className="flex justify-end px-4 py-1 empty:hidden">
      <DiscussButton
        hooks={hooks}
        workspaceId={workspaceId}
        resource={resource}
        onOpenChannel={(channelId) =>
          void navigate({
            to: "/workspaces/$workspaceId/chat/$channelId",
            params: { workspaceId, channelId },
          })
        }
      />
    </div>
  );
}

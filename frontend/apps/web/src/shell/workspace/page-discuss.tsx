import { useMemo } from "react";
import { useNavigate } from "@tanstack/react-router";
import { DiscussButton, createChatHooks } from "@notrelix/features-chat/web";
import { useAppRuntime } from "@notrelix/runtime-web";

/**
 * "Discuss" in a Page's header (UC-CHT-H11): opens the channel linked to this Page. It shows nothing when no channel the person
 * can see is linked (the server lists only those, and nothing to a person who cannot view the Page), so the header has no gap.
 */
export function PageDiscuss({
  workspaceId,
  pageId,
}: {
  readonly workspaceId: string;
  readonly pageId: string;
}) {
  const { api: client } = useAppRuntime();
  const navigate = useNavigate();
  const hooks = useMemo(
    () => createChatHooks({ api: client.api, endpoints: client.endpoints }),
    [client],
  );
  const resource = useMemo(
    () => ({ kind: "documents.page" as const, resourceId: pageId }),
    [pageId],
  );
  return (
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
  );
}

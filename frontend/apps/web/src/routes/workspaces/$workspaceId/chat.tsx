import { useEffect, useMemo } from "react";
import { useNavigate, useParams } from "@tanstack/react-router";
import {
  ChatWorkspace,
  createChatHooks,
  type ChatViewerRole,
} from "@notrelix/features-chat/web";
import { useAppRuntime } from "@notrelix/runtime-web";
import { useWorkspaceContext } from "@/providers/workspace-provider";
import { useWorkspaceViewer } from "@/shell/workspace/use-workspace-viewer";
import { useRealtimeConnection } from "../../../realtime/use-realtime-connection";

const SAFE_ID = /^[A-Za-z0-9_-]{1,64}$/;

/**
 * Chat (CHT-001..004). `/chat` opens the first channel of the caller; `/chat/$channelId`
 * opens that channel. The feature owns the channel, history and read state; this page
 * supplies the transport, the caller and the Workspace member names.
 */
export function ChatPage() {
  const { workspaceId } = useWorkspaceContext();
  const { channelId: rawChannelId } = useParams({ strict: false }) as {
    channelId?: string;
  };
  // The id comes from the address bar and ends up in an API path: only a plain id is used, anything else (such as
  // "..") is treated as no channel and the page opens the first one.
  const channelId =
    rawChannelId && SAFE_ID.test(rawChannelId) ? rawChannelId : undefined;
  const navigate = useNavigate();
  const { api: runtimeClient } = useAppRuntime();
  const viewer = useWorkspaceViewer(workspaceId);
  const realtime = useRealtimeConnection();
  // Only a connection that is down (not one that has not started yet) reads as reconnecting.
  const connection =
    realtime.state === "idle" ||
    realtime.state === "connected" ||
    realtime.state === "connecting"
      ? "live"
      : "reconnecting";

  const deps = useMemo(
    () => ({ api: runtimeClient.api, endpoints: runtimeClient.endpoints }),
    [runtimeClient],
  );
  const hooks = useMemo(() => createChatHooks(deps), [deps]);
  const channels = hooks.useChannels(workspaceId);
  const firstChannelId = channels.data?.[0]?.id ?? null;

  // A bare /chat opens the first channel; the list is the caller's own, so the first one is theirs.
  useEffect(() => {
    if (channelId || !firstChannelId) return;
    void navigate({
      to: "/workspaces/$workspaceId/chat/$channelId",
      params: { workspaceId, channelId: firstChannelId },
      replace: true,
    });
  }, [channelId, firstChannelId, navigate, workspaceId]);

  const people = useMemo(() => {
    const map = new Map<string, string>();
    for (const member of viewer.members.data ?? []) {
      map.set(member.userId, member.name);
    }
    return map;
  }, [viewer.members.data]);

  return (
    <div className="flex h-full min-h-0 flex-col">
      <ChatWorkspace
        deps={deps}
        workspaceId={workspaceId}
        viewerId={viewer.userId ?? ""}
        viewerRole={(viewer.role as ChatViewerRole | null) ?? null}
        channelId={channelId ?? null}
        people={people}
        connection={connection}
        onOpenChannel={(id) =>
          void navigate({
            to: "/workspaces/$workspaceId/chat/$channelId",
            params: { workspaceId, channelId: id },
          })
        }
      />
    </div>
  );
}

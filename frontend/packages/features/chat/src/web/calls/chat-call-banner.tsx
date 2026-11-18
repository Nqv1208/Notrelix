/**
 * What a channel or group conversation shows about a call that is on (Chat-Var-Call-Group and Chat-Var-Call-Full): a strip above the
 * composer with who is in the call and a Join button, and the dialog that says the call is full. The server decides who may join and
 * how many (25 in a voice call, 12 in a video call); the strip only asks.
 */
import { useQuery } from "@tanstack/react-query";
import { useMemo } from "react";
import { PhoneIcon, UsersIcon, VideoIcon } from "lucide-react";
import {
  Button,
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@notrelix/ui-web";
import {
  createChatService,
  type ChatApiClient,
  type ChatEndpoints,
} from "../../core/api/chat.service";
import {
  CHAT_CALL_CAP,
  isCallFull,
  joinedParticipants,
} from "../../core/model/call-model";
import type { ChatCall, ChatCallKind } from "../../core/types/call";
import { chatQueryKeys } from "../../query/keys";
import type { ChatCopy } from "../components/chat.copy";
import { ChatStatusBanner } from "../components/chat-status-banner";
import { fillCopy } from "../components/use-chat-copy";
import type { ChatCallsController } from "./use-chat-calls";

const ACTIVE_CALL_POLL_MS = 20_000;

/** The ringing or active call of the open space, kept current by the call frames (they invalidate this query) and a slow poll. */
export function useActiveCall(options: {
  readonly deps: {
    readonly api: ChatApiClient;
    readonly endpoints: ChatEndpoints;
  };
  readonly workspaceId: string;
  readonly spaceId: string | null;
  readonly enabled: boolean;
}): ChatCall | null {
  const { deps, workspaceId, spaceId, enabled } = options;
  const service = useMemo(
    () => createChatService(deps.api, deps.endpoints),
    [deps.api, deps.endpoints],
  );
  const query = useQuery<ChatCall | null>({
    queryKey: chatQueryKeys.activeCall(workspaceId, spaceId ?? ""),
    queryFn: () => service.getActiveCall(workspaceId, spaceId ?? ""),
    enabled: enabled && spaceId !== null,
    refetchInterval: ACTIVE_CALL_POLL_MS,
  });
  const call = query.data ?? null;
  return call && call.state !== "ended" ? call : null;
}

export function ChatCallJoinBanner({
  call,
  controller,
  nameOf,
  copy,
}: {
  readonly call: ChatCall;
  readonly controller: ChatCallsController;
  readonly nameOf: (userId: string) => string;
  readonly copy: ChatCopy;
}) {
  const c = copy.calls;
  const video = call.kind === "video";
  const count = joinedParticipants(call).length;
  const cap = CHAT_CALL_CAP[call.kind];
  const full = isCallFull(call);
  return (
    <ChatStatusBanner
      tone="info"
      placement="notice"
      icon={video ? <VideoIcon /> : <PhoneIcon />}
      action={
        <Button
          type="button"
          size="sm"
          onClick={() => controller.join(call)}
          data-testid="chat-call-join"
        >
          {c.groupJoin}
        </Button>
      }
    >
      <span data-testid="chat-call-banner">
        {video ? c.groupBannerVideo : c.groupBannerVoice}
        {" · "}
        {nameOf(call.initiatorId)}
        {" · "}
        <UsersIcon
          aria-hidden="true"
          className="inline size-3.5 align-text-bottom"
        />{" "}
        {fillCopy(full ? c.groupBannerFull : c.groupBannerCount, {
          count: String(count),
          cap: String(cap),
        })}
      </span>
    </ChatStatusBanner>
  );
}

export function ChatCallFullDialog({
  kind,
  onClose,
  copy,
}: {
  /** The kind of the call that was full; null keeps the dialog shut. */
  readonly kind: ChatCallKind | null;
  readonly onClose: () => void;
  readonly copy: ChatCopy;
}) {
  const c = copy.calls;
  return (
    <Dialog open={kind !== null} onOpenChange={(open) => !open && onClose()}>
      <DialogContent data-testid="chat-call-full">
        <DialogHeader>
          <DialogTitle>{c.groupFullTitle}</DialogTitle>
          <DialogDescription>
            {fillCopy(kind === "video" ? c.groupFullVideo : c.groupFullVoice, {
              cap: String(CHAT_CALL_CAP[kind ?? "voice"]),
            })}
          </DialogDescription>
        </DialogHeader>
        <DialogFooter>
          <Button type="button" onClick={onClose}>
            {c.close}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}

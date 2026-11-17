import type { ReactNode } from "react";
import { AtSign, MessagesSquare, Pin, Users } from "lucide-react";
import type { ChatHooks } from "../query/create-chat-hooks";
import {
  ChatInfoPanel,
  type ChatInfoPanelProps,
  type ChatInfoSection,
} from "./chat-info-panel";
import {
  FollowedThreadsPanel,
  type FollowedThreadsPanelProps,
} from "./chat-followed-threads";
import {
  MentionsPanel,
  mentionRowsOf,
  type MentionsPanelProps,
} from "./chat-mentions-panel";
import { PinsPanel, type PinsPanelProps } from "./chat-pins";

export interface ChatInfoPanelHostProps extends Omit<
  ChatInfoPanelProps,
  "sections"
> {
  readonly hooks: ChatHooks;
  readonly workspaceId: string;
  readonly channelId: string;
  /** The Members section; omitted for a 1-1 conversation. */
  readonly members?: { readonly count: number; readonly content: ReactNode };
  readonly pins: Omit<
    PinsPanelProps,
    "hooks" | "workspaceId" | "channelId" | "embedded" | "onClose"
  >;
  readonly following: Omit<
    FollowedThreadsPanelProps,
    "hooks" | "workspaceId" | "embedded" | "onClose"
  >;
  readonly mentions: Omit<MentionsPanelProps, "embedded">;
}

/**
 * Builds the sections of the info panel from the data the old tabs showed: Members, Pinned messages, Followed threads
 * and Mentions. The counts come from the same queries the sections use, so a collapsed section still shows its number.
 */
export function ChatInfoPanelHost({
  hooks,
  workspaceId,
  channelId,
  members,
  pins,
  following,
  mentions,
  ...panel
}: ChatInfoPanelHostProps) {
  const { copy } = pins;
  const pinned = hooks.usePins(workspaceId, channelId);
  const followed = hooks.useFollowedThreads(workspaceId, true);
  const pinCount = pinned.data?.pages.flatMap((p) => p.items).length ?? null;
  const followCount =
    followed.data?.pages.flatMap((p) => p.items).length ?? null;

  const sections: ChatInfoSection[] = [
    ...(members
      ? [
          {
            key: "members" as const,
            label: copy.header.membersTitle,
            count: members.count,
            icon: Users,
            content: members.content,
          },
        ]
      : []),
    {
      key: "pins",
      label: copy.pins.title,
      count: pinCount,
      icon: Pin,
      content: (
        <PinsPanel
          {...pins}
          hooks={hooks}
          workspaceId={workspaceId}
          channelId={channelId}
          embedded
        />
      ),
    },
    {
      key: "following",
      label: copy.thread.followedTab,
      count: followCount,
      icon: MessagesSquare,
      content: (
        <FollowedThreadsPanel
          {...following}
          hooks={hooks}
          workspaceId={workspaceId}
          embedded
        />
      ),
    },
    {
      key: "mentions",
      label: copy.panel.sectionMentions,
      count: mentionRowsOf(mentions.rows).length,
      icon: AtSign,
      content: <MentionsPanel {...mentions} embedded />,
    },
  ];
  return <ChatInfoPanel {...panel} sections={sections} />;
}

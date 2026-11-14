import { Fragment } from "react";
import { cn } from "@notrelix/ui-web";
import { splitByMentions } from "../../core/model/mention-model";
import type { ChatMention } from "../../core/types/chat";
import type { ChatCopy } from "./chat.copy";
import { useChatMentions } from "./chat-mention-context";
import { fillCopy } from "./use-chat-copy";

const CHIP =
  "rounded-[5px] bg-warning-subtle px-1 font-semibold text-warning-subtle-foreground";
/**
 * On the caller's own (accent) bubble a mention is an inline chip, translucent primary-foreground on the bubble, so its
 * contrast stays the bubble's own (primary-foreground on primary, AA in every theme).
 */
const OWN_CHIP =
  "rounded-[5px] bg-primary-foreground/[.24] px-1 font-semibold text-primary-foreground";

export interface ChatMentionTextProps {
  readonly content: string;
  readonly mentions: readonly ChatMention[] | undefined;
  readonly copy: ChatCopy;
  /** "own" on the caller's accent bubble. */
  readonly tone?: "own" | "default";
}

/**
 * A message's text with its mentions as chips (UC-CHT-G01, G03, G04). A person shows their current name (the stored text when
 * they are not in the Workspace list), @channel and @here show as stored, and a #channel link opens the channel. A span that
 * does not fit the text is plain text.
 */
export function ChatMentionText({
  content,
  mentions,
  copy,
  tone = "default",
}: ChatMentionTextProps) {
  const context = useChatMentions();
  const chip = tone === "own" ? OWN_CHIP : CHIP;
  if (!mentions || mentions.length === 0) return <>{content}</>;
  return (
    <>
      {splitByMentions(content, mentions).map((segment, index) => {
        if (segment.type === "text") {
          return <Fragment key={index}>{segment.text}</Fragment>;
        }
        const { mention } = segment;
        if (mention.kind === "user" && mention.userId) {
          const name = context.personName(mention.userId);
          const shown = name ? `@${name}` : segment.text;
          return (
            <span
              key={index}
              className={chip}
              data-mention="user"
              data-mention-self={
                mention.userId === context.viewerId ? "true" : undefined
              }
            >
              {shown}
            </span>
          );
        }
        if (mention.kind === "channel-link" && mention.spaceId) {
          const name = context.channelName(mention.spaceId);
          const shown = name ? `#${name}` : segment.text;
          const spaceId = mention.spaceId;
          const open = context.onOpenChannel;
          if (!open) {
            return (
              <span key={index} className={chip} data-mention="channel-link">
                {shown}
              </span>
            );
          }
          return (
            <button
              key={index}
              type="button"
              data-mention="channel-link"
              aria-label={fillCopy(copy.mentions.chipChannel, {
                name: name ?? segment.text.replace(/^#/, ""),
              })}
              onClick={() => open(spaceId)}
              className={cn(
                "inline rounded-sm font-semibold focus-visible:ring-2 focus-visible:ring-focus-ring focus-visible:outline-none",
                tone === "own"
                  ? "bg-primary-foreground/20 px-1 text-primary-foreground"
                  : "bg-accent px-1 text-accent-foreground",
              )}
            >
              {shown}
            </button>
          );
        }
        return (
          <span key={index} className={chip} data-mention="broadcast">
            {segment.text}
          </span>
        );
      })}
    </>
  );
}

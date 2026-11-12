import { createContext, useContext } from "react";

/**
 * What a message needs to render its mentions from identities (UC-CHT-G01, G04): the viewer, the names of the people and
 * channels, and how a channel link opens its channel. The host (ChatWorkspace) provides it; without a provider a mention
 * shows the text the message stored.
 */
export interface ChatMentionContextValue {
  readonly viewerId: string | null;
  readonly personName: (userId: string) => string | null;
  readonly channelName: (spaceId: string) => string | null;
  readonly onOpenChannel?: (spaceId: string) => void;
}

const EMPTY: ChatMentionContextValue = {
  viewerId: null,
  personName: () => null,
  channelName: () => null,
};

const ChatMentionContext = createContext<ChatMentionContextValue>(EMPTY);

export const ChatMentionProvider = ChatMentionContext.Provider;

export function useChatMentions(): ChatMentionContextValue {
  return useContext(ChatMentionContext);
}

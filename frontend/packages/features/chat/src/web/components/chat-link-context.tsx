import { createContext, useContext } from "react";
import type { ChatMessage } from "../../core/types/chat";

/**
 * Builds the address that opens a message (UC-CHT-B10), or null when the message has none (a thread reply that is not
 * also in the channel). The workspace supplies it; outside a provider no Copy link entry is shown.
 */
const ChatMessageLinkContext = createContext<
  ((message: ChatMessage) => string | null) | null
>(null);

export const ChatMessageLinkProvider = ChatMessageLinkContext.Provider;

export function useChatMessageLink() {
  return useContext(ChatMessageLinkContext);
}

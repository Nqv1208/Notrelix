import { createContext, useContext, type ReactNode } from "react";
import type { ChatMessage } from "../../core/types/chat";
import { ChatMessageLinkProvider } from "./chat-link-context";
import type { ChatCallKind } from "../../core/types/call";

/** Starts a call in the open conversation, so a missed-call line in the timeline can offer "Gọi lại". Without a provider no button shows. */
const ChatCallRedialContext = createContext<
  ((kind: ChatCallKind) => void) | null
>(null);

export const ChatCallRedialProvider = ChatCallRedialContext.Provider;

export function useChatCallRedial() {
  return useContext(ChatCallRedialContext);
}

/** The message-link and the call-back contexts in one wrapper, so the workspace tree keeps its depth. */
export function ChatLinkAndRedialProvider({
  link,
  redial,
  children,
}: {
  readonly link: ((message: ChatMessage) => string | null) | null;
  readonly redial: ((kind: ChatCallKind) => void) | null;
  readonly children: ReactNode;
}) {
  return (
    <ChatMessageLinkProvider value={link}>
      <ChatCallRedialContext.Provider value={redial}>
        {children}
      </ChatCallRedialContext.Provider>
    </ChatMessageLinkProvider>
  );
}

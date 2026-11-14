import { createContext, useContext } from "react";

/**
 * The message a search hit or a link points at (UC-CHT-H07). A message row that matches draws a ring, scrolls itself into view and
 * takes focus once; the workspace clears the id after a few seconds. Outside a provider nothing is highlighted.
 */
const ChatHighlightContext = createContext<string | null>(null);

export const ChatHighlightProvider = ChatHighlightContext.Provider;

export function useChatHighlight(): string | null {
  return useContext(ChatHighlightContext);
}

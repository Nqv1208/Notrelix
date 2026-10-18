import { createContext } from "react";
import { en, type BoardHeaderCopy } from "./board-header.copy";

/**
 * Header copy for the surfaces below the board route. The route (container)
 * provides the active-locale copy; the board layout shell renders the toolbar
 * without forwarding props, so the pure surfaces read it from here. Without a
 * provider (stories, isolated tests) the English copy applies.
 */
export const BoardHeaderCopyContext = createContext<BoardHeaderCopy>(en);

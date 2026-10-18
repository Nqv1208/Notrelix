import { createContext } from "react";
import type { BoardHeaderData } from "./board-header-surface";

/**
 * Header data for the board route: the Space crumb, the access summary and
 * the header actions. The route (container) composes it from the board, the
 * Space navigation and the board access query; the board layout shell renders
 * the toolbar without forwarding props, so the toolbar reads it from here.
 * Without a provider (stories, isolated tests) the header shows the title and
 * the view tabs only.
 */
export const BoardHeaderDataContext = createContext<BoardHeaderData | null>(
  null,
);

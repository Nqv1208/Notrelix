const BOARD_PATH_PATTERN = /^\/workspaces\/[^/?#]+\/boards\/([^/?#]+)/;

/** The board open in the route (board, settings or item page), if any. */
export function getActiveBoardIdFromPathname(pathname: string): string | null {
  const match = BOARD_PATH_PATTERN.exec(pathname);
  if (!match?.[1]) return null;

  try {
    return decodeURIComponent(match[1]);
  } catch {
    return null;
  }
}

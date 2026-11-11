const CHANNEL_PATH_PATTERN = /^\/workspaces\/[^/?#]+\/chat\/([^/?#]+)/;

/** The Chat channel open in the route, if any. Only this channel's stream is subscribed (UC-CHT-C01). */
export function getActiveChannelIdFromPathname(
  pathname: string,
): string | null {
  const match = CHANNEL_PATH_PATTERN.exec(pathname);
  if (!match?.[1]) return null;

  try {
    return decodeURIComponent(match[1]);
  } catch {
    return null;
  }
}

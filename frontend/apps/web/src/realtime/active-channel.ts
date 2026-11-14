const CHANNEL_PATH_PATTERN =
  /^\/workspaces\/[^/?#]+\/chat\/(?:dm\/([^/?#]+)|([^/?#]+))/;

/**
 * The Chat space open in the route, if any: a channel (`/chat/{id}`) or a direct or group conversation
 * (`/chat/dm/{id}`; both are spaces). Only this space's stream is subscribed (UC-CHT-C01).
 */
export function getActiveChannelIdFromPathname(
  pathname: string,
): string | null {
  const match = CHANNEL_PATH_PATTERN.exec(pathname);
  const id = match?.[1] ?? (match?.[2] === "dm" ? undefined : match?.[2]);
  if (!id) return null;

  try {
    return decodeURIComponent(id);
  } catch {
    return null;
  }
}

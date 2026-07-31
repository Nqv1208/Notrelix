const WORKSPACE_PATH_PATTERN = /^\/workspaces\/([^/?#]+)/;

export function getActiveWorkspaceIdFromPathname(pathname: string): string | null {
  const match = WORKSPACE_PATH_PATTERN.exec(pathname);
  if (!match?.[1]) return null;

  try {
    return decodeURIComponent(match[1]);
  } catch {
    return null;
  }
}

import type { ReactNode } from "react";

/**
 * Superseded by `WorkspaceShell` (WX-SHELL-004). The frame rendered the
 * mock-only workspace view tabs (removed per WX-DEC-010) inside a nested
 * `h-screen` scroll container. It is no longer mounted; delete it together
 * with `shell/sidebar/` once that directory's pending work lands.
 */
export function WorkspaceTabbedFrame({ children }: { children: ReactNode }) {
  return <>{children}</>;
}

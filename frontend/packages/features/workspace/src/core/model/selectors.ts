import type { WorkspaceMember, WorkspaceView } from '../types/workspace';

export function isOwner(member?: WorkspaceMember | null): boolean {
  return member?.role === 'owner';
}

export function isAdmin(member?: WorkspaceMember | null): boolean {
  return member?.role === 'owner' || member?.role === 'admin';
}

export function getActiveView(views: WorkspaceView[], activeViewId?: string): WorkspaceView | undefined {
  if (!activeViewId) return views.find((v) => v.isDefault) || views[0];
  return views.find((v) => v.id === activeViewId);
}

export function sortViews(views: WorkspaceView[]): WorkspaceView[] {
  return [...views].sort((a, b) => a.position - b.position);
}

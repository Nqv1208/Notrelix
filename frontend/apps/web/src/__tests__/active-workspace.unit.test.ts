import { describe, expect, it } from 'vitest';
import { getActiveWorkspaceIdFromPathname } from '../realtime/active-workspace';

describe('getActiveWorkspaceIdFromPathname', () => {
  it('extracts workspace id from workspace-scoped routes', () => {
    expect(getActiveWorkspaceIdFromPathname('/workspaces/ws-1')).toBe('ws-1');
    expect(getActiveWorkspaceIdFromPathname('/workspaces/ws-1/boards/board-1')).toBe('ws-1');
    expect(getActiveWorkspaceIdFromPathname('/workspaces/ws%202/docs/doc-1')).toBe('ws 2');
  });

  it('returns null outside workspace routes or for malformed ids', () => {
    expect(getActiveWorkspaceIdFromPathname('/home')).toBeNull();
    expect(getActiveWorkspaceIdFromPathname('/workspaces/%E0%A4%A')).toBeNull();
  });
});

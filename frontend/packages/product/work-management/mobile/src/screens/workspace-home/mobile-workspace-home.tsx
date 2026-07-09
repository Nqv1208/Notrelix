import React from 'react';

export interface MobileWorkspaceHomeProps {
  workspaceId: string;
}

export function MobileWorkspaceHome({
  workspaceId,
}: MobileWorkspaceHomeProps) {
  return (
    <div>
      <h1>Workspace Home</h1>
      <p>Workspace: {workspaceId}</p>
      {/* TODO: Implement mobile workspace home with board list */}
    </div>
  );
}

import React from 'react';

export interface MobileBoardScreenProps {
  boardId: string;
  workspaceId: string;
}

export function MobileBoardScreen({
  boardId,
  workspaceId,
}: MobileBoardScreenProps) {
  return (
    <div>
      <h1>Board: {boardId}</h1>
      <p>Workspace: {workspaceId}</p>
      {/* TODO: Implement mobile board list view */}
    </div>
  );
}

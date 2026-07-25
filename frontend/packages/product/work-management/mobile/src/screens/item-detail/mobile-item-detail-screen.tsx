import React from 'react';

export interface MobileItemDetailScreenProps {
  itemId: string;
  boardId: string;
}

export function MobileItemDetailScreen({
  itemId,
  boardId,
}: MobileItemDetailScreenProps) {
  return (
    <div>
      <h1>Item: {itemId}</h1>
      <p>Board: {boardId}</p>
      {/* TODO: Implement mobile item detail with bottom-sheet editors */}
    </div>
  );
}

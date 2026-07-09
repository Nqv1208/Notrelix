import type { FullBoardResponse } from '@notrelix/work-management-core';
import { boardFixture, boardGroupFixture } from '../fixtures/board.fixture';
import { itemFixture } from '../fixtures/item.fixture';

export function createBoardSnapshot(
  itemCount = 3,
  overrides?: Partial<FullBoardResponse>,
): FullBoardResponse {
  const board = boardFixture();
  const items = Array.from({ length: itemCount }, (_, i) =>
    itemFixture({ id: `card-${i}`, position: i + 1, title: `Item ${i + 1}` }),
  );
  const group = boardGroupFixture({ cards: items });

  return {
    board,
    groups: [group],
    fieldDefinitions: board.fieldDefinitions,
    ...overrides,
  };
}

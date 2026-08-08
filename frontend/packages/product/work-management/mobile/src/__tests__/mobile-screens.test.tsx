import { describe, expect, it } from 'vitest';
import { renderToStaticMarkup } from 'react-dom/server';
import React from 'react';
import { MobileBoardScreen } from '../screens/board-screen/mobile-board-screen';
import { MobileItemDetailScreen } from '../screens/item-detail/mobile-item-detail-screen';
import { MobileWorkspaceHome } from '../screens/workspace-home/mobile-workspace-home';

describe('wm-mobile screens', () => {
  it('MobileBoardScreen renders the board and workspace ids it receives', () => {
    const markup = renderToStaticMarkup(
      React.createElement(MobileBoardScreen, {
        boardId: 'board-1',
        workspaceId: 'ws-1',
      }),
    );

    expect(markup).toContain('Board: board-1');
    expect(markup).toContain('Workspace: ws-1');
  });

  it('MobileItemDetailScreen renders the item and board ids it receives', () => {
    const markup = renderToStaticMarkup(
      React.createElement(MobileItemDetailScreen, {
        itemId: 'item-9',
        boardId: 'board-2',
      }),
    );

    expect(markup).toContain('Item: item-9');
    expect(markup).toContain('Board: board-2');
  });

  it('MobileWorkspaceHome renders the workspace id it receives', () => {
    const markup = renderToStaticMarkup(
      React.createElement(MobileWorkspaceHome, { workspaceId: 'ws-3' }),
    );

    expect(markup).toContain('Workspace Home');
    expect(markup).toContain('ws-3');
  });
});

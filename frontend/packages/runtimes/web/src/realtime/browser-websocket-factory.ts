import type { RealtimeConnectionDescriptor, WebSocketFactory, WebSocketLike } from '@notrelix/realtime';

type BrowserSocketConstructor = new (url: string, protocols?: string | string[]) => WebSocketLike;

export function createBrowserWebSocketFactory(): WebSocketFactory {
  return (descriptor: RealtimeConnectionDescriptor) => {
    if (typeof WebSocket === 'undefined') {
      throw new Error('WebSocket is not supported in this browser runtime.');
    }

    return new (WebSocket as unknown as BrowserSocketConstructor)(
      descriptor.url,
      descriptor.protocols ? [...descriptor.protocols] : undefined,
    );
  };
}

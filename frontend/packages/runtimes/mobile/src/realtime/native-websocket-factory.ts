import type { WebSocketFactory } from "@notrelix/realtime";

export function createNativeWebSocketFactory(): WebSocketFactory {
  return (descriptor) =>
    new WebSocket(
      descriptor.url,
      descriptor.protocols as string[],
    ) as unknown as import("@notrelix/realtime").WebSocketLike;
}

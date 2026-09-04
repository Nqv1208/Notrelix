import { describe, expect, it, vi } from "vitest";
import {
  installPureUiNetworkGuard,
  PureUiNetworkAccessError,
} from "./pure-ui-network-guard";

describe("installPureUiNetworkGuard", () => {
  it("fails fetch, XHR and WebSocket and restores previous globals", () => {
    const target = {
      fetch: vi.fn(),
      XMLHttpRequest: class XMLHttpRequest {
        open() {}
      },
      WebSocket: class WebSocket {},
    } as unknown as typeof globalThis;

    const previousFetch = target.fetch;
    const previousXmlHttpRequest = target.XMLHttpRequest;
    const previousWebSocket = target.WebSocket;

    const guard = installPureUiNetworkGuard(target);

    expect(() => target.fetch("https://api.example.test")).toThrow(
      PureUiNetworkAccessError,
    );
    expect(() => {
      const xhr = new target.XMLHttpRequest();
      xhr.open("GET", "/api/v1/workspaces");
    }).toThrow(/XMLHttpRequest.*\/api\/v1\/workspaces/);
    expect(() => new target.WebSocket("wss://example.test")).toThrow(
      /WebSocket.*wss:\/\/example.test/,
    );

    guard.restore();
    guard.restore();

    expect(target.fetch).toBe(previousFetch);
    expect(target.XMLHttpRequest).toBe(previousXmlHttpRequest);
    expect(target.WebSocket).toBe(previousWebSocket);
  });
});

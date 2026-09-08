export class PureUiNetworkAccessError extends Error {
  constructor(
    transport: "fetch" | "XMLHttpRequest" | "WebSocket",
    target: string,
  ) {
    super(`Pure UI attempted ${transport} network access: ${target}`);
    this.name = "PureUiNetworkAccessError";
  }
}

export interface PureUiNetworkGuard {
  restore(): void;
}

type GuardedWindow = typeof globalThis & {
  fetch?: typeof fetch;
  XMLHttpRequest?: typeof XMLHttpRequest;
  WebSocket?: typeof WebSocket;
};

function targetFromFetchInput(input: RequestInfo | URL): string {
  if (typeof input === "string") return input;
  if (input instanceof URL) return input.toString();
  return "url" in input ? input.url : String(input);
}

export function installPureUiNetworkGuard(
  target: GuardedWindow = globalThis as GuardedWindow,
): PureUiNetworkGuard {
  const previousFetch = target.fetch;
  const previousXmlHttpRequest = target.XMLHttpRequest;
  const previousWebSocket = target.WebSocket;

  target.fetch = ((input: RequestInfo | URL) => {
    throw new PureUiNetworkAccessError("fetch", targetFromFetchInput(input));
  }) as typeof fetch;

  class GuardedXMLHttpRequest extends (previousXmlHttpRequest ?? class {}) {
    open(_method: string, url: string | URL) {
      throw new PureUiNetworkAccessError("XMLHttpRequest", String(url));
    }
  }

  class GuardedWebSocket {
    constructor(url: string | URL) {
      throw new PureUiNetworkAccessError("WebSocket", String(url));
    }
  }

  target.XMLHttpRequest = GuardedXMLHttpRequest as typeof XMLHttpRequest;
  target.WebSocket = GuardedWebSocket as typeof WebSocket;

  let restored = false;
  return {
    restore() {
      if (restored) return;
      restored = true;
      if (previousFetch) {
        target.fetch = previousFetch;
      } else {
        Reflect.deleteProperty(target, "fetch");
      }
      if (previousXmlHttpRequest) {
        target.XMLHttpRequest = previousXmlHttpRequest;
      } else {
        Reflect.deleteProperty(target, "XMLHttpRequest");
      }
      if (previousWebSocket) {
        target.WebSocket = previousWebSocket;
      } else {
        Reflect.deleteProperty(target, "WebSocket");
      }
    },
  };
}

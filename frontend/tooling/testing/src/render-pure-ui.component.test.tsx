import React from "react";
import { afterEach, describe, expect, it, vi } from "vitest";

const { queryClientConstructor } = vi.hoisted(() => ({
  queryClientConstructor: vi.fn(),
}));

vi.mock("@tanstack/react-query", () => ({
  QueryClient: queryClientConstructor,
  QueryClientProvider: ({ children }: { children: React.ReactNode }) =>
    children,
}));

import { renderPureUi, screen } from "../web-test-utils";
import { PureUiNetworkAccessError } from "./pure-ui-network-guard";

afterEach(() => {
  queryClientConstructor.mockClear();
});

describe("renderPureUi", () => {
  it("renders without constructing QueryClient and creates a deterministic portal host", () => {
    const result = renderPureUi(<button type="button">Pure action</button>);

    expect(screen.getByRole("button", { name: "Pure action" })).toBeDefined();
    expect(queryClientConstructor).not.toHaveBeenCalled();
    expect(result.pureUiPortalHost.id).toBe("notrelix-pure-ui-portal-root");

    result.unmount();
    expect(document.getElementById("notrelix-pure-ui-portal-root")).toBeNull();
  });

  it("installs the pure network guard during render scope and restores it on unmount", () => {
    const originalFetch = globalThis.fetch;
    const result = renderPureUi(<div>Pure scope</div>);

    expect(() => fetch("https://api.example.invalid")).toThrow(
      PureUiNetworkAccessError,
    );

    result.unmount();
    expect(globalThis.fetch).toBe(originalFetch);
  });
});

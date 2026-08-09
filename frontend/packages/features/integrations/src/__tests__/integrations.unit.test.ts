import { describe, it, expect } from "vitest";
import { integrationsQueryKeys } from "../core/query/keys";

describe("integrationsQueryKeys", () => {
  it("should generate connections query key", () => {
    expect(integrationsQueryKeys.connections("ws-555")).toEqual([
      "integrations",
      "connections",
      "ws-555",
    ]);
  });

  it("should generate webhook integrations query key", () => {
    expect(integrationsQueryKeys.webhooks("ws-555")).toEqual([
      "integrations",
      "webhooks",
      "ws-555",
    ]);
  });
});

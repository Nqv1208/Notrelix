import { describe, expect, it } from "vitest";
import { assertNotrelixQueryKey } from "@notrelix/query";
import { automationQueryKeys } from "../query/keys";

describe("automationQueryKeys Q7 purity tests", () => {
  it("every automationQueryKey satisfies assertNotrelixQueryKey and contains workspaceId", () => {
    const wsId = "ws-999";

    const keys = [
      automationQueryKeys.all(wsId),
      automationQueryKeys.rules(wsId),
      automationQueryKeys.ruleDetail(wsId, "r1"),
      automationQueryKeys.executionHistory(wsId),
      automationQueryKeys.executionHistory(wsId, "r1"),
      automationQueryKeys.executionDetail(wsId, "e1"),
      automationQueryKeys.templates(wsId),
    ];

    for (const key of keys) {
      expect(() => assertNotrelixQueryKey(key)).not.toThrow();
      expect(key[0]).toBe("workspace");
      expect(key[1]).toBe(wsId);
      expect(key[2]).toBe("automation");
    }
  });
});

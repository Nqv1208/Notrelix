import { describe, expect, it } from "vitest";
import { buildAutomationExecutionEvent } from "../fixtures/execution-events";
import { createFakeAutomationRepositories } from "../fakes/fake-automation-repositories";

describe("automation testing fixtures", () => {
  it("creates fake repositories with rule and execution behavior", async () => {
    const repositories = createFakeAutomationRepositories();
    const rules = await repositories.rules.listByWorkspace("workspace-1");
    const execution = await repositories.rules.test(rules[0]!.id);
    const history = await repositories.executions.listHistory({
      workspaceId: "workspace-1",
      ruleId: rules[0]!.id,
    });

    expect(rules).toHaveLength(1);
    expect(execution.status).toBe("queued");
    expect(history.items).toHaveLength(1);
  });

  it("builds automation execution realtime envelopes", () => {
    expect(buildAutomationExecutionEvent({ sequence: 5 })).toMatchObject({
      eventType: "automation.execution.started",
      sequence: 5,
      payload: {
        executionId: "execution-1",
        ruleId: "rule-1",
      },
    });
  });
});

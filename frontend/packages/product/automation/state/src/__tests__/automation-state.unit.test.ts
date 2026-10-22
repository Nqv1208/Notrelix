import { QueryClient } from "@tanstack/react-query";
import { describe, expect, it, vi } from "vitest";
import {
  buildAutomationExecutionEvent,
  createFakeAutomationRepositories,
} from "./fixtures";
import { automationQueryKeys } from "../query/keys";
import { setAutomationRuleEnabledCommand } from "../commands/rule-commands";
import { createAutomationExecutionRealtimeAdapter } from "../realtime/execution-adapter";

describe("automation state", () => {
  it("invalidates rule caches when enabling a rule", async () => {
    const queryClient = new QueryClient();
    const invalidateQueries = vi.spyOn(queryClient, "invalidateQueries");
    const repositories = createFakeAutomationRepositories();

    const result = await setAutomationRuleEnabledCommand({
      queryClient,
      repositories,
      workspaceId: "workspace-1",
      ruleId: "rule-1",
      enabled: false,
      commandId: "command-1",
    });

    expect(result.isEnabled).toBe(false);
    expect(invalidateQueries).toHaveBeenCalledWith({
      queryKey: automationQueryKeys.rules("workspace-1"),
    });
    expect(invalidateQueries).toHaveBeenCalledWith({
      queryKey: automationQueryKeys.ruleDetail("workspace-1", "rule-1"),
    });
  });

  it("treats execution events as invalidation signals for the workspace automation scope", async () => {
    const adapter = createAutomationExecutionRealtimeAdapter();
    const invalidateQueries = vi.fn();
    const context = { workspaceId: "workspace-1", invalidateQueries };

    const event = buildAutomationExecutionEvent({
      eventType: "automation.execution.completed",
    });
    expect(adapter.supports(event)).toBe(true);
    await adapter.validateAndHandle(event, context);

    expect(invalidateQueries).toHaveBeenCalledWith([
      [...automationQueryKeys.all("workspace-1")],
    ]);
  });

  it("ignores execution events of another workspace", async () => {
    const adapter = createAutomationExecutionRealtimeAdapter();
    const invalidateQueries = vi.fn();

    await adapter.validateAndHandle(
      buildAutomationExecutionEvent({ workspaceId: "workspace-2" }),
      { workspaceId: "workspace-1", invalidateQueries },
    );

    expect(invalidateQueries).not.toHaveBeenCalled();
  });
});

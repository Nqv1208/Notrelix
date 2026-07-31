import { QueryClient } from '@tanstack/react-query';
import { describe, expect, it, vi } from 'vitest';
import { buildAutomationExecutionEvent, createFakeAutomationRepositories } from '@notrelix/automation-testing';
import { automationQueryKeys } from '../query/keys';
import { setAutomationRuleEnabledCommand } from '../commands/rule-commands';
import { createAutomationExecutionRealtimeAdapter } from '../realtime/execution-adapter';

describe('automation state', () => {
  it('invalidates rule caches when enabling a rule', async () => {
    const queryClient = new QueryClient();
    const invalidateQueries = vi.spyOn(queryClient, 'invalidateQueries');
    const repositories = createFakeAutomationRepositories();

    const result = await setAutomationRuleEnabledCommand({
      queryClient,
      repositories,
      workspaceId: 'workspace-1',
      ruleId: 'rule-1',
      enabled: false,
      commandId: 'command-1',
    });

    expect(result.isEnabled).toBe(false);
    expect(invalidateQueries).toHaveBeenCalledWith({
      queryKey: automationQueryKeys.rules('workspace-1'),
    });
    expect(invalidateQueries).toHaveBeenCalledWith({
      queryKey: automationQueryKeys.ruleDetail('rule-1'),
    });
  });

  it('reconciles execution detail from realtime events and ignores stale sequence', async () => {
    const queryClient = new QueryClient();
    const invalidateQueries = vi.fn();
    const adapter = createAutomationExecutionRealtimeAdapter(queryClient);
    const context = { workspaceId: 'workspace-1', invalidateQueries };

    await adapter.validateAndHandle(
      buildAutomationExecutionEvent({
        eventType: 'automation.execution.started',
        sequence: 2,
        payload: { status: 'running', sequence: 2 },
      }),
      context
    );
    await adapter.validateAndHandle(
      buildAutomationExecutionEvent({
        eventType: 'automation.execution.completed',
        sequence: 1,
        payload: { status: 'succeeded', sequence: 1 },
      }),
      context
    );

    expect(
      queryClient.getQueryData(automationQueryKeys.executionDetail('execution-1'))
    ).toMatchObject({
      id: 'execution-1',
      status: 'running',
      sequence: 2,
    });
    expect(invalidateQueries).toHaveBeenCalledWith([
      automationQueryKeys.executionHistory('workspace-1', 'rule-1'),
    ]);
  });
});

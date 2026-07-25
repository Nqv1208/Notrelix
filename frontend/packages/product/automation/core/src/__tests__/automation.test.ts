import { describe, it, expect } from 'vitest';

type ExecutionStatus = 'Queued' | 'Running' | 'Succeeded' | 'Failed' | 'Cancelled';

function transitionExecutionStatus(current: ExecutionStatus, target: ExecutionStatus): boolean {
  const validTransitions: Record<ExecutionStatus, ExecutionStatus[]> = {
    Queued: ['Running', 'Cancelled'],
    Running: ['Succeeded', 'Failed', 'Cancelled'],
    Succeeded: [],
    Failed: [],
    Cancelled: [],
  };
  return validTransitions[current]?.includes(target) ?? false;
}

describe('Automation Execution State Machine Invariants', () => {
  it('allows Queued -> Running', () => {
    expect(transitionExecutionStatus('Queued', 'Running')).toBe(true);
  });

  it('allows Running -> Succeeded or Failed', () => {
    expect(transitionExecutionStatus('Running', 'Succeeded')).toBe(true);
    expect(transitionExecutionStatus('Running', 'Failed')).toBe(true);
  });

  it('prevents terminal states Succeeded/Failed from mutating', () => {
    expect(transitionExecutionStatus('Succeeded', 'Running')).toBe(false);
    expect(transitionExecutionStatus('Failed', 'Queued')).toBe(false);
  });
});

export interface CommandResult {
  success: boolean;
  serverRevision?: number;
  error?: string;
}

export interface MockCommandBus {
  execute: (command: unknown) => Promise<CommandResult>;
  getHistory: () => unknown[];
  reset: () => void;
}

export function mockCommandBus(): MockCommandBus {
  const history: unknown[] = [];

  return {
    async execute(command: unknown) {
      history.push(command);
      return { success: true, serverRevision: 1 };
    },
    getHistory() {
      return [...history];
    },
    reset() {
      history.length = 0;
    },
  };
}

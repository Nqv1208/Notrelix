/** A server refusal because the Account is Suspended or Closed (403 `account.suspended`), told to the shell. */
export interface AccountSuspensionSignal {
  notify(): void;
  subscribe(listener: () => void): () => void;
}

export function createAccountSuspensionSignal(): AccountSuspensionSignal {
  const listeners = new Set<() => void>();
  return {
    notify() {
      listeners.forEach((listener) => listener());
    },
    subscribe(listener) {
      listeners.add(listener);
      return () => {
        listeners.delete(listener);
      };
    },
  };
}

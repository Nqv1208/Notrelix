import type { MockRequest } from "./mock-request";

export interface MockRequestJournalEntry extends MockRequest {
  readonly matchedHandlerId: string;
  readonly occurredAt: string;
}

export function createMockRequestJournal() {
  const entries: MockRequestJournalEntry[] = [];
  return {
    record(entry: MockRequestJournalEntry) {
      entries.push(structuredClone(entry));
    },
    getEntries(): readonly MockRequestJournalEntry[] {
      return structuredClone(entries);
    },
    clear() {
      entries.length = 0;
    },
  };
}

export type MockRequestJournal = ReturnType<typeof createMockRequestJournal>;

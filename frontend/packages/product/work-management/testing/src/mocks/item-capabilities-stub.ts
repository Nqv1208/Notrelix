/**
 * The read and view-recording side of the item capabilities service for component tests: the card popup
 * records that the member opened the item and reads who viewed and voted, none of which a view test is about.
 */
export function itemCapabilitiesStub() {
  return {
    recordView: async (): Promise<void> => undefined,
    getViewers: async () => ({ count: 0, viewers: [] as never[] }),
    getVotes: async () => ({ count: 0, voted: false, voters: [] as never[] }),
    getBoardChecklists: async () => [] as never[],
  };
}

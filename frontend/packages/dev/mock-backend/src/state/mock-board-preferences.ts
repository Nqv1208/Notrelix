/**
 * A member's personal view state in the mock world (the producer's
 * `BoardViewUserPreference`): one opaque criteria JSON and one opaque columns
 * JSON per member, board and view. Personal by construction: another member
 * never reads it.
 */
export class MockBoardPreferences {
  private readonly criteria = new Map<string, string>();
  private readonly columns = new Map<string, string>();

  private static key(userId: string, boardId: string, viewId: string): string {
    return `${userId}:${boardId}:${viewId}`;
  }

  clear(): void {
    this.criteria.clear();
    this.columns.clear();
  }

  getColumns(userId: string, boardId: string, viewId: string): string | null {
    return (
      this.columns.get(MockBoardPreferences.key(userId, boardId, viewId)) ??
      null
    );
  }

  /** Blank clears the columns. */
  setColumns(
    userId: string,
    boardId: string,
    viewId: string,
    columns: string,
  ): void {
    const key = MockBoardPreferences.key(userId, boardId, viewId);
    if (columns.trim() === "") this.columns.delete(key);
    else this.columns.set(key, columns);
  }

  get(userId: string, boardId: string, viewId: string): string | null {
    return (
      this.criteria.get(MockBoardPreferences.key(userId, boardId, viewId)) ??
      null
    );
  }

  /** Blank clears the criteria. */
  set(userId: string, boardId: string, viewId: string, criteria: string): void {
    const key = MockBoardPreferences.key(userId, boardId, viewId);
    if (criteria.trim() === "") this.criteria.delete(key);
    else this.criteria.set(key, criteria);
  }
}

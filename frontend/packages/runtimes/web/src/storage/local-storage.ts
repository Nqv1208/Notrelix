export function createLocalStorageAdapter() {
  return {
    getItem(key: string): string | null {
      try {
        return localStorage.getItem(key);
      } catch {
        return null;
      }
    },
    setItem(key: string, value: string): void {
      try {
        localStorage.setItem(key, value);
      } catch {
        // Ignored
      }
    },
    removeItem(key: string): void {
      try {
        localStorage.removeItem(key);
      } catch {
        // Ignored
      }
    },
    clear(): void {
      try {
        localStorage.clear();
      } catch {
        // Ignored
      }
    },
  };
}
export type LocalStorageAdapter = ReturnType<typeof createLocalStorageAdapter>;

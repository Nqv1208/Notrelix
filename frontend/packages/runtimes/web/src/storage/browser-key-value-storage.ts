import type { KeyValueStorage } from "@notrelix/platform";
import type { TelemetryPort } from "@notrelix/observability";

export function createBrowserKeyValueStorage(
  telemetry?: TelemetryPort,
): KeyValueStorage {
  return {
    getItem(key: string): string | null {
      try {
        return typeof window !== "undefined"
          ? window.localStorage.getItem(key)
          : null;
      } catch (error) {
        telemetry?.reportError(
          error instanceof Error ? error : new Error(String(error)),
        );
        return null;
      }
    },
    setItem(key: string, value: string): void {
      try {
        if (typeof window !== "undefined") {
          window.localStorage.setItem(key, value);
        }
      } catch (error) {
        telemetry?.reportError(
          error instanceof Error ? error : new Error(String(error)),
        );
      }
    },
    removeItem(key: string): void {
      try {
        if (typeof window !== "undefined") {
          window.localStorage.removeItem(key);
        }
      } catch (error) {
        telemetry?.reportError(
          error instanceof Error ? error : new Error(String(error)),
        );
      }
    },
    clear(): void {
      try {
        if (typeof window !== "undefined") {
          window.localStorage.clear();
        }
      } catch (error) {
        telemetry?.reportError(
          error instanceof Error ? error : new Error(String(error)),
        );
      }
    },
  };
}

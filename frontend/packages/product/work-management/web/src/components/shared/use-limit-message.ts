import { useCallback } from "react";
import { limitOf } from "@notrelix/work-management-state";
import { limitCopy } from "../board-settings/board-settings-format";
import { useBoardSettingsCopy } from "../board-settings/use-board-settings-copy";

/**
 * What a write refused over a published board limit says (UC-WM-T03, D78.8),
 * in the words board settings uses: the limit, its max and the way out. Null
 * when the error is not a `work-management.limit.*` refusal, so the surface
 * keeps its own message.
 */
export function useLimitMessage(): (error: unknown) => string | null {
  const copy = useBoardSettingsCopy();
  return useCallback(
    (error: unknown) => {
      const limit = limitOf(error);
      return limit ? limitCopy(copy, limit) : null;
    },
    [copy],
  );
}

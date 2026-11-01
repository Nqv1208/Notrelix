import { useEffect } from "react";
import { useParams } from "@tanstack/react-router";
import { useAppRuntime } from "@notrelix/runtime-web";
import { useFullBoard } from "@notrelix/work-management-state";
import { RouterError, RouterNotFound } from "../../../../router/router-states";

/**
 * The board route's error screen. Losing access while the board is open (removed, a lowered Space or Team
 * entry, Private) changes what many queries answer at once; if anything in the open screen throws while that
 * settles, a person who lost the board must land on the same "no longer open it" state as a direct visit,
 * never on a generic failure. Any other error keeps the generic screen. The error is always reported.
 */
export function BoardRouteError({
  error,
  reset,
}: {
  error: unknown;
  reset?: () => void;
}) {
  const { workspaceId, boardId } = useParams({ strict: false }) as {
    workspaceId?: string;
    boardId?: string;
  };
  const runtime = useAppRuntime();
  const { error: boardError } = useFullBoard(boardId, workspaceId);
  const kind = (boardError as { kind?: string } | null)?.kind;
  const gone = kind === "not_found" || kind === "forbidden";

  useEffect(() => {
    runtime.telemetry.reportError(error, {
      context: "board.route",
      boardGone: gone,
    });
  }, [error, gone, runtime.telemetry]);

  return gone ? <RouterNotFound /> : <RouterError reset={reset} />;
}

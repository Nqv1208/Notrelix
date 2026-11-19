import { Skeleton } from "@notrelix/ui-web";
import type { WorkspaceCommandCopy } from "./workspace-command.copy";

/** The server search of the typed text (the palette's own lists answer at once). */
export interface PaletteSearchState {
  readonly status: "loading" | "error";
  readonly onRetry: () => void;
}

/**
 * Loading (`Wsp-Var-Cmd-Loading-T`) and failed (`Wsp-Var-Cmd-Error-T`) answers of the
 * server search. A failure says so and offers a retry: it never reads as "no match".
 */
export function PaletteSearchStatus({
  copy,
  search,
  showLoading,
}: {
  copy: WorkspaceCommandCopy;
  search: PaletteSearchState;
  /** Skeleton rows only when nothing local matched; local rows stay usable. */
  showLoading: boolean;
}) {
  if (search.status === "error") {
    return (
      <div
        role="alert"
        data-f="searcherr"
        className="flex flex-col items-center gap-1.5 px-[18px] py-6 text-center text-muted-foreground"
      >
        <b className="block text-item-heading font-bold text-foreground">
          {copy.searchError.title}
        </b>
        <span>{copy.searchError.text}</span>
        <button
          type="button"
          onClick={search.onRetry}
          className="mt-1 box-border h-[30px] cursor-pointer rounded-[7px] border border-line-strong bg-popover px-3 text-compact leading-[1.45] font-semibold text-foreground outline-none focus-visible:ring-2 focus-visible:ring-focus-ring dark:bg-card"
        >
          {copy.searchError.retry}
        </button>
      </div>
    );
  }
  if (!showLoading) return null;
  return (
    <div
      role="status"
      aria-label={copy.loading}
      data-f="searchload"
      className="flex flex-col gap-3 px-[18px] py-4"
    >
      {[60, 51, 42, 33, 24].map((width) => (
        <div key={width} className="flex items-center gap-3">
          <Skeleton className="size-5 rounded-[5px]" />
          <Skeleton className="h-3.5" style={{ width: `${width}%` }} />
        </div>
      ))}
    </div>
  );
}

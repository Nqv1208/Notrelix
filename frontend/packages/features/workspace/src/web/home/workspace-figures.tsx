import { Skeleton } from "@notrelix/ui-web";
import type { WorkspaceFigures } from "../../core/types/workspace";
import type { SectionState } from "./home-models";
import type { WorkspaceHomeCopy } from "./workspace-home.copy";

/**
 * The workspace figures for Owner and Admin (`Final-Home` figures row): active members, Spaces and pending
 * invitations. Loading, failure and a refused read each speak for themselves; the Billing seat tile waits for
 * the Billing module.
 */
export function WorkspaceFiguresStrip({
  copy,
  state,
  onRetry,
}: {
  copy: WorkspaceHomeCopy;
  state: SectionState<WorkspaceFigures>;
  onRetry: () => void;
}) {
  const text = copy.home.figures;
  if (state.status === "loading")
    return (
      <div
        role="status"
        aria-label={text.loading}
        className="grid grid-cols-3 gap-3 max-md:grid-cols-1"
      >
        {Array.from({ length: 3 }, (_, index) => (
          <Skeleton key={index} className="h-[72px] rounded-[10px]" />
        ))}
      </div>
    );
  if (state.status === "error")
    return state.reason === "forbidden" ? (
      <p role="note" className="m-0 text-compact text-muted-foreground">
        {text.forbidden}
      </p>
    ) : (
      <p
        role="alert"
        className="m-0 flex flex-wrap items-center gap-3 text-compact text-muted-foreground"
      >
        {text.error}
        <button
          type="button"
          onClick={onRetry}
          className="h-7 rounded-[8px] border border-line-strong bg-popover px-2.5 text-crumb font-medium text-foreground hover:bg-state-hover dark:bg-card"
        >
          {text.retry}
        </button>
      </p>
    );
  const figures = state.data;
  const tiles = [
    { label: text.members, value: figures.members },
    { label: text.spaces, value: figures.spaces },
    { label: text.invitations, value: figures.invitations },
  ];
  return (
    <section
      aria-label={text.label}
      className="grid grid-cols-3 gap-3 max-md:grid-cols-1"
    >
      {tiles.map((tile) => (
        <div
          key={tile.label}
          className="flex flex-col gap-1 rounded-[10px] border border-border bg-card px-4 py-3"
        >
          <span className="text-metadata text-muted-foreground">
            {tile.label}
          </span>
          <span className="text-greeting text-foreground tabular-nums">
            {tile.value}
          </span>
        </div>
      ))}
    </section>
  );
}

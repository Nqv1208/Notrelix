import { Check, CircleAlert, MoreHorizontal } from "lucide-react";
import { cn } from "@notrelix/ui-web";
import { fillDocCopy, type DocPageCopy } from "./doc-page.copy";
import type {
  DocHeaderView,
  DocPageCallbacks,
  DocSaveState,
} from "./doc-page-models";
import { DocAvatar } from "./doc-page-ui";

export interface DocPageBarSurfaceProps {
  readonly copy: DocPageCopy;
  readonly header: DocHeaderView;
  readonly saveState: DocSaveState;
  readonly shareOpen?: boolean;
  /** Absent: Share stays disabled with the stated reason. */
  readonly canShare?: boolean;
  readonly readOnly?: boolean;
  readonly callbacks?: DocPageCallbacks;
}

const BUTTON =
  "inline-flex h-8 items-center gap-1.5 rounded-[7px] border border-line-strong bg-doc-panel px-3 text-compact leading-[1.45] font-medium text-foreground outline-none hover:bg-state-hover-strong focus-visible:ring-2 focus-visible:ring-focus-ring aria-disabled:cursor-default aria-disabled:text-muted-foreground";

/**
 * The document bar (Final-Docs `.dbar`): crumb, who edited and the
 * save state on the left; viewers, Share, the review action and More on the
 * right. 49 px with its bottom border.
 */
export function DocPageBarSurface({
  copy,
  header,
  saveState,
  shareOpen = false,
  canShare = false,
  callbacks,
}: DocPageBarSurfaceProps) {
  const h = copy.header;
  const edited =
    header.editedBy && header.editedAt
      ? fillDocCopy(h.editedBy, {
          name: header.editedBy,
          time: header.editedAt,
        })
      : header.editedAt
        ? fillDocCopy(h.editedAt, { time: header.editedAt })
        : null;
  const names = header.presence.map((person) => person.name).join(", ");

  return (
    <header className="box-content flex min-h-12 shrink-0 flex-wrap items-center gap-2.5 border-b border-border pr-4 pl-6">
      <nav
        aria-label={copy.crumbLabel}
        className="flex min-w-0 items-center gap-1.5 text-compact leading-[1.45] text-meta-foreground"
      >
        {header.crumbs.map((crumb, index) => {
          const last = index === header.crumbs.length - 1;
          return (
            <span key={`${index}:${crumb}`} className="contents">
              {last ? (
                <b
                  aria-current="page"
                  className="font-medium whitespace-nowrap text-foreground"
                >
                  {crumb}
                </b>
              ) : (
                <>
                  <span>{crumb}</span>
                  <span aria-hidden="true">/</span>
                </>
              )}
            </span>
          );
        })}
      </nav>
      {edited ? (
        <span className="text-crumb text-meta-foreground">{edited}</span>
      ) : null}
      {saveState === "error" ? (
        <span
          role="alert"
          className="inline-flex items-center gap-[5px] text-crumb font-semibold text-danger-text"
        >
          <CircleAlert
            aria-hidden="true"
            className="size-[13px] shrink-0 stroke-[1.8]"
          />
          {h.saveFailed}
          <button
            type="button"
            onClick={callbacks?.onSaveRetry}
            className="border-0 bg-transparent px-0.5 font-semibold text-brand-subtle-foreground underline outline-none focus-visible:ring-2 focus-visible:ring-focus-ring"
          >
            {h.retry}
          </button>
        </span>
      ) : (
        <span
          role="status"
          className="inline-flex items-center gap-[5px] text-crumb text-meta-foreground"
        >
          <Check
            aria-hidden="true"
            className="size-[13px] shrink-0 stroke-[1.8]"
          />
          {saveState === "saving" ? h.saving : h.saved}
        </span>
      )}
      <span className="flex-1" />
      {header.presence.length > 0 ? (
        <div
          role="group"
          aria-label={fillDocCopy(h.presence, { names })}
          className="flex"
        >
          {header.presence.map((person) => (
            <DocAvatar
              key={person.id}
              person={person}
              className="-ml-1.5 ring-2 ring-doc-page"
            />
          ))}
        </div>
      ) : null}
      <button
        type="button"
        aria-haspopup="dialog"
        aria-expanded={shareOpen}
        aria-disabled={canShare ? undefined : true}
        title={canShare ? undefined : h.shareUnavailable}
        onClick={canShare ? callbacks?.onOpenShare : undefined}
        className={cn(
          BUTTON,
          shareOpen && "border-primary ring-2 ring-primary",
        )}
      >
        {h.share}
      </button>
      <button
        type="button"
        aria-disabled="true"
        title={h.approveUnavailable}
        className={cn(
          BUTTON,
          "border-primary bg-brand-solid font-semibold text-brand-solid-foreground hover:bg-brand-solid aria-disabled:text-brand-solid-foreground",
        )}
      >
        {h.approve}
      </button>
      <button
        type="button"
        aria-label={h.more}
        aria-disabled="true"
        title={h.moreUnavailable}
        className="grid size-8 shrink-0 cursor-default place-items-center rounded-[7px] text-meta-foreground outline-none hover:bg-state-hover hover:text-foreground focus-visible:ring-2 focus-visible:ring-focus-ring"
      >
        <MoreHorizontal aria-hidden="true" className="size-4 stroke-[1.8]" />
      </button>
    </header>
  );
}

import {
  Check,
  ChevronLeft,
  CircleAlert,
  MessageCircle,
  MoreHorizontal,
  PanelRight,
} from "lucide-react";
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
  /** Word and character counts of the page. */
  readonly stats?: { readonly words: number; readonly characters: number };
  readonly shareOpen?: boolean;
  /** Absent: Share stays disabled with the stated reason. */
  readonly canShare?: boolean;
  readonly readOnly?: boolean;
  /** Whether the side panel is open; absent hides the toggle. */
  readonly panelOpen?: boolean;
  /** A phone: the 52 px bar of Final-Docs-2-320 with the overflow behind More. */
  readonly compact?: boolean;
  /** Where Back leads, named; absent reads plain "Back". */
  readonly backLabel?: string;
  /** Open comment threads, shown on the phone bar's Comments button. */
  readonly commentCount?: number;
  /** Opens the phone bar's overflow sheet. */
  readonly onOpenMore?: () => void;
  readonly callbacks?: DocPageCallbacks;
}

/** A hairline between the bar's text groups; it sits inside the flex gaps and adds no width. */
function Divider({ height }: { height: "h-3" | "h-3.5" }) {
  return (
    <span
      aria-hidden="true"
      className={cn("-mx-[5.5px] w-px shrink-0 bg-line-strong", height)}
    />
  );
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
  stats,
  shareOpen = false,
  canShare = false,
  panelOpen,
  compact = false,
  backLabel,
  commentCount = 0,
  onOpenMore,
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
  const saveNode =
    saveState === "error" ? (
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
    );
  const names = header.presence.map((person) => person.name).join(", ");

  if (compact) {
    const PHONE_BUTTON =
      "relative grid size-11 shrink-0 place-items-center rounded-[10px] border-0 bg-transparent text-meta-foreground outline-none hover:bg-state-hover hover:text-foreground focus-visible:ring-2 focus-visible:ring-focus-ring";
    return (
      <>
        <header className="box-border flex h-13 shrink-0 items-center gap-0.5 border-b border-border px-1.5">
          <button
            type="button"
            aria-label={backLabel ?? h.back}
            onClick={callbacks?.onBack}
            className={PHONE_BUTTON}
          >
            <ChevronLeft aria-hidden="true" className="size-5 stroke-[1.8]" />
          </button>
          <nav
            aria-label={copy.crumbLabel}
            className="min-w-0 flex-1 truncate pl-1 text-left text-[15px] leading-[1.45] font-semibold text-foreground [direction:rtl]"
          >
            {/* rtl puts the ellipsis on the left so the page name stays visible */}
            <bdi aria-current="page">{header.crumbs.join(" / ")}</bdi>
          </nav>
          {callbacks?.onPanelOpenChange ? (
            <button
              type="button"
              aria-haspopup="dialog"
              aria-label={fillDocCopy(h.openComments, { count: commentCount })}
              onClick={callbacks.onOpenComments}
              className={PHONE_BUTTON}
            >
              <MessageCircle
                aria-hidden="true"
                className="size-5 stroke-[1.8]"
              />
              {commentCount > 0 ? (
                <i
                  aria-hidden="true"
                  className="absolute top-1 right-0.5 box-border h-4 min-w-4 rounded-full bg-brand-solid px-[3px] text-center text-[10px] leading-4 font-semibold text-brand-solid-foreground not-italic"
                >
                  {commentCount}
                </i>
              ) : null}
            </button>
          ) : null}
          <button
            type="button"
            aria-haspopup="dialog"
            aria-label={h.moreSheet}
            onClick={onOpenMore}
            className={PHONE_BUTTON}
          >
            <MoreHorizontal
              aria-hidden="true"
              className="size-5 stroke-[1.8]"
            />
          </button>
        </header>
        <div className="flex min-h-8 shrink-0 flex-wrap items-center gap-x-1.5 border-b border-border px-4 py-1 text-crumb text-meta-foreground">
          {saveNode}
          {edited ? (
            <>
              <span aria-hidden="true">·</span>
              <span>{edited}</span>
            </>
          ) : null}
        </div>
      </>
    );
  }

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
        <>
          <Divider height="h-3.5" />
          <span className="text-crumb text-meta-foreground">{edited}</span>
        </>
      ) : null}
      {saveNode}
      {stats ? (
        <>
          <Divider height="h-3" />
          <span className="text-crumb text-meta-foreground tabular-nums">
            {fillDocCopy(h.stats, {
              words: stats.words.toLocaleString(),
              characters: stats.characters.toLocaleString(),
            })}
          </span>
        </>
      ) : null}
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
      {panelOpen !== undefined && callbacks?.onPanelOpenChange ? (
        <button
          type="button"
          aria-label={panelOpen ? h.hidePanel : h.showPanel}
          aria-pressed={panelOpen}
          title={panelOpen ? h.hidePanel : h.showPanel}
          onClick={() => callbacks.onPanelOpenChange?.(!panelOpen)}
          className={cn(
            "grid size-8 shrink-0 place-items-center rounded-[7px] text-meta-foreground outline-none hover:bg-state-hover hover:text-foreground focus-visible:ring-2 focus-visible:ring-focus-ring",
            panelOpen && "bg-state-hover-strong text-foreground",
          )}
        >
          <PanelRight aria-hidden="true" className="size-4 stroke-[1.8]" />
        </button>
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

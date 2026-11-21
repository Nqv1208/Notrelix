import {
  CircleAlert,
  History,
  RefreshCw,
  RotateCcw,
  Star,
  X,
} from "lucide-react";
import { cn } from "@notrelix/ui-web";
import { fillDocCopy, type DocPageCopy } from "./doc-page.copy";
import type { DocPageCallbacks, DocVersionView } from "./doc-page-models";
import { DocAvatar } from "./doc-page-ui";

export interface DocVersionBarSurfaceProps {
  readonly copy: DocPageCopy;
  /** The version the page shows. */
  readonly version: DocVersionView;
  readonly callbacks?: DocPageCallbacks;
}

const BAR_BUTTON =
  "relative inline-flex max-sm:after:absolute max-sm:after:-inset-2 max-sm:after:content-[''] h-7.5 items-center gap-1.5 rounded-[7px] border border-line-strong bg-doc-panel px-3 text-compact leading-[1.45] font-medium text-foreground outline-none hover:bg-state-hover-strong focus-visible:ring-2 focus-visible:ring-focus-ring";

const PANEL_BUTTON =
  "relative inline-flex max-sm:after:absolute max-sm:after:-inset-1.5 max-sm:after:content-[''] h-8 shrink-0 cursor-pointer items-center gap-1.5 rounded-[7px] border border-line-strong bg-doc-panel px-3 text-compact leading-[1.45] font-medium text-foreground outline-none hover:bg-state-hover-strong focus-visible:ring-2 focus-visible:ring-focus-ring";

/** The bar above the page while a version is open: it says the page is a read-only copy. */
export function DocVersionBarSurface({
  copy,
  version,
  callbacks,
}: DocVersionBarSurfaceProps) {
  const v = copy.versions;
  return (
    <div
      role="status"
      className="box-border flex min-h-12 shrink-0 flex-wrap items-center gap-2.5 border-b border-border bg-doc-notice py-1.5 pr-4 pl-6 text-compact leading-[1.45] text-doc-body"
    >
      <History
        aria-hidden="true"
        className="size-4 shrink-0 stroke-[1.8] text-brand-solid"
      />
      <span className="flex-1">
        {v.viewing}{" "}
        <b className="font-semibold text-foreground">
          {fillDocCopy(v.versionNo, { n: version.number })}
        </b>{" "}
        {fillDocCopy(v.viewingTail, {
          author: version.author.name,
          stamp: version.stamp,
        })}
      </span>
      <button
        type="button"
        onClick={callbacks?.onCloseVersionView}
        className={BAR_BUTTON}
      >
        {v.backToCurrent}
      </button>
      <button
        type="button"
        onClick={callbacks?.onRestoreVersionOpen}
        className={cn(
          BAR_BUTTON,
          "border-primary bg-brand-solid font-semibold text-brand-solid-foreground hover:bg-brand-solid",
        )}
      >
        <RotateCcw aria-hidden="true" className="size-4 stroke-[1.8]" />
        {v.restore}
      </button>
    </div>
  );
}

export interface DocVersionPanelSurfaceProps {
  readonly copy: DocPageCopy;
  readonly versions: readonly DocVersionView[];
  /** The version the page shows; null while the page shows its current content. */
  readonly selectedId: string | null;
  readonly namedOnly?: boolean;
  readonly canSave?: boolean;
  readonly callbacks?: DocPageCallbacks;
  readonly onNamedOnlyChange?: (value: boolean) => void;
  readonly status?: "ready" | "loading" | "error";
  readonly onRetry?: () => void;
}

/** The version history panel (Doc-Ver `.rp`): a column beside the page, a sheet over it on a tablet or phone. */
export function DocVersionPanelSurface({
  copy,
  versions,
  selectedId,
  namedOnly = false,
  canSave = false,
  callbacks,
  onNamedOnlyChange,
  status = "ready",
  onRetry,
}: DocVersionPanelSurfaceProps) {
  const v = copy.versions;
  const shown = namedOnly ? versions.filter((entry) => entry.name) : versions;
  const days: Array<{ day: string; items: DocVersionView[] }> = [];
  for (const entry of shown) {
    const last = days[days.length - 1];
    if (last && last.day === entry.day) last.items.push(entry);
    else days.push({ day: entry.day, items: [entry] });
  }
  const primarySave = canSave ? (
    <button
      type="button"
      onClick={callbacks?.onSaveVersionOpen}
      className={cn(
        PANEL_BUTTON,
        "border-primary bg-brand-solid font-semibold text-brand-solid-foreground hover:bg-brand-solid",
      )}
    >
      {v.saveNamed}
    </button>
  ) : null;
  const saveButton = canSave ? (
    <button
      type="button"
      onClick={callbacks?.onSaveVersionOpen}
      className={PANEL_BUTTON}
    >
      <Star aria-hidden="true" className="size-4 stroke-[1.8]" />
      {v.saveNamed}
    </button>
  ) : null;

  return (
    <aside
      aria-label={v.title}
      className={cn(
        "box-content flex w-[308px] shrink-0 flex-col border-l border-border bg-doc-panel",
        "max-xl:absolute max-xl:inset-y-0 max-xl:right-0 max-xl:z-20 max-xl:w-[340px] max-xl:shadow-[-12px_0_32px_rgba(15,23,42,0.16)]",
        "max-sm:inset-x-0 max-sm:top-auto max-sm:bottom-0 max-sm:h-[72%] max-sm:w-auto max-sm:rounded-t-[16px] max-sm:shadow-[0_-12px_40px_rgba(15,23,42,0.28)]",
        "max-sm:before:mx-auto max-sm:before:mt-2 max-sm:before:block max-sm:before:h-1 max-sm:before:w-9 max-sm:before:shrink-0 max-sm:before:rounded-[2px] max-sm:before:bg-line-strong max-sm:before:content-['']",
      )}
    >
      <div className="box-content flex h-11 shrink-0 items-center gap-2 border-b border-border pr-2 pl-4">
        <h2
          id="doc-versions-title"
          className="m-0 text-body leading-[1.45] font-semibold"
        >
          {v.title}
        </h2>
        <label className="ml-auto flex cursor-pointer items-center gap-2 text-crumb leading-[1.45] text-muted-foreground">
          <input
            type="checkbox"
            checked={namedOnly}
            onChange={(event) => onNamedOnlyChange?.(event.target.checked)}
            className="m-0 size-[15px] accent-primary"
          />
          {v.namedOnly}
        </label>
        <button
          type="button"
          aria-label={v.closePanel}
          onClick={callbacks?.onCloseVersions}
          className="ml-auto grid size-8 shrink-0 cursor-pointer place-items-center rounded-[7px] text-meta-foreground outline-none hover:bg-state-hover hover:text-foreground focus-visible:ring-2 focus-visible:ring-focus-ring  max-sm:after:absolute max-sm:after:-inset-1.5 max-sm:after:content-[''] max-sm:relative"
        >
          <X aria-hidden="true" className="size-4 stroke-[1.8]" />
        </button>
      </div>
      {status === "loading" ? (
        <p
          role="status"
          className="m-0 px-4 py-3.5 text-compact leading-[1.45] text-meta-foreground"
        >
          {v.loading}
        </p>
      ) : status === "error" ? (
        <div
          role="alert"
          className="flex flex-1 flex-col items-center justify-center gap-2.5 p-8 text-center text-meta-foreground"
        >
          <span className="grid size-14 place-items-center rounded-[16px] bg-danger-subtle text-danger-text">
            <CircleAlert aria-hidden="true" className="size-5 stroke-[1.8]" />
          </span>
          <b className="text-calendar-title leading-[1.45] font-semibold text-foreground">
            {v.errorTitle}
          </b>
          <p className="m-0 max-w-[44ch] text-body leading-[1.55] text-muted-foreground">
            {v.errorBody}
          </p>
          <div className="mt-1.5 flex flex-wrap justify-center gap-2">
            <button type="button" onClick={onRetry} className={PANEL_BUTTON}>
              <RefreshCw aria-hidden="true" className="size-4 stroke-[1.8]" />
              {v.retry}
            </button>
          </div>
        </div>
      ) : versions.length === 0 ? (
        <div className="flex flex-1 flex-col items-center justify-center gap-2.5 p-8 text-center text-meta-foreground">
          <span className="grid size-14 place-items-center rounded-[16px] bg-state-hover text-muted-foreground">
            <History aria-hidden="true" className="size-5 stroke-[1.8]" />
          </span>
          <b className="text-calendar-title leading-[1.45] font-semibold text-foreground">
            {v.emptyTitle}
          </b>
          <p className="m-0 max-w-[44ch] text-body leading-[1.55] text-muted-foreground">
            {v.emptyBody}
          </p>
          {primarySave ? (
            <div className="mt-1.5 flex flex-wrap justify-center gap-2">
              {primarySave}
            </div>
          ) : null}
        </div>
      ) : (
        <div
          role="region"
          aria-labelledby="doc-versions-title"
          className="flex min-h-0 flex-1 flex-col gap-[18px] overflow-auto px-4 py-3.5"
        >
          {saveButton ? <div className="flex">{saveButton}</div> : null}
          {days.length === 0 ? (
            <p className="m-0 text-compact leading-[1.45] text-meta-foreground">
              {v.noNamed}
            </p>
          ) : null}
          {days.map((group, index) => (
            <section
              key={group.day}
              aria-labelledby={`doc-version-day-${index}`}
            >
              <h3
                id={`doc-version-day-${index}`}
                className="m-0 mb-1 flex items-center gap-1.5 text-crumb leading-[1.45] font-semibold text-muted-foreground"
              >
                {group.day}
              </h3>
              <ul className="m-0 flex list-none flex-col gap-0.5 p-0">
                {group.items.map((entry) => {
                  const on = selectedId
                    ? entry.id === selectedId
                    : entry.current === true;
                  return (
                    <li key={entry.id}>
                      <button
                        type="button"
                        aria-current={on ? "true" : undefined}
                        onClick={() => callbacks?.onSelectVersion?.(entry.id)}
                        className={cn(
                          "-mx-2 box-border flex w-[calc(100%+16px)] cursor-pointer items-start gap-[9px] rounded-[8px] border-0 bg-transparent p-2 text-left text-foreground outline-none hover:bg-state-hover focus-visible:ring-2 focus-visible:ring-focus-ring",
                          on && "bg-state-selected hover:bg-state-selected",
                        )}
                      >
                        <DocAvatar
                          person={entry.author}
                          className="mt-px size-6 text-mark-xs"
                        />
                        <span className="min-w-0 flex-1">
                          {entry.name ? (
                            <>
                              <span className="block text-compact leading-[1.45] font-semibold text-foreground">
                                {entry.name}
                              </span>
                              <span className="mt-px block text-metadata leading-[1.45] text-muted-foreground tracking-normal">
                                {fillDocCopy(v.byAuthorNamed, {
                                  author: entry.author.name,
                                  time: entry.time,
                                  n: entry.number,
                                })}
                              </span>
                            </>
                          ) : (
                            <>
                              <span
                                className={cn(
                                  "flex items-center gap-1.5 text-compact leading-[1.45] font-semibold tabular-nums",
                                  on && "text-state-selected-foreground",
                                )}
                              >
                                {entry.time}
                                <span
                                  className={cn(
                                    "rounded-[4px] bg-state-hover-strong px-1.5 text-mark-md leading-[18px] font-semibold text-muted-foreground",
                                    on &&
                                      "bg-doc-panel text-state-selected-foreground",
                                  )}
                                >
                                  {entry.current ? v.current : entry.tag}
                                </span>
                              </span>
                              <span className="mt-px block text-metadata leading-[1.45] text-muted-foreground tracking-normal">
                                {fillDocCopy(v.byAuthor, {
                                  author: entry.author.name,
                                  n: entry.number,
                                })}
                              </span>
                            </>
                          )}
                          {entry.current ? (
                            <small className="mt-0.5 block text-metadata leading-[1.45] text-meta-foreground tracking-normal">
                              {entry.name ? v.current : entry.tag}
                            </small>
                          ) : null}
                        </span>
                      </button>
                    </li>
                  );
                })}
              </ul>
            </section>
          ))}
        </div>
      )}
    </aside>
  );
}

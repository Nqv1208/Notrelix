import { History, X } from "lucide-react";
import { cn } from "@notrelix/ui-web";
import type { DocPageCopy } from "./doc-page.copy";
import type { DocPageCallbacks, DocVersionView } from "./doc-page-models";
import { DocAvatar } from "./doc-page-ui";

export interface DocVersionBarSurfaceProps {
  readonly copy: DocPageCopy;
  /** The version being compared with the current page. */
  readonly version: DocVersionView;
  readonly canRestore?: boolean;
  readonly callbacks?: DocPageCallbacks;
}

const BAR_BUTTON =
  "inline-flex h-7.5 items-center gap-1.5 rounded-[7px] border border-line-strong bg-doc-panel px-3 text-compact leading-[1.45] font-medium text-foreground outline-none hover:bg-state-hover-strong focus-visible:ring-2 focus-visible:ring-focus-ring aria-disabled:cursor-default";

/** The banner above a compared version (Final-Docs `.vbar`). */
export function DocVersionBarSurface({
  copy,
  version,
  canRestore = false,
  callbacks,
}: DocVersionBarSurfaceProps) {
  const v = copy.versions;
  return (
    <div
      role="region"
      aria-label={v.bar}
      className="box-border flex min-h-12 shrink-0 flex-wrap items-center gap-2.5 border-b border-border bg-doc-notice py-1.5 pr-4 pl-6 text-compact leading-[1.45] text-doc-body"
    >
      <History
        aria-hidden="true"
        className="size-4 shrink-0 stroke-[1.8] text-brand-solid"
      />
      <span>
        {v.viewingPre}{" "}
        <b className="font-semibold text-foreground">{version.stamp}</b>{" "}
        {v.viewingBy}{" "}
        <b className="font-semibold text-foreground">{version.author.name}</b>{" "}
        {v.viewingPost}
      </span>
      <span className="ml-1.5 inline-flex items-center gap-2 text-crumb text-meta-foreground">
        {v.legend}
        <ins className="rounded-[2px] bg-doc-chip-done px-px text-inherit underline decoration-work-status-done decoration-[1.5px] underline-offset-3">
          {v.inserted}
        </ins>
        <del className="text-meta-foreground line-through decoration-meta-foreground">
          {v.deleted}
        </del>
      </span>
      <span className="flex-1" />
      <button
        type="button"
        aria-disabled={canRestore ? undefined : true}
        title={canRestore ? undefined : v.restoreUnavailable}
        onClick={canRestore ? callbacks?.onRestoreVersionOpen : undefined}
        className={cn(
          BAR_BUTTON,
          "border-primary bg-brand-solid font-semibold text-brand-solid-foreground hover:bg-brand-solid aria-disabled:text-brand-solid-foreground",
        )}
      >
        {v.restore}
      </button>
      <button
        type="button"
        onClick={callbacks?.onCloseVersions}
        className={BAR_BUTTON}
      >
        {v.close}
      </button>
    </div>
  );
}

export interface DocVersionPanelSurfaceProps {
  readonly copy: DocPageCopy;
  readonly versions: readonly DocVersionView[];
  readonly selectedId: string | null;
  readonly namedOnly?: boolean;
  readonly callbacks?: DocPageCallbacks;
  readonly onNamedOnlyChange?: (value: boolean) => void;
}

/** The version history panel that replaces the side tabs (Final-Docs `.rp` `phien-ban`). */
export function DocVersionPanelSurface({
  copy,
  versions,
  selectedId,
  namedOnly = false,
  callbacks,
  onNamedOnlyChange,
}: DocVersionPanelSurfaceProps) {
  const v = copy.versions;
  const shown = namedOnly ? versions.filter((entry) => entry.name) : versions;
  const days: Array<{ day: string; items: DocVersionView[] }> = [];
  for (const entry of shown) {
    const last = days[days.length - 1];
    if (last && last.day === entry.day) last.items.push(entry);
    else days.push({ day: entry.day, items: [entry] });
  }

  return (
    <aside
      aria-label={v.title}
      className="box-content flex w-[308px] shrink-0 flex-col border-l border-border bg-doc-panel"
    >
      <div className="box-content flex h-11 shrink-0 items-center gap-2 border-b border-border pr-2 pl-4">
        <h2
          id="doc-versions-title"
          className="text-body leading-[1.45] font-semibold"
        >
          {v.title}
        </h2>
        <button
          type="button"
          aria-label={v.closePanel}
          onClick={callbacks?.onCloseVersions}
          className="ml-auto grid size-8 shrink-0 place-items-center rounded-[7px] text-meta-foreground outline-none hover:bg-state-hover hover:text-foreground focus-visible:ring-2 focus-visible:ring-focus-ring"
        >
          <X aria-hidden="true" className="size-4 stroke-[1.8]" />
        </button>
      </div>
      <div
        role="region"
        aria-labelledby="doc-versions-title"
        className="flex min-h-0 flex-1 flex-col gap-[18px] overflow-auto px-4 py-3.5"
      >
        <p className="-mb-1.5 text-metadata leading-[1.5] text-meta-foreground tracking-normal">
          {v.note}
        </p>
        <label className="flex cursor-pointer items-center gap-2 text-crumb text-muted-foreground">
          <input
            type="checkbox"
            checked={namedOnly}
            onChange={(event) => onNamedOnlyChange?.(event.target.checked)}
            className="m-0 size-[15px] accent-primary"
          />
          {v.namedOnly}
        </label>
        {days.length === 0 ? (
          <p className="text-compact text-meta-foreground leading-[1.45]">
            {v.empty}
          </p>
        ) : null}
        {days.map((group, index) => (
          <section key={group.day} aria-labelledby={`doc-version-day-${index}`}>
            <h3
              id={`doc-version-day-${index}`}
              className="mb-1 flex items-center gap-1.5 text-crumb leading-[1.45] font-semibold text-muted-foreground"
            >
              {group.day}
            </h3>
            <ul className="m-0 flex list-none flex-col gap-0.5 p-0">
              {group.items.map((entry) => {
                const on = entry.id === selectedId;
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
                          <span className="block text-compact leading-[1.45] font-semibold text-foreground">
                            {entry.name}
                          </span>
                        ) : null}
                        <span
                          className={cn(
                            "flex items-center gap-1.5 text-compact leading-[1.45] tabular-nums",
                            entry.name ? "font-medium" : "font-semibold",
                            on && "text-state-selected-foreground",
                          )}
                        >
                          {entry.time}
                          {entry.current || on ? (
                            <span
                              className={cn(
                                "rounded-[4px] bg-state-hover-strong px-1.5 text-mark-md leading-[18px] font-semibold text-muted-foreground",
                                on &&
                                  "bg-doc-panel text-state-selected-foreground",
                              )}
                            >
                              {on ? v.viewingTag : v.current}
                            </span>
                          ) : null}
                        </span>
                        <span className="mt-px block text-metadata leading-[1.45] text-muted-foreground tracking-normal">
                          {entry.author.name}
                        </span>
                        <small className="mt-0.5 block text-metadata leading-[1.45] text-meta-foreground tracking-normal">
                          {entry.summary}
                        </small>
                      </span>
                    </button>
                  </li>
                );
              })}
            </ul>
          </section>
        ))}
      </div>
    </aside>
  );
}

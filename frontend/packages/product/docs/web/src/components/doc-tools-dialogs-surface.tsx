import { DialogFrame, OUTLINE, PRIMARY } from "./doc-dialogs-surface";
import { fillDocCopy, type DocPageCopy } from "./doc-page.copy";
import type { DocPageCallbacks } from "./doc-page-models";
import type { DocStatsView } from "../editor/doc-stats";
import {
  SHORTCUTS,
  shortcutLabel,
  type ShortcutGroup,
} from "../editor/shortcut-catalog";

const GROUPS: readonly ShortcutGroup[] = ["text", "blocks", "edit", "page"];

export interface DocShortcutsDialogSurfaceProps {
  readonly copy: DocPageCopy;
  readonly callbacks?: DocPageCallbacks;
}

/** Every keyboard shortcut of the editor, grouped, written for the member's system. */
export function DocShortcutsDialogSurface({
  copy,
  callbacks,
}: DocShortcutsDialogSurfaceProps) {
  const s = copy.shortcutSheet;
  return (
    <DialogFrame
      title={s.title}
      closeLabel={s.close}
      width={640}
      onClose={callbacks?.onCloseOverlay}
    >
      <p className="m-0 px-5 pb-2 text-crumb leading-[1.5] text-meta-foreground">
        {s.hint}
      </p>
      <div className="grid gap-x-8 gap-y-4 overflow-auto px-5 pb-5 sm:grid-cols-2">
        {GROUPS.map((group) => (
          <section key={group} aria-label={s.groups[group]}>
            <h3 className="m-0 mb-1.5 text-metadata font-semibold text-meta-foreground">
              {s.groups[group]}
            </h3>
            <ul className="m-0 flex list-none flex-col p-0">
              {SHORTCUTS.filter((entry) => entry.group === group).map(
                (entry) => (
                  <li
                    key={entry.id}
                    className="flex min-h-8 items-center justify-between gap-3 border-b border-border py-1 text-compact leading-[1.45] text-foreground last:border-b-0"
                  >
                    <span>{s.items[entry.id]}</span>
                    <kbd className="shrink-0 rounded-[5px] border border-line-strong bg-state-hover px-1.5 py-0.5 font-mono text-micro text-muted-foreground">
                      {shortcutLabel(entry.keys)}
                    </kbd>
                  </li>
                ),
              )}
            </ul>
          </section>
        ))}
      </div>
    </DialogFrame>
  );
}

export interface DocStatsDialogSurfaceProps {
  readonly copy: DocPageCopy;
  readonly stats: DocStatsView;
  /** "Document information": adds who changed the page last. */
  readonly info?: {
    readonly editedBy: string | null;
    readonly editedAt: string | null;
  };
  readonly callbacks?: DocPageCallbacks;
}

function Row({ label, value }: { label: string; value: string }) {
  return (
    <div className="flex items-baseline justify-between gap-4 border-b border-border py-2 text-compact leading-[1.45] last:border-b-0">
      <dt className="text-meta-foreground">{label}</dt>
      <dd className="m-0 font-semibold tabular-nums text-foreground">
        {value}
      </dd>
    </div>
  );
}

/** Word count and statistics of the page (and of the selection when there is one). */
export function DocStatsDialogSurface({
  copy,
  stats,
  info,
  callbacks,
}: DocStatsDialogSurfaceProps) {
  const d = copy.statsDialog;
  const fmt = (value: number) => value.toLocaleString();
  const reading =
    stats.page.readingMinutes === 0
      ? d.lessThanMinute
      : fillDocCopy(d.minutes, { count: String(stats.page.readingMinutes) });
  const rows = (
    text: DocStatsView["page"] | NonNullable<DocStatsView["selection"]>,
  ) => (
    <dl className="m-0">
      <Row label={d.words} value={fmt(text.words)} />
      <Row label={d.characters} value={fmt(text.characters)} />
      <Row label={d.charactersNoSpaces} value={fmt(text.charactersNoSpaces)} />
    </dl>
  );
  return (
    <DialogFrame
      title={info ? d.infoTitle : d.title}
      closeLabel={d.close}
      width={420}
      onClose={callbacks?.onCloseOverlay}
    >
      <div className="flex flex-col gap-3 px-5 pt-1 pb-5">
        <section aria-label={d.page}>
          <h3 className="m-0 mb-1 text-metadata font-semibold text-meta-foreground">
            {d.page}
          </h3>
          {rows(stats.page)}
          <dl className="m-0">
            <Row label={d.blocks} value={fmt(stats.page.blocks)} />
            <Row label={d.readingTime} value={reading} />
            {info?.editedBy ? (
              <Row
                label={d.editedBy}
                value={[info.editedBy, info.editedAt]
                  .filter(Boolean)
                  .join(" · ")}
              />
            ) : null}
          </dl>
        </section>
        {stats.selection ? (
          <section aria-label={d.selection}>
            <h3 className="m-0 mb-1 text-metadata font-semibold text-meta-foreground">
              {d.selection}
            </h3>
            {rows(stats.selection)}
          </section>
        ) : null}
        <div className="flex justify-end">
          <button
            type="button"
            onClick={callbacks?.onCloseOverlay}
            className={PRIMARY}
          >
            {d.close}
          </button>
        </div>
      </div>
    </DialogFrame>
  );
}

export type PagePaper = "a4" | "letter";
export type PageOrientation = "portrait" | "landscape";
export type PageMargin = "narrow" | "normal" | "wide";

export interface PageSetup {
  readonly paper: PagePaper;
  readonly orientation: PageOrientation;
  readonly margin: PageMargin;
}

export const DEFAULT_PAGE_SETUP: PageSetup = {
  paper: "a4",
  orientation: "portrait",
  margin: "normal",
};

export interface DocPageSetupDialogSurfaceProps {
  readonly copy: DocPageCopy;
  readonly setup: PageSetup;
  readonly onChange: (setup: PageSetup) => void;
  readonly callbacks?: DocPageCallbacks;
}

/** Paper, orientation and margins used when the page is printed or saved as PDF. */
export function DocPageSetupDialogSurface({
  copy,
  setup,
  onChange,
  callbacks,
}: DocPageSetupDialogSurfaceProps) {
  const p = copy.pageSetup;
  const choice = <T extends string>(
    label: string,
    names: Readonly<Record<T, string>>,
    value: T,
    pick: (next: T) => void,
  ) => (
    <fieldset className="m-0 flex flex-col gap-1.5 border-0 p-0">
      <legend className="mb-1 p-0 text-metadata font-semibold text-meta-foreground">
        {label}
      </legend>
      <div className="flex flex-wrap gap-2">
        {(Object.keys(names) as T[]).map((key) => (
          <label
            key={key}
            className="inline-flex h-8 cursor-pointer items-center gap-2 rounded-[7px] border border-line-strong bg-doc-panel px-3 text-compact text-foreground has-[:checked]:border-primary has-[:checked]:bg-state-selected has-[:focus-visible]:ring-2 has-[:focus-visible]:ring-focus-ring"
          >
            <input
              type="radio"
              name={label}
              checked={value === key}
              onChange={() => pick(key)}
              className="sr-only"
            />
            {names[key]}
          </label>
        ))}
      </div>
    </fieldset>
  );
  return (
    <DialogFrame
      title={p.title}
      closeLabel={p.close}
      width={460}
      onClose={callbacks?.onCloseOverlay}
    >
      <div className="flex flex-col gap-4 px-5 pt-1 pb-5">
        <p className="m-0 text-crumb leading-[1.5] text-meta-foreground">
          {p.note}
        </p>
        {choice(p.paper, p.papers, setup.paper, (paper) =>
          onChange({ ...setup, paper }),
        )}
        {choice(
          p.orientation,
          p.orientations,
          setup.orientation,
          (orientation) => onChange({ ...setup, orientation }),
        )}
        {choice(p.margin, p.margins, setup.margin, (margin) =>
          onChange({ ...setup, margin }),
        )}
        <div className="flex justify-end gap-2">
          <button
            type="button"
            onClick={() => onChange(DEFAULT_PAGE_SETUP)}
            className={OUTLINE}
          >
            {p.reset}
          </button>
          <button
            type="button"
            onClick={callbacks?.onCloseOverlay}
            className={PRIMARY}
          >
            {p.done}
          </button>
        </div>
      </div>
    </DialogFrame>
  );
}

import type { DocPageCopy } from "./doc-page.copy";

export interface DocTocEntry {
  readonly id: string;
  /** 1 to 3, the heading level. */
  readonly level: 1 | 2 | 3;
  readonly text: string;
}

const INDENT: Readonly<Record<1 | 2 | 3, string>> = {
  1: "pl-0",
  2: "pl-4",
  3: "pl-8",
};

/** Scrolls the heading block with this id into view (a no-op when it is not drawn). */
export function jumpToBlock(root: ParentNode, id: string): void {
  const target = Array.from(
    root.querySelectorAll<HTMLElement>("[data-block]"),
  ).find((element) => element.dataset.block === id);
  target?.scrollIntoView?.({ block: "start", behavior: "smooth" });
}

/**
 * The live outline of the page as links to its headings. The same view serves
 * the editor and the read-only article: the caller supplies the entries.
 */
export function DocTableOfContents({
  entries,
  copy,
  onJump,
}: {
  readonly entries: readonly DocTocEntry[];
  readonly copy: DocPageCopy;
  readonly onJump: (id: string) => void;
}) {
  const t = copy.blocks.tableOfContents;
  return (
    <nav
      aria-label={t.title}
      className="mb-3.5 rounded-[9px] border border-border px-3 py-2.5 text-nav"
    >
      <p className="m-0 mb-1.5 text-crumb font-semibold text-meta-foreground">
        {t.title}
      </p>
      {entries.length === 0 ? (
        <p className="m-0 text-compact text-meta-foreground">{t.empty}</p>
      ) : (
        <ul className="m-0 flex list-none flex-col gap-0.5 p-0">
          {entries.map((entry) => (
            <li key={entry.id} className={INDENT[entry.level]}>
              <button
                type="button"
                onClick={() => onJump(entry.id)}
                className="max-w-full cursor-pointer truncate border-0 bg-transparent p-0 text-left text-brand-subtle-foreground underline-offset-2 outline-none hover:underline focus-visible:ring-2 focus-visible:ring-focus-ring"
              >
                {entry.text}
              </button>
            </li>
          ))}
        </ul>
      )}
    </nav>
  );
}

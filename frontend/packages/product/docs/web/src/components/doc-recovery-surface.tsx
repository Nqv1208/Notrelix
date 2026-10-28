import { CircleAlert } from "lucide-react";
import type { DocPageCopy } from "./doc-page.copy";
import type { DocPageCallbacks, DocRecoveryView } from "./doc-page-models";

export interface DocRecoverySurfaceProps {
  readonly copy: DocPageCopy;
  readonly recoveries: readonly DocRecoveryView[];
  /** Ids whose text was just copied. */
  readonly copiedId?: string | null;
  readonly callbacks?: DocPageCallbacks;
}

const ACTION =
  "inline-flex h-7 items-center rounded-[7px] border border-line-strong bg-doc-panel px-2.5 text-compact font-medium text-foreground outline-none hover:bg-state-hover-strong focus-visible:ring-2 focus-visible:ring-focus-ring";

/**
 * The edits a stored change replaced: a block someone else changed while the
 * member had unsaved text in it shows the stored version, and the member's
 * text waits here to copy, re-apply or drop.
 */
export function DocRecoverySurface({
  copy,
  recoveries,
  copiedId = null,
  callbacks,
}: DocRecoverySurfaceProps) {
  if (recoveries.length === 0) return null;
  const r = copy.recovery;
  return (
    <section
      aria-label={r.yourEdit}
      className="mb-4 flex flex-col gap-2.5 rounded-[9px] bg-warning-subtle p-3 text-warning-subtle-foreground"
    >
      {recoveries.map((entry) => (
        <div key={entry.id} role="status" className="flex flex-col gap-1.5">
          <p className="flex items-center gap-1.5 text-compact font-semibold">
            <CircleAlert
              aria-hidden="true"
              className="size-[14px] shrink-0 stroke-[1.8]"
            />
            {r.title}
          </p>
          <p className="text-crumb text-meta-foreground">
            {entry.text ? r.body : r.removed}
          </p>
          {entry.text ? (
            <blockquote
              aria-label={r.yourEdit}
              className="m-0 max-h-24 overflow-auto rounded-[7px] border border-line bg-doc-panel px-2.5 py-1.5 text-compact whitespace-pre-wrap text-foreground"
            >
              {entry.text}
            </blockquote>
          ) : null}
          <div className="flex gap-2">
            {entry.text ? (
              <>
                <button
                  type="button"
                  className={ACTION}
                  onClick={() => callbacks?.onRecoveryCopy?.(entry.id)}
                >
                  {copiedId === entry.id ? r.copied : r.copy}
                </button>
                <button
                  type="button"
                  className={ACTION}
                  onClick={() => callbacks?.onRecoveryReapply?.(entry.id)}
                >
                  {r.reapply}
                </button>
              </>
            ) : null}
            <button
              type="button"
              className={ACTION}
              onClick={() => callbacks?.onRecoveryDismiss?.(entry.id)}
            >
              {r.dismiss}
            </button>
          </div>
        </div>
      ))}
    </section>
  );
}

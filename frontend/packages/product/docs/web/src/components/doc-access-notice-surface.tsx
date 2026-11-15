import { Eye, Lock } from "lucide-react";
import type { DocPageCopy } from "./doc-page.copy";
import type { DocPageCallbacks } from "./doc-page-models";

export interface DocAccessNoticeSurfaceProps {
  readonly copy: DocPageCopy;
  /** The right to edit was lost while the page was open: the text that was never stored. */
  readonly accessLost?: {
    readonly text: string;
    readonly copied: boolean;
  };
  /** The viewer reads the page but may not edit it. */
  readonly readOnly: boolean;
  readonly callbacks?: DocPageCallbacks;
}

const ACTION =
  "inline-flex h-7 shrink-0 items-center rounded-[7px] border border-line-strong bg-doc-panel px-2.5 text-compact font-medium text-foreground outline-none hover:bg-state-hover-strong focus-visible:ring-2 focus-visible:ring-focus-ring";

/**
 * The line above the page that says why it cannot be edited (Doc-Page
 * "Chỉ xem" and "Mất quyền khi đang mở"). Losing the right keeps what the
 * member typed and never stored, ready to copy.
 */
export function DocAccessNoticeSurface({
  copy,
  accessLost,
  readOnly,
  callbacks,
}: DocAccessNoticeSurfaceProps) {
  const n = copy.notice;
  if (accessLost) {
    return (
      <section
        role="alert"
        className="flex flex-col gap-2 bg-warning-subtle px-5 py-2.5 text-compact text-warning-subtle-foreground"
      >
        <div className="flex items-center gap-2">
          <Lock aria-hidden="true" className="size-[14px] shrink-0" />
          <span className="min-w-0 flex-1">
            <b className="font-semibold">{n.lostTitle}</b> {n.lostBody}
          </span>
          {accessLost.text ? (
            <button
              type="button"
              className={ACTION}
              onClick={callbacks?.onAccessLostCopy}
            >
              {accessLost.copied ? n.copied : n.copyUnsaved}
            </button>
          ) : null}
          {callbacks?.onLeavePage ? (
            <button
              type="button"
              className={ACTION}
              onClick={callbacks.onLeavePage}
            >
              {n.leave}
            </button>
          ) : null}
        </div>
        {accessLost.text ? (
          <blockquote
            aria-label={n.unsavedLabel}
            className="m-0 max-h-24 overflow-auto rounded-[7px] border border-line bg-doc-panel px-2.5 py-1.5 whitespace-pre-wrap text-foreground"
          >
            {accessLost.text}
          </blockquote>
        ) : null}
      </section>
    );
  }
  if (!readOnly) return null;
  return (
    <section
      role="status"
      className="flex items-center gap-2 bg-state-hover px-5 py-2.5 text-compact text-meta-foreground"
    >
      <Eye aria-hidden="true" className="size-[14px] shrink-0" />
      <span className="min-w-0 flex-1">
        <b className="font-semibold text-foreground">{n.readOnlyTitle}</b>{" "}
        {n.readOnlyBody}
      </span>
    </section>
  );
}

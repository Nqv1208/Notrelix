import type { ReactNode } from "react";
import { cn } from "@notrelix/ui-web";
import type { DocPerson, DocTaskStatus, DocTone } from "./doc-page-models";

/** Literal class names so Tailwind generates them. Identity only, never status. */
export const DOC_TONE_CLASS: Readonly<Record<DocTone, string>> = {
  blue: "bg-mark-blue text-mark-blue-foreground",
  gold: "bg-mark-gold text-mark-gold-foreground",
  coral: "bg-mark-coral text-mark-coral-foreground",
  sage: "bg-mark-sage text-mark-sage-foreground",
  crimson: "bg-mark-crimson text-mark-crimson-foreground",
  violet: "bg-mark-violet text-mark-violet-foreground",
};

/** A person's round initials (the design's 24/26/30 px avatars). */
export function DocAvatar({
  person,
  className,
}: {
  person: DocPerson;
  className?: string;
}) {
  return (
    <span
      aria-hidden="true"
      className={cn(
        "grid size-6.5 shrink-0 place-items-center rounded-full text-avatar-md font-bold",
        DOC_TONE_CLASS[person.tone],
        className,
      )}
    >
      {person.initials}
    </span>
  );
}

/** A department's rounded initials mark (the design's `.dm`). */
export function DocMark({
  tone,
  children,
  className,
}: {
  tone: DocTone;
  children: ReactNode;
  className?: string;
}) {
  return (
    <span
      aria-hidden="true"
      className={cn(
        "grid shrink-0 place-items-center font-bold",
        DOC_TONE_CLASS[tone],
        className,
      )}
    >
      {children}
    </span>
  );
}

const DOT_CLASS: Readonly<Record<DocTaskStatus, string>> = {
  todo: "shadow-[inset_0_0_0_2px_var(--work-status-todo)]",
  doing: "bg-work-status-doing",
  review: "bg-doc-dot-review",
  done: "bg-work-status-done",
};

/** The 8 px status dot of a task (hollow for "to do"). */
export function DocStatusDot({
  status,
  className,
}: {
  status: DocTaskStatus;
  className?: string;
}) {
  return (
    <span
      aria-hidden="true"
      className={cn(
        "inline-block size-2 shrink-0 rounded-full",
        DOT_CLASS[status],
        className,
      )}
    />
  );
}

/** 16 px line icon at the design's 1.8 stroke. */
export const ICON_CLASS = "size-4 shrink-0 stroke-[1.8]";

/** A phone row scrolls sideways with its scrollbar hidden (the fade is on its wrapper). */
export const SCROLL_ROW =
  "max-md:overflow-x-auto max-md:overflow-y-hidden max-md:[scrollbar-width:none] max-md:[&::-webkit-scrollbar]:hidden";

/** The 24 px fade at the right edge of a row that scrolls. */
export const SCROLL_FADE =
  "max-md:relative max-md:after:pointer-events-none max-md:after:absolute max-md:after:inset-y-0 max-md:after:right-0 max-md:after:w-6 max-md:after:bg-linear-to-r max-md:after:from-transparent max-md:after:to-doc-page";

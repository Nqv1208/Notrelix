import type { MyWorkGroup, MyWorkItem } from "../../core/model/my-work";
import type { NavigationEntry } from "../../core/model/navigation";
import type {
  WorkspaceActivityItem,
  WorkspaceInvitation,
} from "../../core/types/workspace";

/** One independently loaded section of a page (DESIGN.md §31–34). */
export type SectionState<T> =
  | { readonly status: "loading" }
  | { readonly status: "error" }
  | { readonly status: "ready"; readonly data: T };

type DateInput = Date | number | string;

/**
 * Locale formatters the surfaces use. Hosts pass `useFormatters()` from
 * `@notrelix/i18n`; declared structurally so pure surfaces do not import the
 * i18n runtime.
 */
export interface SurfaceFormatters {
  readonly relativeTime: (value: DateInput, now?: DateInput) => string;
  readonly date: (
    value: DateInput,
    options?: Intl.DateTimeFormatOptions,
  ) => string;
}

/** A recent Board or Doc with where it lives. */
export interface ContinueEntry extends NavigationEntry {
  /** Parent Doc or workspace name; `null` when unknown. */
  readonly location: string | null;
}

/** An assigned Item with the name of its Board when the member can see it. */
export interface MyWorkItemView extends MyWorkItem {
  readonly boardName: string | null;
}

export interface MyWorkGroupView extends Omit<MyWorkGroup, "items"> {
  readonly items: readonly MyWorkItemView[];
}

export type HomeActivityEntry = WorkspaceActivityItem;
export type HomeInvitation = WorkspaceInvitation;

/** Plural form for English/Vietnamese copy (`_one` only for exactly 1). */
export function pluralForm(
  forms: { readonly one: string; readonly other: string },
  count: number,
): string {
  return (count === 1 ? forms.one : forms.other).replace(
    /\{\{count\}\}/g,
    String(count),
  );
}

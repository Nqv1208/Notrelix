import type { MyWorkGroup, MyWorkItem } from "../../core/model/my-work";
import type { MarkToneId, NavigationEntry } from "../../core/model/navigation";
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

/** The Status bucket of an Item (the design's four Status tones). */
export type WorkStatusBucket = "todo" | "doing" | "review" | "done";

/** The Priority of an Item as the design names it. */
export type WorkPriority = "urgent" | "high" | "medium" | "low";

/** Why an Item is in My work: assigned, followed, or waiting for approval. */
export type MyWorkReason = "mine" | "follow" | "approve";

/** The department (Space) a Board is placed in. */
export interface MyWorkDepartment {
  readonly name: string;
  readonly markTone: MarkToneId;
}

/**
 * An assigned Item with the name of its Board when the member can see it.
 * The My work read carries no key, Status, Priority, relationship or
 * department; a host fills in what it can derive and the rest stays absent
 * (the surface draws only what it is given).
 */
export interface MyWorkItemView extends MyWorkItem {
  readonly boardName: string | null;
  readonly department?: MyWorkDepartment | null;
  /** Item key, e.g. `ERP-142`. */
  readonly key?: string | null;
  readonly priority?: WorkPriority | null;
  readonly status?: {
    readonly bucket: WorkStatusBucket;
    readonly label: string;
  } | null;
  readonly reason?: MyWorkReason | null;
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

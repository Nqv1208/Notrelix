import {
  criteriaTooLarge,
  serializeSavedFilterCriteria,
  type SavedFilterCriteria,
} from "@notrelix/work-management-core";

/** Why a save was not sent or was refused for good (no retry helps): over the size cap, rejected by the server, or the view no longer exists. */
export type PreferenceSaveIssue = "too_large" | "rejected" | "gone";

/** The view a saved state belongs to; a pending save keeps its own, so a view switch never redirects it. */
export interface PreferenceTarget {
  readonly workspaceId: string;
  readonly boardId: string;
  readonly viewId: string;
}

export interface PreferenceStatus {
  readonly target: PreferenceTarget;
  readonly pending: boolean;
  /** A save failed and sending it again may work. */
  readonly failed: boolean;
  readonly issue: PreferenceSaveIssue | null;
}

export interface PreferenceSaverDeps {
  readonly send: (target: PreferenceTarget, json: string) => Promise<void>;
  /** Puts the saved state in the query cache at once (no silent revert on failure). */
  readonly cache: (
    target: PreferenceTarget,
    criteria: SavedFilterCriteria,
  ) => void;
  readonly report: (status: PreferenceStatus) => void;
}

const kindOf = (error: unknown): string | null =>
  typeof error === "object" &&
  error !== null &&
  "kind" in error &&
  typeof error.kind === "string"
    ? error.kind
    : null;
/** Refusals no retry helps: the server rejected the criteria, or the view is gone (deleted by someone else). */
const refusalOf = (error: unknown): PreferenceSaveIssue | null =>
  kindOf(error) === "validation"
    ? "rejected"
    : kindOf(error) === "not_found"
      ? "gone"
      : null;

/**
 * Saves a member's view state. Single-flight: a save starts only after the
 * previous one settled, so the last state the member made is the last the
 * server gets, whatever the network does. Criteria over the server's cap are
 * never sent; a validation refusal is reported as a reason, not as a retry.
 */
export function createPreferenceSaver(deps: PreferenceSaverDeps) {
  let tail: Promise<unknown> = Promise.resolve();
  let latest = 0;
  let last: {
    criteria: SavedFilterCriteria;
    target: PreferenceTarget;
  } | null = null;

  const save = (criteria: SavedFilterCriteria, target: PreferenceTarget) => {
    if (criteriaTooLarge(criteria)) {
      deps.report({
        target,
        pending: false,
        failed: false,
        issue: "too_large",
      });
      return;
    }
    last = { criteria, target };
    const ticket = ++latest;
    deps.report({ target, pending: true, failed: false, issue: null });
    deps.cache(target, criteria);
    const json =
      Object.keys(criteria).length === 0
        ? ""
        : serializeSavedFilterCriteria(criteria);
    const run = tail.then(() => deps.send(target, json));
    tail = run.catch(() => undefined);
    run.then(
      () => {
        if (ticket === latest)
          deps.report({ target, pending: false, failed: false, issue: null });
      },
      (error: unknown) => {
        if (ticket !== latest) return;
        const issue = refusalOf(error);
        deps.report({ target, pending: false, failed: !issue, issue });
      },
    );
  };
  const retry = () => {
    if (last) save(last.criteria, last.target);
  };
  return { save, retry };
}

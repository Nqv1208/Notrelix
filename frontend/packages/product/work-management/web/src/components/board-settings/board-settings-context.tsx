import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useRef,
  useState,
  type ReactNode,
} from "react";
import type { BoardSettingsDto } from "@notrelix/work-management-core";
import type { BoardSettingsCopy } from "./board-settings.copy";
import type {
  BoardSettingsActions,
  BoardSettingsWriteResult,
} from "./board-settings-types";

export type SettingsFeedback = "failed" | "conflict" | "invalid" | null;

/** The last write that did not go through; `scope` ties it to the control that shows it inline. */
export interface SettingsFailure {
  readonly scope: string | null;
  readonly reason: "failed" | "conflict" | "invalid";
  /** Sends the same write again; absent when the write was not given as a function. */
  readonly retry?: () => void;
}

/**
 * A save the server refused because someone saved first, held until the person
 * picks a way out. `base` is the settings the person edited from; `latest` is
 * what the server holds now (null while it is being read).
 */
export interface SettingsConflict {
  readonly scope: string | null;
  readonly byName: string | null;
  readonly at: string | null;
  readonly base: BoardSettingsDto;
  readonly latest: BoardSettingsDto | null;
  readonly busy: "mine" | "theirs" | null;
}

/** How a conflict ended, shown once where the notice was. */
export interface SettingsConflictOutcome {
  readonly scope: string | null;
  readonly kind: "mine" | "theirs";
  readonly byName: string | null;
}

/** Scopes that hold a stale save for the person to settle: the general panel and one field. */
const settlesConflict = (scope: string | null | undefined) =>
  scope === "general" ||
  (scope != null &&
    (scope.startsWith("field:") || scope.startsWith("options:")));

type WriteSend =
  Promise<BoardSettingsWriteResult> | (() => Promise<BoardSettingsWriteResult>);

interface SettingsContextValue {
  readonly copy: BoardSettingsCopy;
  readonly settings: BoardSettingsDto;
  readonly actions: BoardSettingsActions;
  readonly timeZone: string;
  /** The failure no control shows inline: the page callout. */
  readonly feedback: SettingsFeedback;
  readonly failure: SettingsFailure | null;
  readonly clearFeedback: () => void;
  readonly conflict: SettingsConflict | null;
  readonly conflictOutcome: SettingsConflictOutcome | null;
  readonly takeTheirs: () => Promise<void>;
  readonly keepMine: () => Promise<void>;
  /**
   * Runs a write and reports a failure; resolves with the result. Give a
   * function (not a started promise) to offer Retry, and a `scope` to show
   * the failure under the control instead of in the page callout. A version
   * conflict reloads the latest settings.
   */
  readonly run: (
    send: WriteSend,
    scope?: string,
  ) => Promise<BoardSettingsWriteResult>;
}

const SettingsContext = createContext<SettingsContextValue | null>(null);

export function BoardSettingsProvider({
  copy,
  settings,
  actions,
  timeZone,
  defaultFeedback,
  onReload,
  readLatest,
  children,
}: {
  copy: BoardSettingsCopy;
  settings: BoardSettingsDto;
  actions: BoardSettingsActions;
  timeZone: string;
  defaultFeedback?: SettingsFeedback;
  /** Reads the settings again; a version conflict calls it. */
  onReload?: () => void | Promise<unknown>;
  /** Reads the settings as saved now without replacing the shown ones. */
  readLatest?: () => Promise<BoardSettingsDto | null>;
  children: ReactNode;
}) {
  const [failure, setFailure] = useState<SettingsFailure | null>(
    defaultFeedback ? { scope: null, reason: defaultFeedback } : null,
  );
  const [conflict, setConflict] = useState<SettingsConflict | null>(null);
  const [conflictOutcome, setConflictOutcome] =
    useState<SettingsConflictOutcome | null>(null);
  const resend = useRef<(() => Promise<BoardSettingsWriteResult>) | null>(null);
  const shown = useRef(settings);
  shown.current = settings;
  useEffect(() => {
    if (!conflictOutcome) return;
    const timer = window.setTimeout(() => setConflictOutcome(null), 8000);
    return () => window.clearTimeout(timer);
  }, [conflictOutcome]);
  const run = useCallback(
    async (
      send: WriteSend,
      scope?: string,
    ): Promise<BoardSettingsWriteResult> => {
      const result = await (typeof send === "function" ? send() : send);
      if (result.ok) {
        setFailure(null);
        return result;
      }
      if (
        result.reason === "conflict" &&
        typeof send === "function" &&
        settlesConflict(scope)
      ) {
        // Hold the person's edits: say who saved first and let them choose.
        const base = shown.current;
        resend.current = () => run(send, scope);
        setFailure(null);
        setConflictOutcome(null);
        setConflict({
          scope: scope ?? null,
          byName: result.conflict?.byName ?? null,
          at: result.conflict?.at ?? null,
          base,
          latest: null,
          busy: null,
        });
        void readLatest?.().then((latest) =>
          setConflict((held) =>
            held && held.base === base ? { ...held, latest } : held,
          ),
        );
        return result;
      }
      if (result.reason === "conflict") void onReload?.();
      setFailure({
        scope: scope ?? null,
        reason: result.reason,
        retry:
          typeof send === "function" && result.reason !== "conflict"
            ? () => void run(send, scope)
            : undefined,
      });
      return result;
    },
    [onReload, readLatest],
  );
  const takeTheirs = useCallback(async () => {
    const held = conflict;
    if (!held) return;
    setConflict({ ...held, busy: "theirs" });
    await onReload?.();
    setConflict(null);
    setConflictOutcome({
      scope: held.scope,
      kind: "theirs",
      byName: held.byName,
    });
  }, [conflict, onReload]);
  const keepMine = useCallback(async () => {
    const held = conflict;
    const send = resend.current;
    if (!held || !send) return;
    setConflict({ ...held, busy: "mine" });
    await onReload?.();
    const result = await send();
    if (result.ok) {
      setConflict(null);
      setConflictOutcome({
        scope: held.scope,
        kind: "mine",
        byName: held.byName,
      });
    } else if (result.reason !== "conflict") {
      setConflict({ ...held, busy: null });
    }
  }, [conflict, onReload]);
  const value = useMemo(
    () => ({
      copy,
      settings,
      actions,
      timeZone,
      feedback: failure && failure.scope === null ? failure.reason : null,
      failure,
      clearFeedback: () => setFailure(null),
      conflict,
      conflictOutcome,
      takeTheirs,
      keepMine,
      run,
    }),
    [
      actions,
      conflict,
      conflictOutcome,
      copy,
      failure,
      keepMine,
      run,
      settings,
      takeTheirs,
      timeZone,
    ],
  );
  return (
    <SettingsContext.Provider value={value}>
      {children}
    </SettingsContext.Provider>
  );
}

export function useSettings(): SettingsContextValue {
  const value = useContext(SettingsContext);
  if (!value)
    throw new Error("useSettings must be used inside BoardSettingsProvider");
  return value;
}

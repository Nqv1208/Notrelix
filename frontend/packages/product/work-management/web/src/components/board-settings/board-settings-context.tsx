import {
  createContext,
  useCallback,
  useContext,
  useMemo,
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
  children,
}: {
  copy: BoardSettingsCopy;
  settings: BoardSettingsDto;
  actions: BoardSettingsActions;
  timeZone: string;
  defaultFeedback?: SettingsFeedback;
  /** Reads the settings again; a version conflict calls it. */
  onReload?: () => void;
  children: ReactNode;
}) {
  const [failure, setFailure] = useState<SettingsFailure | null>(
    defaultFeedback ? { scope: null, reason: defaultFeedback } : null,
  );
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
      if (result.reason === "conflict") onReload?.();
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
    [onReload],
  );
  const value = useMemo(
    () => ({
      copy,
      settings,
      actions,
      timeZone,
      feedback: failure && failure.scope === null ? failure.reason : null,
      failure,
      clearFeedback: () => setFailure(null),
      run,
    }),
    [actions, copy, failure, run, settings, timeZone],
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

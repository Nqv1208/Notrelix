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

interface SettingsContextValue {
  readonly copy: BoardSettingsCopy;
  readonly settings: BoardSettingsDto;
  readonly actions: BoardSettingsActions;
  readonly timeZone: string;
  readonly feedback: SettingsFeedback;
  readonly clearFeedback: () => void;
  /** Runs a write and reports a failure; resolves with the result. */
  readonly run: (
    send: Promise<BoardSettingsWriteResult>,
  ) => Promise<BoardSettingsWriteResult>;
}

const SettingsContext = createContext<SettingsContextValue | null>(null);

export function BoardSettingsProvider({
  copy,
  settings,
  actions,
  timeZone,
  defaultFeedback,
  children,
}: {
  copy: BoardSettingsCopy;
  settings: BoardSettingsDto;
  actions: BoardSettingsActions;
  timeZone: string;
  defaultFeedback?: SettingsFeedback;
  children: ReactNode;
}) {
  const [feedback, setFeedback] = useState<SettingsFeedback>(
    defaultFeedback ?? null,
  );
  const run = useCallback(async (send: Promise<BoardSettingsWriteResult>) => {
    const result = await send;
    setFeedback(result.ok ? null : result.reason);
    return result;
  }, []);
  const value = useMemo(
    () => ({
      copy,
      settings,
      actions,
      timeZone,
      feedback,
      clearFeedback: () => setFeedback(null),
      run,
    }),
    [actions, copy, feedback, run, settings, timeZone],
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

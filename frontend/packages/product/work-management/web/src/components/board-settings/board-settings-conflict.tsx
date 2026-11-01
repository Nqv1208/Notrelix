import { CircleCheck } from "lucide-react";
import { useSettings, type SettingsConflict } from "./board-settings-context";
import {
  ConflictNotice,
  joinNames,
  timeOf,
  type ConflictDiffRow,
} from "./board-settings-conflict-parts";
import { Callout } from "./board-settings-parts";
import { fillCopy } from "./board-settings-format";

/** One part of the settings a stale save may have touched, from the person's side and from the server's. */
export interface ConflictPart {
  readonly id: string;
  readonly label: string;
  /** What the person has now, or null when they did not change this part. */
  readonly mine: string | null;
  /** What the server holds now, or null when the other person did not change this part. */
  readonly latest: string | null;
}

type Match = (scope: string | null) => boolean;

/** The conflict held for the panel `match` names, if any. */
export function useConflictFor(match: Match): SettingsConflict | null {
  const { conflict } = useSettings();
  return conflict && match(conflict.scope) ? conflict : null;
}

/** Name of the person who saved first, or "someone else". */
export function useSavedBy(conflict: SettingsConflict | null): string {
  const { copy } = useSettings();
  return conflict?.byName ?? copy.conflict.someone;
}

/**
 * The notice and diff of a stale save in one panel, or the line that says how the
 * last one ended. `kind` picks the wording: the page's general panel or one field.
 */
export function SettingsConflictSlot({
  match,
  kind,
  parts,
}: {
  match: Match;
  kind: "general" | "field";
  parts: readonly ConflictPart[];
}) {
  const { copy, timeZone, conflict, conflictOutcome, keepMine, takeTheirs } =
    useSettings();
  const t = copy.conflict;
  if (conflict && match(conflict.scope)) {
    const by = conflict.byName ?? t.someone;
    const theirs = parts.filter((part) => part.latest !== null);
    const time = timeOf(conflict.at, timeZone);
    const title =
      kind === "field"
        ? fillCopy(t.titleField, { by })
        : fillCopy(time ? t.titleGeneral : t.titleGeneralNoTime, {
            by,
            time: time ?? "",
          });
    const body =
      theirs.length === 0
        ? t.bodyUnknown
        : fillCopy(kind === "field" ? t.bodyField : t.bodyGeneral, {
            they: t.they,
            fields: joinNames(
              theirs.map((part) => part.label),
              t.and,
            ),
          });
    const rows: ConflictDiffRow[] | undefined = conflict.latest
      ? parts
          .filter((part) => part.mine !== null || part.latest !== null)
          .map((part) => ({
            id: part.id,
            label: part.label,
            mine: part.mine,
            latest: part.latest,
            keep: part.mine !== null ? "mine" : "latest",
          }))
      : undefined;
    return (
      <ConflictNotice
        copy={{
          ...t,
          title,
          body,
          columns: { ...t.columns, latest: fillCopy(t.columns.latest, { by }) },
          diffHint: fillCopy(t.diffHint, { by }),
        }}
        busy={conflict.busy}
        rows={rows}
        onTakeTheirs={() => void takeTheirs()}
        onKeepMine={() => void keepMine()}
      />
    );
  }
  if (conflictOutcome && match(conflictOutcome.scope)) {
    const by = conflictOutcome.byName ?? t.someone;
    return (
      <Callout icon={CircleCheck} role="status">
        {fillCopy(
          conflictOutcome.kind === "mine" ? t.savedMine : t.tookTheirs,
          {
            by,
          },
        )}
      </Callout>
    );
  }
  return null;
}

/** Shows a value in the diff: empty text reads as a dash. */
export const show = (value: string): string => (value === "" ? "—" : value);

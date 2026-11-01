import { useCallback, useEffect, useRef, useState } from "react";
import { useCardPopup } from "./card-popup-context";

interface ServerDraftOptions {
  /** Stable id of the field in the popup's notices. */
  readonly key: string;
  /** The field's name, as the notice says it. */
  readonly field: string;
  /** Saves the draft; "Keep my value" calls it with the clash lifted. */
  readonly commit: () => void;
}

/**
 * A text draft that follows the server value until the person edits it. When the
 * server value changes under an edited draft (someone else saved), the draft is
 * kept, saving it is held, and the popup shows the two-way choice: take their value
 * or keep mine, which saves over theirs on purpose. Nothing is overwritten before
 * the person picks.
 */
export function useServerDraft(server: string, options: ServerDraftOptions) {
  const { raiseFieldConflict, clearFieldConflict } = useCardPopup();
  const [draft, setDraft] = useState(server);
  const clash = useRef(false);
  const latest = useRef({ draft, server, options });
  latest.current = { draft, server, options };
  const seen = useRef(server);

  const clear = useCallback(() => {
    clash.current = false;
    clearFieldConflict(latest.current.options.key);
  }, [clearFieldConflict]);

  const raise = useCallback(() => {
    const { draft: mine, server: theirs, options: o } = latest.current;
    clash.current = true;
    raiseFieldConflict({
      key: o.key,
      field: o.field,
      theirs,
      mine,
      takeTheirs: () => {
        setDraft(latest.current.server);
        clear();
      },
      keepMine: () => {
        clear();
        latest.current.options.commit();
      },
    });
  }, [clear, raiseFieldConflict]);

  useEffect(() => {
    const previous = seen.current;
    if (server === previous) return;
    seen.current = server;
    if (draft === previous || draft === server) {
      setDraft(server);
      if (clash.current) clear();
    } else raise();
    // The draft is read only to decide; a later edit must not re-run it.
  }, [server]);

  // A draft typed after the notice shows is what "Keep my value" saves; once it matches theirs there is no clash.
  useEffect(() => {
    if (!clash.current) return;
    if (draft === server) clear();
    else raise();
  }, [draft]);

  useEffect(
    () => () => clearFieldConflict(latest.current.options.key),
    [clearFieldConflict],
  );

  return { draft, setDraft, held: () => clash.current } as const;
}

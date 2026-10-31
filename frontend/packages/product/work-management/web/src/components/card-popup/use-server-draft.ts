import { useEffect, useRef, useState } from "react";

/**
 * A text draft that follows the server value until the person edits it. When the
 * server value changes under an edited draft (someone else saved), the draft is
 * kept and `onClash` fires once, so the conflict notice shows instead of the
 * person's text being overwritten.
 */
export function useServerDraft(server: string, onClash: () => void) {
  const [draft, setDraft] = useState(server);
  const latest = useRef({ draft, onClash });
  latest.current = { draft, onClash };
  const seen = useRef(server);
  useEffect(() => {
    const previous = seen.current;
    if (server === previous) return;
    seen.current = server;
    const { draft: current, onClash: clash } = latest.current;
    if (current === previous || current === server) setDraft(server);
    else clash();
  }, [server]);
  return [draft, setDraft] as const;
}

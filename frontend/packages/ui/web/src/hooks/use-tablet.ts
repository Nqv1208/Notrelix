/**
 * @notrelix/ui-web — useIsTablet hook
 *
 * Detects the tablet viewport band (768–1279 px) using matchMedia. Below the
 * band the app is mobile; from 1280 px it is the desktop layout.
 */
import * as React from "react";

export const TABLET_MIN_WIDTH = 768;
export const TABLET_MAX_WIDTH = 1279;
const TABLET_QUERY = `(min-width: ${TABLET_MIN_WIDTH}px) and (max-width: ${TABLET_MAX_WIDTH}px)`;

export function useIsTablet() {
  return React.useSyncExternalStore(
    (callback) => {
      const mql = window.matchMedia(TABLET_QUERY);
      mql.addEventListener("change", callback);
      return () => mql.removeEventListener("change", callback);
    },
    () => window.matchMedia(TABLET_QUERY).matches,
    () => false,
  );
}

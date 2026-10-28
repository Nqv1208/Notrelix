import { useCallback, useEffect, useMemo, useState } from "react";
import {
  DEFAULT_PAGE_SETUP,
  type PageSetup,
} from "./doc-tools-dialogs-surface";

/** What the View and Tools menus remember for the viewer (UI preferences, not server state). */
export interface DocViewPrefs {
  readonly fullWidth: boolean;
  readonly toolbarShown: boolean;
  readonly spellcheck: boolean;
  readonly pageSetup: PageSetup;
}

const DEFAULTS: DocViewPrefs = {
  fullWidth: false,
  toolbarShown: true,
  spellcheck: true,
  pageSetup: DEFAULT_PAGE_SETUP,
};

export function docViewPrefsKey(viewerId: string | null): string {
  return `notrelix.docs.viewPrefs.${viewerId ?? "anonymous"}`;
}

function storage(): Storage | null {
  try {
    return typeof window === "undefined" ? null : window.localStorage;
  } catch {
    return null;
  }
}

function read(key: string): DocViewPrefs {
  try {
    const raw = storage()?.getItem(key);
    if (!raw) return DEFAULTS;
    const parsed = JSON.parse(raw) as Partial<DocViewPrefs>;
    return {
      fullWidth: parsed.fullWidth === true,
      toolbarShown: parsed.toolbarShown !== false,
      spellcheck: parsed.spellcheck !== false,
      pageSetup: {
        paper: parsed.pageSetup?.paper === "letter" ? "letter" : "a4",
        orientation:
          parsed.pageSetup?.orientation === "landscape"
            ? "landscape"
            : "portrait",
        margin:
          parsed.pageSetup?.margin === "narrow" ||
          parsed.pageSetup?.margin === "wide"
            ? parsed.pageSetup.margin
            : "normal",
      },
    };
  } catch {
    return DEFAULTS;
  }
}

const MARGIN_MM = { narrow: 12, normal: 20, wide: 30 } as const;
const STYLE_ID = "doc-page-setup-style";

/** The `@page` rule for a setup (applies only when printing). */
export function pageRule(setup: PageSetup): string {
  const paper = setup.paper === "letter" ? "letter" : "A4";
  return `@page { size: ${paper} ${setup.orientation}; margin: ${MARGIN_MM[setup.margin]}mm; }`;
}

/**
 * The viewer's document view preferences, kept in local storage per viewer,
 * plus the focus mode (for this visit only). The print setup is installed as
 * a `@page` rule while the page is open.
 */
export function useDocViewPrefs(viewerId: string | null) {
  const key = docViewPrefsKey(viewerId);
  const [state, setState] = useState(() => ({ key, prefs: read(key) }));
  const prefs = state.key === key ? state.prefs : read(key);
  const [focusMode, setFocusMode] = useState(false);

  const update = useCallback(
    (patch: Partial<DocViewPrefs>) => {
      setState((current) => {
        const next = {
          ...(current.key === key ? current.prefs : read(key)),
          ...patch,
        };
        try {
          storage()?.setItem(key, JSON.stringify(next));
        } catch {
          // Quota or blocked storage: the preference is a convenience.
        }
        return { key, prefs: next };
      });
    },
    [key],
  );

  const rule = pageRule(prefs.pageSetup);
  useEffect(() => {
    const style = document.createElement("style");
    style.id = STYLE_ID;
    style.textContent = rule;
    document.head.appendChild(style);
    return () => style.remove();
  }, [rule]);

  return useMemo(
    () => ({ prefs, focusMode, setFocusMode, update }),
    [prefs, focusMode, update],
  );
}

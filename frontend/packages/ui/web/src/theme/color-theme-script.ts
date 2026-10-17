/**
 * Pre-paint theme script (FE-UI-013): applies the stored mode and accent to
 * <html> before first paint so there is no light→dark or default→accent
 * flash. The host inlines {@link themeInitScript} in <head>; the web host
 * keeps `apps/web/index.html` byte-identical (a test enforces it).
 *
 * Generated from the same constants as the runtime (theme-model) so the
 * script and the provider cannot disagree on keys, migration or defaults.
 * It reads browser storage directly because it runs before any runtime
 * exists; the provider itself only uses the injected storage port. For that
 * reason it is exported only from the `@notrelix/ui-web/theme-init-script`
 * subpath, outside the pure UI import graph (check:ui-purity).
 */

import {
  DEFAULT_THEME_ACCENT,
  DEFAULT_THEME_MODE,
  LEGACY_ACCENT_MIGRATION,
  LEGACY_THEME_STORAGE_KEYS,
  THEME_ACCENTS,
  THEME_MODES,
  THEME_STORAGE_KEYS,
} from "./theme-model";

export function createThemeInitScript(): string {
  const config = {
    k: THEME_STORAGE_KEYS,
    l: LEGACY_THEME_STORAGE_KEYS,
    m: LEGACY_ACCENT_MIGRATION,
    modes: THEME_MODES,
    accents: THEME_ACCENTS,
    d: { mode: DEFAULT_THEME_MODE, accent: DEFAULT_THEME_ACCENT },
  };
  return [
    "(function(){",
    `var c=${JSON.stringify(config)};`,
    "var r=document.documentElement,mode=c.d.mode,accent=c.d.accent;",
    "try{",
    "var s=window.localStorage;",
    "var g=function(k){try{return s.getItem(k)}catch(e){return null}};",
    "var v=g(c.k.mode),i;",
    "if(c.modes.indexOf(v)>=0){mode=v}else{for(i=0;i<c.l.mode.length;i++){v=g(c.l.mode[i]);if(v==='light'||v==='dark'||v==='system'){mode=v;break}}}",
    "v=g(c.k.accent);",
    "if(c.accents.indexOf(v)>=0){accent=v}else{for(i=0;i<c.l.accent.length;i++){v=g(c.l.accent[i]);if(v&&c.m[v]){accent=c.m[v];break}}}",
    "}catch(e){}",
    "var dark=mode==='dark';",
    "try{if(mode==='system'){dark=window.matchMedia('(prefers-color-scheme: dark)').matches}}catch(e){}",
    "r.classList.remove('light','dark');",
    "r.classList.add(dark?'dark':'light');",
    "if(mode==='high-contrast'){r.setAttribute('data-contrast','high')}else{r.removeAttribute('data-contrast')}",
    "r.setAttribute('data-accent',accent);",
    "r.style.colorScheme=dark?'dark':'light';",
    "})();",
  ].join("");
}

export const themeInitScript = createThemeInitScript();

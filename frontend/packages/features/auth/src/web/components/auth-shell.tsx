import type { ReactNode } from "react";
import { Globe } from "lucide-react";
import { NotrelixLogoMark, cn } from "@notrelix/ui-web";
import type { AuthCopy } from "../copy/auth.copy";

/**
 * The frame of every sign-in page (the Auth mockup): the brand panel with its
 * promise and the big N, the form column, and under it the legal links and
 * the language switch. Pure: the copy, the locale and what a switch does are
 * injected.
 */
export interface AuthShellProps {
  copy: AuthCopy;
  locale: "vi" | "en";
  onLocaleChange: (locale: "vi" | "en") => void;
  children: ReactNode;
}

export function AuthShell({
  copy,
  locale,
  onLocaleChange,
  children,
}: AuthShellProps) {
  const t = copy.shell;
  return (
    <div className="flex min-h-screen w-full bg-popover text-foreground">
      <aside
        aria-label={t.brand}
        className="relative hidden flex-[0_0_44%] flex-col overflow-hidden border-r border-line bg-muted px-13 py-10 text-foreground lg:flex lg:max-w-[44%]"
      >
        <div className="relative z-10 flex items-center gap-3">
          <NotrelixLogoMark className="size-9" aria-hidden="true" />
          <b className="text-brand-name">{t.brand}</b>
        </div>
        <p className="relative z-10 mt-28 max-w-115 text-brand-promise">
          {t.promise}
        </p>
        <NotrelixLogoMark
          aria-hidden="true"
          className="pointer-events-none absolute -bottom-42.5 -left-7.5 size-175"
        />
      </aside>
      <main className="flex min-w-0 flex-1 flex-col bg-popover px-5 pt-7 pb-6 leading-normal sm:px-12">
        <div className="mb-6 flex items-center gap-2.5 lg:hidden">
          <NotrelixLogoMark className="size-7" aria-hidden="true" />
          <b className="text-dialog-title font-bold tracking-tight">
            {t.brand}
          </b>
        </div>
        <div className="flex min-h-0 flex-1 items-center justify-center">
          <div className="flex w-full max-w-100 flex-col gap-4.5">
            {children}
          </div>
        </div>
        <footer className="flex flex-wrap items-center justify-between gap-x-4 gap-y-2 text-crumb text-meta-foreground">
          <nav aria-label={t.footerLabel} className="flex items-center gap-1.5">
            {[t.terms, t.privacy, t.help].map((label, index) => (
              <span key={label} className="contents">
                {index > 0 ? <span aria-hidden="true">·</span> : null}
                <span className="rounded-[4px] px-0.5 py-1 font-medium">
                  {label}
                </span>
              </span>
            ))}
          </nav>
          <div
            role="group"
            aria-label={t.language}
            className="flex items-center gap-0.5 rounded-[8px] border border-line p-0.5"
          >
            <Globe aria-hidden="true" className="mx-1 ml-1.5 size-3.5" />
            {(["vi", "en"] as const).map((entry) => (
              <button
                key={entry}
                type="button"
                aria-pressed={locale === entry}
                onClick={() => onLocaleChange(entry)}
                className={cn(
                  "h-7 cursor-pointer rounded-[6px] border-0 bg-transparent px-2.5 text-crumb whitespace-nowrap max-sm:h-11 font-medium text-muted-foreground outline-none focus-visible:ring-2 focus-visible:ring-focus-ring",
                  locale === entry &&
                    "bg-state-hover-strong font-semibold text-foreground",
                )}
              >
                {t[entry]}
              </button>
            ))}
          </div>
        </footer>
      </main>
    </div>
  );
}

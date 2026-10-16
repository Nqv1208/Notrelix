import type { ReactNode } from "react";
import { useLocale } from "@notrelix/i18n";
import { AuthShell, useAuthCopy } from "@notrelix/features-auth";

/** The frame of every pre-login page: brand panel, form column, language switch. */
export function AuthLayout({ children }: { children: ReactNode }) {
  const copy = useAuthCopy();
  const { locale, setLocale } = useLocale();
  return (
    <AuthShell copy={copy} locale={locale} onLocaleChange={setLocale}>
      {children}
    </AuthShell>
  );
}

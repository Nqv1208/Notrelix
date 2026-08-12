import Link from "next/link";
import { ArrowLeft, Mail, Reply } from "lucide-react";
import { getTranslations } from "next-intl/server";

export default async function ContactPage() {
  const t = await getTranslations("contact");

  return (
    <div className="page min-h-screen pb-24 pt-16 sm:pt-20">
      <div className="container mx-auto max-w-3xl">
        <Link
          href="/"
          className="inline-flex items-center gap-2 text-sm text-[var(--muted-text)] transition-colors hover:text-[var(--ink)]"
        >
          <ArrowLeft className="size-3.5" /> {t("backHome")}
        </Link>
        <h1 className="mt-8 text-4xl font-semibold tracking-[-0.04em] text-[var(--ink)] sm:text-5xl">
          {t("title")}
        </h1>
        <p className="mt-4 text-base leading-7 text-[var(--muted-text)] sm:text-lg">
          {t("subtitle")}
        </p>

        <div className="mt-10 space-y-4">
          <div className="rounded-2xl border border-[var(--line)] bg-[var(--surface)] p-6">
            <h2 className="flex items-center gap-2 text-base font-semibold text-[var(--ink)]">
              <Mail className="size-4 text-[var(--cobalt)]" /> {t("emailTitle")}
            </h2>
            <p className="mt-3 text-sm leading-6 text-[var(--muted-text)]">
              {t("general")}{" "}
              <a
                href="mailto:hello@notrelix.com"
                className="font-medium text-[var(--cobalt)] hover:underline"
              >
                hello@notrelix.com
              </a>
            </p>
            <p className="mt-1 text-sm leading-6 text-[var(--muted-text)]">
              {t("support")}{" "}
              <a
                href="mailto:support@notrelix.com"
                className="font-medium text-[var(--cobalt)] hover:underline"
              >
                support@notrelix.com
              </a>
            </p>
          </div>

          <div className="rounded-2xl border border-[var(--line)] bg-[var(--surface)] p-6">
            <h2 className="flex items-center gap-2 text-base font-semibold text-[var(--ink)]">
              <Reply className="size-4 text-[var(--cobalt)]" />{" "}
              {t("responseTitle")}
            </h2>
            <p className="mt-3 text-sm leading-6 text-[var(--muted-text)]">
              {t("responseText")}
            </p>
          </div>
        </div>
      </div>
    </div>
  );
}

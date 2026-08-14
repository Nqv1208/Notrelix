import Link from "next/link";
import { ArrowLeft } from "lucide-react";
import { getTranslations } from "next-intl/server";
import type { Messages } from "../../../messages/en";

const CONTACT_EMAIL = "legal@notrelix.com";

export default async function TermsPage() {
  const t = await getTranslations("legal");
  const terms = t.raw("terms") as Messages["legal"]["terms"];
  const sections = terms.sections;

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
          {terms.title}
        </h1>
        <p className="mt-4 text-sm text-[var(--muted-text)]">
          {t("updated", {
            date: new Date().toLocaleDateString("en-US"),
          })}
        </p>

        <div className="mt-10 space-y-8 text-[15px] leading-7 text-[var(--ink)]">
          {sections.map((section, index) => (
            <section key={section.heading}>
              <h2 className="text-lg font-semibold tracking-[-0.02em] text-[var(--ink)]">
                {section.heading}
              </h2>
              <p className="mt-3 text-[var(--muted-text)]">
                {section.body}
                {index === sections.length - 1 ? (
                  <>
                    {" "}
                    <a
                      href={`mailto:${CONTACT_EMAIL}`}
                      className="font-medium text-[var(--cobalt)] hover:underline"
                    >
                      {CONTACT_EMAIL}
                    </a>
                  </>
                ) : null}
              </p>
            </section>
          ))}
        </div>
      </div>
    </div>
  );
}

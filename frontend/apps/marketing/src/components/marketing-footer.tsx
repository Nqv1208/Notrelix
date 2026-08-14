import Link from "next/link";
import {
  ArrowUpRight,
  BriefcaseBusiness,
  CodeXml,
  MessageCircle,
} from "lucide-react";
import { getTranslations } from "next-intl/server";
import type { Messages } from "../messages/en";

import Image from "next/image";

export async function MarketingFooter() {
  const t = await getTranslations("footer");
  const sections = t.raw("sections") as Messages["footer"]["sections"];

  return (
    <footer
      id="resources"
      className="border-t border-[var(--line)] bg-[var(--surface)]"
    >
      <div className="container py-16 sm:py-20">
        <div className="grid gap-12 lg:grid-cols-[1.35fr_2fr]">
          <div className="max-w-xs">
            <Link href="/" aria-label={t("logoAria")}>
              <Image
                src="/logo.svg"
                alt=""
                width={36}
                height={28}
                aria-hidden="true"
                className="h-9 w-auto"
              />
            </Link>
            <p className="mt-5 text-sm leading-6 text-[var(--muted-text)]">
              {t("tagline")}
            </p>
            <a
              href="mailto:hello@notrelix.com"
              className="mt-5 inline-flex items-center gap-1.5 text-sm font-semibold text-[var(--ink)] hover:text-[var(--cobalt)]"
            >
              hello@notrelix.com <ArrowUpRight className="size-3.5" />
            </a>
          </div>

          <div className="grid grid-cols-2 gap-x-6 gap-y-10 sm:grid-cols-4">
            {sections.map((section) => (
              <div key={section.title}>
                <h2 className="text-sm font-semibold text-[var(--ink)]">
                  {section.title}
                </h2>
                <ul className="mt-4 space-y-3">
                  {section.links.map((link) => (
                    <li key={link.label}>
                      <a
                        href={link.href}
                        className="text-sm text-[var(--muted-text)] transition-colors hover:text-[var(--ink)]"
                      >
                        {link.label}
                      </a>
                    </li>
                  ))}
                </ul>
              </div>
            ))}
          </div>
        </div>

        <div className="mt-14 flex flex-col gap-5 border-t border-[var(--line)] pt-6 text-sm text-[var(--muted-text)] sm:flex-row sm:items-center sm:justify-between">
          <p>{t("copyright", { year: new Date().getFullYear() })}</p>
          <div className="flex items-center gap-4">
            <span id="status" className="inline-flex items-center gap-2">
              <span
                className="size-2 rounded-full bg-[var(--mkt-brand-blue-500)]"
                aria-hidden="true"
              />
              {t("status")}
            </span>
            <a
              href="https://github.com/notrelix"
              target="_blank"
              rel="noreferrer"
              aria-label={t("githubAria")}
              className="transition-colors hover:text-[var(--ink)]"
            >
              <CodeXml className="size-4" />
            </a>
            <a
              href="https://www.linkedin.com/company/notrelix"
              target="_blank"
              rel="noreferrer"
              aria-label={t("linkedinAria")}
              className="transition-colors hover:text-[var(--ink)]"
            >
              <BriefcaseBusiness className="size-4" />
            </a>
            <a
              href="mailto:hello@notrelix.com"
              aria-label={t("emailAria")}
              className="transition-colors hover:text-[var(--ink)]"
            >
              <MessageCircle className="size-4" />
            </a>
          </div>
        </div>
      </div>
    </footer>
  );
}

import {
  ArrowUpRight,
  Megaphone,
  PackageOpen,
  Settings2,
  Target,
  UsersRound,
} from "lucide-react";
import { getTranslations } from "next-intl/server";
import type { Messages } from "../messages/en";

import { Reveal } from "../components/reveal";
import { SectionHeading } from "../components/section-heading";

const useCaseIcons = [Megaphone, PackageOpen, Settings2, Target, UsersRound];
const useCaseTones = ["coral", "violet", "teal", "amber", "ink"] as const;

export async function UseCasesSection() {
  const t = await getTranslations("useCases");
  const items = t.raw("items") as Messages["useCases"]["items"];

  return (
    <section id="use-cases" className="section">
      <div className="container">
        <Reveal>
          <SectionHeading
            eyebrow={t("eyebrow")}
            title={
              <>
                {t("title")}{" "}
                <span className="gradient-text">{t("titleHighlight")}</span>
              </>
            }
            description={t("description")}
          />
        </Reveal>
        <div className="mt-14 grid gap-4 sm:grid-cols-2 lg:grid-cols-5">
          {items.map((item, index) => {
            const Icon = useCaseIcons[index] ?? Megaphone;
            return (
              <Reveal key={item.title} delay={index * 60} className="h-full">
                <article className="use-case-card h-full">
                  <div
                    className={`icon-box icon-box--${useCaseTones[index] ?? "coral"}`}
                  >
                    <Icon className="size-5" />
                  </div>
                  <h3 className="mt-7 text-lg font-semibold tracking-[-0.035em] text-[var(--ink)]">
                    {item.title}
                  </h3>
                  <p className="mt-3 text-sm leading-6 text-[var(--muted-text)]">
                    {item.text}
                  </p>
                  <ul className="mt-5 space-y-2">
                    {item.benefits.map((benefit) => (
                      <li
                        key={benefit}
                        className="text-xs font-medium text-[var(--ink)]"
                      >
                        {benefit}
                      </li>
                    ))}
                  </ul>
                  <a
                    href="#pricing"
                    className="mt-7 inline-flex items-center gap-1.5 text-xs font-semibold text-[var(--cobalt)] transition-[gap] hover:gap-2.5"
                  >
                    {t("link")} <ArrowUpRight className="size-3.5" />
                  </a>
                </article>
              </Reveal>
            );
          })}
        </div>
      </div>
    </section>
  );
}

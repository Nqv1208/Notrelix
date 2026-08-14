import { ArrowRight, CircleAlert, Layers3, Route } from "lucide-react";
import { getTranslations } from "next-intl/server";
import type { Messages } from "../messages/en";

import { Reveal } from "../components/reveal";
import { SectionHeading } from "../components/section-heading";

const shiftIcons = [CircleAlert, Layers3, Route];
const shiftColors = ["violet", "brand", "blue"] as const;

export async function ProblemSolutionSection() {
  const t = await getTranslations("problemSolution");
  const shifts = t.raw("shifts") as Messages["problemSolution"]["shifts"];

  return (
    <section className="section bg-[var(--surface)]">
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

        <div className="mt-14 grid gap-4 lg:grid-cols-3">
          {shifts.map((shift, index) => {
            const Icon = shiftIcons[index] ?? CircleAlert;
            return (
              <Reveal key={shift.before} delay={index * 80}>
                <article className="shift-card h-full">
                  <div
                    className={`icon-box icon-box--${shiftColors[index] ?? "violet"}`}
                  >
                    <Icon className="size-5" />
                  </div>
                  <div className="mt-7 flex items-center gap-2 text-sm text-[var(--muted-text)]">
                    <span className="line-through decoration-[var(--mkt-brand-blue-400)]/70">
                      {shift.before}
                    </span>
                    <ArrowRight className="size-3.5" />
                  </div>
                  <h3 className="mt-2 text-xl font-semibold tracking-[-0.035em] text-[var(--ink)]">
                    {shift.after}
                  </h3>
                  <p className="mt-3 text-sm leading-6 text-[var(--muted-text)]">
                    {shift.text}
                  </p>
                </article>
              </Reveal>
            );
          })}
        </div>
      </div>
    </section>
  );
}

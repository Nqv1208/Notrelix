import { ArrowUpRight, Clock3, ListChecks, TrendingUp } from "lucide-react";
import { getTranslations } from "next-intl/server";
import type { Messages } from "../messages/en";

import { Reveal } from "../components/reveal";

const metricIcons = [Clock3, TrendingUp, ListChecks];
const metricTones = ["violet", "teal", "coral"] as const;

export async function MetricsSection() {
  const t = await getTranslations("metrics");
  const items = t.raw("items") as Messages["metrics"]["items"];

  return (
    <section className="section metrics-section">
      <div className="container">
        <div className="grid items-end gap-10 lg:grid-cols-[0.8fr_1.2fr]">
          <Reveal>
            <div>
              <span className="eyebrow text-white/70">{t("eyebrow")}</span>
              <h2 className="mt-4 max-w-md text-3xl font-semibold tracking-[-0.045em] text-white sm:text-4xl">
                {t("title")}
              </h2>
              <p className="mt-5 max-w-md text-sm leading-6 text-white/65">
                {t("subtitle")}
              </p>
              <a
                href="/contact"
                className="mt-7 inline-flex items-center gap-2 text-sm font-semibold text-white hover:gap-3"
              >
                {t("link")} <ArrowUpRight className="size-4" />
              </a>
            </div>
          </Reveal>
          <div className="grid gap-3 sm:grid-cols-3">
            {items.map((metric, index) => {
              const Icon = metricIcons[index] ?? Clock3;
              return (
                <Reveal key={metric.value} delay={index * 80}>
                  <div className="metric-card">
                    <div
                      className={`metric-icon metric-icon--${metricTones[index] ?? "violet"}`}
                    >
                      <Icon className="size-4" />
                    </div>
                    <div className="mt-8 text-4xl font-semibold tracking-[-0.06em] text-white">
                      {metric.value}
                    </div>
                    <p className="mt-2 text-sm leading-5 text-white/65">
                      {metric.label}
                    </p>
                  </div>
                </Reveal>
              );
            })}
          </div>
        </div>
      </div>
    </section>
  );
}

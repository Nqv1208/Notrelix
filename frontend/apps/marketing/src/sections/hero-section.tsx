import { ArrowRight, Check, Sparkles } from "lucide-react";
import { getTranslations } from "next-intl/server";
import type { Messages } from "../messages/en";

import { Badge } from "@notrelix/ui-web/components/ui/badge";
import { env } from "../config/env";
import { MarketingButton } from "../components/marketing-button";
import { Reveal } from "../components/reveal";
import { HeroProductPreview } from "./hero-product-preview";

export async function HeroSection() {
  const t = await getTranslations("hero");
  const floatCards = t.raw("floatCards") as Messages["hero"]["floatCards"];
  const checks = t.raw("checks") as string[];
  const preview = t.raw("preview") as Messages["hero"]["preview"];

  return (
    <section
      id="hero"
      className="relative overflow-hidden pb-24 pt-[160px] sm:pb-32 sm:pt-[160px] lg:pt-[160px]"
    >
      <div className="container">
        <div className="mx-auto max-w-4xl text-center">
          <Reveal>
            <Badge className="beta-badge">
              <span
                className="size-1.5 rounded-full bg-[var(--cobalt)]"
                aria-hidden="true"
              />
              {t("badge")}
            </Badge>
          </Reveal>
          <Reveal delay={70}>
            <h1 className="mt-7 text-4xl font-semibold leading-[1.12] tracking-[-0.01em] text-[var(--ink)] sm:text-5xl lg:text-6xl">
              {t("titleLine1")}{" "}
              <span className="gradient-text-animated block">
                {t("titleHighlight")}
              </span>
            </h1>
          </Reveal>
          <Reveal delay={140}>
            <p className="mx-auto mt-7 max-w-2xl text-base leading-7 text-[var(--muted-text)] sm:text-lg sm:leading-8">
              {t("subtitle")}
            </p>
          </Reveal>
          <Reveal delay={210}>
            <div className="mt-9 flex flex-col items-center justify-center gap-3 sm:flex-row">
              <MarketingButton
                variant="primary"
                size="lg"
                href={`${env.webAppUrl}/sign-up`}
                className="w-full sm:w-auto"
              >
                {t("ctaPrimary")} <ArrowRight className="size-4" />
              </MarketingButton>
              <MarketingButton
                variant="secondary"
                size="lg"
                href="#showcase"
                className="w-full sm:w-auto"
              >
                {t("ctaSecondary")}
                <span className="flex size-5 items-center justify-center rounded-full bg-[var(--lilac)] text-[var(--cobalt)]">
                  ▶
                </span>
              </MarketingButton>
            </div>
          </Reveal>
          <Reveal delay={280}>
            <div className="mt-7 flex flex-wrap items-center justify-center gap-x-5 gap-y-2 text-xs text-[var(--muted-text)] sm:text-sm">
              {checks.map((item) => (
                <span key={item} className="inline-flex items-center gap-1.5">
                  <Check className="size-3.5 text-[var(--cobalt)]" /> {item}
                </span>
              ))}
            </div>
          </Reveal>
        </div>

        <Reveal
          delay={120}
          className="relative mx-auto mt-16 max-w-6xl pb-10 sm:mt-20 sm:pb-24"
        >
          <div className="hero-glow" aria-hidden="true" />
          <HeroProductPreview copy={preview} />
          <p className="hero-preview__hint">{preview.hint}</p>
          <div className="float-card float-card--left hidden sm:block">
            <span className="flex size-8 items-center justify-center rounded-xl bg-[var(--mkt-brand-blue-100)] text-[var(--mkt-brand-blue-600)]">
              <Check className="size-4" />
            </span>
            <span>
              <strong>{floatCards.tasks.strong}</strong>
              <small>{floatCards.tasks.small}</small>
            </span>
          </div>
          <div className="float-card float-card--right hidden sm:flex">
            <span className="flex size-8 items-center justify-center rounded-xl bg-[var(--surface-brand)] text-[var(--cobalt)]">
              <Sparkles className="size-4" />
            </span>
            <span>
              <strong>{floatCards.progress.strong}</strong>
              <small>{floatCards.progress.small}</small>
            </span>
          </div>
        </Reveal>
      </div>
    </section>
  );
}

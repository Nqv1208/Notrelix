import { ArrowRight, Check } from "lucide-react";
import { getTranslations } from "next-intl/server";
import type { Messages } from "../messages/en";
import Logo from "@notrelix/ui-web/assets/logo.svg";

import { Reveal } from "../components/reveal";
import { MarketingButton } from "../components/marketing-button";
import { env } from "../config/env";

export async function FinalCtaSection() {
  const t = await getTranslations("finalCta");
  const checks = t.raw("checks") as Messages["finalCta"]["checks"];

  return (
    <section id="final-cta" className="section pt-8 sm:pt-12">
      <div className="container">
        <Reveal>
          <div className="final-cta">
            <div className="final-cta-grid" aria-hidden="true" />
            <div className="relative z-10 mx-auto max-w-2xl text-center">
              <span className="mx-auto flex size-12 items-center justify-center rounded-2xl bg-white/12">
                <Logo className="size-6 brightness-0 invert" />
              </span>
              <h2 className="mt-6 text-3xl font-semibold tracking-[-0.05em] text-[var(--mkt-inverse-text)] sm:text-5xl sm:leading-[1.05]">
                {t("title")}
              </h2>
              <p className="mx-auto mt-5 max-w-xl text-base leading-7 text-[var(--mkt-inverse-text)]/70">
                {t("description")}
              </p>
              <div className="mt-8 flex flex-col items-center justify-center gap-3 sm:flex-row">
                <MarketingButton
                  variant="inverse"
                  size="lg"
                  href={`${env.webAppUrl}/sign-up`}
                  className="w-full sm:w-auto"
                >
                  {t("ctaPrimary")} <ArrowRight className="size-4" />
                </MarketingButton>
                <MarketingButton
                  variant="inverse-ghost"
                  size="lg"
                  href="/contact"
                  className="w-full sm:w-auto"
                >
                  {t("ctaSecondary")}
                </MarketingButton>
              </div>
              <div className="mt-7 flex flex-wrap items-center justify-center gap-x-5 gap-y-2 text-xs text-[var(--mkt-inverse-text)]/60">
                {checks.map((check) => (
                  <span
                    key={check}
                    className="inline-flex items-center gap-1.5"
                  >
                    <Check className="size-3.5 text-[var(--mkt-cta-text-inverse)]" />{" "}
                    {check}
                  </span>
                ))}
              </div>
            </div>
          </div>
        </Reveal>
      </div>
    </section>
  );
}

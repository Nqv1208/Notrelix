"use client";

import * as React from "react";
import { Check, Sparkles } from "lucide-react";
import { useTranslations } from "next-intl";
import type { Messages } from "../messages/en";

import { Reveal } from "../components/reveal";
import { SectionHeading } from "../components/section-heading";
import { env } from "../config/env";

type BillingCycle = "monthly" | "yearly";

const CONTACT_SALES = "Contact sales";
const suffixKeys: Array<"freeSuffix" | "perUserSuffix"> = [
  "freeSuffix",
  "perUserSuffix",
];

export function PricingTeaserSection() {
  const t = useTranslations("pricing");
  const plans = t.raw("plans") as Messages["pricing"]["plans"];

  const [cycle, setCycle] = React.useState<BillingCycle>("monthly");

  return (
    <section id="pricing" className="section bg-[var(--surface)]">
      <div className="container">
        <Reveal>
          <SectionHeading
            align="center"
            eyebrow={t("eyebrow")}
            title={
              <>
                {t("titleTagline")}{" "}
                <span className="gradient-text">{t("titleHighlight")}</span>
              </>
            }
            description={t("description")}
          />
        </Reveal>
        <Reveal delay={80} className="mt-8 flex justify-center">
          <div
            className="billing-toggle"
            role="group"
            aria-label={t("billingAria")}
          >
            <button
              type="button"
              aria-pressed={cycle === "monthly"}
              onClick={() => setCycle("monthly")}
              className={cycle === "monthly" ? "is-active" : ""}
            >
              {t("monthly")}
            </button>
            <button
              type="button"
              aria-pressed={cycle === "yearly"}
              onClick={() => setCycle("yearly")}
              className={cycle === "yearly" ? "is-active" : ""}
            >
              {t("yearly")} <span>{t("yearlySave")}</span>
            </button>
          </div>
        </Reveal>
        <div className="mt-12 grid gap-4 lg:grid-cols-3">
          {plans.map((plan, index) => (
            <Reveal key={plan.name} delay={index * 70}>
              <article
                className={`price-card ${plan.name === "Growth" ? "is-featured" : ""} ${plan.name === "Enterprise" ? "is-dark" : ""}`}
              >
                {plan.name === "Growth" ? (
                  <div className="popular-label">
                    <Sparkles className="size-3" /> {t("popular")}
                  </div>
                ) : null}
                <h3 className="text-lg font-semibold tracking-[-0.03em]">
                  {plan.name}
                </h3>
                <p className="mt-3 min-h-12 text-sm leading-6 opacity-70">
                  {plan.description}
                </p>
                <div className="mt-7 flex items-baseline gap-1">
                  {plan[cycle] === CONTACT_SALES ? (
                    <span className="text-4xl font-semibold tracking-[-0.06em]">
                      {t("contactSales")}
                    </span>
                  ) : plan[cycle] === "0" ? (
                    <span className="text-4xl font-semibold tracking-[-0.06em]">
                      {t("freeSuffix")}
                    </span>
                  ) : (
                    <>
                      <span className="text-4xl font-semibold tracking-[-0.06em]">
                        ${plan[cycle]}
                      </span>
                      <span className="text-xs opacity-65">
                        {" "}
                        / {t(suffixKeys[index] ?? "freeSuffix")}
                      </span>
                    </>
                  )}
                </div>
                <a
                  href={
                    plan.name === "Enterprise"
                      ? "/contact"
                      : `${env.webAppUrl}/sign-up`
                  }
                  className={`mt-7 flex h-11 items-center justify-center rounded-xl px-4 text-sm font-semibold transition-transform hover:-translate-y-0.5 ${plan.name === "Growth" ? "bg-[var(--ink)] text-white" : plan.name === "Enterprise" ? "bg-white text-[var(--ink)]" : "border border-[var(--line-strong)] text-[var(--ink)] hover:bg-[var(--surface)]"}`}
                >
                  {plan.name === "Enterprise"
                    ? t("talkToSales")
                    : t("startFree")}
                </a>
                <ul className="mt-8 space-y-3 border-t border-current/10 pt-7">
                  {plan.features.map((feature) => (
                    <li
                      key={feature}
                      className="flex gap-2.5 text-sm opacity-80"
                    >
                      <Check className="mt-0.5 size-4 shrink-0 text-[var(--teal)]" />
                      {feature}
                    </li>
                  ))}
                </ul>
              </article>
            </Reveal>
          ))}
        </div>
      </div>
    </section>
  );
}

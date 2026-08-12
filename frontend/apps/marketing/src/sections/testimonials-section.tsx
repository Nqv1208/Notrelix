"use client";

import * as React from "react";
import { ArrowLeft, ArrowRight, Quote } from "lucide-react";
import { useTranslations } from "next-intl";
import type { Messages } from "../messages/en";

import { Avatar, AvatarFallback } from "@notrelix/ui-web/components/ui/avatar";
import { Reveal } from "../components/reveal";

const tones = [
  "bg-[var(--mkt-surface-brand-strong)] text-[var(--mkt-text-on-brand)]",
  "bg-[var(--lilac)] text-[var(--cobalt)]",
  "bg-[var(--mkt-brand-100)] text-[var(--mkt-brand-700)]",
];

export function TestimonialsSection() {
  const t = useTranslations("testimonials");
  const items = t.raw("items") as Messages["testimonials"]["items"];

  const [active, setActive] = React.useState(0);
  const testimonial = items[active] ?? items[0]!;

  return (
    <section id="testimonials" className="section">
      <div className="container">
        <Reveal>
          <div className="mx-auto max-w-4xl rounded-[2rem] border border-[var(--line)] bg-[var(--mkt-surface-raised)] p-7 shadow-[0_24px_80px_rgb(var(--mkt-shadow-rgb)_/_6%)] sm:p-12 lg:p-16">
            <div className="flex items-center justify-between">
              <span className="eyebrow">{t("eyebrow")}</span>
              <Quote className="size-8 text-[var(--lilac-strong)]" />
            </div>
            <blockquote className="mt-10 max-w-3xl text-2xl font-medium leading-[1.2] tracking-[-0.04em] text-[var(--ink)] sm:text-4xl">
              “{testimonial.quote}”
            </blockquote>
            <div className="mt-10 flex flex-col gap-5 sm:flex-row sm:items-center sm:justify-between">
              <div className="flex items-center gap-3">
                <Avatar className="size-11">
                  <AvatarFallback className={tones[active % tones.length]!}>
                    {testimonial.initials}
                  </AvatarFallback>
                </Avatar>
                <div>
                  <p className="text-sm font-semibold text-[var(--ink)]">
                    {testimonial.name}
                  </p>
                  <p className="mt-1 text-xs text-[var(--muted-text)]">
                    {testimonial.role} · {testimonial.company}
                  </p>
                </div>
              </div>
              <div className="flex items-center gap-2">
                <button
                  type="button"
                  aria-label={t("prevAria")}
                  onClick={() =>
                    setActive(
                      (current) => (current - 1 + items.length) % items.length,
                    )
                  }
                  className="icon-button"
                >
                  <ArrowLeft className="size-4" />
                </button>
                <button
                  type="button"
                  aria-label={t("nextAria")}
                  onClick={() =>
                    setActive((current) => (current + 1) % items.length)
                  }
                  className="icon-button"
                >
                  <ArrowRight className="size-4" />
                </button>
                <div
                  className="ml-2 flex items-center gap-1.5"
                  aria-label={t("viewingAria", {
                    current: active + 1,
                    total: items.length,
                  })}
                >
                  {items.map((item, index) => (
                    <button
                      key={item.name}
                      type="button"
                      aria-label={t("viewAria", { name: item.name })}
                      aria-current={active === index}
                      onClick={() => setActive(index)}
                      className={`size-1.5 rounded-full transition-all ${active === index ? "w-5 bg-[var(--cobalt)]" : "bg-[var(--line-strong)]"}`}
                    />
                  ))}
                </div>
              </div>
            </div>
          </div>
        </Reveal>
      </div>
    </section>
  );
}

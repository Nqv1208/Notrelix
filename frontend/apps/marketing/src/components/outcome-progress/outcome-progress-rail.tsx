"use client";

import * as React from "react";
import { MarketingContainer } from "../primitives/marketing-container";
import { MarketingSection } from "../primitives/marketing-section";
import { OUTCOME_PROGRESS_ITEMS } from "./outcome-progress.data";
import { OutcomeProgressItem } from "./outcome-progress-item";

export function OutcomeProgressRail() {
  const [isVisible, setIsVisible] = React.useState(false);
  const containerRef = React.useRef<HTMLDivElement>(null);

  React.useEffect(() => {
    const element = containerRef.current;
    if (!element) return;

    const observer = new IntersectionObserver(
      ([entry]) => {
        if (!entry) return;
        if (entry.isIntersecting) {
          setIsVisible(true);
          observer.disconnect();
        }
      },
      { threshold: 0.2 },
    );

    observer.observe(element);
    return () => observer.disconnect();
  }, []);

  return (
    <MarketingSection
      id="outcome-progress-rail"
      variant="default"
      spacing="lg"
      className="relative overflow-hidden py-16 lg:py-24"
    >
      <MarketingContainer>
        <div
          ref={containerRef}
          className={`outcome-progress-rail ${isVisible ? "is-visible" : ""}`}
        >
          {/* Desktop Rail View (Hidden on mobile) */}
          <div className="relative hidden w-full lg:block">
            {/* Baseline Track & Animated Progress Fill */}
            <div
              aria-hidden="true"
              className="absolute left-[12.5%] right-[12.5%] top-[36px] z-0 h-[2px] -translate-y-1/2"
            >
              {/* Neutral Base Line */}
              <div className="absolute inset-0 h-full w-full bg-[color-mix(in_srgb,var(--mkt-border)_40%,transparent)]" />

              {/* Animated Gradient Progress Fill */}
              <div className="outcome-progress-rail__fill-h absolute inset-0 h-full w-full rounded-full bg-gradient-to-r from-[var(--mkt-brand-red-500)] via-[var(--mkt-brand-bridge-orange)] via-[var(--mkt-brand-bridge-gold)] to-[var(--mkt-brand-blue-500)]" />

              {/* Intermediate Anchor Dots between Major Milestones */}
              <div className="absolute inset-0 flex items-center justify-between px-[16.66%] pointer-events-none">
                <span className="h-2 w-2 rounded-full border border-[var(--mkt-border)] bg-[var(--mkt-surface)] shadow-sm" />
                <span className="h-2 w-2 rounded-full border border-[var(--mkt-border)] bg-[var(--mkt-surface)] shadow-sm" />
                <span className="h-2 w-2 rounded-full border border-[var(--mkt-border)] bg-[var(--mkt-surface)] shadow-sm" />
              </div>
            </div>

            {/* Semantic Milestone List */}
            <ol className="relative z-10 grid grid-cols-4 items-start gap-4">
              {OUTCOME_PROGRESS_ITEMS.map((item, idx) => (
                <OutcomeProgressItem key={item.id} item={item} index={idx} />
              ))}
            </ol>
          </div>

          {/* Mobile Vertical Timeline View (Visible on mobile/tablet) */}
          <div className="relative w-full lg:hidden">
            {/* Vertical Base Line & Progress Fill */}
            <div
              aria-hidden="true"
              className="absolute left-[32px] sm:left-[36px] top-[32px] sm:top-[36px] bottom-[32px] sm:bottom-[36px] z-0 w-[2px] -translate-x-1/2"
            >
              <div className="absolute inset-0 h-full w-full bg-[color-mix(in_srgb,var(--mkt-border)_40%,transparent)]" />
              <div className="outcome-progress-rail__fill-v absolute inset-0 h-full w-full rounded-full bg-gradient-to-b from-[var(--mkt-brand-red-500)] via-[var(--mkt-brand-bridge-orange)] via-[var(--mkt-brand-bridge-gold)] to-[var(--mkt-brand-blue-500)]" />
            </div>

            {/* Mobile Vertical Milestone List */}
            <ol className="relative z-10 flex flex-col gap-10">
              {OUTCOME_PROGRESS_ITEMS.map((item, idx) => (
                <li
                  key={item.id}
                  className="outcome-progress-item group flex items-start gap-5"
                  style={{ "--node-index": idx } as React.CSSProperties}
                >
                  {/* Marker Node */}
                  <div className="outcome-progress-item__marker relative z-10 flex h-16 w-16 shrink-0 items-center justify-center rounded-full sm:h-18 sm:w-18">
                    <div className="absolute inset-0 rounded-full bg-[color-mix(in_srgb,var(--mkt-brand-bridge-orange)_15%,transparent)] blur-sm opacity-60" />
                    <div className="relative h-full w-full rounded-full p-[2px] bg-gradient-to-br from-[var(--mkt-brand-red-500)] via-[var(--mkt-brand-bridge-orange)] to-[var(--mkt-brand-blue-500)] shadow-md">
                      <div className="flex h-full w-full items-center justify-center rounded-full bg-[#0c1017] text-white shadow-inner">
                        <item.icon className="size-5 text-white sm:size-6" />
                      </div>
                    </div>
                  </div>

                  {/* Content Group */}
                  <div className="outcome-progress-item__content flex flex-col pt-2">
                    <span className="outcome-progress-item__value text-xl font-bold tracking-tight text-[var(--mkt-text)] sm:text-2xl">
                      {item.value}
                    </span>
                    <span className="outcome-progress-item__label mt-1 text-xs font-medium text-[var(--mkt-text-muted)] sm:text-sm">
                      {item.label}
                    </span>
                  </div>
                </li>
              ))}
            </ol>
          </div>
        </div>
      </MarketingContainer>
    </MarketingSection>
  );
}

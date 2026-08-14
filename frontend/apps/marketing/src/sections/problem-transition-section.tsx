import * as React from "react";
import { getTranslations } from "next-intl/server";

import { MarketingContainer } from "../components/primitives/marketing-container";
import { MarketingSection } from "../components/primitives/marketing-section";
import { Reveal } from "../components/reveal";
import { ProblemRotator } from "./problem-rotator";
import { PROBLEM_KEYS, type ProblemKey } from "./problem-transition.data";
import { SparkleDecoration } from "./sparkle-decoration";
import { SwirlArrowDecoration } from "./swirl-arrow-decoration";

export async function ProblemTransitionSection() {
  const t = await getTranslations("problemTransition");

  const problemsMap = PROBLEM_KEYS.reduce(
    (acc, key) => {
      acc[key] = t(`problems.${key}`);
      return acc;
    },
    {} as Record<ProblemKey, string>
  );

  return (
    <MarketingSection
      id="problem-transition"
      variant="default"
      spacing="lg"
      className="relative overflow-hidden py-16 lg:py-24"
    >
      <MarketingContainer>
        <Reveal>
          {/* Main Top Grid */}
          <div className="relative grid grid-cols-1 items-center gap-10 lg:grid-cols-[0.85fr_1.15fr] lg:gap-16">
            {/* Left Column: Heading, Sparkle & Swirl Arrow */}
            <div className="relative flex flex-col items-start justify-center pr-4">
              <div className="relative w-full">
                {/* Sparkle SVG Decoration positioned at top-left right above the letter W of Wave */}
                <div
                  aria-hidden="true"
                  className="absolute -top-2 -left-5 sm:-top-2.5 sm:-left-6 lg:-top-3 lg:-left-6 pointer-events-none select-none z-10 text-[var(--mkt-text)]"
                >
                  <SparkleDecoration className="size-5 sm:size-6 lg:size-7" />
                </div>

                <h2 className="relative z-10 text-4xl font-extrabold tracking-[-0.04em] text-[var(--mkt-text)] sm:text-5xl lg:text-6xl">
                  {t("heading")}
                </h2>

                {/* Inline SVG Swirl Arrow (positioned higher and behind text) */}
                <div
                  aria-hidden="true"
                  className="absolute -top-16 -right-4 sm:-top-20 sm:-right-12 lg:-top-24 lg:-right-16 w-32 sm:w-40 lg:w-48 pointer-events-none select-none z-0 opacity-90"
                >
                  <SwirlArrowDecoration className="w-full h-auto" />
                </div>
              </div>
            </div>

            {/* Right Column: Problem Rotator Focus Window */}
            <div className="relative w-full">
              <div aria-hidden="true">
                <ProblemRotator problemsMap={problemsMap} />
              </div>

              {/* Accessible Summary for Screen Readers */}
              <p className="sr-only">{t("accessibleSummary")}</p>
            </div>
          </div>
        </Reveal>
      </MarketingContainer>
    </MarketingSection>
  );
}

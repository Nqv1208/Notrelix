import * as React from "react";

import { MarketingContainer } from "../components/primitives/marketing-container";
import { MarketingSection } from "../components/primitives/marketing-section";
import { Marquee } from "../components/primitives/marquee";
import { SectionHeading } from "../components/section-heading";

const problems = [
  "Tool Fragmentation",
  "Lost Context",
  "Manual Handoffs",
  "Duplicate Updates",
  "Scattered Documents",
  "Unclear Ownership",
  "Workflow Bottlenecks",
  "Permission Chaos",
];

export function ProblemMarqueeSection() {
  return (
    <MarketingSection
      variant="soft"
      spacing="md"
      className="border-y border-[color-mix(in_srgb,var(--mkt-border)_50%,transparent)]"
    >
      <MarketingContainer>
        <SectionHeading
          eyebrow="Eliminate Friction"
          title="Leave fragmented work behind"
          description="Consolidate disconnected tools, context switching, and manual updates into one unified workspace."
          align="center"
          className="mb-10 lg:mb-14"
        />
      </MarketingContainer>

      <Marquee speedSeconds={40} pauseOnHover={true} className="py-3">
        {problems.map((problem) => (
          <div
            key={problem}
            className="flex items-center gap-3 rounded-full border border-[color-mix(in_srgb,var(--mkt-border)_80%,transparent)] bg-[color-mix(in_srgb,var(--mkt-surface)_90%,transparent)] px-5 py-2.5 shadow-sm transition-transform hover:scale-105"
          >
            <span className="flex h-2 w-2 rounded-full bg-[var(--mkt-brand-red-500)]" />
            <span className="text-sm font-semibold tracking-wide text-[var(--mkt-text)] whitespace-nowrap sm:text-base">
              {problem}
            </span>
          </div>
        ))}
      </Marquee>
    </MarketingSection>
  );
}

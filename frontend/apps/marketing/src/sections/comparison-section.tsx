import * as React from "react";

import { MarketingContainer } from "../components/primitives/marketing-container";
import { MarketingSection } from "../components/primitives/marketing-section";
import { SectionHeading } from "../components/section-heading";

interface ComparisonPoint {
  title: string;
  traditional: string;
  notrelix: string;
}

const comparisonPoints: ComparisonPoint[] = [
  {
    title: "Tool Alignment",
    traditional: "5+ separate apps for tasks, docs, chat, and status tracking",
    notrelix: "One connected workspace unifying docs, boards, and automation",
  },
  {
    title: "Context & Handoffs",
    traditional: "Copy-pasting links, lost threads, manual status updates",
    notrelix: "Live linked context, automatic cross-workspace updates",
  },
  {
    title: "Workflow Control",
    traditional: "Rigid static templates with complex setup overhead",
    notrelix: "Flexible visual workflows tailored to team governance",
  },
  {
    title: "Security & Access",
    traditional: "Scattered permission models across disconnected SaaS",
    notrelix: "Unified enterprise RBAC, audit trails, and data isolation",
  },
];

export function ComparisonSection() {
  return (
    <MarketingSection variant="default" spacing="lg" id="comparison">
      <MarketingContainer>
        <SectionHeading
          eyebrow="Why Notrelix"
          title="Designed for clarity, built for enterprise work"
          description="See how a connected workspace compares to managing work across fragmented tools."
          align="center"
          className="mb-12 lg:mb-16"
        />

        <div className="grid gap-8 lg:grid-cols-2 lg:gap-10">
          {/* Traditional Card */}
          <div className="relative rounded-2xl border border-[color-mix(in_srgb,var(--mkt-border)_60%,transparent)] bg-[color-mix(in_srgb,var(--mkt-surface)_60%,transparent)] p-6 sm:p-8 opacity-85 shadow-sm">
            <div className="mb-6 flex items-center justify-between border-b border-[color-mix(in_srgb,var(--mkt-border)_40%,transparent)] pb-4">
              <div>
                <span className="text-xs font-semibold tracking-wider uppercase text-[var(--mkt-text-muted)]">
                  Traditional Approach
                </span>
                <h3 className="mt-1 text-xl font-bold text-[var(--mkt-text-muted)]">
                  Fragmented Tools
                </h3>
              </div>
              <span
                className="flex h-8 w-8 items-center justify-center rounded-full bg-[color-mix(in_srgb,var(--mkt-border)_60%,transparent)] text-[var(--mkt-text-muted)] font-bold text-sm"
                aria-hidden="true"
              >
                ✕
              </span>
            </div>

            <ul className="space-y-5">
              {comparisonPoints.map((pt) => (
                <li key={pt.title} className="flex items-start gap-3">
                  <span
                    className="mt-1 flex h-4 w-4 shrink-0 items-center justify-center rounded-full bg-red-500/15 text-[10px] font-bold text-red-500"
                    aria-hidden="true"
                  >
                    ✕
                  </span>
                  <div>
                    <span className="font-semibold text-sm text-[var(--mkt-text-muted)]">
                      {pt.title}:
                    </span>{" "}
                    <span className="text-sm text-[var(--mkt-text-muted)]">
                      {pt.traditional}
                    </span>
                  </div>
                </li>
              ))}
            </ul>
          </div>

          {/* Notrelix Card */}
          <div className="relative rounded-2xl border-2 border-[color-mix(in_srgb,var(--mkt-brand-blue-500)_60%,transparent)] bg-[color-mix(in_srgb,var(--mkt-surface)_96%,transparent)] p-6 sm:p-8 shadow-xl ring-1 ring-[color-mix(in_srgb,var(--mkt-brand-blue-500)_20%,transparent)]">
            <div className="absolute -top-3.5 right-6 rounded-full bg-gradient-to-r from-[var(--mkt-brand-red-500)] to-[var(--mkt-brand-blue-600)] px-3 py-1 text-xs font-bold text-white shadow-md">
              The Notrelix Advantage
            </div>

            <div className="mb-6 flex items-center justify-between border-b border-[color-mix(in_srgb,var(--mkt-border)_80%,transparent)] pb-4">
              <div>
                <span className="text-xs font-semibold tracking-wider uppercase text-[var(--mkt-brand-blue-600)] dark:text-[var(--mkt-brand-blue-400)]">
                  Unified Work OS
                </span>
                <h3 className="mt-1 text-xl font-bold text-[var(--mkt-text)]">
                  Notrelix Connected Workspace
                </h3>
              </div>
              <span
                className="flex h-8 w-8 items-center justify-center rounded-full bg-emerald-500/15 text-emerald-600 dark:text-emerald-400 font-bold text-sm"
                aria-hidden="true"
              >
                ✓
              </span>
            </div>

            <ul className="space-y-5">
              {comparisonPoints.map((pt) => (
                <li key={pt.title} className="flex items-start gap-3">
                  <span
                    className="mt-1 flex h-4 w-4 shrink-0 items-center justify-center rounded-full bg-emerald-500/15 text-[10px] font-bold text-emerald-600 dark:text-emerald-400"
                    aria-hidden="true"
                  >
                    ✓
                  </span>
                  <div>
                    <span className="font-semibold text-sm text-[var(--mkt-text)]">
                      {pt.title}:
                    </span>{" "}
                    <span className="text-sm leading-relaxed text-[var(--mkt-text-muted)]">
                      {pt.notrelix}
                    </span>
                  </div>
                </li>
              ))}
            </ul>
          </div>
        </div>
      </MarketingContainer>
    </MarketingSection>
  );
}

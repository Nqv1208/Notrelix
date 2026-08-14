import * as React from "react";

import { FaqAccordion } from "../components/primitives/faq-accordion";
import type { FaqItem } from "../components/primitives/faq-accordion";
import { MarketingContainer } from "../components/primitives/marketing-container";
import { MarketingSection } from "../components/primitives/marketing-section";
import { SectionHeading } from "../components/section-heading";

const faqItems: FaqItem[] = [
  {
    id: "what-is-notrelix",
    question: "What makes Notrelix different from traditional task managers?",
    answer:
      "Notrelix is an enterprise workspace operating system that connects project boards, collaborative documents, and automated workflows into one unified platform. Unlike isolated task apps, Notrelix ensures real-time context sharing across teams.",
  },
  {
    id: "data-security",
    question: "How does Notrelix protect enterprise data and privacy?",
    answer:
      "Notrelix enforces multi-tenant row-level security (RLS), encrypted data persistence, granular role-based access control (RBAC), and full compliance audit logging to keep enterprise data isolated and secure.",
  },
  {
    id: "migration",
    question: "Can we import existing projects and documents from other tools?",
    answer:
      "Yes. Notrelix provides standard import paths for popular project management platforms and Markdown/Rich-Text document formats, preserving team structure and history during migration.",
  },
  {
    id: "pricing-plans",
    question: "Is there a free trial or flexible billing option?",
    answer:
      "Yes! Notrelix offers a free tier for small teams and a 14-day trial of our Pro and Enterprise plans with monthly or annual billing options.",
  },
  {
    id: "collaboration",
    question: "Does Notrelix support real-time team collaboration?",
    answer:
      "Absolutely. Notrelix features live presence indicators, instant document co-editing, thread comments, and real-time board updates across all workspace members.",
  },
];

export function FaqSection() {
  return (
    <MarketingSection variant="soft" spacing="lg" id="faq">
      <MarketingContainer>
        <SectionHeading
          eyebrow="Frequently Asked Questions"
          title="Everything you need to know about Notrelix"
          description="Have questions about our workspace platform? Here are answers to common questions."
          align="center"
          className="mb-12 lg:mb-16"
        />

        <div className="mx-auto max-w-3xl">
          <FaqAccordion items={faqItems} defaultOpenId="what-is-notrelix" />
        </div>
      </MarketingContainer>
    </MarketingSection>
  );
}

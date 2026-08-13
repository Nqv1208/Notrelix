"use client";

import * as React from "react";

export interface FaqItem {
  id: string;
  question: string;
  answer: string;
  category?: string;
}

interface FaqAccordionProps {
  items: FaqItem[];
  defaultOpenId?: string;
  className?: string;
}

export function FaqAccordion({
  items,
  defaultOpenId,
  className = "",
}: FaqAccordionProps) {
  const [openId, setOpenId] = React.useState<string | null>(
    defaultOpenId ?? (items[0]?.id || null)
  );

  const toggle = (id: string) => {
    setOpenId((prev) => (prev === id ? null : id));
  };

  return (
    <div className={`divide-y divide-[color-mix(in_srgb,var(--mkt-border)_65%,transparent)] border-y border-[color-mix(in_srgb,var(--mkt-border)_65%,transparent)] ${className}`}>
      {items.map((item) => {
        const isOpen = openId === item.id;
        const buttonId = `faq-btn-${item.id}`;
        const panelId = `faq-panel-${item.id}`;

        return (
          <div key={item.id} className="py-4 transition-colors">
            <h3>
              <button
                id={buttonId}
                type="button"
                aria-expanded={isOpen}
                aria-controls={panelId}
                onClick={() => toggle(item.id)}
                className="flex w-full items-center justify-between gap-4 text-left font-semibold text-[var(--mkt-text)] transition-colors hover:text-[var(--mkt-brand-blue-600)] focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--mkt-brand-blue-500)] focus-visible:ring-offset-2 rounded-lg p-2"
              >
                <span className="text-base sm:text-lg">{item.question}</span>
                <span
                  className={`flex h-7 w-7 shrink-0 items-center justify-center rounded-full border border-[color-mix(in_srgb,var(--mkt-border)_80%,transparent)] bg-[color-mix(in_srgb,var(--mkt-surface)_80%,transparent)] text-sm font-semibold transition-transform duration-200 ${
                    isOpen ? "rotate-45 text-[var(--mkt-brand-blue-600)]" : "text-[var(--mkt-text-muted)]"
                  }`}
                  aria-hidden="true"
                >
                  +
                </span>
              </button>
            </h3>
            <div
              id={panelId}
              role="region"
              aria-labelledby={buttonId}
              aria-hidden={!isOpen}
              className={`grid transition-[grid-template-rows,opacity] duration-200 ease-out ${
                isOpen ? "grid-rows-[1fr] opacity-100 mt-2" : "grid-rows-[0fr] opacity-0"
              }`}
            >
              <div className="overflow-hidden">
                <p className="px-2 pb-2 text-sm leading-relaxed text-[var(--mkt-text-muted)] sm:text-base">
                  {item.answer}
                </p>
              </div>
            </div>
          </div>
        );
      })}
    </div>
  );
}

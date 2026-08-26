import * as React from "react";
import type { OutcomeProgressItemData } from "./outcome-progress.types";

type OutcomeProgressItemProps = {
  item: OutcomeProgressItemData;
  index: number;
};

export function OutcomeProgressItem({ item, index }: OutcomeProgressItemProps) {
  const Icon = item.icon;

  return (
    <li
      className="outcome-progress-item group flex flex-col items-center text-center"
      style={{ "--node-index": index } as React.CSSProperties}
    >
      {/* Milestone Marker Node */}
      <div className="outcome-progress-item__marker relative z-10 flex h-16 w-16 items-center justify-center rounded-full sm:h-18 sm:w-18">
        {/* Soft Ambient Halo */}
        <div className="absolute inset-0 rounded-full bg-[color-mix(in_srgb,var(--mkt-brand-bridge-orange)_15%,transparent)] blur-sm opacity-60 transition-opacity duration-300 group-hover:opacity-100" />

        {/* Static Brand Gradient Ring */}
        <div className="relative h-full w-full rounded-full p-[2px] bg-gradient-to-br from-[var(--mkt-brand-red-500)] via-[var(--mkt-brand-bridge-orange)] to-[var(--mkt-brand-blue-500)] shadow-md transition-transform duration-300 group-hover:-translate-y-0.5">
          {/* Inverse Dark Core for High Contrast Icon Visibility */}
          <div className="flex h-full w-full items-center justify-center rounded-full bg-[#0c1017] text-white shadow-inner">
            <Icon className="size-5 text-white sm:size-6" />
          </div>
        </div>
      </div>

      {/* Value & Label Group */}
      <div className="outcome-progress-item__content mt-4 flex flex-col items-center text-center">
        <span className="outcome-progress-item__value text-xl font-bold tracking-tight text-[var(--mkt-text)] sm:text-2xl lg:text-3xl">
          {item.value}
        </span>
        <span className="outcome-progress-item__label mt-1.5 max-w-[200px] text-xs font-medium text-[var(--mkt-text)] sm:text-sm">
          {item.label}
        </span>
      </div>
    </li>
  );
}

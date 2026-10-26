"use client";

import * as React from "react";
import Image from "next/image";
import { CalendarDays, GanttChart, LayoutGrid, Table2 } from "lucide-react";

import type { Messages } from "../messages/en";

type PreviewCopy = Messages["hero"]["preview"];
type PreviewTab = PreviewCopy["tabs"][number]["id"];

const AUTO_ADVANCE_MS = 6000;

const tabIcons: Record<PreviewTab, typeof LayoutGrid> = {
  table: Table2,
  kanban: LayoutGrid,
  timeline: GanttChart,
  calendar: CalendarDays,
};

export function HeroProductPreview({ copy }: { copy: PreviewCopy }) {
  const { tabs } = copy;
  const [activeId, setActiveId] = React.useState<PreviewTab>("table");
  const [paused, setPaused] = React.useState(false);
  const [reducedMotion, setReducedMotion] = React.useState(true);
  const tabRefs = React.useRef<Array<HTMLButtonElement | null>>([]);

  React.useEffect(() => {
    const query = window.matchMedia("(prefers-reduced-motion: reduce)");
    const sync = () => setReducedMotion(query.matches);
    sync();
    query.addEventListener("change", sync);
    return () => query.removeEventListener("change", sync);
  }, []);

  React.useEffect(() => {
    if (paused || reducedMotion) return;
    const timer = window.setInterval(() => {
      setActiveId((current) => {
        const index = tabs.findIndex((tab) => tab.id === current);
        return tabs[(index + 1) % tabs.length]?.id ?? current;
      });
    }, AUTO_ADVANCE_MS);
    return () => window.clearInterval(timer);
  }, [paused, reducedMotion, tabs]);

  const moveTab = (direction: 1 | -1) => {
    const index = tabs.findIndex((tab) => tab.id === activeId);
    const nextIndex = (index + direction + tabs.length) % tabs.length;
    const nextTab = tabs[nextIndex];
    if (!nextTab) return;
    setActiveId(nextTab.id);
    tabRefs.current[nextIndex]?.focus();
  };

  return (
    <div
      className="product-window hero-preview"
      onMouseEnter={() => setPaused(true)}
      onMouseLeave={() => setPaused(false)}
      onFocusCapture={() => setPaused(true)}
      onBlurCapture={() => setPaused(false)}
    >
      <div className="hero-preview__chrome">
        <div className="flex items-center gap-1.5" aria-hidden="true">
          <span className="hero-preview__dot" />
          <span className="hero-preview__dot" />
          <span className="hero-preview__dot" />
        </div>
        <span className="hero-preview__url" aria-hidden="true">
          {copy.urlLabel}
        </span>
        <div
          role="tablist"
          aria-label={copy.tabsLabel}
          className="hero-preview__tabs"
        >
          {tabs.map((tab, index) => {
            const Icon = tabIcons[tab.id];
            const selected = activeId === tab.id;
            return (
              <button
                key={tab.id}
                ref={(element) => {
                  tabRefs.current[index] = element;
                }}
                type="button"
                role="tab"
                id={`hero-preview-tab-${tab.id}`}
                aria-selected={selected}
                aria-controls="hero-preview-panel"
                tabIndex={selected ? 0 : -1}
                onClick={() => setActiveId(tab.id)}
                onKeyDown={(event) => {
                  if (event.key === "ArrowRight") {
                    event.preventDefault();
                    moveTab(1);
                  }
                  if (event.key === "ArrowLeft") {
                    event.preventDefault();
                    moveTab(-1);
                  }
                }}
                className={`hero-preview__tab ${selected ? "is-active" : ""}`}
              >
                <Icon className="size-3.5" aria-hidden="true" />
                {tab.label}
              </button>
            );
          })}
        </div>
      </div>
      <div
        role="tabpanel"
        id="hero-preview-panel"
        aria-labelledby={`hero-preview-tab-${activeId}`}
        className="hero-preview__stage"
      >
        {tabs.map((tab, index) => {
          const active = tab.id === activeId;
          return (
            <div
              key={tab.id}
              className={`hero-preview__layer ${active ? "is-active" : ""}`}
              aria-hidden={active ? undefined : true}
            >
              {(["light", "dark"] as const).map((theme) => (
                <Image
                  key={theme}
                  src={`/product/${tab.id}-${theme}.png`}
                  alt={active ? tab.alt : ""}
                  width={2880}
                  height={1800}
                  sizes="(min-width: 1152px) 1152px, 100vw"
                  priority={index === 0}
                  quality={90}
                  className={`hero-preview__img hero-preview__img--${theme}`}
                />
              ))}
            </div>
          );
        })}
      </div>
    </div>
  );
}

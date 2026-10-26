"use client";

import * as React from "react";
import Image from "next/image";

import type { Messages } from "../messages/en";
import hotspots from "./hero-preview-hotspots.json";

type PreviewCopy = Messages["hero"]["preview"];
type PreviewTab = PreviewCopy["tabs"][number]["id"];

const AUTO_ADVANCE_MS = 6000;

/**
 * The screenshots carry the web app's own view tabs. Transparent buttons sit
 * exactly over those tabs (boxes recorded by `pnpm marketing:previews`), so the
 * app's tabs are the switcher.
 */
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

  const activeIndex = tabs.findIndex((tab) => tab.id === activeId);
  const nextId = tabs[(activeIndex + 1) % tabs.length]?.id;
  const hinting = !paused && !reducedMotion;

  const moveTab = (direction: 1 | -1) => {
    const nextIndex = (activeIndex + direction + tabs.length) % tabs.length;
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
      </div>
      <div className="hero-preview__body">
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
        <div
          role="tablist"
          aria-label={copy.tabsLabel}
          className="hero-preview__hotspots"
        >
          {tabs.map((tab, index) => {
            const selected = activeId === tab.id;
            const box = hotspots[tab.id];
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
                style={{
                  left: `${box.left}%`,
                  top: `${box.top}%`,
                  width: `${box.width}%`,
                  height: `${box.height}%`,
                }}
                className={`hero-preview__hotspot ${
                  hinting && tab.id === nextId && !selected ? "is-next" : ""
                }`}
              >
                <span className="sr-only">{tab.label}</span>
              </button>
            );
          })}
        </div>
      </div>
    </div>
  );
}

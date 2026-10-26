"use client";

import * as React from "react";
import Image from "next/image";

export type ShotBox = {
  left: number;
  top: number;
  width: number;
  height: number;
  /** Where the label sits against the region; set where the default would cover the screen. */
  place?:
    | "above-start"
    | "above-end"
    | "below-start"
    | "below-end"
    | "right"
    | "inside-end";
};

export type ShotNote = { key: string; label: string; box: ShotBox };

export type ShotTab = {
  id: string;
  label: string;
  alt: string;
  notes?: readonly ShotNote[];
};

type ProductShotTabsProps = {
  tabs: readonly ShotTab[];
  /** Prefix of the ids the tabs and the panel carry. */
  idPrefix: string;
  /** Accessible name of the tablist. */
  label: string;
  /** Screenshots are `${srcPrefix}${tab.id}-${theme}.png` in the public folder. */
  srcPrefix: string;
  /**
   * Boxes (percent of the screenshot) of the app's own tabs inside the image.
   * Transparent buttons sit over them, so the app's tabs are the switcher.
   * Without it a segmented control above the frame switches the screens.
   */
  hotspots?: Record<string, ShotBox>;
  /** Shows a window bar with this address above the screenshot. */
  chrome?: string;
  /** Moves to the next tab on this interval until the pointer or focus arrives. */
  autoAdvanceMs?: number;
  /** Loads the first screenshot eagerly (above the fold). */
  priorityFirst?: boolean;
  /** Intrinsic size of the screenshots. */
  width: number;
  height: number;
  className?: string;
};

/**
 * One fixed-ratio frame with a set of real product screenshots, light and dark
 * swapped by CSS, cross-faded on change. Annotations pin short labels to the
 * regions the capture script recorded; under 720px they collapse to numbered
 * markers with a legend under the image.
 */
export function ProductShotTabs({
  tabs,
  idPrefix,
  label,
  srcPrefix,
  hotspots,
  chrome,
  autoAdvanceMs,
  priorityFirst = false,
  width,
  height,
  className = "",
}: ProductShotTabsProps) {
  const [activeId, setActiveId] = React.useState(tabs[0]?.id ?? "");
  const [paused, setPaused] = React.useState(false);
  const [reducedMotion, setReducedMotion] = React.useState(true);
  const tabRefs = React.useRef<Array<HTMLButtonElement | null>>([]);
  const barRef = React.useRef<HTMLDivElement>(null);
  const ownBar = !hotspots;

  React.useEffect(() => {
    const query = window.matchMedia("(prefers-reduced-motion: reduce)");
    const sync = () => setReducedMotion(query.matches);
    sync();
    query.addEventListener("change", sync);
    return () => query.removeEventListener("change", sync);
  }, []);

  React.useEffect(() => {
    if (!autoAdvanceMs || paused || reducedMotion) return;
    const timer = window.setInterval(() => {
      setActiveId((current) => {
        const index = tabs.findIndex((tab) => tab.id === current);
        return tabs[(index + 1) % tabs.length]?.id ?? current;
      });
    }, autoAdvanceMs);
    return () => window.clearInterval(timer);
  }, [autoAdvanceMs, paused, reducedMotion, tabs]);

  // On a narrow screen the segmented control scrolls sideways: keep the
  // active tab in view without moving the page.
  React.useEffect(() => {
    const bar = barRef.current;
    const index = tabs.findIndex((tab) => tab.id === activeId);
    const tab = tabRefs.current[index];
    if (!bar || !tab || bar.scrollWidth <= bar.clientWidth) return;
    bar.scrollTo({
      left: tab.offsetLeft - (bar.clientWidth - tab.offsetWidth) / 2,
      behavior: reducedMotion ? "auto" : "smooth",
    });
  }, [activeId, reducedMotion, tabs]);

  const activeIndex = Math.max(
    0,
    tabs.findIndex((tab) => tab.id === activeId),
  );
  const active = tabs[activeIndex];
  const nextId = tabs[(activeIndex + 1) % tabs.length]?.id;
  const hinting = Boolean(autoAdvanceMs) && !paused && !reducedMotion;
  const panelId = `${idPrefix}-panel`;

  const moveTab = (direction: 1 | -1) => {
    const nextIndex = (activeIndex + direction + tabs.length) % tabs.length;
    const nextTab = tabs[nextIndex];
    if (!nextTab) return;
    setActiveId(nextTab.id);
    tabRefs.current[nextIndex]?.focus();
  };

  const tabProps = (tab: ShotTab, index: number) => {
    const selected = tab.id === activeId;
    return {
      ref: (element: HTMLButtonElement | null) => {
        tabRefs.current[index] = element;
      },
      type: "button" as const,
      role: "tab" as const,
      id: `${idPrefix}-tab-${tab.id}`,
      "aria-selected": selected,
      "aria-controls": panelId,
      tabIndex: selected ? 0 : -1,
      onClick: () => setActiveId(tab.id),
      onKeyDown: (event: React.KeyboardEvent) => {
        if (event.key === "ArrowRight") {
          event.preventDefault();
          moveTab(1);
        } else if (event.key === "ArrowLeft") {
          event.preventDefault();
          moveTab(-1);
        } else if (event.key === "Home") {
          event.preventDefault();
          setActiveId(tabs[0]?.id ?? activeId);
          tabRefs.current[0]?.focus();
        } else if (event.key === "End") {
          event.preventDefault();
          setActiveId(tabs[tabs.length - 1]?.id ?? activeId);
          tabRefs.current[tabs.length - 1]?.focus();
        }
      },
    };
  };

  return (
    <div className={`shot ${className}`}>
      {ownBar ? (
        <div
          ref={barRef}
          role="tablist"
          aria-label={label}
          className="shot-tabs"
        >
          {tabs.map((tab, index) => (
            <button key={tab.id} {...tabProps(tab, index)} className="shot-tab">
              {tab.label}
            </button>
          ))}
        </div>
      ) : null}
      <div
        className="product-window shot-frame"
        onMouseEnter={() => setPaused(true)}
        onMouseLeave={() => setPaused(false)}
        onFocusCapture={() => setPaused(true)}
        onBlurCapture={() => setPaused(false)}
      >
        {chrome ? (
          <div className="shot-chrome">
            <div className="flex items-center gap-1.5" aria-hidden="true">
              <span className="shot-chrome__dot" />
              <span className="shot-chrome__dot" />
              <span className="shot-chrome__dot" />
            </div>
            <span className="shot-chrome__url" aria-hidden="true">
              {chrome}
            </span>
          </div>
        ) : null}
        <div className="shot-body">
          <div
            role="tabpanel"
            id={panelId}
            aria-labelledby={`${idPrefix}-tab-${activeId}`}
            className="shot-stage"
            style={{ aspectRatio: `${width} / ${height}` }}
          >
            {tabs.map((tab, index) => {
              const isActive = tab.id === activeId;
              return (
                <div
                  key={tab.id}
                  className={`shot-layer ${isActive ? "is-active" : ""}`}
                  aria-hidden={isActive ? undefined : true}
                >
                  {(["light", "dark"] as const).map((theme) => (
                    <Image
                      key={theme}
                      src={`${srcPrefix}${tab.id}-${theme}.png`}
                      alt={isActive ? tab.alt : ""}
                      width={width}
                      height={height}
                      sizes="(min-width: 1152px) 1152px, 100vw"
                      priority={priorityFirst && index === 0}
                      quality={90}
                      className={`shot-img shot-img--${theme}`}
                    />
                  ))}
                </div>
              );
            })}
            {active?.notes?.length ? (
              <ol key={active.id} className="shot-notes" aria-hidden="true">
                {active.notes.map((note, index) => (
                  <li
                    key={note.key}
                    className="shot-note"
                    data-place={note.box.place ?? "above-start"}
                    style={
                      {
                        left: `${note.box.left}%`,
                        top: `${note.box.top}%`,
                        width: `${note.box.width}%`,
                        height: `${note.box.height}%`,
                        "--note-index": index,
                      } as React.CSSProperties
                    }
                  >
                    <span className="shot-note__ring" />
                    <span className="shot-note__tag">
                      <b>{index + 1}</b>
                      <span>{note.label}</span>
                    </span>
                  </li>
                ))}
              </ol>
            ) : null}
          </div>
          {hotspots ? (
            <div role="tablist" aria-label={label} className="shot-hotspots">
              {tabs.map((tab, index) => {
                const box = hotspots[tab.id];
                if (!box) return null;
                return (
                  <button
                    key={tab.id}
                    {...tabProps(tab, index)}
                    style={{
                      left: `${box.left}%`,
                      top: `${box.top}%`,
                      width: `${box.width}%`,
                      height: `${box.height}%`,
                    }}
                    className={`shot-hotspot ${
                      hinting && tab.id === nextId && tab.id !== activeId
                        ? "is-next"
                        : ""
                    }`}
                  >
                    <span className="sr-only">{tab.label}</span>
                  </button>
                );
              })}
            </div>
          ) : null}
        </div>
      </div>
      {active?.notes?.length ? (
        <ol className="shot-legend" aria-label={active.label}>
          {active.notes.map((note, index) => (
            <li key={note.key}>
              <b aria-hidden="true">{index + 1}</b>
              <span>{note.label}</span>
            </li>
          ))}
        </ol>
      ) : null}
    </div>
  );
}

"use client";

import * as React from "react";
import { PROBLEM_KEYS, type ProblemKey } from "./problem-transition.data";

interface ProblemRotatorProps {
  problemsMap: Record<ProblemKey, string>;
  className?: string;
}

export function ProblemRotator({ problemsMap, className }: ProblemRotatorProps) {
  const [activeIndex, setActiveIndex] = React.useState(0);
  const [isPaused, setIsPaused] = React.useState(false);
  const [prefersReducedMotion, setPrefersReducedMotion] = React.useState(false);

  const total = PROBLEM_KEYS.length as number;

  React.useEffect(() => {
    const mediaQuery = window.matchMedia("(prefers-reduced-motion: reduce)");
    setPrefersReducedMotion(mediaQuery.matches);

    const handleChange = (e: MediaQueryListEvent) => {
      setPrefersReducedMotion(e.matches);
    };

    mediaQuery.addEventListener("change", handleChange);
    return () => mediaQuery.removeEventListener("change", handleChange);
  }, []);

  React.useEffect(() => {
    if (isPaused || prefersReducedMotion || total === 0) {
      return;
    }

    // Hold for 2000ms before advancing to next item (transition takes 500ms via CSS)
    const interval = setInterval(() => {
      setActiveIndex((prev) => (prev + 1) % total);
    }, 2500);

    return () => clearInterval(interval);
  }, [isPaused, prefersReducedMotion, total]);

  return (
    <div
      className={`problem-rotator-container relative h-[260px] sm:h-[300px] lg:h-[340px] w-full overflow-hidden problem-rotator-mask ${
        className ?? ""
      }`}
      onMouseEnter={() => setIsPaused(true)}
      onMouseLeave={() => setIsPaused(false)}
    >
      <div className="relative flex h-full w-full flex-col items-start justify-center">
        {PROBLEM_KEYS.map((key, index) => {
          // Compute shortest distance in circular list
          let diff = index - activeIndex;
          if (diff > total / 2) diff -= total;
          if (diff < -total / 2) diff += total;

          const isActive = diff === 0;
          const isVisible = Math.abs(diff) <= 2;

          // Vertical offset: 64px spacing per step
          const translateY = diff * 64;

          let opacity = 0;
          if (isActive) {
            opacity = 1;
          } else if (Math.abs(diff) === 1) {
            opacity = 0.35;
          } else if (Math.abs(diff) === 2) {
            opacity = 0.12;
          }

          return (
            <div
              key={key}
              data-active={isActive ? "true" : "false"}
              className="problem-rotator__phrase absolute left-0 right-0 py-1 transition-all duration-500 ease-[cubic-bezier(0.16,1,0.3,1)] select-none"
              style={{
                transform: `translateY(${translateY}px)`,
                opacity: isVisible ? opacity : 0,
                visibility: isVisible ? "visible" : "hidden",
                pointerEvents: "none",
              }}
            >
              <h3 className="text-3xl font-bold tracking-tight sm:text-4xl lg:text-5xl whitespace-nowrap">
                <span className="problem-rotator__text-gradient">
                  {problemsMap[key] ?? key}
                </span>
              </h3>
            </div>
          );
        })}
      </div>
    </div>
  );
}

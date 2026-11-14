"use client";

import { useEffect, useRef, type ReactNode } from "react";

/** How long the intro runs (ms): after it the pieces stop asking for a GPU layer of their own. */
const INTRO_MS = 1900;

/**
 * The layer the hero orbit sits in. Once the pieces have flown out it eases `--mx` / `--my` (-1..1)
 * toward the pointer's place over the hero; each piece moves by its own depth. Only with a fine,
 * hovering pointer and no reduced-motion request; the idle float and the intro are CSS.
 */
export function HeroOrbitParallax({ children }: { children: ReactNode }) {
  const layer = useRef<HTMLDivElement | null>(null);

  useEffect(() => {
    const node = layer.current;
    if (!node) return;
    const intro = window.setTimeout(
      () => node.classList.remove("is-intro"),
      INTRO_MS,
    );
    const allowed =
      window.matchMedia("(hover: hover) and (pointer: fine)").matches &&
      !window.matchMedia("(prefers-reduced-motion: reduce)").matches;
    const hero = node.closest<HTMLElement>("#hero");
    if (!allowed || !hero) return () => window.clearTimeout(intro);

    let tx = 0;
    let ty = 0;
    let x = 0;
    let y = 0;
    let frame = 0;
    const tick = () => {
      x += (tx - x) * 0.08;
      y += (ty - y) * 0.08;
      node.style.setProperty("--mx", x.toFixed(3));
      node.style.setProperty("--my", y.toFixed(3));
      frame =
        Math.abs(tx - x) > 0.002 || Math.abs(ty - y) > 0.002
          ? window.requestAnimationFrame(tick)
          : 0;
    };
    const wake = () => {
      if (!frame && !document.hidden)
        frame = window.requestAnimationFrame(tick);
    };
    const onMove = (event: PointerEvent) => {
      const box = hero.getBoundingClientRect();
      tx = ((event.clientX - box.left) / box.width) * 2 - 1;
      ty = ((event.clientY - box.top) / Math.min(box.height, 760)) * 2 - 1;
      tx = Math.max(-1, Math.min(1, tx));
      ty = Math.max(-1, Math.min(1, ty));
      wake();
    };
    const onLeave = () => {
      tx = 0;
      ty = 0;
      wake();
    };
    hero.addEventListener("pointermove", onMove);
    hero.addEventListener("pointerleave", onLeave);
    return () => {
      window.clearTimeout(intro);
      window.cancelAnimationFrame(frame);
      hero.removeEventListener("pointermove", onMove);
      hero.removeEventListener("pointerleave", onLeave);
    };
  }, []);

  return (
    <div ref={layer} className="orbit-layer is-intro" aria-hidden="true">
      {children}
    </div>
  );
}

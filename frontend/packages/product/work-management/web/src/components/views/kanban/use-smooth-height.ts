import { useCallback, useEffect, useRef } from "react";

/**
 * Lets an element whose height follows its content (a title that wraps when its row is narrowed) change height
 * with a short animation instead of jumping, so the cards under it move smoothly too.
 *
 * A resize observer reports the new height before the frame is painted; the previous height is kept and the
 * element's height is animated from it. The first measure, a reduced-motion preference and a change of a pixel
 * or less animate nothing. Returns a ref callback to compose with the element's other ref.
 */
let quieted = false;
/**
 * Animating the height of the element a resize observer watches makes the browser report "ResizeObserver loop
 * completed with undelivered notifications" once per change. It is harmless (the animation still runs and the
 * guard below stops any feedback), but dev overlays treat it as an error, so that one message is dropped.
 */
function quietObserverLoopNotice() {
  if (quieted || typeof window === "undefined") return;
  quieted = true;
  window.addEventListener(
    "error",
    (event) => {
      if (event.message?.startsWith("ResizeObserver loop"))
        event.stopImmediatePropagation();
    },
    true,
  );
}

export function useSmoothHeight(enabled: boolean, duration = 150) {
  const element = useRef<HTMLElement | null>(null);
  const last = useRef<number | null>(null);
  const observer = useRef<ResizeObserver | null>(null);
  const running = useRef<Animation | null>(null);
  const enabledRef = useRef(enabled);
  enabledRef.current = enabled;

  useEffect(
    () => () => {
      observer.current?.disconnect();
      observer.current = null;
    },
    [],
  );

  return useCallback(
    (node: HTMLElement | null) => {
      observer.current?.disconnect();
      observer.current = null;
      element.current = node;
      last.current = null;
      running.current = null;
      if (
        !node ||
        typeof ResizeObserver === "undefined" ||
        typeof node.animate !== "function"
      )
        return;
      quietObserverLoopNotice();
      const watcher = new ResizeObserver((entries) => {
        // The animation itself changes the height every frame: those reports are not content changes.
        if (running.current) return;
        const box = entries[0]?.borderBoxSize?.[0];
        const next = box ? box.blockSize : node.getBoundingClientRect().height;
        const prev = last.current;
        last.current = next;
        if (
          prev === null ||
          Math.abs(next - prev) <= 1 ||
          !enabledRef.current ||
          window.matchMedia?.("(prefers-reduced-motion: reduce)").matches
        )
          return;
        const animation = node.animate(
          [
            { height: `${prev}px`, overflow: "hidden", flexShrink: 0 },
            { height: `${next}px`, overflow: "hidden", flexShrink: 0 },
          ],
          { duration, easing: "ease-out" },
        );
        running.current = animation;
        const done = () => {
          if (running.current !== animation) return;
          running.current = null;
          last.current = node.getBoundingClientRect().height;
        };
        animation.addEventListener("finish", done);
        animation.addEventListener("cancel", done);
      });
      watcher.observe(node, { box: "border-box" });
      observer.current = watcher;
    },
    [duration],
  );
}

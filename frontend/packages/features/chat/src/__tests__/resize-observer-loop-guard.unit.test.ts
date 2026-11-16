// @vitest-environment jsdom
import { describe, expect, it } from "vitest";
import { guardResizeObserverLoop } from "../web/components/resize-observer-loop-guard";

function raise(message: string): { prevented: boolean; reached: boolean } {
  let reached = false;
  const seen = () => {
    reached = true;
  };
  window.addEventListener("error", seen);
  const event = new ErrorEvent("error", { message, cancelable: true });
  window.dispatchEvent(event);
  window.removeEventListener("error", seen);
  return { prevented: event.defaultPrevented, reached };
}

describe("guardResizeObserverLoop", () => {
  it("swallows the benign resize-observer message and nothing else", () => {
    const stop = guardResizeObserverLoop();
    expect(
      raise("ResizeObserver loop completed with undelivered notifications."),
    ).toEqual({ prevented: true, reached: false });
    expect(raise("Cannot read properties of undefined")).toEqual({
      prevented: false,
      reached: true,
    });
    stop();
    expect(
      raise("ResizeObserver loop completed with undelivered notifications."),
    ).toEqual({ prevented: false, reached: true });
  });
});

import { describe, expect, it } from "vitest";
import {
  MAX_ZOOM,
  MIN_ZOOM,
  clampZoom,
  pinchZoom,
  swipeStep,
  wheelZoom,
} from "../core/model/image-zoom-model";

describe("image zoom model (UC-CHT-I03)", () => {
  it("keeps the scale between 1 and 4", () => {
    expect(clampZoom(0.2)).toBe(MIN_ZOOM);
    expect(clampZoom(9)).toBe(MAX_ZOOM);
  });
  it("magnifies on a scroll up, shrinks on a scroll down and never goes below the fitted size", () => {
    expect(wheelZoom(1, -100)).toBeGreaterThan(1);
    expect(wheelZoom(2, 100)).toBeLessThan(2);
    expect(wheelZoom(1, 100)).toBe(1);
    expect(wheelZoom(MAX_ZOOM, -100)).toBe(MAX_ZOOM);
  });
  it("scales a pinch by how far the fingers moved apart", () => {
    expect(pinchZoom(1, 100, 200)).toBe(2);
    expect(pinchZoom(2, 100, 10)).toBe(1);
    expect(pinchZoom(1.5, 0, 50)).toBe(1.5);
  });
  it("reads a long, flat drag of the fitted image as a swipe and nothing else", () => {
    expect(swipeStep(-80, 5, 1)).toBe(1);
    expect(swipeStep(80, -5, 1)).toBe(-1);
    expect(swipeStep(-20, 0, 1)).toBe(0);
    expect(swipeStep(-80, 70, 1)).toBe(0);
    expect(swipeStep(-80, 0, 2)).toBe(0);
  });
});

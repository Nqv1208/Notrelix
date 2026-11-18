/** The viewer's zoom range: 1 is the fitted image, 4 the most it magnifies. */
export const MIN_ZOOM = 1;
export const MAX_ZOOM = 4;
/** The distance, in pixels, a finger or pointer must travel sideways to count as a swipe. */
export const SWIPE_DISTANCE = 50;

export function clampZoom(scale: number): number {
  return Math.min(MAX_ZOOM, Math.max(MIN_ZOOM, scale));
}

/** The scale after one wheel notch: a scroll up magnifies, a scroll down shrinks (UC-CHT-I03). */
export function wheelZoom(scale: number, deltaY: number): number {
  return clampZoom(scale * (deltaY < 0 ? 1.15 : 1 / 1.15));
}

/** The scale of a pinch: the start scale times how much the two fingers moved apart. */
export function pinchZoom(
  startScale: number,
  startDistance: number,
  distance: number,
): number {
  if (startDistance <= 0) return startScale;
  return clampZoom(startScale * (distance / startDistance));
}

/**
 * A mostly horizontal drag of the fitted image is a swipe: -1 for the previous image (dragged right), 1 for the next
 * (dragged left), 0 when it is too short, too steep, or the image is zoomed (a drag then pans).
 */
export function swipeStep(dx: number, dy: number, scale: number): -1 | 0 | 1 {
  if (scale > MIN_ZOOM) return 0;
  if (Math.abs(dx) < SWIPE_DISTANCE || Math.abs(dx) < Math.abs(dy) * 2)
    return 0;
  return dx < 0 ? 1 : -1;
}

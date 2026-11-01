/**
 * The person has just asked for a new checklist: the list that appears next opens its item box and
 * takes focus. A list that a colleague adds (or a refetch brings) does not.
 */
let askedAt = 0;
const WINDOW_MS = 8000;

export function noteChecklistRequested(): void {
  askedAt = Date.now();
}

/** True once for the list that answers the request. */
export function takeChecklistRequest(): boolean {
  const fresh = Date.now() - askedAt < WINDOW_MS;
  if (fresh) askedAt = 0;
  return fresh;
}

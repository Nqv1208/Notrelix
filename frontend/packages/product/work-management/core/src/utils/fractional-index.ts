export function generatePosition(before?: number, after?: number): number {
  if (before === undefined && after === undefined) return 1;
  if (before === undefined) return after === undefined ? 1 : after - 1;
  if (after === undefined) return before + 1;
  return before + (after - before) / 2;
}

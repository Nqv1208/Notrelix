/**
 * The ten colours a board label can take, as the values the server stores
 * (data, not interface colours: the chip itself is painted by its tone).
 */
export const LABEL_PALETTE = [
  ["blue", "#2F6FD0"],
  ["teal", "#1F8A76"],
  ["violet", "#7B4BD1"],
  ["amber", "#C48A00"],
  ["rose", "#D13A5B"],
  ["gray", "#6B7482"],
  ["orange", "#D9660E"],
  ["green", "#2E9A5E"],
  ["indigo", "#4650C8"],
  ["red", "#D12F2F"],
] as const;

export type LabelColorName = (typeof LABEL_PALETTE)[number][0];

/** The palette entry a stored colour is, if it is one of the ten. */
export function paletteNameOf(color: string): LabelColorName | null {
  const folded = color.trim().toLowerCase();
  return (
    LABEL_PALETTE.find(([, value]) => value.toLowerCase() === folded)?.[0] ??
    null
  );
}

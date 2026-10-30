import { createContext, useContext, useMemo, type ReactNode } from "react";
import { cn } from "@notrelix/ui-web";
import { useCapabilitiesCopy } from "./use-capabilities-copy";

/** The ten shapes, one per colour family, drawn in the colour of the text beside them. */
const SHAPES = [
  "circle",
  "square",
  "triangle",
  "diamond",
  "star",
  "dash",
  "hexagon",
  "plus",
  "cross",
  "ring",
] as const;
export type LabelShape = (typeof SHAPES)[number];

/** The copy key of each shape's spoken name. */
const SHAPE_COLOR = {
  circle: "blue",
  square: "teal",
  triangle: "violet",
  diamond: "amber",
  star: "rose",
  dash: "gray",
  hexagon: "orange",
  plus: "green",
  cross: "indigo",
  ring: "red",
} as const;

const PATHS: Readonly<Record<LabelShape, ReactNode>> = {
  circle: <circle cx="6" cy="6" r="4" fill="currentColor" />,
  square: <rect x="2" y="2" width="8" height="8" rx="1" fill="currentColor" />,
  triangle: <path d="M6 1.5 11 10H1Z" fill="currentColor" />,
  diamond: <path d="M6 1 11 6 6 11 1 6Z" fill="currentColor" />,
  star: (
    <path
      d="m6 1 1.4 3.2 3.4.3-2.6 2.2.8 3.3L6 8.3 3 10l.8-3.3L1.2 4.5l3.4-.3Z"
      fill="currentColor"
    />
  ),
  dash: <rect x="1.5" y="5" width="9" height="2" rx="1" fill="currentColor" />,
  hexagon: <path d="M3.5 1.5h5L11 6l-2.5 4.5h-5L1 6Z" fill="currentColor" />,
  plus: (
    <path d="M5 1.5h2V5h3.5v2H7v3.5H5V7H1.5V5H5Z" fill="currentColor" />
  ),
  cross: (
    <path
      d="m2.5 1 3.5 3.5L9.5 1 11 2.5 7.5 6 11 9.5 9.5 11 6 7.5 2.5 11 1 9.5 4.5 6 1 2.5Z"
      fill="currentColor"
    />
  ),
  ring: (
    <circle cx="6" cy="6" r="3.5" fill="none" stroke="currentColor" strokeWidth="2" />
  ),
};

/** A stable shape for a label colour: the same colour string always gets the same shape. */
export function labelShapeFor(color: string): LabelShape {
  let hash = 0;
  for (let index = 0; index < color.length; index += 1) {
    hash = (hash * 31 + color.charCodeAt(index)) | 0;
  }
  return SHAPES[Math.abs(hash) % SHAPES.length] ?? "circle";
}

interface ColourBlindControl {
  readonly enabled: boolean;
  readonly setEnabled?: (next: boolean) => void;
}

const ColourBlindLabelsContext = createContext<ColourBlindControl>({
  enabled: false,
});

/**
 * The host tells the labels whether the person turned colour-blind mode on (an account preference), and
 * how to change it.
 */
export function ColourBlindLabelsProvider({
  enabled,
  setEnabled,
  children,
}: {
  enabled: boolean;
  setEnabled?: (next: boolean) => void;
  children: ReactNode;
}) {
  const value = useMemo(() => ({ enabled, setEnabled }), [enabled, setEnabled]);
  return (
    <ColourBlindLabelsContext.Provider value={value}>
      {children}
    </ColourBlindLabelsContext.Provider>
  );
}

export function useColourBlindLabels(): boolean {
  return useContext(ColourBlindLabelsContext).enabled;
}

/** Whether colour-blind mode is on and how to switch it; `setEnabled` is absent when the host cannot save it. */
export function useColourBlindLabelsControl(): ColourBlindControl {
  return useContext(ColourBlindLabelsContext);
}

/**
 * The small shape beside a label's name when colour-blind mode is on; nothing otherwise. Pass `force`
 * to draw it regardless (the toggle's own preview).
 */
export function ColourBlindLabelMark({
  color,
  force = false,
  className,
}: {
  color: string;
  force?: boolean;
  className?: string;
}) {
  const on = useColourBlindLabels();
  const copy = useCapabilitiesCopy();
  if (!on && !force) return null;
  const shape = labelShapeFor(color);
  return (
    <svg
      viewBox="0 0 12 12"
      role="img"
      aria-label={copy.colorBlind.shapes[SHAPE_COLOR[shape]]}
      data-label-shape={shape}
      className={cn("size-2.5 shrink-0", className)}
    >
      {PATHS[shape]}
    </svg>
  );
}

/** The switch in the label picker's footer: turns the account preference on or off. */
export function ColourBlindToggle({
  enabled,
  onToggle,
  disabled,
}: {
  enabled: boolean;
  onToggle: (next: boolean) => void;
  disabled?: boolean;
}) {
  const copy = useCapabilitiesCopy();
  return (
    <div className="flex flex-col gap-0.5 border-t border-border pt-2">
      <button
        type="button"
        disabled={disabled}
        aria-pressed={enabled}
        onClick={() => onToggle(!enabled)}
        className="rounded-md px-1 py-1 text-left text-nav font-medium text-foreground hover:bg-state-hover focus-visible:outline-2 focus-visible:outline-ring"
      >
        {enabled ? copy.colorBlind.off : copy.colorBlind.on}
      </button>
      <p className="px-1 text-micro text-meta-foreground">
        {copy.colorBlind.note}
      </p>
    </div>
  );
}

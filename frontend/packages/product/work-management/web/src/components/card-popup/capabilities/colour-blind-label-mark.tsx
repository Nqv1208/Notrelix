import { createContext, useContext, useMemo, type ReactNode } from "react";
import { cn } from "@notrelix/ui-web";
import { itemLabelTone, type ItemLabelTone } from "../../shared/item-display";
import { PanelBtn } from "../popup-ui";
import { useCapabilitiesCopy } from "./use-capabilities-copy";

/** The copy key of the spoken name of each tone's shape. */
const TONE_COLOR = {
  data: "blue",
  security: "rose",
  training: "teal",
  report: "amber",
  doc: "gray",
  test: "violet",
  orange: "orange",
  green: "green",
  indigo: "indigo",
  red: "red",
} as const satisfies Record<ItemLabelTone, string>;

/** One shape per tone, drawn on a 24 grid: filled, or stroked only for the open ones (plus, dash, cross, ring). */
const PATHS: Readonly<Record<ItemLabelTone, ReactNode>> = {
  data: <circle cx="12" cy="12" r="7" />,
  security: <path d="M12 4v16M4 12h16" style={{ fill: "none" }} />,
  training: <path d="M12 4 21 20H3z" />,
  report: <rect x="5" y="5" width="14" height="14" />,
  doc: <path d="M5 12h14" style={{ fill: "none" }} />,
  test: <path d="M12 3 21 12 12 21 3 12z" />,
  orange: <path d="M12 3 20 7.5v9L12 21 4 16.5v-9z" />,
  green: (
    <path d="m12 3 2.6 5.8 6.4.6-4.8 4.3 1.4 6.3L12 16.7 6.4 20l1.4-6.3L3 9.4l6.4-.6Z" />
  ),
  indigo: <path d="m6 6 12 12M18 6 6 18" style={{ fill: "none" }} />,
  red: <circle cx="12" cy="12" r="6.5" style={{ fill: "none" }} />,
};

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
 * The small shape beside a label's name when colour-blind mode is on; nothing otherwise. The shape follows
 * the tone the chip is painted with, so one colour always has one shape. Pass `force` to draw it regardless.
 */
export function ColourBlindLabelMark({
  label,
  force = false,
  className,
}: {
  label: { readonly name: string; readonly color?: string | null };
  force?: boolean;
  className?: string;
}) {
  const on = useColourBlindLabels();
  // The copy is read only when a shape is drawn, so a card without the mode needs no locale provider.
  if (!on && !force) return null;
  return <LabelShape label={label} className={className} />;
}

function LabelShape({
  label,
  className,
}: {
  label: { readonly name: string; readonly color?: string | null };
  className?: string;
}) {
  const copy = useCapabilitiesCopy();
  const tone = itemLabelTone(label);
  return (
    <svg
      viewBox="0 0 24 24"
      role="img"
      aria-label={copy.colorBlind.shapes[TONE_COLOR[tone]]}
      data-label-shape={tone}
      className={cn(
        "size-3 shrink-0 fill-current stroke-current stroke-[2.4] [stroke-linecap:round] [stroke-linejoin:round]",
        className,
      )}
    >
      {PATHS[tone]}
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
    <>
      <PanelBtn
        className="w-full"
        disabled={disabled}
        aria-pressed={enabled}
        onClick={() => onToggle(!enabled)}
      >
        {enabled ? copy.colorBlind.off : copy.colorBlind.on}
      </PanelBtn>
      {enabled ? (
        <p className="mx-0.5 mt-2 text-[12.5px] text-meta-foreground">
          {copy.colorBlind.note}
        </p>
      ) : null}
    </>
  );
}

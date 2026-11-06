import * as React from "react";
import { createPortal } from "react-dom";
import { useIsTablet } from "../../hooks/use-tablet";

/** The slots the page toolbars expose in the tablet band (Final-T-*). */
export const TABLET_SLOT = {
  /** The header's "+" (add an item), which the open view contributes. */
  headerAdd: "tablet-header-add",
  /** Start of a view's toolbar row (access chip). */
  toolbarLead: "tablet-toolbar-lead",
  /** End of a view's toolbar row (avatar stack). */
  toolbarTrail: "tablet-toolbar-trail",
} as const;

export type TabletSlotId = (typeof TABLET_SLOT)[keyof typeof TABLET_SLOT];

/** A mount point for {@link TabletPortal}; the element carries the slot id. */
export function TabletSlot({
  slot,
  className,
}: {
  slot: TabletSlotId;
  className?: string;
}) {
  return <div id={slot} className={className} />;
}

function useSlotElement(slot: TabletSlotId, enabled: boolean) {
  const [element, setElement] = React.useState<HTMLElement | null>(null);
  React.useLayoutEffect(() => {
    if (!enabled) {
      setElement(null);
      return;
    }
    const find = () => document.getElementById(slot);
    setElement(find());
    // The slot may mount after its content (data loaded later), or be replaced when its toolbar
    // remounts (a loading toolbar giving way to the loaded one): follow the current element.
    const observer = new MutationObserver(() => {
      const found = find();
      setElement((current) => (current === found ? current : found));
    });
    observer.observe(document.body, { childList: true, subtree: true });
    return () => observer.disconnect();
  }, [slot, enabled]);
  return element;
}

/**
 * Renders its children into a tablet slot (768-1279 px only), so a page
 * contributes controls to a sibling toolbar without that toolbar knowing
 * the page. Nothing renders outside the tablet band or
 * while the slot is absent.
 */
export function TabletPortal({
  slot,
  children,
}: {
  slot: TabletSlotId;
  children: React.ReactNode;
}) {
  const isTablet = useIsTablet();
  const element = useSlotElement(slot, isTablet);
  return element ? createPortal(children, element) : null;
}

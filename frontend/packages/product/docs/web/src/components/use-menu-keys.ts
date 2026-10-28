import {
  useEffect,
  useRef,
  type KeyboardEvent as ReactKeyboardEvent,
  type RefObject,
} from "react";

/** The focusable entries directly inside a menu panel, in order. */
export function menuItemsOf(panel: HTMLElement | null): HTMLElement[] {
  if (!panel) return [];
  return Array.from(
    panel.querySelectorAll<HTMLElement>("[data-menu-item]"),
  ).filter((node) => node.closest("[data-menu-panel]") === panel);
}

export interface MenuKeysOptions {
  /** Focus the first entry when the panel appears. */
  readonly autoFocus?: boolean;
  /** Escape or Tab: close the menu and give focus back. */
  readonly onClose?: (reason: "escape" | "tab") => void;
  /** ArrowLeft / ArrowRight that the panel itself does not use. */
  readonly onSide?: (direction: -1 | 1) => void;
  /** ArrowLeft inside a submenu: go back to its trigger. */
  readonly onBack?: () => void;
  /** Items per row when the panel is a grid (arrows then also move by rows). */
  readonly columns?: number;
}

/**
 * Menu keyboard handling shared by the File menu and the menu bar's menus:
 * arrows, Home and End move between entries, Escape closes. The panel's
 * entries carry `data-menu-item`.
 */
export function useMenuKeys(
  panel: RefObject<HTMLElement | null>,
  { autoFocus = true, onClose, onSide, onBack, columns }: MenuKeysOptions,
) {
  const first = useRef(autoFocus);
  useEffect(() => {
    if (!first.current) return;
    first.current = false;
    const items = menuItemsOf(panel.current);
    const target =
      items.find((node) => node.getAttribute("aria-disabled") !== "true") ??
      items[0];
    target?.focus();
  }, [panel]);

  return (event: ReactKeyboardEvent<HTMLElement>) => {
    const items = menuItemsOf(panel.current);
    const at = items.indexOf(document.activeElement as HTMLElement);
    const move = (to: number) => {
      event.preventDefault();
      event.stopPropagation();
      if (items.length === 0) return;
      items[(to + items.length) % items.length]?.focus();
    };
    const step = columns ?? 1;
    switch (event.key) {
      case "ArrowDown":
        return move(
          at < 0 ? 0 : columns ? Math.min(at + step, items.length - 1) : at + 1,
        );
      case "ArrowUp":
        return move(
          at < 0 ? items.length - 1 : columns ? Math.max(at - step, 0) : at - 1,
        );
      case "ArrowRight":
        if (columns) return move(at + 1);
        if (
          onSide &&
          !(event.target as HTMLElement).hasAttribute("data-menu-sub")
        ) {
          event.preventDefault();
          event.stopPropagation();
          onSide(1);
        }
        return;
      case "ArrowLeft":
        if (columns) return move(at - 1);
        if (onBack) {
          event.preventDefault();
          event.stopPropagation();
          onBack();
        } else if (onSide) {
          event.preventDefault();
          event.stopPropagation();
          onSide(-1);
        }
        return;
      case "Home":
        return move(0);
      case "End":
        return move(items.length - 1);
      case "Escape":
        event.preventDefault();
        event.stopPropagation();
        onClose?.("escape");
        return;
      case "Tab":
        onClose?.("tab");
        return;
      default:
    }
  };
}

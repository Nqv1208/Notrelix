import * as React from "react";

/**
 * Radix returns focus on close only to a Trigger it owns. A dialog opened from
 * state (a menu item that runs, a button that sets a flag) has none, so focus
 * fell to the page. The element that held focus before the dialog took it is
 * the one to go back to; it is tracked from `focusin` outside any dialog,
 * because Radix does not announce the opening when a field inside the dialog
 * focuses itself.
 */
let lastOutside: HTMLElement | null = null;
let listening = false;

function listen() {
  if (listening || typeof document === "undefined") return;
  listening = true;
  document.addEventListener(
    "focusin",
    (event) => {
      const target = event.target;
      if (!(target instanceof HTMLElement) || target === document.body) return;
      if (target.closest('[role="dialog"],[role="alertdialog"]')) return;
      lastOutside = target;
    },
    true,
  );
}

// Start tracking as soon as the module loads: a dialog that is only rendered
// once open would otherwise miss the focus that opened it.
listen();

export function useRestoreFocus(handlers: {
  readonly onOpenAutoFocus?: (event: Event) => void;
  readonly onCloseAutoFocus?: (event: Event) => void;
}) {
  React.useEffect(listen, []);
  const { onOpenAutoFocus, onCloseAutoFocus } = handlers;
  return {
    onOpenAutoFocus,
    onCloseAutoFocus: (event: Event) => {
      onCloseAutoFocus?.(event);
      if (event.defaultPrevented) return;
      const target = lastOutside;
      if (target?.isConnected) {
        event.preventDefault();
        target.focus();
      }
    },
  };
}

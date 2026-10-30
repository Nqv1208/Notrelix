import { useCardPopup } from "../card-popup-context";

/** Stub: filled in by the states part of the popup. */
export function PopupSkeleton() {
  return null;
}

export function PopupLoadError(_props: {
  onRetry: () => void;
  onClose: () => void;
}) {
  void useCardPopup;
  return null;
}

export function PopupGone(_props: { onClose: () => void }) {
  return null;
}

/** The read-only and conflict notices at the top of the left column. */
export function PopupBanners(_props: {
  conflict?: boolean;
  onReload?: () => void;
}) {
  return null;
}

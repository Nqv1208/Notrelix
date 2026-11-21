/** The browser's note that a resize observer's callback changed layout again within one frame: not an error. */
const BENIGN_MESSAGE =
  "ResizeObserver loop completed with undelivered notifications";

/**
 * The list measures its rows in the frame they are painted, which makes the browser raise that one message each time a
 * row's height settles. Swallows exactly it (everything else reaches the other listeners); returns the remover.
 */
export function guardResizeObserverLoop(target: Window = window): () => void {
  const onError = (event: ErrorEvent) => {
    if (!event.message.includes(BENIGN_MESSAGE)) return;
    event.preventDefault();
    event.stopImmediatePropagation();
  };
  target.addEventListener("error", onError, true);
  return () => target.removeEventListener("error", onError, true);
}

import { useEffect, useRef } from "react";

const TURNSTILE_SRC =
  "https://challenges.cloudflare.com/turnstile/v0/api.js?render=explicit";

interface TurnstileApi {
  render(
    container: HTMLElement,
    options: {
      sitekey: string;
      theme?: "auto" | "light" | "dark";
      callback: (token: string) => void;
      "expired-callback": () => void;
      "error-callback": () => void;
    },
  ): string;
  reset(widgetId: string): void;
  remove(widgetId: string): void;
}

declare global {
  interface Window {
    turnstile?: TurnstileApi;
  }
}

export interface CaptchaWidgetProps {
  readonly siteKey: string;
  /** A fresh single-use token: the person solved the challenge. */
  readonly onToken: (token: string) => void;
  /** The token ran out before it was used. */
  readonly onExpire: () => void;
  /** The challenge could not load or run. */
  readonly onError: () => void;
  /** Changes after each send: the token was spent, so the challenge starts again. */
  readonly resetKey?: number;
}

let loading: Promise<TurnstileApi> | null = null;

/** Loads the Turnstile script once for the page; a failed load can be tried again. */
function loadTurnstile(): Promise<TurnstileApi> {
  if (window.turnstile) return Promise.resolve(window.turnstile);
  loading ??= new Promise<TurnstileApi>((resolve, reject) => {
    const script = document.createElement("script");
    script.src = TURNSTILE_SRC;
    script.async = true;
    script.onload = () =>
      window.turnstile
        ? resolve(window.turnstile)
        : reject(new Error("Turnstile did not start."));
    script.onerror = () => {
      script.remove();
      reject(new Error("Turnstile did not load."));
    };
    document.head.appendChild(script);
  }).catch((error: unknown) => {
    loading = null;
    throw error;
  });
  return loading;
}

/** Cloudflare Turnstile, rendered explicitly into its own box. The page decides what a token is worth. */
export function CaptchaWidget({
  siteKey,
  onToken,
  onExpire,
  onError,
  resetKey = 0,
}: CaptchaWidgetProps) {
  const box = useRef<HTMLDivElement>(null);
  const widget = useRef<string | null>(null);
  // The callbacks change every render of the page; the widget keeps the latest.
  const latest = useRef({ onToken, onExpire, onError });
  latest.current = { onToken, onExpire, onError };

  useEffect(() => {
    let gone = false;
    let api: TurnstileApi | null = null;
    loadTurnstile().then(
      (turnstile) => {
        if (gone || !box.current) return;
        api = turnstile;
        widget.current = turnstile.render(box.current, {
          sitekey: siteKey,
          theme: "auto",
          callback: (token) => latest.current.onToken(token),
          "expired-callback": () => latest.current.onExpire(),
          "error-callback": () => latest.current.onError(),
        });
      },
      () => {
        if (!gone) latest.current.onError();
      },
    );
    return () => {
      gone = true;
      if (api && widget.current) api.remove(widget.current);
      widget.current = null;
    };
  }, [siteKey]);

  const first = useRef(true);
  useEffect(() => {
    if (first.current) {
      first.current = false;
      return;
    }
    if (widget.current) window.turnstile?.reset(widget.current);
  }, [resetKey]);

  return <div ref={box} />;
}

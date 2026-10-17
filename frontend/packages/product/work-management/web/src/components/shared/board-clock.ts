import { useEffect, useState } from "react";

/** The browser's IANA time zone, when it reports one. */
export function runtimeZone(): string | undefined {
  try {
    return Intl.DateTimeFormat().resolvedOptions().timeZone;
  } catch {
    return undefined;
  }
}

/** "GMT+7" of the zone at `now`. */
export function zoneOffset(now: Date, timeZone?: string): string {
  try {
    const part = new Intl.DateTimeFormat("en", {
      timeZone,
      timeZoneName: "shortOffset",
    })
      .formatToParts(now)
      .find((candidate) => candidate.type === "timeZoneName");
    return part?.value ?? "GMT";
  } catch {
    return "GMT";
  }
}

/** The zones the design labels "Giờ Hà Nội". */
export const HANOI_ZONES = ["Asia/Ho_Chi_Minh", "Asia/Saigon"];

/**
 * A clock that ticks once a minute, so a "now" marker follows the day. An
 * injected clock (stories, tests) is used as it is and never ticks.
 */
export function useNow(injected?: Date): Date {
  const [now, setNow] = useState(() => injected ?? new Date());
  useEffect(() => {
    if (injected) return undefined;
    const timer = window.setInterval(() => setNow(new Date()), 60_000);
    return () => window.clearInterval(timer);
  }, [injected]);
  return injected ?? now;
}

import type { ReactNode } from "react";
import { fillCopy } from "./board-settings-format";
import type { AutomationEditorCopy } from "./board-automation.copy";
import { dayMonthOf, isSameDay, weekdayOf } from "./automation-format";

const WEEKDAY_KEYS = ["sun", "mon", "tue", "wed", "thu", "fri", "sat"] as const;

export function dayLabel(
  copy: AutomationEditorCopy,
  iso: string,
  nowIso: string,
  timeZone: string,
): string {
  if (isSameDay(iso, nowIso, timeZone)) return copy.log.today;
  const [day, month] = dayMonthOf(iso, timeZone).split("/");
  return fillCopy(copy.log.dayLabel, {
    weekday: copy.log.weekdays[WEEKDAY_KEYS[weekdayOf(iso, timeZone)] ?? "sun"],
    day: day ?? "",
    month: month ?? "",
  });
}

/** An item key in the accent mono ink (the item page has no key address yet). */
export function ItemKey({ children }: { children: ReactNode }) {
  return (
    <span className="font-mono text-item-note leading-[inherit] font-medium whitespace-nowrap text-brand-subtle-foreground">
      {children}
    </span>
  );
}

/** A text where "{key}" stands for an item key. */
export function WithKey({
  text,
  itemKey,
}: {
  text: string;
  itemKey: string | null;
}) {
  const parts = text.split("{key}");
  if (parts.length === 1 || !itemKey) return <>{text}</>;
  return (
    <>
      {parts[0]}
      <ItemKey>{itemKey}</ItemKey>
      {parts[1]}
    </>
  );
}

import { Check } from "lucide-react";
import { cn } from "@notrelix/ui-web";
import type { OnboardingCopy } from "./onboarding.copy";
import { fillOnboarding } from "./fill-copy";

/**
 * "Bắt đầu với Sao Mai 0/3": the getting-started list a workspace made
 * through onboarding keeps on Home until it is done or hidden. Pure: the
 * items, what a tick does and the hide are injected.
 */
export interface GettingStartedItem {
  readonly key: string;
  readonly title: string;
  readonly note: string;
  readonly done: boolean;
  /** Where the task is done; the title is a link when present. */
  readonly href?: string | null;
}

export interface GettingStartedCardProps {
  readonly copy: OnboardingCopy;
  readonly name: string;
  readonly items: readonly GettingStartedItem[];
  /** Absent: the tasks complete by themselves and the check is not clickable. */
  readonly onToggle?: (key: string) => void;
  readonly onHide: () => void;
  readonly onOpen?: (item: GettingStartedItem) => void;
}

export function GettingStartedCard({
  copy,
  name,
  items,
  onToggle,
  onHide,
  onOpen,
}: GettingStartedCardProps) {
  const t = copy.started;
  const done = items.filter((item) => item.done).length;
  const percent =
    items.length === 0 ? 0 : Math.round((done / items.length) * 100);
  return (
    <section
      aria-labelledby="home-getting-started"
      data-getting-started
      className="flex flex-col gap-3 rounded-[12px] border border-line bg-popover px-4 py-3.5"
    >
      <div className="flex items-baseline justify-between gap-3">
        <h2
          id="home-getting-started"
          className="m-0 text-item-prose font-semibold"
        >
          {fillOnboarding(t.title, { name })}
        </h2>
        <span className="text-item-ui text-meta-foreground">
          {fillOnboarding(t.count, { done, total: items.length })}
        </span>
      </div>
      <span
        role="progressbar"
        aria-valuemin={0}
        aria-valuemax={100}
        aria-valuenow={percent}
        aria-label={fillOnboarding(t.title, { name })}
        className="h-1 overflow-hidden rounded-full bg-state-hover-strong"
      >
        <span
          className="block h-full rounded-full bg-primary"
          style={{ width: `${percent}%` }}
        />
      </span>
      <ul className="m-0 flex list-none flex-col gap-1 p-0">
        {items.map((item) => (
          <li key={item.key} className="flex items-center gap-3 py-1">
            <button
              type="button"
              role="checkbox"
              aria-checked={item.done}
              aria-label={fillOnboarding(t.markDone, { title: item.title })}
              disabled={item.done || !onToggle}
              onClick={() => onToggle?.(item.key)}
              className={cn(
                "grid size-5 shrink-0 cursor-pointer place-items-center rounded-full border p-0 outline-none focus-visible:ring-2 focus-visible:ring-focus-ring",
                item.done
                  ? "border-primary bg-primary text-primary-foreground"
                  : "border-line-strong bg-transparent",
              )}
            >
              {item.done ? <Check className="size-3" strokeWidth={3} /> : null}
            </button>
            <span className="min-w-0 flex-1">
              {item.href ? (
                <a
                  href={item.href}
                  onClick={(event) => {
                    if (
                      !onOpen ||
                      event.defaultPrevented ||
                      event.button !== 0 ||
                      event.metaKey ||
                      event.ctrlKey
                    )
                      return;
                    event.preventDefault();
                    onOpen(item);
                  }}
                  className={cn(
                    "text-item-prose font-medium text-foreground no-underline hover:underline",
                    item.done && "text-meta-foreground line-through",
                  )}
                >
                  {item.title}
                </a>
              ) : (
                <span
                  className={cn(
                    "text-item-prose font-medium",
                    item.done && "text-meta-foreground line-through",
                  )}
                >
                  {item.title}
                </span>
              )}
              <small className="block text-item-note text-meta-foreground">
                {item.note}
              </small>
            </span>
          </li>
        ))}
      </ul>
      <div className="flex items-center justify-between gap-3 text-item-note text-meta-foreground">
        <span>{t.note}</span>
        <button
          type="button"
          onClick={onHide}
          className="cursor-pointer border-0 bg-transparent p-0 text-item-note font-medium text-brand-subtle-foreground hover:underline"
        >
          {t.hide}
        </button>
      </div>
    </section>
  );
}

import {
  useEffect,
  useId,
  useRef,
  useState,
  type KeyboardEvent,
  type ReactNode,
} from "react";
import { ChevronRight, X, type LucideIcon } from "lucide-react";
import { Button, cn } from "@notrelix/ui-web";

export type ChatInfoSectionKey = "members" | "pins" | "following" | "mentions";

export interface ChatInfoAction {
  readonly key: string;
  readonly label: string;
  readonly icon: LucideIcon;
  readonly onClick: () => void;
}

export interface ChatInfoSection {
  readonly key: ChatInfoSectionKey;
  readonly label: string;
  /** The number beside the label; null while it is still loading. */
  readonly count: number | null;
  readonly icon: LucideIcon;
  readonly content: ReactNode;
}

export interface ChatInfoPanelProps {
  /** The avatar or tile drawn 72 px wide at the top. */
  readonly avatar: ReactNode;
  readonly title: string;
  readonly subtitle: string;
  /** The label of the scrolling region, such as "Group info". */
  readonly regionLabel: string;
  readonly closeLabel: string;
  readonly actions: readonly ChatInfoAction[];
  readonly sections: readonly ChatInfoSection[];
  /** Opens this section and scrolls it into view; a new `seq` repeats the request for the same key. */
  readonly focus?: { readonly key: ChatInfoSectionKey; readonly seq: number };
  /** Buttons under the sections (add people, hide, leave). */
  readonly footer?: ReactNode;
  readonly onClose: () => void;
}

/**
 * The one side panel of a conversation or channel (Chat-Info boards): a close cross alone in the top-right corner, a
 * 72 px avatar, the name and a status line, a row of round quick actions, then collapsible sections with a count. It is
 * one scrolling column with no tabs. Escape closes it unless a menu inside it took the key.
 */
export function ChatInfoPanel({
  avatar,
  title,
  subtitle,
  regionLabel,
  closeLabel,
  actions,
  sections,
  focus,
  footer,
  onClose,
}: ChatInfoPanelProps) {
  const baseId = useId();
  const [open, setOpen] = useState<ReadonlySet<ChatInfoSectionKey>>(
    () => new Set([focus?.key ?? sections[0]?.key].filter(Boolean) as never),
  );
  const refs = useRef(new Map<ChatInfoSectionKey, HTMLElement>());

  useEffect(() => {
    if (!focus) return;
    setOpen((current) => new Set(current).add(focus.key));
    const reduced =
      typeof window !== "undefined" &&
      window.matchMedia?.("(prefers-reduced-motion: reduce)").matches;
    refs.current.get(focus.key)?.scrollIntoView?.({
      block: "start",
      behavior: reduced ? "auto" : "smooth",
    });
  }, [focus?.key, focus?.seq]);

  const toggle = (key: ChatInfoSectionKey) =>
    setOpen((current) => {
      const next = new Set(current);
      if (!next.delete(key)) next.add(key);
      return next;
    });

  const onKeyDown = (event: KeyboardEvent<HTMLElement>) => {
    // Portaled menus bubble through the React tree; only a key pressed inside the panel itself closes it.
    if (
      event.key === "Escape" &&
      !event.defaultPrevented &&
      event.currentTarget.contains(event.target as Node)
    ) {
      onClose();
    }
  };

  return (
    <div
      className="relative flex min-h-0 flex-1 flex-col"
      onKeyDown={onKeyDown}
      data-testid="chat-info-panel"
    >
      <Button
        type="button"
        size="icon-sm"
        variant="ghost"
        className="absolute end-3 top-3 z-10 text-muted-foreground max-md:end-1.5 max-md:top-1.5 max-md:size-11"
        aria-label={closeLabel}
        title={closeLabel}
        onClick={onClose}
      >
        <X aria-hidden="true" />
      </Button>
      <div
        role="region"
        aria-label={regionLabel}
        className="flex min-h-0 flex-1 flex-col overflow-y-auto"
      >
        <div className="flex flex-col items-center px-4 pt-8 text-center">
          {avatar}
          <h2
            className="mt-3 max-w-full truncate text-lg leading-[26px] font-[650] text-foreground"
            data-testid="chat-info-title"
          >
            {title}
          </h2>
          <p className="mt-0.5 max-w-full truncate text-compact leading-[19px] text-muted-foreground">
            {subtitle}
          </p>
        </div>
        <div className="flex justify-center gap-2 px-4 pt-4 pb-5">
          {actions.map(({ key, label, icon: Icon, onClick }) => (
            <button
              key={key}
              type="button"
              onClick={onClick}
              className="group flex w-[76px] flex-col items-center gap-1.5 rounded-md text-xs leading-4 font-semibold text-muted-foreground focus-visible:ring-2 focus-visible:ring-focus-ring focus-visible:outline-none"
            >
              <span className="flex size-11 items-center justify-center rounded-full bg-muted text-foreground group-hover:bg-muted-foreground/20">
                <Icon className="size-5" aria-hidden="true" />
              </span>
              {label}
            </button>
          ))}
        </div>
        {sections.map(({ key, label, count, icon: Icon, content }) => {
          const expanded = open.has(key);
          const headingId = `${baseId}-${key}-h`;
          const bodyId = `${baseId}-${key}`;
          return (
            <section
              key={key}
              ref={(el) => {
                if (el) refs.current.set(key, el);
                else refs.current.delete(key);
              }}
              className="border-t border-border"
              data-section={key}
            >
              <h3>
                <button
                  type="button"
                  id={headingId}
                  aria-expanded={expanded}
                  aria-controls={bodyId}
                  onClick={() => toggle(key)}
                  className="flex h-12 w-full items-center gap-2 px-4 text-start text-sm font-[650] text-foreground focus-visible:ring-2 focus-visible:ring-focus-ring focus-visible:outline-none focus-visible:ring-inset"
                >
                  <ChevronRight
                    className={cn(
                      "size-4 shrink-0 text-muted-foreground transition-transform duration-150 motion-reduce:transition-none",
                      expanded && "rotate-90",
                    )}
                    aria-hidden="true"
                  />
                  <Icon
                    className="size-4 shrink-0 text-muted-foreground"
                    aria-hidden="true"
                  />
                  <span className="min-w-0 flex-1 truncate">{label}</span>
                  {count !== null ? (
                    <span className="shrink-0 text-compact font-semibold text-muted-foreground tabular-nums">
                      {count}
                    </span>
                  ) : null}
                </button>
              </h3>
              <div
                id={bodyId}
                role="region"
                aria-labelledby={headingId}
                hidden={!expanded}
                className="pb-3"
              >
                {expanded ? content : null}
              </div>
            </section>
          );
        })}
        {footer ? (
          <div className="flex flex-col gap-2 border-t border-border px-4 py-3">
            {footer}
          </div>
        ) : null}
      </div>
    </div>
  );
}

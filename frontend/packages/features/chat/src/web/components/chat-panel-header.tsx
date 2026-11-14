import type { ReactNode } from "react";
import { ChevronLeft, X } from "lucide-react";
import { Button } from "@notrelix/ui-web";

export interface ChatPanelHeaderProps {
  readonly title: string;
  /** The line under the title on a phone, such as "# erp-rollout · 3 tin". */
  readonly subtitle?: string;
  readonly closeLabel: string;
  readonly onClose: () => void;
  /** Chips and buttons between the title and the close control. */
  readonly children?: ReactNode;
}

/**
 * The one header of a Chat side panel (Chat-Pins, Chat-Thread-M, Chat-Search-M boards): on a phone the panel is a page, so
 * a 56 high bar with a back arrow, a 17/650 title and a 12 px line under it; from 768 px a 88 high panel header with a 16 px
 * title and a close cross on the right.
 */
export function ChatPanelHeader({
  title,
  subtitle,
  closeLabel,
  onClose,
  children,
}: ChatPanelHeaderProps) {
  return (
    <header
      className="flex h-14 shrink-0 items-center gap-2 border-b border-border px-3 md:h-[88px] md:gap-3 md:px-5"
      data-testid="chat-panel-header"
    >
      <div className="min-w-0 flex-1">
        <h2 className="truncate text-[17px] leading-[1.45] font-[650] md:text-base md:font-bold">
          {title}
        </h2>
        {subtitle ? (
          <p className="truncate text-xs leading-[1.45] text-muted-foreground md:hidden">
            {subtitle}
          </p>
        ) : null}
      </div>
      {children}
      <Button
        type="button"
        size="icon-sm"
        variant="ghost"
        className="order-first text-muted-foreground md:order-last"
        aria-label={closeLabel}
        title={closeLabel}
        onClick={onClose}
      >
        <ChevronLeft className="md:hidden" aria-hidden="true" />
        <X className="max-md:hidden" aria-hidden="true" />
      </Button>
    </header>
  );
}

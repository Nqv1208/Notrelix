import type { ReactNode } from "react";
import { X, type LucideIcon } from "lucide-react";
import {
  Button,
  Sheet,
  SheetContent,
  SheetDescription,
  SheetTitle,
  cn,
} from "@notrelix/ui-web";

/** One row of a bottom sheet (the Chat-Pins-M and Chat-Thread-M boards: 48 high, 14 gap, destructive in red). */
export interface ChatSheetAction {
  readonly key: string;
  readonly label: string;
  readonly icon: LucideIcon;
  readonly onSelect: () => void;
  readonly destructive?: boolean;
}

export interface ChatBottomSheetProps {
  readonly open: boolean;
  readonly title: string;
  readonly closeLabel: string;
  readonly onOpenChange: (open: boolean) => void;
  readonly testId?: string;
  readonly children: ReactNode;
}

/** The phone sheet of the Chat boards: a grab handle, a 16/700 title with a close control, then the content. */
export function ChatBottomSheet({
  open,
  title,
  closeLabel,
  onOpenChange,
  testId,
  children,
}: ChatBottomSheetProps) {
  return (
    <Sheet open={open} onOpenChange={onOpenChange}>
      <SheetContent
        side="bottom"
        showCloseButton={false}
        className="gap-0 rounded-t-[20px] px-2 pt-2 pb-5"
        data-testid={testId}
      >
        <div
          className="mx-auto mt-1 mb-2.5 h-1 w-10 rounded-full bg-border"
          aria-hidden="true"
        />
        <div className="flex items-center ps-3 pe-3 pb-2">
          <SheetTitle className="flex-1 text-base font-bold">
            {title}
          </SheetTitle>
          <SheetDescription className="sr-only">{title}</SheetDescription>
          <Button
            type="button"
            size="icon-sm"
            variant="ghost"
            className="text-muted-foreground"
            aria-label={closeLabel}
            onClick={() => onOpenChange(false)}
          >
            <X aria-hidden="true" />
          </Button>
        </div>
        {children}
      </SheetContent>
    </Sheet>
  );
}

/** A phone sheet that lists actions; choosing one closes the sheet first. */
export function ChatActionSheet({
  open,
  title,
  closeLabel,
  actions,
  onOpenChange,
  testId,
}: Omit<ChatBottomSheetProps, "children"> & {
  readonly actions: readonly ChatSheetAction[];
}) {
  return (
    <ChatBottomSheet
      open={open}
      title={title}
      closeLabel={closeLabel}
      onOpenChange={onOpenChange}
      testId={testId}
    >
      <ul className="flex flex-col">
        {actions.map((action) => {
          const Icon = action.icon;
          return (
            <li key={action.key}>
              <button
                type="button"
                onClick={() => {
                  onOpenChange(false);
                  action.onSelect();
                }}
                className={cn(
                  "flex min-h-12 w-full items-center gap-3.5 rounded-[10px] px-3.5 text-start text-sm outline-none hover:bg-muted focus-visible:ring-2 focus-visible:ring-focus-ring focus-visible:ring-inset",
                  action.destructive ? "text-destructive" : "text-foreground",
                )}
              >
                <Icon className="size-4 shrink-0" aria-hidden="true" />
                {action.label}
              </button>
            </li>
          );
        })}
      </ul>
    </ChatBottomSheet>
  );
}

import { useEffect, useRef, type ReactNode } from "react";
import { ArrowLeft, Search, X } from "lucide-react";
import { cn } from "@notrelix/ui-web";
import { useCardPopup } from "../card-popup-context";
import { PanelHead } from "../popup-ui";

/** The panel's title row with an optional back arrow and the close button. */
export function PickerHead({
  title,
  onBack,
  onClose,
}: {
  title: ReactNode;
  onBack?: () => void;
  onClose: () => void;
}) {
  const { copy } = useCardPopup();
  return (
    <PanelHead
      title={title}
      onBack={onBack}
      onClose={onClose}
      backLabel={copy.pickers.back}
      closeLabel={copy.pickers.close}
      icons={{
        back: <ArrowLeft aria-hidden="true" className="size-4" />,
        close: <X aria-hidden="true" className="size-4" />,
      }}
    />
  );
}

/** `.in.focus`: the search box of a panel; it takes focus when the panel opens. */
export function SearchField({
  value,
  onChange,
  label,
  placeholder,
  autoFocus = true,
}: {
  value: string;
  onChange: (value: string) => void;
  label: string;
  placeholder?: string;
  autoFocus?: boolean;
}) {
  const ref = useRef<HTMLInputElement>(null);
  useEffect(() => {
    if (autoFocus) ref.current?.focus();
  }, [autoFocus]);
  return (
    <label className="box-border flex h-9 w-full items-center gap-2 rounded-[8px] border border-line-strong bg-popover px-3 text-[14px] focus-within:border-primary focus-within:ring-[3px] focus-within:ring-brand-subtle">
      <span className="sr-only">{label}</span>
      <Search
        aria-hidden="true"
        className="size-4 shrink-0 text-meta-foreground"
      />
      <input
        ref={ref}
        type="text"
        data-picker-search=""
        value={value}
        placeholder={placeholder}
        onChange={(event) => onChange(event.target.value)}
        className="min-w-0 flex-1 border-0 bg-transparent p-0 text-[14px] text-foreground outline-none placeholder:text-meta-foreground"
      />
    </label>
  );
}

/** `.cap`: the small heading above a list. */
export function Caption({
  children,
  className,
  id,
}: {
  children: ReactNode;
  className?: string;
  id?: string;
}) {
  return (
    <div
      id={id}
      className={cn(
        "mt-3 mb-1.5 text-[12px] leading-[1.45] font-semibold text-muted-foreground",
        className,
      )}
    >
      {children}
    </div>
  );
}

/** `.hint`: a quiet line under a form; `error` tints it. */
export function Hint({
  children,
  error,
  className,
}: {
  children: ReactNode;
  error?: boolean;
  className?: string;
}) {
  return (
    <p
      role={error ? "alert" : undefined}
      className={cn(
        "m-0 text-[12.5px] leading-[1.45]",
        error
          ? "flex gap-1.5 font-medium text-danger-text"
          : "text-meta-foreground",
        className,
      )}
    >
      {children}
    </p>
  );
}

/** `.lb2`: the label of a text field. */
export function FieldLabel({
  htmlFor,
  children,
  className,
}: {
  htmlFor?: string;
  children: ReactNode;
  className?: string;
}) {
  return (
    <label
      htmlFor={htmlFor}
      className={cn(
        "mb-1.5 block text-[12px] font-semibold text-muted-foreground",
        className,
      )}
    >
      {children}
    </label>
  );
}

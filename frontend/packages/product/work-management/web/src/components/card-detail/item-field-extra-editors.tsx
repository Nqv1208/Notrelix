import { useEffect, useState, type ReactNode } from "react";
import { AlertCircle } from "lucide-react";
import { Popover, PopoverContent, PopoverTrigger } from "@notrelix/ui-web";
import {
  fieldTypeRegistry,
  type ComputedValue,
  type FieldDefinition,
} from "@notrelix/work-management-core";
import type { ItemSheetCopy } from "./item-sheet.copy";

/**
 * Inline editors of the long text and link properties, and the text of a
 * calculated (Formula / Rollup) value. Like the text popover they save on
 * Enter and on leaving the box, and Escape puts the old value back; a draft
 * that does not parse (a link that is not http or https) stays open with its
 * reason and is never sent.
 */

const INPUT_CLASS =
  "w-full rounded-[7px] border border-line-strong bg-background px-2 text-item-ui text-foreground aria-invalid:border-danger";

function useDraftPopover(
  value: string,
  autoOpen: boolean,
  readOnly: boolean,
  commitDraft: (draft: string) => boolean,
) {
  const [open, setOpen] = useState(false);
  const [draft, setDraft] = useState("");
  useEffect(() => {
    if (autoOpen) setOpen(true);
  }, [autoOpen]);
  const commit = () => {
    if (draft === value) return setOpen(false);
    if (commitDraft(draft)) setOpen(false);
  };
  const onOpenChange = (next: boolean) => {
    if (next) setDraft(value);
    setOpen(next);
  };
  return { open, setOpen, draft, setDraft, commit, onOpenChange, readOnly };
}

/** A multi-line text typed into a popover: Enter saves, Shift+Enter breaks the line. */
export function ItemLongTextPopover({
  value,
  label,
  readOnly,
  copy,
  onChange,
  autoOpen = false,
  children,
}: {
  value: string;
  label: string;
  readOnly: boolean;
  copy: Pick<ItemSheetCopy["properties"], "saveLongText" | "cancelLongText">;
  onChange: (value: string | null) => void;
  autoOpen?: boolean;
  children: ReactNode;
}) {
  const state = useDraftPopover(value, autoOpen, readOnly, (draft) => {
    onChange(draft.trim() === "" ? null : draft);
    return true;
  });
  if (readOnly) return <>{children}</>;
  return (
    <Popover open={state.open} onOpenChange={state.onOpenChange}>
      <PopoverTrigger asChild>{children}</PopoverTrigger>
      <PopoverContent align="start" className="w-80 p-2">
        <textarea
          autoFocus
          rows={5}
          value={state.draft}
          aria-label={label}
          onChange={(event) => state.setDraft(event.target.value)}
          onKeyDown={(event) => {
            if (event.key === "Enter" && !event.shiftKey) {
              event.preventDefault();
              state.commit();
            }
          }}
          className={`${INPUT_CLASS} py-1.5`}
        />
        <div className="mt-2 flex justify-end gap-1.5">
          <button
            type="button"
            className="h-7 rounded-md px-2.5 text-compact text-muted-foreground hover:bg-state-hover"
            onMouseDown={(event) => event.preventDefault()}
            onClick={() => state.setOpen(false)}
          >
            {copy.cancelLongText}
          </button>
          <button
            type="button"
            className="h-7 rounded-md bg-primary px-2.5 text-compact font-semibold text-primary-foreground"
            onMouseDown={(event) => event.preventDefault()}
            onClick={state.commit}
          >
            {copy.saveLongText}
          </button>
        </div>
      </PopoverContent>
    </Popover>
  );
}

/** A link typed into a popover; only http and https addresses are accepted. */
export function ItemLinkPopover({
  value,
  label,
  readOnly,
  copy,
  onChange,
  autoOpen = false,
  children,
}: {
  value: string;
  label: string;
  readOnly: boolean;
  copy: Pick<ItemSheetCopy["properties"], "invalidLink">;
  onChange: (value: string | null) => void;
  autoOpen?: boolean;
  children: ReactNode;
}) {
  const [invalid, setInvalid] = useState(false);
  const state = useDraftPopover(value, autoOpen, readOnly, (draft) => {
    const parsed = fieldTypeRegistry.link.parseDraft(draft);
    setInvalid(!parsed.ok);
    if (!parsed.ok) return false;
    onChange(parsed.value as string | null);
    return true;
  });
  if (readOnly) return <>{children}</>;
  return (
    <Popover
      open={state.open}
      onOpenChange={(next) => {
        setInvalid(false);
        state.onOpenChange(next);
      }}
    >
      <PopoverTrigger asChild>{children}</PopoverTrigger>
      <PopoverContent align="start" className="w-72 p-2">
        <input
          type="text"
          autoFocus
          value={state.draft}
          aria-label={label}
          aria-invalid={invalid || undefined}
          onChange={(event) => {
            setInvalid(false);
            state.setDraft(event.target.value);
          }}
          onKeyDown={(event) => {
            if (event.key !== "Enter") return;
            event.preventDefault();
            state.commit();
          }}
          onBlur={state.commit}
          className={`${INPUT_CLASS} h-8`}
        />
        {invalid ? (
          <p
            role="alert"
            className="mt-1.5 mb-0 flex items-center gap-1 text-item-small font-semibold text-danger-text"
          >
            <AlertCircle aria-hidden="true" className="size-3.5" />
            {copy.invalidLink}
          </p>
        ) : null}
      </PopoverContent>
    </Popover>
  );
}

/** What a calculated value reads as, and its tooltip: a number, a text, "—", or the error's reason. */
export function describeComputed(
  field: FieldDefinition,
  computed: ComputedValue | undefined,
  copy: ItemSheetCopy["properties"]["computed"],
): {
  readonly text: string;
  readonly title: string;
  readonly tone: "value" | "empty" | "error" | "stale";
} {
  const title = `${field.name} · ${copy.label}`;
  if (!computed || computed.state === "empty") {
    return { text: "—", title: `${title} · ${copy.empty}`, tone: "empty" };
  }
  if (computed.state === "error") {
    return {
      text: copy.error,
      title: `${title} · ${copy.errors[computed.errorCode] ?? copy.unknownError}`,
      tone: "error",
    };
  }
  const shown = Array.isArray(computed.value)
    ? computed.value.join(", ")
    : String(computed.value ?? "");
  return {
    text: shown === "" ? "—" : shown,
    title: computed.state === "stale" ? `${title} · ${copy.stale}` : title,
    tone: computed.state === "stale" ? "stale" : "value",
  };
}

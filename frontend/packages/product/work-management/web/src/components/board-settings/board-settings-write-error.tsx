import { cn } from "@notrelix/ui-web";
import { useSettings } from "./board-settings-context";
import { BUTTON_CLASS } from "./board-settings-parts";

/**
 * The failure of a write made from one control, shown right under it: what
 * failed, that the typed value is kept, and Retry. A control points
 * `aria-describedby` at `id`; a version conflict says the latest settings
 * were loaded instead of offering Retry.
 */
export function WriteError({ scope, id }: { scope: string; id: string }) {
  const { copy, failure } = useSettings();
  if (!failure || failure.scope !== scope) return null;
  const t = copy.feedback;
  return (
    <div
      id={id}
      role="alert"
      className="mt-1 flex flex-wrap items-center gap-x-2 text-crumb text-danger-text"
    >
      <span>
        {failure.reason === "conflict"
          ? t.conflictReloaded
          : failure.reason === "invalid"
            ? t.invalid
            : t.failedKept}
      </span>
      {failure.retry ? (
        <button
          type="button"
          onClick={failure.retry}
          className={cn(BUTTON_CLASS.ghostBrand, "h-6")}
        >
          {t.retry}
        </button>
      ) : null}
    </div>
  );
}

/** The `aria-describedby` and `aria-invalid` a control carries while its write failed. */
export function useWriteErrorProps(scope: string, id: string) {
  const { failure } = useSettings();
  const failed = failure?.scope === scope;
  return {
    "aria-describedby": failed ? id : undefined,
    "aria-invalid": failed ? true : undefined,
  } as const;
}

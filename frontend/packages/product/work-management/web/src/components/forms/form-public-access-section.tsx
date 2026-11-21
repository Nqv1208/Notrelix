import { useEffect, useId, useRef, useState } from "react";
import { Globe, Lock } from "lucide-react";
import { Skeleton, cn } from "@notrelix/ui-web";
import type {
  FormPublicAccess,
  FormPublicAccessResult,
} from "@notrelix/work-management-core";
import {
  BUTTON_CLASS,
  HINT_CLASS,
  INPUT_CLASS,
  SwitchControl,
} from "../board-settings/board-settings-parts";
import { FormAlert } from "./forms-parts";
import type { FormsCopy } from "./forms.copy";

export interface FormPublicAccessSectionProps {
  readonly copy: FormsCopy;
  /** `denied`: the read was refused (403/404), so the person is not a workspace Owner or Admin. */
  readonly status: "loading" | "error" | "denied" | "ready";
  readonly access: FormPublicAccess | null;
  /** What the link starts with, e.g. `https://app.notrelix.com`; the link is `<origin>/f/<key>`. */
  readonly origin: string;
  readonly onSwitch: (enabled: boolean) => Promise<FormPublicAccessResult>;
  /** Copies the link; resolves false when the browser refused. */
  readonly onCopyLink: (link: string) => Promise<boolean>;
  readonly onRetry: () => void;
}

/** The public link of a form: a Private/Public switch and, while public, the link with its Copy button. */
export function FormPublicAccessSection(props: FormPublicAccessSectionProps) {
  const { copy, status, access } = props;
  const t = copy.publicAccess;
  const noteId = useId();
  const inputRef = useRef<HTMLInputElement>(null);
  const timer = useRef<number | undefined>(undefined);
  const [pending, setPending] = useState(false);
  const [problem, setProblem] = useState<"failed" | "linksOff" | null>(null);
  const [copied, setCopied] = useState<"yes" | "no" | null>(null);

  useEffect(() => () => window.clearTimeout(timer.current), []);

  const isPublic = access?.isPublic === true && access.publicKey !== null;
  const link = isPublic ? `${props.origin}/f/${access.publicKey}` : "";

  async function change(enabled: boolean) {
    setPending(true);
    setProblem(null);
    setCopied(null);
    const result = await props.onSwitch(enabled);
    setPending(false);
    if (!result.ok)
      setProblem(result.reason === "linksOff" ? "linksOff" : "failed");
  }

  async function copyLink() {
    const done = await props.onCopyLink(link);
    if (!done) inputRef.current?.select();
    setCopied(done ? "yes" : "no");
    window.clearTimeout(timer.current);
    timer.current = window.setTimeout(() => setCopied(null), 2000);
  }

  return (
    <section
      aria-label={t.title}
      className="flex flex-col gap-2.5 rounded-[10px] border border-line bg-popover p-3"
    >
      <div className="flex items-center gap-2">
        {isPublic ? (
          <Globe
            aria-hidden="true"
            className="size-4 shrink-0 text-brand-subtle-foreground"
          />
        ) : (
          <Lock
            aria-hidden="true"
            className="size-4 shrink-0 text-meta-foreground"
          />
        )}
        <h2 className="m-0 min-w-0 flex-1 text-item-ui font-semibold">
          {t.title}
        </h2>
        {status === "ready" || status === "denied" ? (
          <>
            <span className="text-crumb font-semibold text-muted-foreground">
              {pending ? t.saving : isPublic ? t.stateOn : t.stateOff}
            </span>
            <SwitchControl
              checked={isPublic}
              disabled={status === "denied" || pending}
              describedBy={noteId}
              label={t.switchLabel}
              onChange={(next) => void change(next)}
            />
          </>
        ) : null}
      </div>

      {status === "loading" ? (
        <div
          role="status"
          aria-label={t.loading}
          className="flex flex-col gap-2"
        >
          <Skeleton className="h-4 w-3/4" />
          <Skeleton className="h-8" />
        </div>
      ) : null}

      {status === "error" ? (
        <FormAlert
          message={t.loadError}
          onRetry={props.onRetry}
          retryLabel={copy.common.retry}
        />
      ) : null}

      {status === "denied" ? (
        <p id={noteId} className={cn(HINT_CLASS, "m-0")}>
          {t.denied}
        </p>
      ) : null}

      {status === "ready" ? (
        <p id={noteId} className={cn(HINT_CLASS, "m-0")}>
          {isPublic ? t.onNote : t.offNote}
        </p>
      ) : null}

      {problem === "linksOff" ? (
        <p role="alert" className="m-0 text-crumb text-danger-text">
          {t.linksOff}
        </p>
      ) : null}
      {problem === "failed" ? <FormAlert message={t.saveError} /> : null}

      {/* The row grows in place (rows 0fr to 1fr) instead of popping in under the switch. */}
      <div
        className={cn(
          "grid transition-[grid-template-rows,opacity] duration-200 ease-out motion-reduce:transition-none",
          isPublic
            ? "grid-rows-[1fr] opacity-100"
            : "grid-rows-[0fr] opacity-0",
        )}
      >
        <div className="-m-0.5 min-h-0 overflow-hidden p-0.5">
          {isPublic ? (
            <div className="flex flex-wrap items-center gap-2">
              <input
                ref={inputRef}
                readOnly
                value={link}
                aria-label={t.linkLabel}
                onFocus={(event) => event.currentTarget.select()}
                className={cn(
                  INPUT_CLASS,
                  "min-w-0 flex-[2] max-md:basis-full",
                )}
              />
              <button
                type="button"
                onClick={() => void copyLink()}
                className={BUTTON_CLASS.primary}
              >
                {copied === "yes" ? t.copied : t.copy}
              </button>
              {copied === "no" ? (
                <p
                  role="status"
                  className="m-0 basis-full text-crumb text-danger-text"
                >
                  {t.copyFailed}
                </p>
              ) : null}
            </div>
          ) : null}
        </div>
      </div>
    </section>
  );
}

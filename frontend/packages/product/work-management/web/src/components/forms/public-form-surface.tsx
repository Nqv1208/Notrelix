import { useId, useState, type ComponentType, type FormEvent } from "react";
import { Check, Lock } from "lucide-react";
import { Skeleton, cn } from "@notrelix/ui-web";
import type {
  FormAnswerErrors,
  PublicForm,
  PublicFormSubmitResult,
} from "@notrelix/work-management-core";
import {
  BUTTON_CLASS,
  HINT_CLASS,
} from "../board-settings/board-settings-parts";
import type { CaptchaWidgetProps } from "./captcha-widget";
import { Question } from "./form-fill-surface";
import { isAnswerable, validateAnswers } from "./form-model";
import { FormAlert } from "./forms-parts";
import type { FormsCopy } from "./forms.copy";

export interface PublicFormSurfaceProps {
  readonly copy: FormsCopy;
  /** `unavailable`: the key opens nothing (private, closed, archived or unknown: the server does not say which). */
  readonly status: "loading" | "error" | "unavailable" | "ready";
  readonly form: PublicForm | null;
  /** The captcha is injected so a test can solve it at once. */
  readonly Captcha: ComponentType<CaptchaWidgetProps>;
  readonly onReload: () => void;
  readonly onSubmit: (
    answers: Readonly<Record<string, unknown>>,
    captchaToken: string,
    honeypot: string,
  ) => Promise<PublicFormSubmitResult>;
}

const CARD =
  "rounded-[12px] border border-line bg-popover p-5 max-sm:border-0 max-sm:bg-transparent max-sm:p-0 sm:p-7";

/**
 * The page a visitor with no account fills a public form on: one column, no app shell, no way back into the
 * workspace. A send needs a fresh captcha token; the token is spent by every attempt, so the challenge restarts
 * after each one and the send stays off until it is solved again. Pure: the send is a callback.
 */
export function PublicFormSurface(props: PublicFormSurfaceProps) {
  const { copy, status, form } = props;
  const p = copy.public;

  let body;
  if (status === "loading")
    body = (
      <div
        role="status"
        aria-label={copy.common.loading}
        className={cn("flex flex-col gap-3", CARD)}
      >
        <Skeleton className="h-7 w-3/5" />
        <Skeleton className="h-4" />
        <Skeleton className="h-10" />
        <Skeleton className="h-10" />
        <Skeleton className="h-[90px]" />
      </div>
    );
  else if (status === "error")
    body = (
      <div className={CARD}>
        <FormAlert
          message={copy.fill.loadError}
          onRetry={props.onReload}
          retryLabel={copy.common.retry}
        />
      </div>
    );
  else if (status === "unavailable" || !form || !form.captchaSiteKey)
    body = <Unavailable copy={copy} />;
  else body = <Fill key={form.name} {...props} form={form} />;

  return (
    <div className="min-h-screen bg-background text-foreground">
      <main
        aria-label={form?.name ?? p.unavailableTitle}
        className="mx-auto flex w-full max-w-[668px] flex-col gap-4 px-4 py-6 sm:px-6 sm:py-10"
      >
        {body}
      </main>
    </div>
  );
}

function Unavailable({ copy }: { copy: FormsCopy }) {
  return (
    <section
      role="status"
      className={cn("flex flex-col items-start gap-2.5", CARD)}
    >
      <Lock aria-hidden="true" className="size-4 text-meta-foreground" />
      <h1 className="m-0 text-base leading-[1.45] font-[650]">
        {copy.public.unavailableTitle}
      </h1>
      <p className="m-0 text-item-small text-meta-foreground">
        {copy.public.unavailableBody}
      </p>
    </section>
  );
}

type Failure = "captcha" | "captchaLoad" | "limited" | "failed" | null;

function Fill(props: PublicFormSurfaceProps & { form: PublicForm }) {
  const { copy, form, Captcha } = props;
  const p = copy.public;
  const idBase = useId();
  const [answers, setAnswers] = useState<Record<string, unknown>>({});
  const [errors, setErrors] = useState<FormAnswerErrors>({});
  const [token, setToken] = useState<string | null>(null);
  const [resetKey, setResetKey] = useState(0);
  const [sending, setSending] = useState(false);
  const [done, setDone] = useState(false);
  const [gone, setGone] = useState(false);
  const [failure, setFailure] = useState<Failure>(null);
  const [limitedMessage, setLimitedMessage] = useState("");
  const [honeypot, setHoneypot] = useState("");
  const questions = form.questions.filter(isAnswerable);

  function set(key: string, value: unknown) {
    setAnswers((prev) => ({ ...prev, [key]: value }));
    setErrors((prev) => {
      if (!(key in prev)) return prev;
      const { [key]: _gone, ...rest } = prev;
      return rest;
    });
  }

  async function submit(event: FormEvent) {
    event.preventDefault();
    if (sending || !token) return;
    const found = validateAnswers(questions, answers, copy);
    setErrors(found);
    if (Object.keys(found).length > 0) return;
    setSending(true);
    setFailure(null);
    const result = await props.onSubmit(answers, token, honeypot);
    setSending(false);
    // The token is spent whatever the answer: a new challenge, and no send until it is solved.
    setToken(null);
    setResetKey((n) => n + 1);
    if (result.ok) {
      setDone(true);
      return;
    }
    if (result.reason === "missing") setGone(true);
    else if (result.reason === "validation") {
      const mapped: Record<string, string> = {};
      for (const q of questions) {
        const hit = Object.keys(result.errors ?? {}).some(
          (key) => key.toLowerCase() === q.key.toLowerCase(),
        );
        if (hit) mapped[q.key] = copy.fill.errServer;
      }
      setErrors(mapped);
      if (Object.keys(mapped).length === 0) setFailure("failed");
    } else if (result.reason === "captcha") setFailure("captcha");
    else if (result.reason === "limited") {
      setLimitedMessage(result.message ?? "");
      setFailure("limited");
    } else setFailure("failed");
  }

  if (gone) return <Unavailable copy={copy} />;

  if (done)
    return (
      <section
        role="status"
        className={cn("flex flex-col items-start gap-3.5", CARD, "sm:p-8")}
      >
        <span className="grid size-10 place-items-center rounded-full bg-success-subtle text-success-text">
          <Check aria-hidden="true" className="size-4" />
        </span>
        <h1 className="m-0 text-xl leading-[1.3] font-[650] max-sm:text-base">
          {p.thanks}
        </h1>
        <p className="m-0 text-item-ui text-muted-foreground">{p.thanksBody}</p>
        <button
          type="button"
          onClick={() => {
            setAnswers({});
            setErrors({});
            setDone(false);
          }}
          className={cn(
            BUTTON_CLASS.outline,
            "max-md:h-10 max-sm:w-full max-sm:justify-center",
          )}
        >
          {p.another}
        </button>
      </section>
    );

  return (
    <form
      onSubmit={(event) => void submit(event)}
      noValidate
      className={cn("flex flex-col gap-4.5 max-sm:gap-3.5", CARD)}
    >
      <input
        type="text"
        name="website"
        value={honeypot}
        onChange={(e) => setHoneypot(e.target.value)}
        tabIndex={-1}
        autoComplete="off"
        aria-hidden="true"
        className="pointer-events-none absolute -left-[9999px] size-px opacity-0"
      />
      <div className="flex flex-col gap-1 max-sm:gap-3.5">
        <h1 className="m-0 text-xl leading-[1.3] font-[650] tracking-[-0.01em] max-sm:text-base">
          {form.name}
        </h1>
        {form.description ? (
          <p className="m-0 text-item-ui text-meta-foreground">
            {form.description}
          </p>
        ) : null}
      </div>

      {questions.map((q) => (
        <Question
          key={q.id}
          copy={copy}
          q={q}
          id={`${idBase}-${q.id}`}
          value={answers[q.key]}
          error={errors[q.key]}
          fields={[]}
          onChange={(value) => set(q.key, value)}
        />
      ))}

      <div className="flex flex-col gap-1.5">
        <span className="text-item-small font-semibold text-foreground">
          {p.captchaLabel}
        </span>
        <Captcha
          siteKey={form.captchaSiteKey ?? ""}
          resetKey={resetKey}
          onToken={(value) => {
            setToken(value);
            setFailure((now) =>
              now === "captcha" || now === "captchaLoad" ? null : now,
            );
          }}
          onExpire={() => setToken(null)}
          onError={() => {
            setToken(null);
            setFailure("captchaLoad");
          }}
        />
        {failure === "captcha" || failure === "captchaLoad" ? (
          <p role="alert" className="m-0 text-crumb text-danger-text">
            {failure === "captcha" ? p.captchaError : p.captchaLoadError}
          </p>
        ) : token ? null : (
          <span className={HINT_CLASS}>{p.captchaWaiting}</span>
        )}
      </div>

      {failure === "limited" ? (
        <FormAlert message={limitedMessage || p.limited} />
      ) : null}
      {failure === "failed" ? <FormAlert message={p.failed} /> : null}

      <button
        type="submit"
        disabled={sending || !token}
        className={cn(
          BUTTON_CLASS.primary,
          "h-11 w-full justify-center text-label font-semibold sm:h-10 sm:w-auto sm:self-start sm:px-[18px]",
        )}
      >
        {sending ? p.sending : p.submit}
      </button>
    </form>
  );
}

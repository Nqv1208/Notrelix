import { useId, useRef, useState, type ReactNode } from "react";
import { ChevronLeft, CircleCheck, Lock, TriangleAlert } from "lucide-react";
import { Skeleton, cn } from "@notrelix/ui-web";
import type {
  FormAnswerErrors,
  FormDetail,
  FormQuestion,
} from "@notrelix/work-management-core";
import type { FormWriteResult } from "@notrelix/work-management-state";
import {
  BUTTON_CLASS,
  FIELD_LABEL_CLASS,
  HINT_CLASS,
  INPUT_CLASS,
  TEXTAREA_CLASS,
} from "../board-settings/board-settings-parts";
import { FormAlert } from "./forms-parts";
import { fillFormsCopy, type FormsCopy } from "./forms.copy";
import {
  questionOptions,
  validateAnswers,
  type FormBoardField,
} from "./form-model";

export interface FormFillSurfaceProps {
  readonly copy: FormsCopy;
  readonly status: "loading" | "error" | "notFound" | "ready";
  readonly form: FormDetail | null;
  /** Board fields (the options of a mapped select come from them). */
  readonly fields: readonly FormBoardField[];
  readonly boardTitle: string;
  readonly user?: { readonly name: string; readonly email?: string };
  /** "Thứ Hai 12/10/2026": shown under a date question. */
  readonly todayLabel?: string;
  /** The member may create items on the board (the server decides; false shows why). */
  readonly canSubmit?: boolean;
  /** The manager's preview: same page, the button does nothing. */
  readonly preview?: boolean;
  readonly onBack: () => void;
  readonly onHome?: () => void;
  readonly onReload: () => void;
  /** Sends the answers; `key` is the same on a retry of the same attempt. */
  readonly onSubmit: (
    answers: Readonly<Record<string, unknown>>,
    key: string,
  ) => Promise<FormWriteResult>;
}

/**
 * The page a member fills a form on (also the manager's preview): one column,
 * label above the box, "*" with a hidden "(bắt buộc)", errors under the box and
 * a summary that links to each. Answers survive a refused or failed send; a
 * retry replays the same attempt. Pure: the send is a callback.
 */
export function FormFillSurface(props: FormFillSurfaceProps) {
  const { copy, status, form } = props;
  const f = copy.fill;

  if (status === "loading")
    return (
      <Frame {...props}>
        <div
          role="status"
          aria-label={copy.common.loading}
          className="flex flex-col gap-3"
        >
          <Skeleton className="h-7 w-2/3" />
          <Skeleton className="h-11" />
          <Skeleton className="h-11" />
          <Skeleton className="h-11" />
        </div>
      </Frame>
    );
  if (status === "error")
    return (
      <Frame {...props}>
        <div className="rounded-xl border border-line bg-popover p-4">
          <FormAlert
            message={f.loadError}
            onRetry={props.onReload}
            retryLabel={copy.common.retry}
          />
        </div>
      </Frame>
    );
  if (status === "notFound" || !form)
    return (
      <Frame {...props}>
        <Notice
          tone="danger"
          title={f.notFoundTitle}
          body={f.notFoundBody}
          action={props.onHome ?? props.onBack}
          actionLabel={f.toHome}
        />
      </Frame>
    );
  if (!props.preview && form.status === "draft")
    return (
      <Frame {...props}>
        <Notice
          title={f.draftTitle}
          body={f.draftBody}
          action={props.onBack}
          actionLabel={f.toBoard}
        />
      </Frame>
    );
  if (!props.preview && form.status === "closed")
    return (
      <Frame {...props}>
        <Notice
          title={f.closedTitle}
          body={fillFormsCopy(f.closedBody, { title: form.title })}
          action={props.onBack}
          actionLabel={f.toBoard}
        />
      </Frame>
    );
  if (!props.preview && props.canSubmit === false)
    return (
      <Frame {...props}>
        <Notice
          tone="danger"
          title={f.cannotSubmitTitle}
          body={f.cannotSubmitBody}
          action={props.onBack}
          actionLabel={f.toBoard}
        />
      </Frame>
    );
  return (
    <Frame {...props}>
      <FillForm key={form.id} {...props} form={form} />
    </Frame>
  );
}

/** The page column: the way back and the card's place. */
function Frame({
  copy,
  boardTitle,
  form,
  preview,
  onBack,
  children,
}: FormFillSurfaceProps & { children: ReactNode }) {
  return (
    <div className="flex min-h-0 min-w-0 flex-1 flex-col overflow-auto bg-background text-foreground">
      <main
        aria-label={form?.title ?? copy.common.back}
        className="mx-auto flex w-full max-w-[620px] flex-col gap-4 px-4 py-6 sm:px-6 sm:py-8"
      >
        {preview ? null : (
          <nav
            aria-label={copy.common.back}
            className="text-crumb text-meta-foreground"
          >
            <button
              type="button"
              onClick={onBack}
              aria-label={fillFormsCopy(copy.fill.crumbBoard, {
                board: boardTitle,
              })}
              className="inline-flex cursor-pointer items-center gap-1 border-0 bg-transparent p-0 font-medium text-brand-subtle-foreground hover:underline"
            >
              <ChevronLeft aria-hidden="true" className="size-3.5" />
              {boardTitle}
            </button>
            {form ? (
              <>
                {" › "}
                <b
                  aria-current="page"
                  className="font-semibold text-foreground"
                >
                  {form.title}
                </b>
              </>
            ) : null}
          </nav>
        )}
        {children}
      </main>
    </div>
  );
}

function Notice({
  title,
  body,
  action,
  actionLabel,
  tone,
}: {
  title: string;
  body: string;
  action: () => void;
  actionLabel: string;
  tone?: "danger";
}) {
  return (
    <section
      role={tone === "danger" ? "alert" : "status"}
      className="flex flex-col items-start gap-3 rounded-xl border border-line bg-popover p-5"
    >
      <span
        aria-hidden="true"
        className="grid size-10 place-items-center rounded-xl border border-line bg-background text-muted-foreground"
      >
        <Lock className="size-4" />
      </span>
      <h1 className="m-0 text-[19px] leading-[1.35] font-[650]">{title}</h1>
      <p className="m-0 text-item-ui text-muted-foreground">{body}</p>
      <button type="button" onClick={action} className={BUTTON_CLASS.outline}>
        {actionLabel}
      </button>
    </section>
  );
}

function FillForm(props: FormFillSurfaceProps & { form: FormDetail }) {
  const { copy, form, fields, preview } = props;
  const f = copy.fill;
  const idBase = useId();
  const [answers, setAnswers] = useState<Record<string, unknown>>({});
  const [errors, setErrors] = useState<FormAnswerErrors>({});
  const [sending, setSending] = useState(false);
  const [done, setDone] = useState(false);
  const [failure, setFailure] = useState<"network" | "closed" | null>(null);
  const [copiedAnswers, setCopiedAnswers] = useState(false);
  const attempt = useRef<string | null>(null);
  const questions = form.questions;
  const idOf = (q: FormQuestion) => `${idBase}-${q.id}`;

  function set(key: string, value: unknown) {
    setAnswers((prev) => ({ ...prev, [key]: value }));
    setErrors((prev) => {
      if (!(key in prev)) return prev;
      const { [key]: _gone, ...rest } = prev;
      return rest;
    });
    // A changed answer is a new attempt: it must not replay the first.
    attempt.current = null;
  }

  async function submit(event: React.FormEvent) {
    event.preventDefault();
    if (sending || preview) return;
    const found = validateAnswers(questions, answers, copy);
    setErrors(found);
    if (Object.keys(found).length > 0) return;
    attempt.current ??= crypto.randomUUID();
    setSending(true);
    setFailure(null);
    const result = await props.onSubmit(answers, attempt.current);
    setSending(false);
    if (result.ok) {
      attempt.current = null;
      setDone(true);
    } else if (result.reason === "validation") {
      const mapped: Record<string, string> = {};
      for (const q of questions) {
        const message = Object.entries(result.errors ?? {}).find(
          ([key]) => key.toLowerCase() === q.key.toLowerCase(),
        )?.[1];
        if (message) mapped[q.key] = f.errServer;
      }
      setErrors(mapped);
      attempt.current = null;
    } else if (result.reason === "conflict") setFailure("closed");
    else setFailure("network");
  }

  function focusQuestion(q: FormQuestion) {
    document.getElementById(idOf(q))?.focus();
  }

  if (done)
    return (
      <section
        role="status"
        className="flex flex-col items-start gap-3 rounded-xl border border-line bg-popover p-5"
      >
        <CircleCheck aria-hidden="true" className="size-6 text-success-text" />
        <h1 className="m-0 text-[19px] leading-[1.35] font-[650]">
          {f.thanks}
        </h1>
        <p className="m-0 text-item-ui text-muted-foreground">
          {fillFormsCopy(f.thanksBody, {
            title: String(answers.title ?? form.title),
            board: props.boardTitle,
          })}
        </p>
        <div className="flex flex-wrap gap-2">
          <button
            type="button"
            onClick={() => {
              setAnswers({});
              setErrors({});
              setDone(false);
            }}
            className={BUTTON_CLASS.outline}
          >
            {f.another}
          </button>
          <button
            type="button"
            onClick={props.onBack}
            className={BUTTON_CLASS.ghost}
          >
            {f.toBoard}
          </button>
        </div>
      </section>
    );

  const errorQuestions = questions.filter((q) => errors[q.key]);
  return (
    <div className="flex flex-col gap-4">
      {preview ? (
        <div
          role="status"
          className="flex items-center gap-3 rounded-[10px] bg-warning-subtle px-3.5 py-2.5 text-item-small font-medium text-foreground"
        >
          <span className="flex-1">{copy.builder.previewBanner}</span>
          <button
            type="button"
            onClick={props.onBack}
            className={cn(BUTTON_CLASS.outline, "h-7")}
          >
            {copy.builder.backToEdit}
          </button>
        </div>
      ) : null}
      <form
        onSubmit={(event) => void submit(event)}
        noValidate
        className="flex flex-col gap-4 rounded-xl border border-line bg-popover p-5 sm:p-6"
      >
        <div className="flex flex-col gap-1.5">
          <h1 className="m-0 text-[22px] leading-[1.3] font-[650] tracking-[-0.01em]">
            {form.title}
          </h1>
          {form.description ? (
            <p className="m-0 text-item-ui text-meta-foreground">
              {form.description}
            </p>
          ) : null}
        </div>

        {errorQuestions.length > 0 ? (
          <div
            role="alert"
            className="rounded-[10px] bg-danger-subtle px-3.5 py-2.5 text-item-small text-danger-text"
          >
            <b>{f.summary}</b>{" "}
            {fillFormsCopy(f.summaryCount, { count: errorQuestions.length })}{" "}
            {errorQuestions.map((q, index) => (
              <span key={q.id}>
                <button
                  type="button"
                  onClick={() => focusQuestion(q)}
                  className="cursor-pointer border-0 bg-transparent p-0 font-semibold text-danger-text underline"
                >
                  {q.label}
                </button>
                {index < errorQuestions.length - 1 ? ", " : "."}
              </span>
            ))}
          </div>
        ) : null}

        {failure === "closed" ? (
          <div
            role="alert"
            className="flex flex-col gap-2 rounded-[10px] bg-danger-subtle px-3.5 py-2.5"
          >
            <div className="flex items-start gap-2 text-item-small text-danger-text">
              <TriangleAlert
                aria-hidden="true"
                className="mt-0.5 size-3.5 shrink-0"
              />
              <span>
                <b>{f.closedMidwayTitle}</b> {f.closedMidwayBody}
              </span>
            </div>
            <button
              type="button"
              onClick={() => {
                void navigator.clipboard
                  ?.writeText(
                    questions
                      .map((q) => `${q.label}: ${String(answers[q.key] ?? "")}`)
                      .join("\n"),
                  )
                  .then(() => setCopiedAnswers(true));
              }}
              className={cn(BUTTON_CLASS.outline, "h-7 self-start")}
            >
              {copiedAnswers ? copy.builder.copied : f.copyAnswers}
            </button>
          </div>
        ) : null}

        {questions.map((q) => (
          <Question
            key={q.id}
            copy={copy}
            q={q}
            id={idOf(q)}
            value={answers[q.key]}
            error={errors[q.key]}
            fields={fields}
            todayLabel={props.todayLabel}
            onChange={(value) => set(q.key, value)}
          />
        ))}

        {failure === "network" ? (
          <FormAlert
            message={f.networkError}
            onRetry={() =>
              void submit({ preventDefault() {} } as React.FormEvent)
            }
            retryLabel={copy.common.retry}
          />
        ) : null}

        <div className="flex flex-col gap-2">
          <button
            type="submit"
            disabled={sending || preview || failure === "closed"}
            className={cn(
              BUTTON_CLASS.primary,
              "h-11 w-full justify-center text-item-ui sm:h-10 sm:w-auto sm:self-start sm:px-[18px]",
            )}
          >
            {sending ? f.sending : f.submit}
          </button>
          {preview ? (
            <span className={HINT_CLASS}>{copy.builder.previewSubmit}</span>
          ) : props.user ? (
            <span className={HINT_CLASS}>
              {props.user.email
                ? fillFormsCopy(f.asUser, {
                    name: props.user.name,
                    email: props.user.email,
                  })
                : fillFormsCopy(f.asUserNoEmail, { name: props.user.name })}
            </span>
          ) : null}
        </div>
      </form>
    </div>
  );
}

const BOX = "h-11 sm:h-[34px]";

function Question({
  copy,
  q,
  id,
  value,
  error,
  fields,
  todayLabel,
  onChange,
}: {
  copy: FormsCopy;
  q: FormQuestion;
  id: string;
  value: unknown;
  error?: string;
  fields: readonly FormBoardField[];
  todayLabel?: string;
  onChange: (value: unknown) => void;
}) {
  const f = copy.fill;
  const errorId = `${id}-e`;
  const describe = error ? errorId : q.helpText ? `${id}-h` : undefined;
  const common = {
    id,
    "aria-invalid": error ? true : undefined,
    "aria-describedby": describe,
    "aria-required": q.required || undefined,
  } as const;
  const options = questionOptions(q, fields);
  const text = typeof value === "string" ? value : "";
  let control: ReactNode;

  switch (q.type) {
    case "longtext":
      control = (
        <textarea
          {...common}
          rows={4}
          value={text}
          maxLength={q.maxLength ?? 2000}
          placeholder={q.placeholder ?? undefined}
          onChange={(e) => onChange(e.target.value)}
          className={TEXTAREA_CLASS}
        />
      );
      break;
    case "number":
      control = (
        <input
          {...common}
          type="number"
          inputMode="decimal"
          step="any"
          value={typeof value === "number" ? value : text}
          placeholder={q.placeholder ?? undefined}
          onChange={(e) =>
            onChange(e.target.value === "" ? "" : Number(e.target.value))
          }
          className={cn(INPUT_CLASS, BOX)}
        />
      );
      break;
    case "date":
      control = (
        <input
          {...common}
          type="date"
          value={text}
          onChange={(e) => onChange(e.target.value)}
          className={cn(INPUT_CLASS, BOX)}
        />
      );
      break;
    case "select":
      control = (
        <select
          {...common}
          value={text}
          onChange={(e) => onChange(e.target.value)}
          className={cn(INPUT_CLASS, BOX)}
        >
          <option value="">{f.pick}</option>
          {options.map((o) => (
            <option key={o.key} value={o.key}>
              {o.label}
            </option>
          ))}
        </select>
      );
      break;
    case "multiselect": {
      const picked = Array.isArray(value) ? (value as string[]) : [];
      control = (
        <div
          role="group"
          aria-labelledby={`${id}-l`}
          className="flex flex-col gap-1.5"
        >
          {options.map((o) => (
            <label
              key={o.key}
              className="inline-flex min-h-8 items-center gap-2 text-item-ui"
            >
              <input
                type="checkbox"
                checked={picked.includes(o.key)}
                onChange={(e) =>
                  onChange(
                    e.target.checked
                      ? [...picked, o.key]
                      : picked.filter((entry) => entry !== o.key),
                  )
                }
                className="size-4.5 accent-primary"
              />
              {o.label}
            </label>
          ))}
        </div>
      );
      break;
    }
    case "checkbox":
      control = (
        <label className="inline-flex min-h-8 items-center gap-2 text-item-ui">
          <input
            {...common}
            type="checkbox"
            checked={value === true}
            onChange={(e) => onChange(e.target.checked)}
            className="size-4.5 accent-primary"
          />
          {q.placeholder ?? ""}
        </label>
      );
      break;
    case "file":
    case "signature":
      control = <p className={cn(HINT_CLASS, "m-0")}>{f.unsupported}</p>;
      break;
    default:
      control = (
        <input
          {...common}
          type={
            q.type === "email" ? "email" : q.type === "phone" ? "tel" : "text"
          }
          value={text}
          maxLength={q.maxLength ?? undefined}
          placeholder={q.placeholder ?? undefined}
          onChange={(e) => onChange(e.target.value)}
          className={cn(INPUT_CLASS, BOX)}
        />
      );
  }

  return (
    <div className="flex min-w-0 flex-col gap-1.5">
      <label
        id={`${id}-l`}
        htmlFor={q.type === "multiselect" ? undefined : id}
        className={FIELD_LABEL_CLASS}
      >
        {q.label}
        {q.required ? (
          <>
            {" "}
            <span aria-hidden="true">*</span>
            <span className="sr-only">{copy.common.required}</span>
          </>
        ) : null}
      </label>
      {control}
      {q.type === "date" && todayLabel ? (
        <span className={HINT_CLASS}>
          {fillFormsCopy(f.todayIs, { date: todayLabel })}
        </span>
      ) : null}
      {q.helpText ? (
        <span id={`${id}-h`} className={HINT_CLASS}>
          {q.helpText}
        </span>
      ) : null}
      {error ? (
        <span
          id={errorId}
          className="flex items-start gap-1.5 text-crumb text-danger-text"
        >
          <TriangleAlert
            aria-hidden="true"
            className="mt-0.5 size-3.5 shrink-0"
          />
          {error}
        </span>
      ) : null}
    </div>
  );
}

import { useEffect, useId, useRef, useState } from "react";
import {
  ArrowDown,
  ArrowUp,
  ChevronLeft,
  GripVertical,
  Info,
  Plus,
  TriangleAlert,
  X,
} from "lucide-react";
import { Skeleton, cn } from "@notrelix/ui-web";
import {
  FORM_DESCRIPTION_KEY,
  FORM_DUE_KEY,
  FORM_SUPPORTED_QUESTION_TYPES,
  FORM_TITLE_KEY,
  duplicateFieldMappings,
  orderKeyBetween,
  type FormDetail,
  type FormQuestion,
  type FormQuestionOption,
  type FormQuestionType,
} from "@notrelix/work-management-core";
import type {
  FormQuestionChanges,
  FormWriteResult,
  NewFormQuestion,
} from "@notrelix/work-management-core";
import {
  BUTTON_CLASS,
  FIELD_LABEL_CLASS,
  HINT_CLASS,
  ICON_BUTTON_CLASS,
  INPUT_CLASS,
  TEXTAREA_CLASS,
} from "../board-settings/board-settings-parts";
import { FormFillSurface } from "./form-fill-surface";
import { Chip, FormAlert, FormStatusChip } from "./forms-parts";
import { fillFormsCopy, type FormsCopy } from "./forms.copy";
import {
  builtInTarget,
  compatibleFieldTypes,
  targetName,
  type BuiltInTarget,
  type FormBoardField,
} from "./form-model";

export interface FormBuilderSurfaceProps {
  readonly copy: FormsCopy;
  readonly status: "loading" | "error" | "missing" | "ready";
  readonly form: FormDetail | null;
  readonly fields: readonly FormBoardField[];
  readonly boardTitle: string;
  /** The question open in the side panel. */
  readonly selectedId: string | null;
  readonly onSelect: (questionId: string | null) => void;
  /** "09:42": when the form was last saved. */
  readonly savedAt?: string;
  readonly preview: boolean;
  readonly onPreview: (on: boolean) => void;
  /** The member link, shown once the form is published. */
  readonly linkUrl: string;
  readonly todayLabel?: string;
  readonly onUpdateDetails: (
    title: string,
    description: string,
  ) => Promise<FormWriteResult>;
  readonly onAddQuestion: (
    question: NewFormQuestion,
  ) => Promise<FormWriteResult>;
  readonly onUpdateQuestion: (
    question: FormQuestion,
    changes: FormQuestionChanges,
  ) => Promise<FormWriteResult>;
  readonly onRemoveQuestion: (
    question: FormQuestion,
  ) => Promise<FormWriteResult>;
  readonly onPublish: () => Promise<FormWriteResult>;
  readonly onClose: () => Promise<FormWriteResult>;
  readonly onCopyLink: () => Promise<boolean>;
  readonly onReload: () => void;
  readonly onBack: () => void;
}

const BUILT_IN_TYPE: Readonly<Record<BuiltInTarget, FormQuestionType>> = {
  title: "text",
  description: "longtext",
  due: "date",
};
const BUILT_IN_KEY: Readonly<Record<BuiltInTarget, string>> = {
  title: FORM_TITLE_KEY,
  description: FORM_DESCRIPTION_KEY,
  due: FORM_DUE_KEY,
};
const MAX_OPTIONS = 50;

const newKey = (prefix: string) =>
  `${prefix}_${crypto.randomUUID().replace(/-/g, "").slice(0, 12)}`;

/**
 * The form builder: the form's name and description, its questions in order
 * (drag the grip or use Up/Down), and the side panel of the open question.
 * Every field saves when it is left; a refusal names what did not save and
 * keeps what was typed. A closed form is read-only. Pure: writes are callbacks.
 */
export function FormBuilderSurface(props: FormBuilderSurfaceProps) {
  const { copy, status, form } = props;
  const b = copy.builder;

  if (status === "loading")
    return (
      <Page>
        <div
          role="status"
          aria-label={copy.common.loading}
          className="flex flex-col gap-2.5"
        >
          {[0, 1, 2, 3].map((n) => (
            <Skeleton key={n} className="h-12 rounded-[10px]" />
          ))}
        </div>
      </Page>
    );
  if (status === "error")
    return (
      <Page>
        <div className="rounded-xl border border-line bg-popover p-4">
          <FormAlert
            message={b.loadError}
            onRetry={props.onReload}
            retryLabel={copy.common.retry}
          />
        </div>
      </Page>
    );
  if (status === "missing" || !form)
    return (
      <Page>
        <p className="m-0 rounded-xl border border-line bg-popover p-4 text-item-ui text-muted-foreground">
          {copy.common.noAccess}
        </p>
      </Page>
    );
  if (props.preview)
    return (
      <FormFillSurface
        copy={copy}
        status="ready"
        form={form}
        fields={props.fields}
        boardTitle={props.boardTitle}
        todayLabel={props.todayLabel}
        preview
        onBack={() => props.onPreview(false)}
        onReload={props.onReload}
        onSubmit={async () => ({ ok: false, reason: "failed" })}
      />
    );
  return <Builder {...props} form={form} />;
}

function Page({ children }: { children: React.ReactNode }) {
  return (
    <div className="flex min-h-0 min-w-0 flex-1 flex-col overflow-auto bg-background p-6 text-foreground">
      <div className="mx-auto w-full max-w-[1100px]">{children}</div>
    </div>
  );
}

function Builder(props: FormBuilderSurfaceProps & { form: FormDetail }) {
  const { copy, form, fields } = props;
  const b = copy.builder;
  const baseId = useId();
  const closed = form.status === "closed";
  const questions = form.questions;
  const selected = questions.find((q) => q.id === props.selectedId) ?? null;
  const duplicates = duplicateFieldMappings(questions);
  const duplicateIds = new Set(
    [...duplicates.values()].flat().map((q) => q.id),
  );
  const firstDuplicate = [...duplicates.entries()][0];

  const [title, setTitle] = useState(form.title);
  const [description, setDescription] = useState(form.description);
  const [titleError, setTitleError] = useState(false);
  const [problem, setProblem] = useState<null | {
    kind: "conflict" | "save" | "publish";
    retry?: () => void;
  }>(null);
  const [adding, setAdding] = useState(false);
  const [justPublished, setJustPublished] = useState(false);
  const [copied, setCopied] = useState(false);
  const [dragId, setDragId] = useState<string | null>(null);
  const [dropIndex, setDropIndex] = useState<number | null>(null);
  const syncedVersion = useRef(form.version);

  // A newer read replaces the drafts of the name and description.
  useEffect(() => {
    if (form.version === syncedVersion.current) return;
    syncedVersion.current = form.version;
    setTitle(form.title);
    setDescription(form.description);
  }, [form.version, form.title, form.description]);

  function settle(result: FormWriteResult, retry: () => void): boolean {
    if (result.ok) {
      setProblem(null);
      return true;
    }
    setProblem(
      result.reason === "conflict"
        ? { kind: "conflict" }
        : { kind: "save", retry },
    );
    return false;
  }

  async function saveDetails() {
    if (closed) return;
    if (title.trim() === "") return setTitleError(true);
    setTitleError(false);
    if (title.trim() === form.title && description === form.description) return;
    const send = () => props.onUpdateDetails(title.trim(), description);
    settle(await send(), () => void saveDetails());
  }

  async function add(input: {
    type: FormQuestionType;
    target?: BuiltInTarget;
  }) {
    setAdding(false);
    const last = questions[questions.length - 1];
    const key = input.target ? BUILT_IN_KEY[input.target] : newKey("q");
    const label =
      input.target === "title"
        ? b.mapTitle
        : input.target === "description"
          ? b.mapDescription
          : input.target === "due"
            ? b.mapDue
            : b.defaultLabel;
    const draft: NewFormQuestion = {
      key,
      type: input.type,
      label,
      required: input.target === "title",
      boardFieldId: null,
      position: orderKeyBetween(last?.position ?? null, null),
    };
    const result = await props.onAddQuestion(draft);
    if (settle(result, () => void add(input))) setJustPublished(false);
  }

  async function move(question: FormQuestion, toIndex: number) {
    const rest = questions.filter((q) => q.id !== question.id);
    const position = orderKeyBetween(
      rest[toIndex - 1]?.position ?? null,
      rest[toIndex]?.position ?? null,
    );
    const send = () =>
      props.onUpdateQuestion(question, changesOf(question, { position }));
    settle(await send(), () => void move(question, toIndex));
  }

  async function publish() {
    const result = await props.onPublish();
    if (result.ok) {
      setProblem(null);
      setJustPublished(true);
    } else
      setProblem(
        result.reason === "conflict"
          ? { kind: "conflict" }
          : { kind: "publish", retry: () => void publish() },
      );
  }

  async function copyLink() {
    if (await props.onCopyLink()) {
      setCopied(true);
      window.setTimeout(() => setCopied(false), 2000);
    }
  }

  const noQuestions = questions.length === 0;
  const blocked = noQuestions || duplicates.size > 0;
  const blockedMessage = noQuestions
    ? b.publishNoQuestions
    : firstDuplicate
      ? fillFormsCopy(b.publishDuplicates, {
          a: questions.findIndex((q) => q.id === firstDuplicate[1][0]!.id) + 1,
          b: questions.findIndex((q) => q.id === firstDuplicate[1][1]!.id) + 1,
          field: fields.find((f) => f.id === firstDuplicate[0])?.name ?? "",
        })
      : null;

  return (
    <div className="flex min-h-0 min-w-0 flex-1 flex-col overflow-auto bg-background text-foreground">
      <div className="mx-auto flex w-full max-w-[1400px] flex-col gap-4 px-4 py-5 lg:flex-row lg:items-start lg:px-6">
        <section
          aria-label={b.aria}
          className="min-w-0 flex-1 overflow-hidden rounded-xl border border-line bg-popover"
        >
          <div className="flex flex-wrap items-center gap-x-2.5 gap-y-2 border-b border-line px-4 py-3.5 sm:px-5 xl:flex-nowrap">
            <button
              type="button"
              onClick={props.onBack}
              className={cn(BUTTON_CLASS.ghost, "h-7 px-1.5")}
            >
              <ChevronLeft aria-hidden="true" />
              {copy.common.back}
            </button>
            <span
              aria-hidden="true"
              className="hidden h-5 w-px bg-line sm:block"
            />
            <div className="min-w-[160px] flex-1 sm:max-w-[300px]">
              <input
                value={title}
                disabled={closed}
                maxLength={200}
                aria-label={b.titleLabel}
                aria-invalid={titleError || undefined}
                onChange={(e) => {
                  setTitle(e.target.value);
                  setTitleError(false);
                }}
                onBlur={() => void saveDetails()}
                className={cn(INPUT_CLASS, "font-semibold")}
              />
            </div>
            <FormStatusChip status={form.status} copy={copy} />
            <span className={cn(HINT_CLASS, "min-w-0 flex-1 truncate")}>
              {props.savedAt
                ? fillFormsCopy(b.savedAt, { time: props.savedAt })
                : ""}
            </span>
            <button
              type="button"
              onClick={() => props.onPreview(true)}
              className={BUTTON_CLASS.outline}
            >
              {b.preview}
            </button>
            {form.status === "draft" ? (
              <button
                type="button"
                disabled={blocked}
                onClick={() => void publish()}
                className={BUTTON_CLASS.primary}
              >
                {b.publish}
              </button>
            ) : null}
            {form.status === "published" ? (
              <button
                type="button"
                onClick={() => {
                  void props.onClose().then((r) => {
                    if (!r.ok)
                      setProblem({
                        kind: "save",
                        retry: () => void props.onClose(),
                      });
                  });
                }}
                className={BUTTON_CLASS.outline}
              >
                {b.close}
              </button>
            ) : null}
          </div>
          {titleError ? (
            <p
              role="alert"
              className="m-0 px-5 pt-2 text-crumb text-danger-text"
            >
              {b.titleRequired}
            </p>
          ) : null}

          <div className="flex flex-col gap-4 p-4 sm:p-5">
            {form.status === "published" ? (
              <div className="flex flex-wrap items-center gap-3 rounded-[10px] border border-line bg-success-subtle/40 p-3">
                <span className="min-w-[200px] flex-1">
                  <b className="block text-item-ui font-semibold">
                    {justPublished ? b.justPublished : b.publishedTitle}
                  </b>
                  <span className={HINT_CLASS}>{b.publishedNote}</span>
                </span>
                <input
                  readOnly
                  value={props.linkUrl}
                  aria-label={b.linkLabel}
                  className={cn(INPUT_CLASS, "min-w-0 flex-[2]")}
                />
                <button
                  type="button"
                  onClick={() => void copyLink()}
                  className={BUTTON_CLASS.primary}
                >
                  {copied ? b.copied : b.copy}
                </button>
              </div>
            ) : null}
            {closed ? (
              <div className="flex items-start gap-2 rounded-[10px] border border-line bg-background p-3 text-item-small text-muted-foreground">
                <Info aria-hidden="true" className="mt-0.5 size-3.5 shrink-0" />
                <span>{b.closedNote}</span>
              </div>
            ) : null}
            {problem?.kind === "conflict" ? (
              <div
                role="alert"
                className="flex flex-col gap-2 rounded-[10px] bg-warning-subtle p-3 text-item-small"
              >
                <span className="flex items-start gap-2">
                  <TriangleAlert
                    aria-hidden="true"
                    className="mt-0.5 size-3.5 shrink-0 text-warning-text"
                  />
                  {b.conflict}
                </span>
                <button
                  type="button"
                  onClick={() => {
                    setProblem(null);
                    props.onReload();
                  }}
                  className={cn(BUTTON_CLASS.outline, "h-7 self-start")}
                >
                  {b.reload}
                </button>
              </div>
            ) : problem ? (
              <FormAlert
                message={
                  problem.kind === "publish" ? b.publishFailed : b.saveFailed
                }
                onRetry={problem.retry}
                retryLabel={copy.common.retry}
              />
            ) : null}

            <div className="flex flex-col gap-1.5">
              <label htmlFor={`${baseId}-d`} className={FIELD_LABEL_CLASS}>
                {b.description}
              </label>
              <textarea
                id={`${baseId}-d`}
                value={description}
                disabled={closed}
                maxLength={2000}
                onChange={(e) => setDescription(e.target.value)}
                onBlur={() => void saveDetails()}
                className={cn(TEXTAREA_CLASS, "h-16")}
              />
            </div>

            <div className="flex items-center gap-2">
              <h2 className="m-0 flex-1 text-[16px] font-[650]">
                {b.questions}{" "}
                <span className="font-medium text-meta-foreground">
                  {fillFormsCopy(b.questionsCount, { count: questions.length })}
                </span>
              </h2>
              {closed ? null : (
                <button
                  type="button"
                  aria-expanded={adding}
                  onClick={() => setAdding((on) => !on)}
                  className={cn(BUTTON_CLASS.outline, "h-7")}
                >
                  <Plus aria-hidden="true" />
                  {b.addQuestion}
                </button>
              )}
            </div>

            {adding ? (
              <AddMenu
                copy={copy}
                taken={new Set(questions.map((q) => q.key))}
                onAdd={(input) => void add(input)}
              />
            ) : null}

            {blockedMessage && form.status === "draft" ? (
              <p
                role="status"
                className="m-0 flex items-start gap-1.5 text-crumb text-danger-text"
              >
                <TriangleAlert
                  aria-hidden="true"
                  className="mt-0.5 size-3.5 shrink-0"
                />
                {blockedMessage}
              </p>
            ) : null}

            {noQuestions ? (
              <p className="m-0 rounded-[10px] border border-dashed border-line-strong px-4 py-5 text-center text-item-small text-meta-foreground">
                {b.emptyList}
              </p>
            ) : (
              <ul
                aria-label={b.list}
                className="m-0 flex list-none flex-col gap-2 p-0"
                onDragLeave={() => setDropIndex(null)}
              >
                {questions.map((q, index) => (
                  <li key={q.id} className="contents">
                    {dragId && dropIndex === index ? (
                      <div
                        aria-hidden="true"
                        className="-my-1 h-[3px] rounded-sm bg-primary"
                      />
                    ) : null}
                    <QuestionRow
                      copy={copy}
                      q={q}
                      n={index + 1}
                      first={index === 0}
                      last={index === questions.length - 1}
                      selected={q.id === props.selectedId}
                      duplicate={duplicateIds.has(q.id)}
                      readOnly={closed}
                      dragging={dragId === q.id}
                      target={targetName(q, fields, copy)}
                      onSelect={() =>
                        props.onSelect(q.id === props.selectedId ? null : q.id)
                      }
                      onMove={(to) => void move(q, to)}
                      onDragStart={() => setDragId(q.id)}
                      onDragEnd={() => {
                        setDragId(null);
                        setDropIndex(null);
                      }}
                      onDragOver={() =>
                        dragId && dragId !== q.id && setDropIndex(index)
                      }
                      onDrop={() => {
                        const from = questions.findIndex(
                          (entry) => entry.id === dragId,
                        );
                        const dragged = questions[from];
                        setDragId(null);
                        setDropIndex(null);
                        if (!dragged || from === index) return;
                        void move(dragged, index);
                      }}
                    />
                  </li>
                ))}
              </ul>
            )}
          </div>
        </section>

        {selected ? (
          <QuestionPanel
            key={selected.id}
            copy={copy}
            q={selected}
            n={questions.findIndex((q) => q.id === selected.id) + 1}
            fields={fields}
            questions={questions}
            readOnly={closed}
            onSave={async (changes) =>
              settle(
                await props.onUpdateQuestion(selected, changes),
                () => void props.onUpdateQuestion(selected, changes),
              )
            }
            onRemove={async () => {
              const result = await props.onRemoveQuestion(selected);
              if (settle(result, () => void props.onRemoveQuestion(selected)))
                props.onSelect(null);
            }}
          />
        ) : (
          <aside
            aria-label={b.panel}
            className="hidden w-[380px] shrink-0 rounded-xl border border-dashed border-line-strong p-5 text-item-small text-meta-foreground lg:block"
          >
            {b.pickHere}
          </aside>
        )}
      </div>
    </div>
  );
}

/** What an update of a question sends: the question as it is, with the change. */
function changesOf(
  q: FormQuestion,
  patch: Partial<FormQuestionChanges>,
): FormQuestionChanges {
  return {
    label: q.label,
    required: q.required,
    boardFieldId: q.boardFieldId,
    position: q.position,
    placeholder: q.placeholder,
    helpText: q.helpText,
    maxLength: q.maxLength,
    minValue: q.minValue,
    maxValue: q.maxValue,
    options: q.options,
    ...patch,
  };
}

function AddMenu({
  copy,
  taken,
  onAdd,
}: {
  copy: FormsCopy;
  taken: ReadonlySet<string>;
  onAdd: (input: { type: FormQuestionType; target?: BuiltInTarget }) => void;
}) {
  const b = copy.builder;
  const unsupported: FormQuestionType[] = ["file", "signature"];
  const targets: BuiltInTarget[] = ["title", "description", "due"];
  return (
    <div className="flex flex-col gap-3 rounded-[10px] border border-line bg-background p-3">
      <fieldset className="m-0 flex flex-wrap gap-1.5 border-0 p-0">
        <legend className={cn(FIELD_LABEL_CLASS, "mb-1.5")}>
          {b.addTypes}
        </legend>
        {FORM_SUPPORTED_QUESTION_TYPES.map((type) => (
          <button
            key={type}
            type="button"
            onClick={() => onAdd({ type })}
            className={cn(BUTTON_CLASS.outline, "h-7")}
          >
            {b.types[type]}
          </button>
        ))}
        {unsupported.map((type) => (
          <button
            key={type}
            type="button"
            aria-disabled="true"
            onClick={(e) => e.preventDefault()}
            className={cn(BUTTON_CLASS.outline, "h-7 opacity-60")}
          >
            {b.types[type]} <Chip tone="neutral">{b.soon}</Chip>
          </button>
        ))}
      </fieldset>
      <fieldset className="m-0 flex flex-wrap gap-1.5 border-0 p-0">
        <legend className={cn(FIELD_LABEL_CLASS, "mb-1.5")}>{b.addItem}</legend>
        {targets.map((target) => (
          <button
            key={target}
            type="button"
            disabled={taken.has(BUILT_IN_KEY[target])}
            onClick={() => onAdd({ type: BUILT_IN_TYPE[target], target })}
            className={cn(BUTTON_CLASS.outline, "h-7")}
          >
            {target === "title"
              ? b.mapTitle
              : target === "description"
                ? b.mapDescription
                : b.mapDue}
          </button>
        ))}
      </fieldset>
      <span className={HINT_CLASS}>{b.unsupportedHint}</span>
    </div>
  );
}

function QuestionRow({
  copy,
  q,
  n,
  first,
  last,
  selected,
  duplicate,
  readOnly,
  dragging,
  target,
  onSelect,
  onMove,
  onDragStart,
  onDragEnd,
  onDragOver,
  onDrop,
}: {
  copy: FormsCopy;
  q: FormQuestion;
  n: number;
  first: boolean;
  last: boolean;
  selected: boolean;
  duplicate: boolean;
  readOnly: boolean;
  dragging: boolean;
  target: string | null;
  onSelect: () => void;
  onMove: (toIndex: number) => void;
  onDragStart: () => void;
  onDragEnd: () => void;
  onDragOver: () => void;
  onDrop: () => void;
}) {
  const b = copy.builder;
  const name = q.label || b.unnamed;
  return (
    <div
      draggable={!readOnly}
      onDragStart={(e) => {
        e.dataTransfer.effectAllowed = "move";
        onDragStart();
      }}
      onDragEnd={onDragEnd}
      onDragOver={(e) => {
        e.preventDefault();
        onDragOver();
      }}
      onDrop={(e) => {
        e.preventDefault();
        onDrop();
      }}
      className={cn(
        "grid grid-cols-[22px_28px_minmax(0,1fr)_auto] items-center gap-2 rounded-[10px] border bg-popover px-3 py-2.5",
        selected
          ? "border-primary bg-brand-subtle/50 shadow-[0_0_0_1px_var(--primary)]"
          : "border-line",
        dragging && "rotate-[-0.4deg] border-primary shadow-popover",
      )}
    >
      <span
        aria-hidden="true"
        className="grid cursor-grab place-items-center text-meta-foreground"
        title={fillFormsCopy(b.drag, { name })}
      >
        <GripVertical className="size-4" />
      </span>
      <span className="grid size-6 place-items-center rounded-md bg-state-hover-strong text-crumb font-semibold text-muted-foreground">
        {n}
      </span>
      <button
        type="button"
        onClick={onSelect}
        aria-pressed={selected}
        title={name}
        className="flex min-w-0 cursor-pointer flex-col border-0 bg-transparent p-0 text-left outline-none focus-visible:ring-2 focus-visible:ring-focus-ring"
      >
        <b className="truncate text-item-ui font-semibold">{name}</b>
        <small className="truncate text-metadata text-meta-foreground">
          {b.types[q.type]} ·{" "}
          {target ? fillFormsCopy(b.mappedTo, { field: target }) : b.noMapping}
        </small>
      </button>
      <span className="flex items-center gap-1">
        {q.required ? <Chip tone="brand">{b.required}</Chip> : null}
        {duplicate ? <Chip tone="danger">{b.duplicate}</Chip> : null}
        {readOnly ? null : (
          <>
            <button
              type="button"
              aria-label={fillFormsCopy(b.moveUp, { name })}
              aria-disabled={first || undefined}
              onClick={() => !first && onMove(n - 2)}
              className={ICON_BUTTON_CLASS}
            >
              <ArrowUp aria-hidden="true" />
            </button>
            <button
              type="button"
              aria-label={fillFormsCopy(b.moveDown, { name })}
              aria-disabled={last || undefined}
              onClick={() => !last && onMove(n)}
              className={ICON_BUTTON_CLASS}
            >
              <ArrowDown aria-hidden="true" />
            </button>
          </>
        )}
      </span>
    </div>
  );
}

function QuestionPanel({
  copy,
  q,
  n,
  fields,
  questions,
  readOnly,
  onSave,
  onRemove,
}: {
  copy: FormsCopy;
  q: FormQuestion;
  n: number;
  fields: readonly FormBoardField[];
  questions: readonly FormQuestion[];
  readOnly: boolean;
  onSave: (changes: FormQuestionChanges) => Promise<boolean>;
  onRemove: () => Promise<void>;
}) {
  const b = copy.builder;
  const id = useId();
  const [label, setLabel] = useState(q.label);
  const [labelError, setLabelError] = useState(false);
  const [placeholder, setPlaceholder] = useState(q.placeholder ?? "");
  const [maxLength, setMaxLength] = useState(q.maxLength?.toString() ?? "");
  const [minValue, setMinValue] = useState(q.minValue?.toString() ?? "");
  const [maxValue, setMaxValue] = useState(q.maxValue?.toString() ?? "");
  const [options, setOptions] = useState<readonly FormQuestionOption[]>(
    q.options,
  );
  const [confirm, setConfirm] = useState(false);
  const built = builtInTarget(q);
  const mapped = q.boardFieldId
    ? fields.find((f) => f.id === q.boardFieldId)
    : undefined;
  const choice = q.type === "select" || q.type === "multiselect";
  const taken = new Set(
    questions
      .filter((other) => other.id !== q.id && other.boardFieldId)
      .map((o) => o.boardFieldId),
  );
  const candidates = fields.filter((f) =>
    compatibleFieldTypes(q.type).includes(f.type),
  );
  const duplicateWith = q.boardFieldId
    ? questions.find((o) => o.id !== q.id && o.boardFieldId === q.boardFieldId)
    : undefined;
  const int = (text: string) =>
    text.trim() === "" || Number.isNaN(Number(text))
      ? null
      : Math.trunc(Number(text));

  const save = (patch: Partial<FormQuestionChanges>) =>
    onSave({
      label: q.label,
      required: q.required,
      boardFieldId: q.boardFieldId,
      position: q.position,
      placeholder: q.placeholder,
      helpText: q.helpText,
      maxLength: q.maxLength,
      minValue: q.minValue,
      maxValue: q.maxValue,
      options: q.options,
      ...patch,
    });

  function saveOptions(next: readonly FormQuestionOption[]) {
    setOptions(next);
    if (next.every((o) => o.label.trim() !== ""))
      void save({
        options: next.map((o) => ({ key: o.key, label: o.label.trim() })),
      });
  }

  return (
    <aside
      aria-label={b.panel}
      className="flex w-full shrink-0 flex-col gap-3.5 rounded-xl border border-line bg-popover p-[18px] lg:w-[380px]"
    >
      <div className="flex items-center gap-2">
        <h2 className="m-0 flex-1 text-[16px] font-[650]">
          {fillFormsCopy(b.panelTitle, { n })}
        </h2>
        <span className={HINT_CLASS}>{b.autosave}</span>
      </div>

      <div className="flex flex-col gap-1.5">
        <label htmlFor={`${id}-l`} className={FIELD_LABEL_CLASS}>
          {b.label}
        </label>
        <input
          id={`${id}-l`}
          value={label}
          disabled={readOnly}
          maxLength={500}
          aria-invalid={labelError || undefined}
          onChange={(e) => {
            setLabel(e.target.value);
            setLabelError(false);
          }}
          onBlur={() => {
            if (label.trim() === "") return setLabelError(true);
            if (label.trim() !== q.label) void save({ label: label.trim() });
          }}
          className={INPUT_CLASS}
        />
        {labelError ? (
          <span
            role="alert"
            className="flex items-start gap-1.5 text-crumb text-danger-text"
          >
            <TriangleAlert
              aria-hidden="true"
              className="mt-0.5 size-3.5 shrink-0"
            />
            {b.labelRequired}
          </span>
        ) : null}
      </div>

      <div className="flex flex-col gap-1.5">
        <span className={FIELD_LABEL_CLASS}>{b.type}</span>
        <div
          className={cn(INPUT_CLASS, "justify-between text-muted-foreground")}
          title={b.typeFixed}
        >
          {b.types[q.type]}
        </div>
      </div>

      <div className="flex flex-col gap-1.5">
        <label htmlFor={`${id}-m`} className={FIELD_LABEL_CLASS}>
          {b.mapping}
        </label>
        {built ? (
          <>
            <div className={cn(INPUT_CLASS, "text-muted-foreground")}>
              {built === "title"
                ? b.mapTitle
                : built === "description"
                  ? b.mapDescription
                  : b.mapDue}
            </div>
            <span className={HINT_CLASS}>
              {fillFormsCopy(b.mapFixed, {
                field:
                  built === "title"
                    ? b.mapTitle
                    : built === "description"
                      ? b.mapDescription
                      : b.mapDue,
              })}
            </span>
          </>
        ) : (
          <>
            <select
              id={`${id}-m`}
              value={q.boardFieldId ?? ""}
              disabled={readOnly}
              aria-invalid={duplicateWith ? true : undefined}
              onChange={(e) =>
                void save({ boardFieldId: e.target.value || null })
              }
              className={INPUT_CLASS}
            >
              <option value="">{b.noMap}</option>
              {candidates.map((f) => (
                <option key={f.id} value={f.id}>
                  {f.name}
                  {taken.has(f.id) ? " •" : ""}
                </option>
              ))}
            </select>
            <span className={HINT_CLASS}>{b.mappingHint}</span>
          </>
        )}
        {duplicateWith ? (
          <span
            role="alert"
            className="flex items-start gap-1.5 text-crumb text-danger-text"
          >
            <TriangleAlert
              aria-hidden="true"
              className="mt-0.5 size-3.5 shrink-0"
            />
            {fillFormsCopy(b.duplicateError, {
              n: questions.findIndex((o) => o.id === duplicateWith.id) + 1,
              name: duplicateWith.label,
              field: mapped?.name ?? "",
            })}
          </span>
        ) : null}
      </div>

      <div className="flex items-center gap-2.5">
        <button
          type="button"
          role="switch"
          aria-checked={q.required}
          aria-label={b.requiredSwitch}
          disabled={readOnly}
          onClick={() => void save({ required: !q.required })}
          className={cn(
            "relative h-5 w-[34px] shrink-0 cursor-pointer rounded-full border-0 outline-none focus-visible:ring-2 focus-visible:ring-focus-ring",
            q.required ? "bg-primary" : "bg-line-strong",
          )}
        >
          <span
            aria-hidden="true"
            className={cn(
              "absolute top-0.5 size-4 rounded-full bg-white transition-[left]",
              q.required ? "left-4" : "left-0.5",
            )}
          />
        </button>
        <span className="text-item-ui">{b.requiredSwitch}</span>
      </div>

      {q.type === "text" ||
      q.type === "longtext" ||
      q.type === "email" ||
      q.type === "phone" ||
      q.type === "number" ? (
        <div className="flex flex-col gap-1.5">
          <label htmlFor={`${id}-p`} className={FIELD_LABEL_CLASS}>
            {q.type === "email" ? b.placeholderShort : b.placeholder}
          </label>
          <input
            id={`${id}-p`}
            value={placeholder}
            disabled={readOnly}
            onChange={(e) => setPlaceholder(e.target.value)}
            onBlur={() =>
              placeholder !== (q.placeholder ?? "") &&
              void save({ placeholder: placeholder.trim() || null })
            }
            className={INPUT_CLASS}
          />
          {q.type === "email" ? (
            <span className={HINT_CLASS}>{b.emailHint}</span>
          ) : null}
        </div>
      ) : null}

      {q.type === "text" || q.type === "longtext" ? (
        <div className="flex flex-col gap-1.5">
          <label htmlFor={`${id}-x`} className={FIELD_LABEL_CLASS}>
            {b.maxLength}
          </label>
          <input
            id={`${id}-x`}
            inputMode="numeric"
            value={maxLength}
            disabled={readOnly}
            onChange={(e) => setMaxLength(e.target.value.replace(/\D/g, ""))}
            onBlur={() =>
              int(maxLength) !== q.maxLength &&
              void save({ maxLength: int(maxLength) })
            }
            className={cn(INPUT_CLASS, "w-[120px]")}
          />
        </div>
      ) : null}

      {q.type === "number" ? (
        <div className="flex gap-2.5">
          <div className="flex flex-1 flex-col gap-1.5">
            <label htmlFor={`${id}-n`} className={FIELD_LABEL_CLASS}>
              {b.minValue}
            </label>
            <input
              id={`${id}-n`}
              inputMode="numeric"
              value={minValue}
              disabled={readOnly}
              onChange={(e) => setMinValue(e.target.value)}
              onBlur={() =>
                int(minValue) !== q.minValue &&
                void save({ minValue: int(minValue) })
              }
              className={INPUT_CLASS}
            />
          </div>
          <div className="flex flex-1 flex-col gap-1.5">
            <label htmlFor={`${id}-v`} className={FIELD_LABEL_CLASS}>
              {b.maxValue}
            </label>
            <input
              id={`${id}-v`}
              inputMode="numeric"
              value={maxValue}
              disabled={readOnly}
              onChange={(e) => setMaxValue(e.target.value)}
              onBlur={() =>
                int(maxValue) !== q.maxValue &&
                void save({ maxValue: int(maxValue) })
              }
              className={INPUT_CLASS}
            />
          </div>
        </div>
      ) : null}

      {choice && mapped ? (
        <div
          data-testid="mapped-options"
          className="flex items-start gap-2 rounded-lg border border-line bg-background px-3 py-2.5 text-crumb text-muted-foreground"
        >
          <Info aria-hidden="true" className="mt-0.5 size-3.5 shrink-0" />
          <span>
            {fillFormsCopy(b.mappedOptions, {
              field: mapped.name,
              options: mapped.options.map((o) => o.name).join(", "),
            })}
          </span>
        </div>
      ) : null}

      {choice && !mapped ? (
        <div className="flex flex-col gap-1.5">
          <span className={FIELD_LABEL_CLASS}>{b.options}</span>
          <div className="flex flex-col gap-1.5">
            {options.map((option, index) => (
              <div key={option.key} className="flex items-center gap-1">
                <input
                  value={option.label}
                  disabled={readOnly}
                  aria-label={fillFormsCopy(b.optionN, { n: index + 1 })}
                  onChange={(e) =>
                    setOptions(
                      options.map((o) =>
                        o.key === option.key
                          ? { ...o, label: e.target.value }
                          : o,
                      ),
                    )
                  }
                  onBlur={() => saveOptions(options)}
                  className={INPUT_CLASS}
                />
                {readOnly ? null : (
                  <>
                    <button
                      type="button"
                      aria-label={fillFormsCopy(b.moveOptionUp, {
                        name: option.label,
                      })}
                      aria-disabled={index === 0 || undefined}
                      onClick={() => {
                        if (index === 0) return;
                        const next = [...options];
                        [next[index - 1], next[index]] = [
                          next[index]!,
                          next[index - 1]!,
                        ];
                        saveOptions(next);
                      }}
                      className={ICON_BUTTON_CLASS}
                    >
                      <ArrowUp aria-hidden="true" />
                    </button>
                    <button
                      type="button"
                      aria-label={fillFormsCopy(b.moveOptionDown, {
                        name: option.label,
                      })}
                      aria-disabled={index === options.length - 1 || undefined}
                      onClick={() => {
                        if (index === options.length - 1) return;
                        const next = [...options];
                        [next[index + 1], next[index]] = [
                          next[index]!,
                          next[index + 1]!,
                        ];
                        saveOptions(next);
                      }}
                      className={ICON_BUTTON_CLASS}
                    >
                      <ArrowDown aria-hidden="true" />
                    </button>
                    <button
                      type="button"
                      aria-label={fillFormsCopy(b.removeOption, {
                        name: option.label,
                      })}
                      onClick={() =>
                        saveOptions(options.filter((o) => o.key !== option.key))
                      }
                      className={ICON_BUTTON_CLASS}
                    >
                      <X aria-hidden="true" />
                    </button>
                  </>
                )}
              </div>
            ))}
            {readOnly || options.length >= MAX_OPTIONS ? null : (
              <button
                type="button"
                onClick={() =>
                  setOptions([...options, { key: newKey("o"), label: "" }])
                }
                className={cn(BUTTON_CLASS.outline, "h-7 self-start")}
              >
                <Plus aria-hidden="true" />
                {b.addOption}
              </button>
            )}
          </div>
          <span className={HINT_CLASS}>
            {fillFormsCopy(b.optionsHint, { max: MAX_OPTIONS })}
          </span>
        </div>
      ) : null}

      {readOnly ? null : confirm ? (
        <div
          role="alertdialog"
          aria-label={b.removeQuestion}
          className="flex flex-col gap-2 rounded-[10px] border border-line-strong p-3"
        >
          <p className="m-0 text-item-small">
            {fillFormsCopy(b.removeAsk, { name: q.label })}
          </p>
          <div className="flex justify-end gap-2">
            <button
              type="button"
              autoFocus
              onClick={() => setConfirm(false)}
              className={cn(BUTTON_CLASS.outline, "h-7")}
            >
              {copy.common.keep}
            </button>
            <button
              type="button"
              onClick={() => void onRemove()}
              className={cn(BUTTON_CLASS.danger, "h-7")}
            >
              {b.remove}
            </button>
          </div>
        </div>
      ) : (
        <div className="mt-1 flex justify-end">
          <button
            type="button"
            onClick={() => setConfirm(true)}
            className={cn(BUTTON_CLASS.ghostDanger, "h-7")}
          >
            {b.removeQuestion}
          </button>
        </div>
      )}
    </aside>
  );
}

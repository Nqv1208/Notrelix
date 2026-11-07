import { useEffect, useId, useRef, useState } from "react";
import { AlertCircle, Info } from "lucide-react";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogTitle,
  cn,
} from "@notrelix/ui-web";
import type {
  BoardSettingsComputedDto,
  BoardSettingsFieldDto,
  BoardSettingsWriteResult,
  FormulaPreviewDraft,
  FormulaPreviewDto,
} from "@notrelix/work-management-core";
import type { BoardSettingsCopy } from "./board-settings.copy";
import { useSettings } from "./board-settings-context";
import { fillCopy } from "./board-settings-format";
import { BUTTON_CLASS } from "./board-settings-parts";

type RollupFunction = NonNullable<BoardSettingsComputedDto["rollupFunction"]>;
const ROLLUP_FUNCTIONS: readonly RollupFunction[] = [
  "count",
  "sum",
  "avg",
  "min",
  "max",
];

/** The most elements a formula may have; the server enforces it, this only warns early. */
export const FORMULA_NODE_CAP = 200;

const isDigit = (ch: string) => ch >= "0" && ch <= "9";
const isLetter = (ch: string) => /[A-Za-z]/.test(ch);

/**
 * An estimate of the elements of a formula: references, literals, calls and operators.
 * One pass over the text (no backtracking regex), so a long or unclosed input stays linear.
 */
export function formulaNodeCount(expression: string): number {
  const text = expression;
  let count = 0;
  let i = 0;
  while (i < text.length) {
    const ch = text.charAt(i);
    if (ch === "[") {
      const end = text.indexOf("]", i + 1);
      if (end === -1) break;
      count++;
      i = end + 1;
    } else if (ch === '"') {
      let j = i + 1;
      while (
        j < text.length &&
        !(text.charAt(j) === '"' && text.charAt(j + 1) !== '"')
      ) {
        j += text.charAt(j) === '"' ? 2 : 1;
      }
      if (j >= text.length) break;
      count++;
      i = j + 1;
    } else if (isDigit(ch)) {
      while (i < text.length && isDigit(text.charAt(i))) i++;
      if (text.charAt(i) === "." && isDigit(text.charAt(i + 1))) {
        i++;
        while (i < text.length && isDigit(text.charAt(i))) i++;
      }
      count++;
    } else if (isLetter(ch)) {
      const start = i;
      while (i < text.length && isLetter(text.charAt(i))) i++;
      const word = text.slice(start, i).toUpperCase();
      let k = i;
      while (k < text.length && /\s/.test(text.charAt(k))) k++;
      if (text.charAt(k) === "(" || word === "TRUE" || word === "FALSE")
        count++;
    } else if (["<>", "<=", ">="].includes(text.slice(i, i + 2))) {
      count++;
      i += 2;
    } else {
      if ("-+*/&=<>".includes(ch)) count++;
      i++;
    }
  }
  return count;
}

/** The settings JSON a Formula saves; the server turns the names into field ids. */
export function formulaSettings(expression: string): string {
  return JSON.stringify({ expression: expression.trim() });
}

/** The settings JSON a Rollup saves: `count` needs no field. */
export function rollupSettings(
  fn: RollupFunction,
  fieldId: string | null,
): string {
  return JSON.stringify(
    fn === "count" || !fieldId ? { function: fn } : { function: fn, fieldId },
  );
}

/** "Xem thử với ERP-145:" with the detail inside the colon's reach: "Xem thử với ERP-145 (Điểm = 13):". */
function previewLabel(label: string, detail: string | null): string {
  return detail === null ? label : label.replace(/:$/, ` (${detail}):`);
}

/**
 * What the preview read, as the artboard words it after the key: "Điểm = 13" for a formula, "2 việc con: 3 và 4"
 * for a rollup (up to five values, then an ellipsis), "chưa có việc con" when there are none; a count names no value.
 */
export function previewDetail(
  t: BoardSettingsCopy["computedDialog"],
  kind: "formula" | "rollup",
  preview: FormulaPreviewDto,
): string | null {
  if (preview.state === "error") return null;
  const dash = "—";
  if (kind === "formula") {
    return preview.inputs.length === 0
      ? null
      : preview.inputs
          .map((input) => `${input.name} = ${input.value ?? dash}`)
          .join(", ");
  }
  const count = preview.subItemCount;
  if (count === null) return null;
  if (count === 0) return t.previewNoSubItems;
  if (preview.inputs.length === 0) return null;
  const values = preview.inputs.map((input) => input.value ?? dash);
  const list =
    values.length === 1
      ? (values[0] ?? dash)
      : count > values.length
        ? `${values.join(", ")}, …`
        : `${values.slice(0, -1).join(", ")} ${t.previewAnd} ${values.at(-1) ?? dash}`;
  return fillCopy(t.previewSubItems, { count: String(count), values: list });
}

/**
 * Creates or edits a Formula or Rollup field. It is evaluated by the server
 * on every board read; this dialog only writes the definition. A refusal
 * keeps what was typed and says why, by the server's rule code; a transport
 * failure offers Retry.
 */
export function ComputedFieldDialog({
  kind,
  field,
  onClose,
}: {
  kind: "formula" | "rollup";
  /** Present when an existing field is edited. */
  field?: BoardSettingsFieldDto;
  onClose: () => void;
}) {
  const { copy, settings, actions } = useSettings();
  const t = copy.computedDialog;
  const nameId = useId();
  const exprId = useId();
  const errorId = useId();
  const area = useRef<HTMLTextAreaElement>(null);
  const numberFields = settings.fields.filter((f) => f.type === "number");
  const boardFields = settings.fields.filter((f) => f.id !== field?.id);

  const [name, setName] = useState(field?.name ?? "");
  const [expression, setExpression] = useState(
    field?.computed?.expression ?? "",
  );
  const [fn, setFn] = useState<RollupFunction>(
    field?.computed?.rollupFunction ?? "count",
  );
  const [source, setSource] = useState<string | null>(
    field?.computed?.sourceFieldId ?? numberFields[0]?.id ?? null,
  );
  const [working, setWorking] = useState(false);
  const [failure, setFailure] = useState<Extract<
    BoardSettingsWriteResult,
    { ok: false }
  > | null>(null);

  // "Xem thử": the draft is evaluated by the server on one item once typing pauses; nothing is saved.
  const previewItem = settings.previewItem;
  const [preview, setPreview] = useState<FormulaPreviewDto | null | "checking">(
    null,
  );
  useEffect(() => {
    const draft: FormulaPreviewDraft | null =
      kind === "formula"
        ? expression.trim() === ""
          ? null
          : { expression: expression.trim() }
        : fn !== "count" && !source
          ? null
          : {
              rollupFunction: fn,
              rollupFieldId: fn === "count" ? null : source,
            };
    if (!previewItem || !draft) {
      setPreview(null);
      return;
    }
    let current = true;
    setPreview("checking");
    const timer = setTimeout(() => {
      void actions.previewFormula(previewItem.id, draft).then(
        (result) => {
          if (current) setPreview(result);
        },
        () => {
          if (current) setPreview(null);
        },
      );
    }, 400);
    return () => {
      current = false;
      clearTimeout(timer);
    };
  }, [actions, expression, fn, kind, previewItem, source]);

  const nodes = formulaNodeCount(expression);
  const tooBig = kind === "formula" && nodes > FORMULA_NODE_CAP;
  const needsSource = kind === "rollup" && fn !== "count";
  const noNumber = needsSource && numberFields.length === 0;
  const valid =
    name.trim() !== "" &&
    !tooBig &&
    (kind === "formula"
      ? expression.trim() !== ""
      : !needsSource || source !== null);

  function insert(fieldName: string) {
    const el = area.current;
    const at = el?.selectionStart ?? expression.length;
    const end = el?.selectionEnd ?? at;
    setExpression(
      `${expression.slice(0, at)}[${fieldName}]${expression.slice(end)}`,
    );
    el?.focus();
  }

  async function save() {
    if (!valid || working) return;
    setWorking(true);
    setFailure(null);
    const definition =
      kind === "formula"
        ? formulaSettings(expression)
        : rollupSettings(fn, source);
    const result = field
      ? await actions.updateField(field.id, {
          name: name.trim(),
          settings: definition,
        })
      : await actions.createField(kind, {
          name: name.trim(),
          settings: definition,
        });
    setWorking(false);
    if (result.ok) onClose();
    else setFailure(result);
  }

  const message = !failure
    ? null
    : failure.reason === "conflict"
      ? t.conflict
      : failure.reason === "invalid"
        ? ((failure.code ? t.errors[failure.code] : undefined) ??
          t.unknownError)
        : t.failed;

  const previewNode =
    previewItem && preview !== null ? (
      <>
        <div
          role="status"
          aria-live="polite"
          data-preview-state={
            preview === "checking" ? "checking" : preview.state
          }
          className="flex items-center gap-2 rounded-[8px] bg-state-hover px-2.5 py-2 text-[13px] text-muted-foreground"
        >
          <Info aria-hidden="true" className="size-4 flex-none max-sm:hidden" />
          <span className="max-sm:hidden">
            {previewLabel(
              fillCopy(t.preview, { key: previewItem.key }),
              preview === "checking" ? null : previewDetail(t, kind, preview),
            )}
          </span>
          <span className="sm:hidden">
            {kind === "rollup" &&
            preview !== "checking" &&
            preview.subItemCount !== null
              ? fillCopy(t.previewSubItemsShort, {
                  count: String(preview.subItemCount),
                })
              : fillCopy(t.previewShort, { key: previewItem.key })}
          </span>
          <span
            className={cn(
              "min-w-0 truncate text-[14px] font-bold text-foreground tabular-nums max-sm:order-first",
              preview !== "checking" &&
                preview.state === "error" &&
                "text-danger-text",
            )}
          >
            {preview === "checking"
              ? t.previewChecking
              : preview.state === "ok"
                ? preview.value
                : preview.state === "empty"
                  ? kind === "rollup"
                    ? "—"
                    : t.previewEmpty
                  : (t.previewErrors[preview.errorCode ?? ""] ??
                    t.previewError)}
          </span>
        </div>
        {kind === "rollup" &&
        preview !== "checking" &&
        preview.subItemCount === 0 ? (
          <p className="m-0 text-[12.5px] text-meta-foreground max-sm:hidden">
            {t.previewNoSubItemsNote}
          </p>
        ) : null}
      </>
    ) : null;

  return (
    <Dialog open onOpenChange={(open) => (open ? undefined : onClose())}>
      <DialogContent
        showCloseButton={false}
        overlayClassName="bg-modal-scrim backdrop-blur-none"
        className="top-[90px] box-border flex max-h-[calc(100vh-120px)] w-[560px] max-w-[calc(100%-2rem)] translate-y-0 flex-col gap-0 overflow-auto rounded-[12px] border border-line bg-popover p-0 text-body leading-[1.45] font-normal shadow-dialog ring-0 max-sm:w-full max-sm:max-w-full max-sm:gap-2.5 max-sm:rounded-[18px] max-sm:p-3.5 sm:max-w-[calc(100%-2rem)] dark:bg-card"
      >
        <div className="flex items-center gap-2 border-b border-line px-5 py-4 max-sm:border-0 max-sm:p-0">
          <span
            aria-hidden="true"
            className="inline-flex h-[18px] items-center rounded-[5px] bg-state-hover-strong px-[5px] font-mono text-[11px] font-medium text-muted-foreground"
          >
            {kind === "formula" ? "ƒx" : "∑"}
          </span>
          <DialogTitle className="m-0 text-[15px] leading-[1.45] font-[650] text-foreground">
            {kind === "formula" ? t.formulaTitle : t.rollupTitle}
          </DialogTitle>
        </div>
        <DialogDescription className="sr-only">
          {kind === "formula" ? t.functions : t.subItemsOnly}
        </DialogDescription>

        <div className="flex flex-col gap-3 px-5 py-4 max-sm:gap-2.5 max-sm:p-0">
          <div>
            <label
              htmlFor={nameId}
              className="mb-1 block text-metadata leading-[1.45] font-semibold text-meta-foreground"
            >
              {t.name}
            </label>
            <input
              id={nameId}
              value={name}
              onChange={(event) => setName(event.target.value)}
              className="h-[34px] w-full rounded-md border border-line-strong bg-background px-2.5 text-[13px] text-foreground max-sm:h-11"
            />
          </div>

          {kind === "formula" ? (
            <>
              <div>
                <label
                  htmlFor={exprId}
                  className="mb-1 block text-metadata leading-[1.45] font-semibold text-meta-foreground"
                >
                  {t.expression}
                </label>
                <textarea
                  id={exprId}
                  ref={area}
                  rows={2}
                  value={expression}
                  placeholder={t.expressionHint}
                  aria-invalid={tooBig || undefined}
                  aria-describedby={message ? errorId : undefined}
                  onChange={(event) => setExpression(event.target.value)}
                  className={cn(
                    "block min-h-16 w-full rounded-[7px] border border-line-strong bg-background px-3 py-2.5 font-mono text-[13px] leading-[1.55] text-foreground",
                    (tooBig || (failure && failure.reason === "invalid")) &&
                      "border-danger",
                  )}
                />
                <p
                  className={cn(
                    "m-0 mt-3 text-right text-[12px] tabular-nums max-sm:hidden",
                    tooBig
                      ? "font-semibold text-danger-text"
                      : "text-meta-foreground",
                  )}
                >
                  {fillCopy(t.nodes, { count: String(nodes) })}
                </p>
              </div>
              {previewNode}
              <div className="max-sm:hidden">
                <span className="mb-1 block text-metadata leading-[1.45] font-semibold text-meta-foreground">
                  {t.insertField}
                </span>
                <div className="flex flex-wrap gap-1.5">
                  {boardFields.map((f) => (
                    <button
                      key={f.id}
                      type="button"
                      onClick={() => insert(f.name)}
                      className="h-5.5 rounded-sm border border-line-strong bg-popover px-2 text-[12px] font-semibold text-foreground hover:bg-state-hover-strong"
                    >
                      {f.name}
                    </button>
                  ))}
                </div>
              </div>
              <p className="m-0 text-[12.5px] text-meta-foreground max-sm:hidden">
                {t.functions}
              </p>
            </>
          ) : (
            <>
              <div>
                <span
                  id={`${exprId}-fn`}
                  className="mb-1 block text-metadata leading-[1.45] font-semibold text-meta-foreground max-sm:sr-only"
                >
                  {t.rollupFunction}
                </span>
                <div
                  role="radiogroup"
                  aria-labelledby={`${exprId}-fn`}
                  className="flex flex-wrap gap-1"
                >
                  {ROLLUP_FUNCTIONS.map((option) => (
                    <button
                      key={option}
                      type="button"
                      role="radio"
                      aria-checked={fn === option}
                      onClick={() => setFn(option)}
                      className={cn(
                        "h-8 rounded-[7px] border border-line-strong bg-popover px-3 text-item-small font-medium",
                        fn === option &&
                          "border-primary bg-row-selected font-semibold text-primary",
                      )}
                    >
                      {t.functionNames[option]}
                    </button>
                  ))}
                </div>
              </div>
              <div>
                <label
                  htmlFor={`${exprId}-src`}
                  className="mb-1 block text-metadata leading-[1.45] font-semibold text-meta-foreground max-sm:sr-only"
                >
                  {t.sourceField}
                </label>
                <select
                  id={`${exprId}-src`}
                  disabled={!needsSource || noNumber}
                  value={needsSource ? (source ?? "") : ""}
                  onChange={(event) => setSource(event.target.value || null)}
                  className="h-[34px] w-full rounded-[7px] border border-line-strong bg-background px-2.5 text-item-ui text-foreground disabled:bg-state-hover disabled:text-meta-foreground max-sm:h-11"
                >
                  {!needsSource ? (
                    <option value="">{t.noSourceNeeded}</option>
                  ) : null}
                  {numberFields.map((f) => (
                    <option key={f.id} value={f.id}>
                      {f.name}
                    </option>
                  ))}
                </select>
              </div>
              {noNumber ? (
                <p className="m-0 flex items-start gap-2 rounded-lg bg-warning-subtle px-2.5 py-2 text-item-small text-warning-text">
                  <AlertCircle
                    aria-hidden="true"
                    className="mt-0.5 size-3.5 shrink-0"
                  />
                  {t.noNumberField}
                </p>
              ) : null}
              {needsSource ? (
                <p className="m-0 flex items-start gap-2 rounded-[8px] bg-state-hover px-2.5 py-2 text-[12.5px] leading-[1.45] text-muted-foreground max-sm:hidden">
                  <Info aria-hidden="true" className="mt-0.5 size-4 shrink-0" />
                  {t.subItemsOnly}
                </p>
              ) : null}
              {previewNode}
            </>
          )}

          {message ? (
            <div
              id={errorId}
              role="alert"
              className="flex flex-wrap items-center gap-2 rounded-lg bg-danger-subtle px-2.5 py-2 text-item-small text-danger-text"
            >
              <AlertCircle aria-hidden="true" className="size-3.5 shrink-0" />
              <span>
                {message}
                {failure?.reason === "failed" ? ` ${t.keptNote}` : ""}
              </span>
            </div>
          ) : null}
        </div>

        <div className="flex items-center justify-end gap-2 border-t border-line px-5 py-3 max-sm:border-0 max-sm:p-0 [&>button]:max-sm:h-11 [&>button]:max-sm:flex-1">
          <button
            type="button"
            onClick={onClose}
            className={BUTTON_CLASS.outline}
          >
            {t.cancel}
          </button>
          <button
            type="button"
            disabled={!valid || working}
            onClick={() => void save()}
            className={BUTTON_CLASS.primary}
          >
            {working
              ? t.saving
              : failure?.reason === "failed"
                ? t.retry
                : t.save}
          </button>
        </div>
      </DialogContent>
    </Dialog>
  );
}

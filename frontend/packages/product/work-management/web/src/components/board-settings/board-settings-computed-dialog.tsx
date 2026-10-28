import { useId, useRef, useState } from "react";
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
} from "@notrelix/work-management-core";
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

const NODE_PATTERN =
  /\[[^\]]*\]|"(?:[^"]|"")*"|\d+(?:\.\d+)?|[A-Za-z]+\s*\(|\bTRUE\b|\bFALSE\b|<>|<=|>=|[-+*/&=<>]/gi;

/** An estimate of the elements of a formula: references, literals, calls and operators. */
export function formulaNodeCount(expression: string): number {
  return expression.match(NODE_PATTERN)?.length ?? 0;
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

  return (
    <Dialog open onOpenChange={(open) => (open ? undefined : onClose())}>
      <DialogContent
        showCloseButton={false}
        overlayClassName="bg-modal-scrim backdrop-blur-none"
        className="top-[90px] box-border flex max-h-[calc(100vh-120px)] w-[560px] max-w-[calc(100%-2rem)] translate-y-0 flex-col gap-0 overflow-auto rounded-[14px] border-0 bg-popover p-0 text-body leading-[1.45] font-normal shadow-dialog ring-0 sm:max-w-[calc(100%-2rem)] dark:bg-card"
      >
        <div className="flex items-center gap-2 border-b border-line px-5 py-3.5">
          <span
            aria-hidden="true"
            className="rounded-[5px] bg-state-hover-strong px-1 font-mono text-[11px] font-medium text-muted-foreground"
          >
            {kind === "formula" ? "ƒx" : "∑"}
          </span>
          <DialogTitle className="m-0 text-[16px] leading-[1.45] font-[650] text-foreground">
            {kind === "formula" ? t.formulaTitle : t.rollupTitle}
          </DialogTitle>
        </div>
        <DialogDescription className="sr-only">
          {kind === "formula" ? t.functions : t.subItemsOnly}
        </DialogDescription>

        <div className="flex flex-col gap-3 px-5 py-4">
          <div>
            <label
              htmlFor={nameId}
              className="mb-1 block text-metadata font-semibold text-meta-foreground"
            >
              {t.name}
            </label>
            <input
              id={nameId}
              value={name}
              onChange={(event) => setName(event.target.value)}
              className="h-[34px] w-full rounded-[7px] border border-line-strong bg-background px-2.5 text-item-ui text-foreground"
            />
          </div>

          {kind === "formula" ? (
            <>
              <div>
                <label
                  htmlFor={exprId}
                  className="mb-1 block text-metadata font-semibold text-meta-foreground"
                >
                  {t.expression}
                </label>
                <textarea
                  id={exprId}
                  ref={area}
                  rows={3}
                  value={expression}
                  placeholder={t.expressionHint}
                  aria-invalid={tooBig || undefined}
                  aria-describedby={message ? errorId : undefined}
                  onChange={(event) => setExpression(event.target.value)}
                  className={cn(
                    "w-full rounded-[7px] border border-line-strong bg-background px-3 py-2 font-mono text-[13px] leading-[1.55] text-foreground",
                    (tooBig || (failure && failure.reason === "invalid")) &&
                      "border-danger",
                  )}
                />
                <p
                  className={cn(
                    "m-0 mt-1 text-[12px] tabular-nums",
                    tooBig
                      ? "font-semibold text-danger-text"
                      : "text-meta-foreground",
                  )}
                >
                  {fillCopy(t.nodes, { count: String(nodes) })}
                </p>
              </div>
              <div>
                <span className="mb-1 block text-metadata font-semibold text-meta-foreground">
                  {t.insertField}
                </span>
                <div className="flex flex-wrap gap-1.5">
                  {boardFields.map((f) => (
                    <button
                      key={f.id}
                      type="button"
                      onClick={() => insert(f.name)}
                      className="h-6 rounded-md border border-line-strong bg-popover px-2 text-[12px] font-medium text-foreground hover:bg-state-hover-strong"
                    >
                      {f.name}
                    </button>
                  ))}
                </div>
              </div>
              <p className="m-0 text-[12px] text-meta-foreground">
                {t.functions}
              </p>
            </>
          ) : (
            <>
              <div
                role="radiogroup"
                aria-label={t.rollupFunction}
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
              <div>
                <label
                  htmlFor={`${exprId}-src`}
                  className="mb-1 block text-metadata font-semibold text-meta-foreground"
                >
                  {t.sourceField}
                </label>
                <select
                  id={`${exprId}-src`}
                  disabled={!needsSource || noNumber}
                  value={needsSource ? (source ?? "") : ""}
                  onChange={(event) => setSource(event.target.value || null)}
                  className="h-[34px] w-full rounded-[7px] border border-line-strong bg-background px-2.5 text-item-ui text-foreground disabled:bg-state-hover disabled:text-meta-foreground"
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
              <p className="m-0 flex items-start gap-2 rounded-lg bg-state-hover px-2.5 py-2 text-item-small text-muted-foreground">
                <Info aria-hidden="true" className="mt-0.5 size-3.5 shrink-0" />
                {t.subItemsOnly}
              </p>
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

        <div className="flex items-center justify-end gap-2 border-t border-line px-5 py-3">
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

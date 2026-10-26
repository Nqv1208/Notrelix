import { useId, useRef, useState, type FormEvent } from "react";
import { DialogFrame, FIELD, OUTLINE, PRIMARY } from "./doc-dialogs-surface";
import { fillDocCopy, type DocPageCopy } from "./doc-page.copy";
import type {
  DocHistoryActionView,
  DocImportView,
  DocPageCallbacks,
  DocTemplateActionView,
} from "./doc-page-models";

/** The accepted size of an imported file (a page holds at most this much text). */
export const IMPORT_MAX_MB = 5;

const ACCEPT = ".md,.markdown,.txt,.docx";

export interface DocSaveVersionDialogSurfaceProps {
  readonly copy: DocPageCopy;
  readonly action: DocHistoryActionView;
  readonly callbacks?: DocPageCallbacks;
}

/** Names the page as it is now ("Save version"). */
export function DocSaveVersionDialogSurface({
  copy,
  action,
  callbacks,
}: DocSaveVersionDialogSurfaceProps) {
  const d = copy.versions.saveDialog;
  const [name, setName] = useState("");
  const [touched, setTouched] = useState(false);
  const inputId = useId();
  const errorId = useId();
  const trimmed = name.trim();
  const message =
    touched && trimmed === ""
      ? d.required
      : action.saveFailed
        ? d.failed
        : null;

  const submit = (event: FormEvent) => {
    event.preventDefault();
    setTouched(true);
    if (trimmed === "" || action.saving) return;
    callbacks?.onSaveVersion?.(trimmed);
  };

  return (
    <DialogFrame
      title={d.title}
      closeLabel={d.close}
      width={460}
      onClose={callbacks?.onCloseOverlay}
    >
      <form
        onSubmit={submit}
        noValidate
        className="flex flex-col gap-3 px-5 pt-2 pb-5"
      >
        <p className="m-0 text-crumb leading-[1.5] text-meta-foreground">
          {d.note}
        </p>
        <label
          htmlFor={inputId}
          className="flex flex-col gap-1 text-metadata font-semibold text-meta-foreground"
        >
          {d.label}
          <input
            id={inputId}
            value={name}
            autoFocus
            maxLength={200}
            placeholder={d.placeholder}
            aria-invalid={message ? true : undefined}
            aria-describedby={message ? errorId : undefined}
            onChange={(event) => setName(event.target.value)}
            className={FIELD}
          />
        </label>
        {message ? (
          <p
            id={errorId}
            role="alert"
            className="m-0 text-crumb font-semibold text-danger-text"
          >
            {message}
          </p>
        ) : null}
        <div className="flex justify-end gap-2">
          <button
            type="button"
            onClick={callbacks?.onCloseOverlay}
            className={OUTLINE}
          >
            {d.cancel}
          </button>
          <button type="submit" disabled={action.saving} className={PRIMARY}>
            {action.saving ? d.saving : d.save}
          </button>
        </div>
      </form>
    </DialogFrame>
  );
}

export interface DocRestoreVersionDialogSurfaceProps {
  readonly copy: DocPageCopy;
  /** The compared version's stamp ("10/10 15:42"). */
  readonly stamp: string;
  readonly action: DocHistoryActionView;
  readonly callbacks?: DocPageCallbacks;
}

/** Confirms a restore (never the browser's confirm): says what it does and keeps the history. */
export function DocRestoreVersionDialogSurface({
  copy,
  stamp,
  action,
  callbacks,
}: DocRestoreVersionDialogSurfaceProps) {
  const d = copy.versions.restoreDialog;
  return (
    <DialogFrame
      title={d.title}
      closeLabel={d.close}
      width={460}
      onClose={callbacks?.onCloseOverlay}
    >
      <div className="flex flex-col gap-3 px-5 pt-2 pb-5">
        <p className="m-0 text-compact leading-[1.5] text-foreground">
          {fillDocCopy(d.body, { stamp })}
        </p>
        {action.restoreFailed ? (
          <p
            role="alert"
            className="m-0 text-crumb font-semibold text-danger-text"
          >
            {action.restoreFailed === "conflict" ? d.conflict : d.failed}
          </p>
        ) : null}
        <div className="flex justify-end gap-2">
          <button
            type="button"
            onClick={callbacks?.onCloseOverlay}
            className={OUTLINE}
          >
            {d.cancel}
          </button>
          <button
            type="button"
            autoFocus
            disabled={action.restoring || action.restoreFailed === "conflict"}
            onClick={callbacks?.onRestoreVersion}
            className={PRIMARY}
          >
            {action.restoring ? d.restoring : d.confirm}
          </button>
        </div>
      </div>
    </DialogFrame>
  );
}

export interface DocSaveTemplateDialogSurfaceProps {
  readonly copy: DocPageCopy;
  readonly action: DocTemplateActionView;
  readonly callbacks?: DocPageCallbacks;
}

/** Names the template a page is saved as ("Save as template"). */
export function DocSaveTemplateDialogSurface({
  copy,
  action,
  callbacks,
}: DocSaveTemplateDialogSurfaceProps) {
  const d = copy.template;
  const [name, setName] = useState("");
  const [description, setDescription] = useState("");
  const [touched, setTouched] = useState(false);
  const nameId = useId();
  const descriptionId = useId();
  const errorId = useId();
  const trimmed = name.trim();
  const message =
    touched && trimmed === "" ? d.required : action.failed ? d.failed : null;

  const submit = (event: FormEvent) => {
    event.preventDefault();
    setTouched(true);
    if (trimmed === "" || action.saving) return;
    callbacks?.onSaveTemplate?.(trimmed, description.trim());
  };

  return (
    <DialogFrame
      title={d.title}
      closeLabel={d.close}
      width={480}
      onClose={callbacks?.onCloseOverlay}
    >
      <form
        onSubmit={submit}
        noValidate
        className="flex flex-col gap-3 px-5 pt-2 pb-5"
      >
        <p className="m-0 text-crumb leading-[1.5] text-meta-foreground">
          {d.note}
        </p>
        <label
          htmlFor={nameId}
          className="flex flex-col gap-1 text-metadata font-semibold text-meta-foreground"
        >
          {d.nameLabel}
          <input
            id={nameId}
            value={name}
            autoFocus
            maxLength={256}
            placeholder={d.namePlaceholder}
            aria-invalid={message ? true : undefined}
            aria-describedby={message ? errorId : undefined}
            onChange={(event) => setName(event.target.value)}
            className={FIELD}
          />
        </label>
        <label
          htmlFor={descriptionId}
          className="flex flex-col gap-1 text-metadata font-semibold text-meta-foreground"
        >
          {d.descriptionLabel}
          <input
            id={descriptionId}
            value={description}
            maxLength={1024}
            onChange={(event) => setDescription(event.target.value)}
            className={FIELD}
          />
        </label>
        {message ? (
          <p
            id={errorId}
            role="alert"
            className="m-0 text-crumb font-semibold text-danger-text"
          >
            {message}
          </p>
        ) : null}
        <div className="flex justify-end gap-2">
          <button
            type="button"
            onClick={callbacks?.onCloseOverlay}
            className={OUTLINE}
          >
            {d.cancel}
          </button>
          <button type="submit" disabled={action.saving} className={PRIMARY}>
            {action.saving ? d.saving : d.save}
          </button>
        </div>
      </form>
    </DialogFrame>
  );
}

export interface DocImportDialogSurfaceProps {
  readonly copy: DocPageCopy;
  readonly state: DocImportView;
  readonly callbacks?: DocPageCallbacks;
}

/** Chooses a Markdown or Word file to turn into a new page. */
export function DocImportDialogSurface({
  copy,
  state,
  callbacks,
}: DocImportDialogSurfaceProps) {
  const d = copy.importDialog;
  const input = useRef<HTMLInputElement | null>(null);
  const message =
    state.error === "unsupported"
      ? d.unsupported
      : state.error === "tooLarge"
        ? fillDocCopy(d.tooLarge, { max: String(IMPORT_MAX_MB) })
        : state.error === "empty"
          ? d.empty
          : state.error === "failed"
            ? d.failed
            : null;
  return (
    <DialogFrame
      title={d.title}
      closeLabel={d.close}
      width={480}
      onClose={callbacks?.onCloseOverlay}
    >
      <div className="flex flex-col gap-3 px-5 pt-2 pb-5">
        <p className="m-0 text-crumb leading-[1.5] text-meta-foreground">
          {d.note}
        </p>
        <input
          ref={input}
          type="file"
          accept={ACCEPT}
          aria-label={d.chooseLabel}
          disabled={state.busy}
          onChange={(event) => {
            const file = event.target.files?.[0];
            event.target.value = "";
            if (file) callbacks?.onImportFile?.(file);
          }}
          className="sr-only"
        />
        <div className="flex items-center gap-2.5">
          <button
            type="button"
            autoFocus
            disabled={state.busy}
            onClick={() => input.current?.click()}
            className={PRIMARY}
          >
            {state.busy ? d.importing : d.choose}
          </button>
          {state.fileName ? (
            <span className="min-w-0 flex-1 truncate text-compact text-meta-foreground">
              {fillDocCopy(d.chosen, { name: state.fileName })}
            </span>
          ) : null}
        </div>
        {message ? (
          <p
            role="alert"
            className="m-0 text-crumb font-semibold text-danger-text"
          >
            {message}
          </p>
        ) : null}
        {state.imagesDropped > 0 ? (
          <p role="status" className="m-0 text-crumb text-meta-foreground">
            {fillDocCopy(d.imagesDropped, {
              count: String(state.imagesDropped),
            })}
          </p>
        ) : null}
        <div className="flex justify-end gap-2">
          <button
            type="button"
            onClick={callbacks?.onCloseOverlay}
            className={OUTLINE}
          >
            {d.cancel}
          </button>
        </div>
      </div>
    </DialogFrame>
  );
}

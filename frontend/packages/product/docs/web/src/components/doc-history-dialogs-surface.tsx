import { useId, useRef, useState, type FormEvent } from "react";
import { cn } from "@notrelix/ui-web";
import { CircleAlert, Lock } from "lucide-react";
import { DialogFrame, FIELD, OUTLINE, PRIMARY } from "./doc-dialogs-surface";
import { IMPORT_MAX_BYTES } from "../import/read-import-file";
import { fillDocCopy, type DocPageCopy } from "./doc-page.copy";
import type {
  DocHistoryActionView,
  DocImportView,
  DocPageCallbacks,
  DocTemplateActionView,
  DocVersionView,
} from "./doc-page-models";

/** The accepted size of an imported file, in MB. */
export const IMPORT_MAX_MB = IMPORT_MAX_BYTES / (1024 * 1024);

const ACCEPT = ".md,.markdown,.txt,.docx";

export interface DocSaveVersionDialogSurfaceProps {
  readonly copy: DocPageCopy;
  readonly action: DocHistoryActionView;
  readonly callbacks?: DocPageCallbacks;
}

/** The footer of the version dialogs: a rule above, actions at the right (Doc-Ver `.df`). */
const VERSION_BUTTON =
  "relative max-sm:h-8 max-sm:after:absolute max-sm:after:-inset-1.5 max-sm:after:content-['']";

const VERSION_FOOTER =
  "mt-3.5 flex items-center justify-end gap-2 border-t border-border px-5 py-3.5";

const VERSION_FIELD =
  "box-border h-9 w-full rounded-[8px] border border-line-strong bg-doc-panel px-2.5 text-nav leading-[1.45] font-semibold text-foreground outline-none placeholder:font-semibold placeholder:text-meta-foreground focus-visible:border-primary focus-visible:ring-2 focus-visible:ring-focus-ring aria-invalid:border-danger-text";

export interface DocSaveVersionDialogSurfaceProps {
  readonly copy: DocPageCopy;
  readonly action: DocHistoryActionView;
  readonly callbacks?: DocPageCallbacks;
}

/** Names the page as it is now ("Save a named version"). */
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
      : trimmed.length > 200
        ? d.tooLong
        : action.saveFailed
          ? d.failed
          : null;

  const submit = (event: FormEvent) => {
    event.preventDefault();
    setTouched(true);
    if (trimmed === "" || trimmed.length > 200 || action.saving) return;
    callbacks?.onSaveVersion?.(trimmed);
  };

  return (
    <DialogFrame
      title={d.title}
      closeLabel={d.close}
      width={440}
      sheet
      onClose={callbacks?.onCloseOverlay}
    >
      <form onSubmit={submit} noValidate className="flex flex-col">
        <div className="flex flex-col gap-4 px-5 pt-4 pb-1.5">
          <div>
            <label
              htmlFor={inputId}
              className="flex flex-col gap-1.5 text-crumb font-semibold text-muted-foreground"
            >
              {d.label}
              <input
                id={inputId}
                value={name}
                autoFocus
                placeholder={d.placeholder}
                aria-invalid={message ? true : undefined}
                aria-describedby={message ? errorId : undefined}
                onChange={(event) => setName(event.target.value)}
                className={VERSION_FIELD}
              />
            </label>
            {message ? (
              <span
                id={errorId}
                role="alert"
                className="mt-2.5 flex items-center gap-[5px] text-crumb text-danger-text"
              >
                <CircleAlert
                  aria-hidden="true"
                  className="size-[13px] shrink-0 stroke-[1.8]"
                />
                {message}
              </span>
            ) : null}
          </div>
          <p className="m-0 text-crumb leading-[1.45] text-meta-foreground">
            {d.note}
          </p>
        </div>
        <div className={VERSION_FOOTER}>
          <button
            type="button"
            onClick={callbacks?.onCloseOverlay}
            className={cn(OUTLINE, VERSION_BUTTON)}
          >
            {d.cancel}
          </button>
          <button
            type="submit"
            disabled={action.saving}
            className={cn(PRIMARY, VERSION_BUTTON)}
          >
            {d.save}
          </button>
        </div>
      </form>
    </DialogFrame>
  );
}

export interface DocRestoreVersionDialogSurfaceProps {
  readonly copy: DocPageCopy;
  /** The version being restored. */
  readonly version: DocVersionView;
  readonly action: DocHistoryActionView;
  readonly callbacks?: DocPageCallbacks;
}

/** Confirms a restore (never the browser's confirm): says what it does and keeps the history. */
export function DocRestoreVersionDialogSurface({
  copy,
  version,
  action,
  callbacks,
}: DocRestoreVersionDialogSurfaceProps) {
  const d = copy.versions.restoreDialog;
  const conflict = action.restoreFailed === "conflict";
  const denied = !action.canRestore;
  const values = { n: version.number, stamp: version.stamp };
  return (
    <DialogFrame
      title={fillDocCopy(d.title, values)}
      closeLabel={d.close}
      width={440}
      sheet
      onClose={callbacks?.onCloseOverlay}
    >
      <p className="m-0 px-5 pt-1.5 text-nav leading-[1.55] font-normal tracking-normal text-doc-body">
        {fillDocCopy(d.body, values)}
      </p>
      {conflict ? (
        <div className="px-5 pt-4 pb-1.5">
          <div
            role="alert"
            className="flex items-start gap-2 rounded-[8px] border border-danger-text bg-danger-subtle px-3 py-2 text-compact leading-[1.45] text-foreground"
          >
            <CircleAlert
              aria-hidden="true"
              className="mt-0.5 size-4 shrink-0 stroke-[1.8] text-danger-text"
            />
            <div>
              <b className="font-bold">{d.conflictTitle}</b>
              <small className="mt-0.5 block text-crumb text-muted-foreground">
                {fillDocCopy(d.conflictBody, {
                  count: action.conflictCount ?? 0,
                  time: action.conflictTime ?? "",
                })}
              </small>
            </div>
          </div>
        </div>
      ) : action.restoreFailed === "failed" ? (
        <p
          role="alert"
          className="m-0 px-5 pt-4 pb-1.5 text-crumb font-semibold text-danger-text"
        >
          {d.failed}
        </p>
      ) : null}
      {denied ? (
        <div className="px-5 pt-4 pb-1.5">
          <div className="flex items-start gap-[9px] rounded-[9px] bg-info-subtle px-3 py-2.5 text-crumb leading-[1.5] text-doc-body">
            <Lock
              aria-hidden="true"
              className="mt-px size-[15px] shrink-0 stroke-[1.8] text-brand-solid"
            />
            {d.denied}
          </div>
        </div>
      ) : null}
      <div className={VERSION_FOOTER}>
        <button
          type="button"
          onClick={callbacks?.onCloseOverlay}
          className={cn(OUTLINE, VERSION_BUTTON)}
        >
          {d.cancel}
        </button>
        {conflict ? (
          <button
            type="button"
            autoFocus
            onClick={callbacks?.onCloseVersionView}
            className={cn(PRIMARY, VERSION_BUTTON)}
          >
            {d.review}
          </button>
        ) : (
          <button
            type="button"
            autoFocus
            disabled={action.restoring || denied}
            onClick={callbacks?.onRestoreVersion}
            className={cn(PRIMARY, VERSION_BUTTON)}
          >
            {action.restoring ? d.restoring : d.confirm}
          </button>
        )}
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

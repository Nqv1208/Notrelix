import { useId, useState, type FormEvent } from "react";
import { cn } from "@notrelix/ui-web";
import {
  DANGER,
  DialogFrame,
  FIELD,
  OUTLINE,
  PRIMARY,
} from "./doc-dialogs-surface";
import { fillDocCopy, type DocPageCopy } from "./doc-page.copy";
import type {
  DocPageCallbacks,
  DocTemplateRowView,
  DocTemplatesView,
} from "./doc-page-models";

/** What a row is doing besides listing: renaming in place, or asking before it archives or deletes. */
type RowMode =
  | { readonly kind: "rename" }
  | { readonly kind: "archive" }
  | { readonly kind: "delete" };

const SMALL =
  "inline-flex h-7 max-sm:h-11 items-center rounded-[6px] px-2 text-crumb leading-[1.45] font-medium text-meta-foreground outline-none hover:bg-state-hover hover:text-foreground focus-visible:ring-2 focus-visible:ring-focus-ring disabled:cursor-default disabled:opacity-60";

export interface DocTemplatesDialogSurfaceProps {
  readonly copy: DocPageCopy;
  readonly view: DocTemplatesView;
  readonly callbacks?: DocPageCallbacks;
}

/**
 * The workspace's page templates. A template's creator or a Workspace Admin renames, archives or deletes it
 * right in its row; archive and delete ask first. Everyone else only reads the list.
 */
export function DocTemplatesDialogSurface({
  copy,
  view,
  callbacks,
}: DocTemplatesDialogSurfaceProps) {
  const d = copy.templates;
  const [mode, setMode] = useState<{
    readonly id: string;
    readonly mode: RowMode;
  } | null>(null);
  const leave = () => setMode(null);

  return (
    <DialogFrame
      title={d.title}
      closeLabel={d.close}
      width={560}
      onClose={callbacks?.onCloseOverlay}
    >
      <div className="flex flex-col gap-2 px-5 pt-1 pb-5">
        <p className="m-0 text-crumb leading-[1.5] text-meta-foreground">
          {d.note}
        </p>
        {view.status === "loading" ? (
          <p
            role="status"
            className="m-0 py-6 text-center text-compact text-meta-foreground"
          >
            {d.loading}
          </p>
        ) : view.status === "error" ? (
          <div role="alert" className="flex flex-col items-center gap-3 py-6">
            <p className="m-0 text-compact font-semibold text-danger-text">
              {d.loadFailed}
            </p>
            <button
              type="button"
              onClick={callbacks?.onTemplatesRetry}
              className={OUTLINE}
            >
              {d.retry}
            </button>
          </div>
        ) : view.templates.length === 0 ? (
          <div className="flex flex-col items-center gap-1 py-6 text-center">
            <p className="m-0 text-compact font-semibold text-foreground">
              {d.emptyTitle}
            </p>
            <p className="m-0 max-w-[36ch] text-crumb leading-[1.5] text-meta-foreground">
              {d.emptyBody}
            </p>
          </div>
        ) : (
          <ul className="m-0 flex list-none flex-col divide-y divide-border p-0">
            {view.templates.map((template) => (
              <TemplateRow
                key={template.id}
                copy={copy}
                template={template}
                busy={view.busyId === template.id}
                failure={
                  view.failure?.id === template.id ? view.failure.kind : null
                }
                mode={mode?.id === template.id ? mode.mode : null}
                onMode={(next) =>
                  setMode(next ? { id: template.id, mode: next } : null)
                }
                onDone={leave}
                callbacks={callbacks}
              />
            ))}
          </ul>
        )}
      </div>
    </DialogFrame>
  );
}

function TemplateRow({
  copy,
  template,
  busy,
  failure,
  mode,
  onMode,
  onDone,
  callbacks,
}: {
  readonly copy: DocPageCopy;
  readonly template: DocTemplateRowView;
  readonly busy: boolean;
  readonly failure: "conflict" | "forbidden" | "notFound" | "failed" | null;
  readonly mode: RowMode | null;
  readonly onMode: (next: RowMode | null) => void;
  readonly onDone: () => void;
  readonly callbacks?: DocPageCallbacks;
}) {
  const d = copy.templates;
  const [name, setName] = useState(template.name);
  const [description, setDescription] = useState(template.description ?? "");
  const [touched, setTouched] = useState(false);
  const nameId = useId();
  const descriptionId = useId();
  const errorId = useId();
  const trimmed = name.trim();
  const invalid = touched && trimmed === "";
  const message = invalid
    ? d.required
    : failure === "conflict"
      ? d.conflict
      : failure === "forbidden"
        ? d.forbidden
        : failure === "notFound"
          ? d.gone
          : failure === "failed"
            ? d.failed
            : null;

  const submit = (event: FormEvent) => {
    event.preventDefault();
    setTouched(true);
    if (trimmed === "" || busy) return;
    callbacks?.onTemplateRename?.(template.id, trimmed, description.trim());
    onDone();
  };

  return (
    <li className="flex flex-col gap-2 py-3 first:pt-2 last:pb-0">
      {mode?.kind === "rename" ? (
        <form
          onSubmit={submit}
          noValidate
          aria-label={fillDocCopy(d.renameLabel, { name: template.name })}
          className="flex flex-col gap-2"
        >
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
              aria-invalid={invalid ? true : undefined}
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
          <div className="flex justify-end gap-2">
            <button
              type="button"
              onClick={() => {
                setName(template.name);
                setDescription(template.description ?? "");
                setTouched(false);
                onMode(null);
              }}
              className={OUTLINE}
            >
              {d.cancel}
            </button>
            <button type="submit" disabled={busy} className={PRIMARY}>
              {busy ? d.saving : d.save}
            </button>
          </div>
        </form>
      ) : (
        <div className="flex items-start gap-3">
          <div className="min-w-0 flex-1">
            <p className="m-0 truncate text-compact font-semibold text-foreground">
              {template.name}
            </p>
            {template.description ? (
              <p className="m-0 text-crumb leading-[1.5] text-meta-foreground">
                {template.description}
              </p>
            ) : null}
            <p className="m-0 text-metadata text-meta-foreground">
              {fillDocCopy(d.blocks, { count: template.blockCount })}
            </p>
          </div>
          {template.canManage && mode === null ? (
            <div className="flex shrink-0 items-center gap-0.5">
              <button
                type="button"
                disabled={busy}
                aria-label={fillDocCopy(d.renameLabel, {
                  name: template.name,
                })}
                onClick={() => {
                  setName(template.name);
                  setDescription(template.description ?? "");
                  setTouched(false);
                  onMode({ kind: "rename" });
                }}
                className={SMALL}
              >
                {d.rename}
              </button>
              <button
                type="button"
                disabled={busy}
                aria-label={fillDocCopy(d.archiveLabel, {
                  name: template.name,
                })}
                onClick={() => onMode({ kind: "archive" })}
                className={SMALL}
              >
                {d.archive}
              </button>
              <button
                type="button"
                disabled={busy}
                aria-label={fillDocCopy(d.deleteLabel, {
                  name: template.name,
                })}
                onClick={() => onMode({ kind: "delete" })}
                className={cn(SMALL, "text-danger-text hover:text-danger-text")}
              >
                {d.delete}
              </button>
            </div>
          ) : null}
        </div>
      )}
      {mode?.kind === "archive" || mode?.kind === "delete" ? (
        <div
          role="group"
          aria-label={fillDocCopy(
            mode.kind === "delete" ? d.deleteLabel : d.archiveLabel,
            { name: template.name },
          )}
          className="flex flex-col gap-2 rounded-[8px] bg-state-hover px-3 py-2.5"
        >
          <p className="m-0 text-crumb leading-[1.5] text-foreground">
            {fillDocCopy(
              mode.kind === "delete" ? d.deleteBody : d.archiveBody,
              { name: template.name },
            )}
          </p>
          <div className="flex justify-end gap-2">
            <button
              type="button"
              autoFocus
              onClick={() => onMode(null)}
              className={OUTLINE}
            >
              {d.cancel}
            </button>
            <button
              type="button"
              disabled={busy}
              onClick={() => {
                if (mode.kind === "delete") {
                  callbacks?.onTemplateDelete?.(template.id);
                } else {
                  callbacks?.onTemplateArchive?.(template.id);
                }
                onDone();
              }}
              className={mode.kind === "delete" ? DANGER : PRIMARY}
            >
              {mode.kind === "delete" ? d.deleteConfirm : d.archiveConfirm}
            </button>
          </div>
        </div>
      ) : null}
      {message ? (
        <p
          id={errorId}
          role="alert"
          className="m-0 text-crumb font-semibold text-danger-text"
        >
          {message}
        </p>
      ) : null}
    </li>
  );
}

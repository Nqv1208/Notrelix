import { useId, useState, type FormEvent } from "react";
import { Link2, Plus, Trash2 } from "lucide-react";
import { Skeleton, cn } from "@notrelix/ui-web";
import type { Form } from "@notrelix/work-management-core";
import type { FormWriteResult } from "@notrelix/work-management-core";
import {
  BUTTON_CLASS,
  ICON_BUTTON_CLASS,
  INPUT_CLASS,
  HINT_CLASS,
  SectionHeader,
} from "../board-settings/board-settings-parts";
import { FormAlert, FormStatusChip } from "./forms-parts";
import { fillFormsCopy, type FormsCopy } from "./forms.copy";
import { dayText } from "./form-model";

/** A form of the board with what the list shows besides it (counts read apart, so they may be unknown). */
export interface FormListRow extends Form {
  readonly questionCount?: number;
  /** Responses still in the New tab, and all that are kept. */
  readonly newCount?: number;
  readonly totalCount?: number;
}

export interface BoardFormsPanelProps {
  readonly copy: FormsCopy;
  readonly status: "loading" | "error" | "ready";
  readonly forms: readonly FormListRow[];
  readonly onReload: () => void;
  readonly onCreate: (title: string) => Promise<FormWriteResult>;
  readonly onPublish: (formId: string) => Promise<FormWriteResult>;
  readonly onClose: (formId: string) => Promise<FormWriteResult>;
  readonly onDelete: (form: FormListRow) => Promise<FormWriteResult>;
  readonly onOpenBuilder: (formId: string) => void;
  readonly onOpenResponses: (formId: string) => void;
  /** Copies the member link of the form; resolves false when the browser refused. */
  readonly onCopyLink: (formId: string) => Promise<boolean>;
}

type RowAction = "publish" | "close" | "remove";

/**
 * The "Biểu mẫu" section of the board settings: the board's forms with their
 * status, the new responses and what a manager does with each (copy the member
 * link, close, publish a draft, delete with undo). A new form is
 * named in place and opens in the builder. Pure: every write is a callback.
 */
export function FormsListPanel(props: BoardFormsPanelProps) {
  const { copy, status, forms } = props;
  const t = copy.list;
  const [adding, setAdding] = useState(false);
  const [name, setName] = useState("");
  const [nameError, setNameError] = useState(false);
  const [createFailed, setCreateFailed] = useState(false);
  const [creating, setCreating] = useState(false);
  const [confirmId, setConfirmId] = useState<string | null>(null);
  const [failed, setFailed] = useState<{
    id: string;
    action: RowAction;
  } | null>(null);
  const [copiedId, setCopiedId] = useState<string | null>(null);
  const nameId = useId();
  const open = forms.filter((form) => form.status === "published").length;

  async function create(event: FormEvent) {
    event.preventDefault();
    if (creating) return;
    if (name.trim() === "") return setNameError(true);
    setCreating(true);
    setCreateFailed(false);
    const result = await props.onCreate(name.trim());
    setCreating(false);
    if (result.ok) {
      setAdding(false);
      setName("");
    } else setCreateFailed(true);
  }

  async function run(
    row: FormListRow,
    action: RowAction,
    send: () => Promise<FormWriteResult>,
  ) {
    setFailed(null);
    const result = await send();
    if (result.ok) setConfirmId(null);
    else setFailed({ id: row.id, action });
  }

  async function copyLink(formId: string) {
    if (await props.onCopyLink(formId)) {
      setCopiedId(formId);
      window.setTimeout(
        () => setCopiedId((id) => (id === formId ? null : id)),
        2000,
      );
    }
  }

  return (
    <section
      aria-labelledby="bs-forms-h"
      className="min-w-0 flex-1 overflow-auto px-6 pt-5 pb-6"
    >
      <div className="@container max-w-[780px]">
        <SectionHeader
          id="bs-forms-h"
          title={t.title}
          intro={
            status === "ready" && forms.length > 0
              ? `${fillFormsCopy(t.summary, { count: forms.length, open })} · ${t.summaryRule}`
              : t.summaryRule
          }
          actions={
            <button
              type="button"
              aria-expanded={adding}
              onClick={() => {
                setAdding(true);
                setNameError(false);
              }}
              className={BUTTON_CLASS.primary}
            >
              <Plus aria-hidden="true" />
              {t.create}
            </button>
          }
        />

        {adding ? (
          <form
            onSubmit={create}
            onKeyDown={(event) => {
              if (event.key === "Escape") setAdding(false);
            }}
            className="mb-3 flex flex-col gap-2.5 rounded-xl border border-line bg-popover p-3.5"
          >
            <div className="flex gap-2">
              <input
                id={nameId}
                autoFocus
                value={name}
                maxLength={200}
                aria-label={t.nameLabel}
                aria-invalid={nameError || undefined}
                aria-describedby={nameError ? `${nameId}-e` : undefined}
                placeholder={t.namePlaceholder}
                onChange={(event) => {
                  setName(event.target.value);
                  setNameError(false);
                }}
                className={INPUT_CLASS}
              />
              <button
                type="submit"
                disabled={creating || nameError}
                className={BUTTON_CLASS.primary}
              >
                {t.submitCreate}
              </button>
              <button
                type="button"
                onClick={() => setAdding(false)}
                className={BUTTON_CLASS.ghost}
              >
                {copy.common.cancel}
              </button>
            </div>
            {nameError ? (
              <p
                id={`${nameId}-e`}
                role="alert"
                className="m-0 text-crumb text-danger-text"
              >
                {t.nameRequired}
              </p>
            ) : (
              <span className={HINT_CLASS}>{t.createHint}</span>
            )}
            {createFailed ? <FormAlert message={t.createFailed} /> : null}
          </form>
        ) : null}

        {status === "loading" ? (
          <div
            role="status"
            aria-label={copy.common.loading}
            className="flex flex-col gap-2"
          >
            {[0, 1, 2].map((n) => (
              <Skeleton key={n} className="h-11 rounded-[10px]" />
            ))}
          </div>
        ) : status === "error" ? (
          <div className="rounded-xl border border-line bg-popover p-3.5">
            <FormAlert
              message={t.loadError}
              onRetry={props.onReload}
              retryLabel={copy.common.retry}
            />
          </div>
        ) : forms.length === 0 ? (
          <div className="rounded-[10px] border border-dashed border-line-strong bg-popover px-4 py-5 text-center">
            <b className="block text-item-ui font-semibold">{t.empty}</b>
            <span className="text-item-small text-meta-foreground">
              {t.emptyBody}
            </span>
          </div>
        ) : (
          <div className="overflow-hidden rounded-xl border border-line bg-popover">
            <div className="hidden min-h-[34px] grid-cols-[minmax(0,1fr)_110px_120px_230px] items-center gap-2.5 bg-background px-3.5 text-metadata font-semibold text-meta-foreground @[640px]:grid">
              <span>{t.colForm}</span>
              <span>{t.colStatus}</span>
              <span>{t.colResponses}</span>
              <span />
            </div>
            <ul className="m-0 list-none p-0">
              {forms.map((form) => (
                <li
                  key={form.id}
                  className="border-t border-line first:border-t-0 @[640px]:first:border-t"
                >
                  <div
                    className={cn(
                      "grid min-h-14 grid-cols-[minmax(0,1fr)_auto] items-center gap-x-2.5 gap-y-1.5 px-3.5 py-2 @[640px]:grid-cols-[minmax(0,1fr)_110px_120px_230px] @[640px]:py-0",
                      form.status === "draft" && "bg-brand-subtle/40",
                    )}
                  >
                    <span className="flex min-w-0 flex-col">
                      <button
                        type="button"
                        title={form.title}
                        aria-label={fillFormsCopy(t.openBuilder, {
                          name: form.title,
                        })}
                        onClick={() => props.onOpenBuilder(form.id)}
                        className="cursor-pointer truncate border-0 bg-transparent p-0 text-left text-item-ui font-semibold text-foreground outline-none hover:underline focus-visible:ring-2 focus-visible:ring-focus-ring"
                      >
                        {form.title}
                      </button>
                      <span className={HINT_CLASS}>{metaLine(form, copy)}</span>
                    </span>
                    <span className="justify-self-end @[640px]:justify-self-start">
                      <FormStatusChip status={form.status} copy={copy} />
                    </span>
                    <span
                      className={cn(
                        HINT_CLASS,
                        "col-span-2 @[640px]:col-span-1",
                      )}
                    >
                      {responsesText(form, copy)}
                    </span>
                    <span className="col-span-2 flex flex-wrap items-center justify-end gap-1 @[640px]:col-span-1">
                      {form.status === "published" ? (
                        <button
                          type="button"
                          onClick={() => void copyLink(form.id)}
                          className={cn(
                            BUTTON_CLASS.outline,
                            "h-7 px-2.5 text-crumb",
                          )}
                        >
                          <Link2 aria-hidden="true" />
                          {copiedId === form.id ? t.copied : t.copyLink}
                        </button>
                      ) : null}
                      {form.status === "published" ? (
                        <button
                          type="button"
                          onClick={() =>
                            void run(form, "close", () =>
                              props.onClose(form.id),
                            )
                          }
                          className={cn(
                            BUTTON_CLASS.outline,
                            "h-7 px-2.5 text-crumb",
                          )}
                        >
                          {t.closeForm}
                        </button>
                      ) : null}
                      {form.status === "draft" ? (
                        <button
                          type="button"
                          onClick={() =>
                            void run(form, "publish", () =>
                              props.onPublish(form.id),
                            )
                          }
                          className={cn(
                            BUTTON_CLASS.primary,
                            "h-7 px-2.5 text-crumb",
                          )}
                        >
                          {t.publish}
                        </button>
                      ) : null}
                      {form.status !== "draft" ? (
                        <button
                          type="button"
                          onClick={() => props.onOpenResponses(form.id)}
                          aria-label={fillFormsCopy(t.openResponses, {
                            name: form.title,
                          })}
                          className={cn(
                            BUTTON_CLASS.ghost,
                            "h-7 px-2 text-crumb",
                          )}
                        >
                          {copy.inbox.form}
                        </button>
                      ) : null}
                      <button
                        type="button"
                        aria-label={fillFormsCopy(t.removeAria, {
                          name: form.title,
                        })}
                        onClick={() => setConfirmId(form.id)}
                        className={ICON_BUTTON_CLASS}
                      >
                        <Trash2 aria-hidden="true" />
                      </button>
                    </span>
                  </div>
                  {failed?.id === form.id ? (
                    <div className="px-3.5 pb-2.5">
                      <FormAlert
                        message={
                          failed.action === "close"
                            ? t.closeFailed
                            : failed.action === "remove"
                              ? t.removeFailed
                              : form.questionCount === 0
                                ? t.noQuestions
                                : t.publishFailed
                        }
                        onRetry={() => {
                          if (failed.action === "close")
                            void run(form, "close", () =>
                              props.onClose(form.id),
                            );
                          else if (failed.action === "publish")
                            void run(form, "publish", () =>
                              props.onPublish(form.id),
                            );
                          else
                            void run(form, "remove", () =>
                              props.onDelete(form),
                            );
                        }}
                        retryLabel={copy.common.retry}
                      />
                    </div>
                  ) : null}
                  {confirmId === form.id ? (
                    <div className="border-t border-line bg-danger-subtle px-3.5 py-2.5">
                      <div
                        role="alertdialog"
                        aria-labelledby={`${nameId}-d-${form.id}`}
                        className="flex flex-wrap items-center gap-3 rounded-[10px] border border-line-strong bg-popover p-3"
                      >
                        <p
                          id={`${nameId}-d-${form.id}`}
                          className="m-0 min-w-0 flex-1 text-item-small"
                        >
                          {fillFormsCopy(t.removeAsk, { name: form.title })}
                        </p>
                        <button
                          type="button"
                          autoFocus
                          onClick={() => setConfirmId(null)}
                          className={cn(BUTTON_CLASS.outline, "h-7")}
                        >
                          {copy.common.keep}
                        </button>
                        <button
                          type="button"
                          onClick={() =>
                            void run(form, "remove", () => props.onDelete(form))
                          }
                          className={cn(BUTTON_CLASS.danger, "h-7")}
                        >
                          {t.remove}
                        </button>
                      </div>
                    </div>
                  ) : null}
                </li>
              ))}
            </ul>
          </div>
        )}
        <p className={cn(HINT_CLASS, "mt-3 mb-0")}>{t.footnote}</p>
      </div>
    </section>
  );
}

function metaLine(form: FormListRow, copy: FormsCopy): string {
  const questions = form.questionCount ?? 0;
  const t = copy.list;
  if (form.questionCount === undefined)
    return fillFormsCopy(t.meta, {
      questions: "…",
      date: dayText(form.createdAt),
    });
  return fillFormsCopy(
    form.status === "draft"
      ? t.metaDraft
      : form.status === "closed"
        ? t.metaClosed
        : t.meta,
    { questions, date: dayText(form.createdAt.slice(0, 10)) },
  );
}

function responsesText(form: FormListRow, copy: FormsCopy) {
  const t = copy.list;
  if (form.totalCount === undefined) return "";
  if (form.totalCount === 0) return t.zero;
  return (
    <>
      {(form.newCount ?? 0) > 0 ? (
        <>
          <b className="text-brand-subtle-foreground">
            {fillFormsCopy(t.newCount, { count: form.newCount ?? 0 })}
          </b>
          {" · "}
        </>
      ) : null}
      {fillFormsCopy(t.total, { count: form.totalCount })}
    </>
  );
}

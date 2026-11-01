import { useId, useState } from "react";
import { CircleCheck, Plus, Trash2 } from "lucide-react";
import { Checkbox, Label, Skeleton, cn } from "@notrelix/ui-web";
import { useSettings } from "./board-settings-context";
import { fillCopy, plural } from "./board-settings-format";
import {
  BUTTON_CLASS,
  HINT_CLASS,
  ICON_BUTTON_CLASS,
  INPUT_CLASS,
  SectionHeader,
  SectionLine,
} from "./board-settings-parts";
import type {
  BoardTemplateRow,
  BoardTemplatesPanelProps,
} from "./board-settings-types";
import { WriteError, useWriteErrorProps } from "./board-settings-write-error";

const STATUS_CHIP: Readonly<Record<BoardTemplateRow["status"], string>> = {
  published: "bg-success-subtle text-success-text",
  draft: "bg-warning-subtle text-warning-text",
  archived: "bg-state-hover-strong text-muted-foreground",
};

/**
 * The "Mẫu board" section: save this board as a template (the form grows in
 * place) and manage the workspace's templates: publish a draft, archive a
 * published template, delete any.
 */
export function TemplatesSection({
  panel,
}: {
  panel: BoardTemplatesPanelProps;
}) {
  const { copy, run, settings } = useSettings();
  const t = copy.templates;
  const [adding, setAdding] = useState(false);
  const [saved, setSaved] = useState<string | null>(null);
  const [name, setName] = useState("");
  const [description, setDescription] = useState("");
  const [includeItems, setIncludeItems] = useState(false);
  const [deleteId, setDeleteId] = useState<string | null>(null);
  const nameId = useId();
  const descriptionId = useId();
  const itemsId = useId();
  const errorId = useId();
  const failed = useWriteErrorProps("template-save", errorId);
  const trimmed = name.trim();
  const items = panel.itemCount ?? 0;
  // A template never holds a silently shortened board: past the cap the save is refused.
  const overCap = includeItems && items > 500;

  function open() {
    setSaved(null);
    setName(settings.general.title);
    setDescription("");
    setIncludeItems(false);
    setAdding(true);
  }

  const close = () => setAdding(false);

  async function save() {
    if (trimmed === "") return;
    const result = await run(
      () =>
        panel.onSave({
          name: trimmed,
          ...(description.trim() === ""
            ? {}
            : { description: description.trim() }),
          includeItems,
        }),
      "template-save",
    );
    if (result.ok) {
      setAdding(false);
      setSaved(trimmed);
    }
  }

  return (
    <section
      aria-labelledby="bs-templates-h"
      className="min-w-0 flex-1 overflow-auto px-6 pt-5 pb-6"
    >
      <div className="max-w-[780px]">
        <SectionHeader
          id="bs-templates-h"
          title={t.title}
          intro={t.intro}
          actions={
            <button
              type="button"
              aria-expanded={adding}
              onClick={open}
              className={BUTTON_CLASS.primary}
            >
              <Plus aria-hidden="true" />
              {t.save}
            </button>
          }
        />

        {adding ? (
          <form
            aria-label={fillCopy(t.formLabel, { name: settings.general.title })}
            className="mb-3 flex flex-col gap-3.5 rounded-[12px] border border-primary bg-popover p-4 shadow-[0_0_0_3px_var(--accent-soft)]"
            onSubmit={(event) => {
              event.preventDefault();
              void save();
            }}
          >
            <div className="flex flex-col gap-1.5">
              <Label htmlFor={nameId}>{t.nameLabel}</Label>
              <input
                id={nameId}
                autoFocus
                value={name}
                maxLength={255}
                {...failed}
                onChange={(event) => setName(event.target.value)}
                onKeyDown={(event) => {
                  if (event.key === "Escape") {
                    event.stopPropagation();
                    close();
                  }
                }}
                className={INPUT_CLASS}
              />
            </div>
            <div className="flex flex-col gap-1.5">
              <Label htmlFor={descriptionId}>{t.descriptionLabel}</Label>
              <input
                id={descriptionId}
                value={description}
                maxLength={5000}
                onChange={(event) => setDescription(event.target.value)}
                className={INPUT_CLASS}
              />
            </div>
            <div className="flex items-start gap-2.5">
              <Checkbox
                id={itemsId}
                checked={includeItems}
                disabled={items === 0}
                aria-describedby={`${itemsId}-hint`}
                onCheckedChange={(checked) => setIncludeItems(checked === true)}
                className="mt-0.5"
              />
              <div className="flex flex-col gap-0.5">
                <Label htmlFor={itemsId}>
                  {panel.itemCount === undefined
                    ? t.includeItems
                    : plural(
                        t.includeItemsCount_one,
                        t.includeItemsCount_other,
                        items,
                      )}
                </Label>
                <span id={`${itemsId}-hint`} className={HINT_CLASS}>
                  {t.includeItemsHint}
                </span>
              </div>
            </div>
            {overCap ? (
              <p role="alert" className="m-0 text-crumb text-danger-text">
                {fillCopy(t.includeItemsOverCap, { count: String(items) })}
              </p>
            ) : null}
            <WriteError scope="template-save" id={errorId} />
            <div className="flex items-center justify-end gap-2">
              <button
                type="button"
                onClick={close}
                className={BUTTON_CLASS.ghost}
              >
                {copy.common.cancel}
              </button>
              <button
                type="submit"
                disabled={trimmed === "" || overCap}
                className={BUTTON_CLASS.primary}
              >
                {t.saveConfirm}
              </button>
            </div>
          </form>
        ) : saved ? (
          <div
            role="status"
            className="mb-3 flex items-start gap-2.5 rounded-lg bg-success-subtle px-3 py-2.5 text-item-small text-success-text"
          >
            <CircleCheck
              aria-hidden="true"
              className="mt-0.5 size-4 shrink-0"
            />
            <span className="flex-1">{fillCopy(t.saved, { name: saved })}</span>
            <button
              type="button"
              onClick={open}
              className={cn(BUTTON_CLASS.outline, "h-7")}
            >
              {t.saveAnother}
            </button>
          </div>
        ) : null}

        <SectionLine
          title={t.list}
          count={plural(t.summary_one, t.summary_other, panel.templates.length)}
          note=""
        />

        {panel.status === "loading" ? (
          <div
            role="status"
            aria-label={t.loading}
            className="flex flex-col gap-2"
          >
            <Skeleton className="h-11 rounded-[10px]" />
            <Skeleton className="h-11 rounded-[10px]" />
          </div>
        ) : panel.status === "error" ? (
          <div
            role="alert"
            className="flex items-center gap-3 rounded-[10px] border border-line bg-popover px-3.5 py-3 text-item-ui"
          >
            <span>{t.error}</span>
            <button
              type="button"
              onClick={panel.onReload}
              className={BUTTON_CLASS.outline}
            >
              {t.retry}
            </button>
          </div>
        ) : panel.templates.length === 0 ? (
          <div className="rounded-[10px] border border-dashed border-line-strong bg-popover px-4 py-6 text-center">
            <b className="block text-item-ui font-semibold">{t.empty}</b>
            <span className={HINT_CLASS}>{t.emptyHint}</span>
          </div>
        ) : (
          <ul
            aria-label={t.list}
            className="m-0 flex list-none flex-col overflow-hidden rounded-[10px] border border-line bg-popover p-0"
          >
            {panel.templates.map((template) => (
              <TemplateRow
                key={template.id}
                template={template}
                deleting={deleteId === template.id}
                onDelete={() => setDeleteId(template.id)}
                onDeleteDone={() => setDeleteId(null)}
                panel={panel}
              />
            ))}
          </ul>
        )}
        <p className={cn(HINT_CLASS, "mt-2.5 mb-0 px-0.5")}>{t.footnote}</p>
      </div>
    </section>
  );
}

function TemplateRow({
  template,
  deleting,
  onDelete,
  onDeleteDone,
  panel,
}: {
  template: BoardTemplateRow;
  deleting: boolean;
  onDelete: () => void;
  onDeleteDone: () => void;
  panel: BoardTemplatesPanelProps;
}) {
  const { copy, run } = useSettings();
  const t = copy.templates;
  const scope = `template:${template.id}`;
  const errorId = useId();

  async function confirmDelete() {
    const result = await run(() => panel.onDelete(template.id), scope);
    if (result.ok) onDeleteDone();
  }

  return (
    <li className="border-t border-line first:border-t-0">
      <div className="flex min-h-[52px] items-center gap-2.5 pr-2.5 pl-3.5 text-item-ui">
        <span className="flex min-w-0 flex-1 flex-col">
          <b className="truncate text-item-ui font-semibold">{template.name}</b>
          {template.description ? (
            <span className={cn(HINT_CLASS, "truncate")}>
              {template.description}
            </span>
          ) : null}
        </span>
        <span
          className={cn(
            "inline-flex h-5.5 items-center rounded-[6px] px-2 text-micro font-semibold",
            STATUS_CHIP[template.status],
          )}
        >
          {t.status[template.status]}
        </span>
        {template.status === "draft" ? (
          <button
            type="button"
            aria-label={fillCopy(t.publish, { name: template.name })}
            onClick={() => void run(() => panel.onPublish(template.id), scope)}
            className={cn(BUTTON_CLASS.primary, "h-7")}
          >
            {t.publishShort}
          </button>
        ) : null}
        {template.status === "published" ? (
          <button
            type="button"
            aria-label={fillCopy(t.archive, { name: template.name })}
            onClick={() => void run(() => panel.onArchive(template.id), scope)}
            className={cn(BUTTON_CLASS.outline, "h-7")}
          >
            {t.archiveShort}
          </button>
        ) : null}
        <button
          type="button"
          aria-label={fillCopy(t.delete, { name: template.name })}
          aria-expanded={deleting}
          onClick={onDelete}
          className={ICON_BUTTON_CLASS}
        >
          <Trash2 aria-hidden="true" />
        </button>
      </div>
      <div className="px-3.5 empty:hidden">
        <WriteError scope={scope} id={errorId} />
      </div>
      {deleting ? (
        <div
          role="group"
          aria-label={fillCopy(t.deleteQuestion, { name: template.name })}
          className="flex flex-col gap-2.5 border-t border-line bg-background px-3.5 pt-3 pb-3.5"
        >
          <p className="m-0 text-item-small text-muted-foreground">
            <b className="font-semibold text-foreground">
              {fillCopy(t.deleteQuestion, { name: template.name })}
            </b>{" "}
            {t.deleteBody}
          </p>
          <div className="flex items-center justify-end gap-2">
            <button
              type="button"
              onClick={onDeleteDone}
              className={BUTTON_CLASS.outline}
            >
              {t.keep}
            </button>
            <button
              type="button"
              onClick={() => void confirmDelete()}
              className={BUTTON_CLASS.danger}
            >
              {t.deleteConfirm}
            </button>
          </div>
        </div>
      ) : null}
    </li>
  );
}

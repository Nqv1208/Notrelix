import { Check, Download, Plus, Upload } from "lucide-react";
import { cn } from "@notrelix/ui-web";
import type { CardDetail, CardFile } from "@notrelix/work-management-core";
import { formatItemDate, itemDueState } from "../shared/item-display";
import { fillSheetCopy } from "../card-detail/item-sheet.copy";
import type {
  ItemSheetChildItem,
  TaskDetailCapabilities,
} from "../card-detail/task-detail-types";
import type { ItemPageCopy } from "./item-page.copy";
import {
  describeFile,
  formatFileSize,
  type FileKind,
} from "./item-page-format";
import {
  BUTTON_SM_VARIANT_CLASS,
  DisabledControl,
  ICON_BUTTON_CLASS,
  PersonMark,
  SectionHeading,
} from "./item-page-parts";

/** Blocks of a description: paragraphs, and "- " lines as a bulleted list. */
function descriptionBlocks(text: string) {
  return text
    .trim()
    .split(/\n{2,}/)
    .map((block) => {
      const lines = block.split("\n").map((line) => line.trimEnd());
      const items = lines.every((line) => /^\s*[-*]\s+/.test(line))
        ? lines.map((line) => line.replace(/^\s*[-*]\s+/, ""))
        : null;
      return items
        ? { kind: "list" as const, items }
        : { kind: "text" as const, text: block };
    });
}

export function ItemPageDescription({
  card,
  copy,
}: {
  card: CardDetail;
  copy: ItemPageCopy;
}) {
  const text = card.descriptionMd?.trim();
  return (
    <section aria-labelledby="item-page-description">
      <SectionHeading id="item-page-description">
        {copy.description.heading}
      </SectionHeading>
      {text ? (
        <div className="max-w-[68ch] text-item-prose text-foreground">
          {descriptionBlocks(text).map((block, index) =>
            block.kind === "list" ? (
              <ul key={index} className="m-0 mb-1 pl-5">
                {block.items.map((item) => (
                  <li key={item} className="my-[3px]">
                    {item}
                  </li>
                ))}
              </ul>
            ) : (
              <p key={index} className="m-0 mb-2.5 whitespace-pre-line">
                {block.text}
              </p>
            ),
          )}
        </div>
      ) : (
        <p className="m-0 text-item-text text-muted-foreground">
          {copy.description.empty}
        </p>
      )}
    </section>
  );
}

/**
 * "Việc con": the count and the roll-up from the item's `childProgress`; the
 * rows only when the item detail carries them. There is no producer for
 * sub-items yet, so adding one and ticking one stay disabled with the reason.
 */
export function ItemPageChildItems({
  card,
  items,
  capabilities,
  copy,
  now,
  timeZone,
}: {
  card: CardDetail;
  items: readonly ItemSheetChildItem[] | undefined;
  capabilities: TaskDetailCapabilities;
  copy: ItemPageCopy;
  now: Date;
  timeZone?: string;
}) {
  const t = copy.children;
  const rows = items ?? [];
  const progress =
    rows.length > 0
      ? { done: rows.filter((row) => row.done).length, total: rows.length }
      : card.childProgress && card.childProgress.total > 0
        ? card.childProgress
        : null;
  const percent = progress
    ? Math.round((progress.done / progress.total) * 100)
    : null;
  const showAdd = capabilities.canEditFields;

  return (
    <section aria-labelledby="item-page-children">
      <SectionHeading
        id="item-page-children"
        count={progress ? fillSheetCopy(t.progress, progress) : undefined}
      >
        {t.heading}
      </SectionHeading>
      {percent !== null ? (
        <div className="-mt-0.5 mb-2 flex items-center gap-2.5 text-crumb text-meta-foreground tabular-nums">
          <span
            role="progressbar"
            aria-valuemin={0}
            aria-valuemax={100}
            aria-valuenow={percent}
            className="h-1.5 w-40 flex-none overflow-hidden rounded-[3px] bg-state-hover-strong"
          >
            <span
              className="block h-full rounded-[3px] bg-work-status-done"
              style={{ width: `${percent}%` }}
            />
          </span>
          {fillSheetCopy(t.rollup, { percent })}
        </div>
      ) : null}
      {rows.length > 0 || showAdd ? (
        <div
          role="list"
          className="overflow-hidden rounded-[10px] border border-line bg-popover"
        >
          {rows.map((row) => (
            <ChildRow
              key={row.id}
              row={row}
              copy={copy}
              now={now}
              timeZone={timeZone}
            />
          ))}
          {showAdd ? (
            <DisabledControl reason={t.addUnavailable}>
              <label
                className={cn(
                  "grid min-h-10.5 grid-cols-[20px_1fr] items-center gap-2.5 px-3 text-item-text text-meta-foreground",
                  rows.length > 0 && "min-h-10.75 border-t border-line",
                )}
              >
                <Plus aria-hidden="true" className="size-4" />
                <input
                  type="text"
                  readOnly
                  aria-label={t.heading}
                  placeholder={t.add}
                  className="min-w-0 border-0 bg-transparent p-0 text-item-text text-foreground outline-none placeholder:text-meta-foreground"
                />
              </label>
            </DisabledControl>
          ) : null}
        </div>
      ) : (
        <p className="m-0 text-item-text text-muted-foreground">{t.empty}</p>
      )}
    </section>
  );
}

function ChildRow({
  row,
  copy,
  now,
  timeZone,
}: {
  row: ItemSheetChildItem;
  copy: ItemPageCopy;
  now: Date;
  timeZone?: string;
}) {
  const due = itemDueState(row.dueAt, now, timeZone);
  const overdue = due?.kind === "overdue" && !row.done;
  return (
    <div
      role="listitem"
      className="grid min-h-10.5 grid-cols-[20px_minmax(0,1fr)_92px_84px_28px] items-center gap-2.5 border-t border-line px-3 text-item-text first:border-t-0 not-first:min-h-10.75"
    >
      <DisabledControl reason={copy.children.addUnavailable}>
        <button
          type="button"
          aria-disabled="true"
          role="checkbox"
          aria-checked={row.done}
          aria-label={row.title}
          className={cn(
            "grid size-4 flex-none place-items-center rounded-[4px] border-[1.5px] p-0 text-background",
            row.done
              ? "border-work-status-done bg-work-status-done"
              : "border-line-strong bg-transparent",
          )}
        >
          {row.done ? (
            <Check aria-hidden="true" className="size-2.75" strokeWidth={3} />
          ) : null}
        </button>
      </DisabledControl>
      <span
        className={cn(
          "min-w-0 truncate",
          row.done && "text-meta-foreground line-through",
        )}
      >
        {row.title}
      </span>
      <span className="font-mono text-item-note text-meta-foreground">
        {row.itemKey}
      </span>
      <span
        className={cn(
          "text-right text-crumb tabular-nums text-meta-foreground",
          overdue && "font-semibold text-danger-text",
        )}
      >
        {row.dueAt ? formatItemDate(row.dueAt, now, timeZone) : null}
      </span>
      {row.assignee ? (
        <PersonMark
          person={row.assignee}
          title={row.assignee.name}
          className="size-5.5 justify-self-end text-mark-xs"
        />
      ) : (
        <span />
      )}
    </div>
  );
}

/**
 * "Liên kết": links between items have no producer yet, so the section keeps
 * its place with the add action disabled and says why.
 */
export function ItemPageLinks({ copy }: { copy: ItemPageCopy }) {
  const t = copy.links;
  return (
    <section aria-labelledby="item-page-links">
      <SectionHeading
        id="item-page-links"
        action={
          <DisabledControl reason={t.addUnavailable}>
            <button
              type="button"
              aria-disabled="true"
              className={BUTTON_SM_VARIANT_CLASS.ghost}
            >
              <Plus aria-hidden="true" className="size-4" />
              {t.add}
            </button>
          </DisabledControl>
        }
      >
        {t.heading}
      </SectionHeading>
      <p className="m-0 text-item-text text-muted-foreground">{t.empty}</p>
    </section>
  );
}

const FILE_KIND_CLASS: Readonly<Record<FileKind, string>> = {
  sheet: "bg-item-file-sheet text-item-file-sheet-foreground",
  pdf: "bg-item-file-pdf text-item-file-pdf-foreground",
  other: "bg-state-hover-strong text-muted-foreground",
};

export function ItemPageFiles({
  files,
  capabilities,
  copy,
  locale,
  now,
  timeZone,
}: {
  files: readonly CardFile[];
  capabilities: TaskDetailCapabilities;
  copy: ItemPageCopy;
  locale: string;
  now: Date;
  timeZone?: string;
}) {
  const t = copy.files;
  return (
    <section aria-labelledby="item-page-files">
      <SectionHeading
        id="item-page-files"
        count={files.length > 0 ? files.length : undefined}
      >
        {t.heading}
      </SectionHeading>
      {files.length === 0 && !capabilities.canEditFields ? (
        <p className="m-0 text-item-text text-muted-foreground">{t.empty}</p>
      ) : null}
      {files.map((file) => {
        const kind = describeFile(file);
        const date = formatItemDate(file.createdAt, now, timeZone);
        return (
          <div
            key={file.id}
            className="flex min-h-11.5 items-center gap-2.5 rounded-lg px-2.5 hover:bg-state-hover-strong"
          >
            <span
              aria-hidden="true"
              className={cn(
                "grid size-8.5 flex-none place-items-center rounded-[7px] text-avatar-md tracking-[0.02em]",
                FILE_KIND_CLASS[kind.kind],
              )}
            >
              {kind.label}
            </span>
            <span className="min-w-0">
              <span className="block truncate text-item-ui font-medium text-foreground">
                {file.name}
              </span>
              <small className="block text-item-note text-meta-foreground">
                {fillSheetCopy(t.meta, {
                  size: formatFileSize(file.size, locale),
                  author: file.createdBy.name,
                  date: date ?? "",
                })}
              </small>
            </span>
            {file.url ? (
              <a
                href={file.url}
                download={file.name}
                aria-label={fillSheetCopy(t.download, { name: file.name })}
                className={cn(ICON_BUTTON_CLASS, "ml-auto size-7")}
              >
                <Download aria-hidden="true" />
              </a>
            ) : (
              <DisabledControl reason={t.downloadUnavailable}>
                <button
                  type="button"
                  aria-disabled="true"
                  aria-label={fillSheetCopy(t.download, { name: file.name })}
                  className={cn(ICON_BUTTON_CLASS, "ml-auto size-7")}
                >
                  <Download aria-hidden="true" />
                </button>
              </DisabledControl>
            )}
          </div>
        );
      })}
      {capabilities.canEditFields ? (
        <DisabledControl reason={t.dropUnavailable}>
          <div
            role="button"
            tabIndex={0}
            className="mt-1.5 flex h-10.5 items-center justify-center gap-2 rounded-[8px] border border-dashed border-line-strong text-item-small text-meta-foreground outline-none focus-visible:ring-2 focus-visible:ring-focus-ring"
          >
            <Upload aria-hidden="true" className="size-4" />
            <span>{t.drop}</span>
            <u className="font-medium text-brand-subtle-foreground no-underline">
              {t.choose}
            </u>
          </div>
        </DisabledControl>
      ) : null}
    </section>
  );
}

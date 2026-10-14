import { useRef, useState } from "react";
import { Check, Download, Plus, Upload, X } from "lucide-react";
import {
  Command,
  CommandEmpty,
  CommandGroup,
  CommandInput,
  CommandItem,
  CommandList,
  Popover,
  PopoverContent,
  PopoverTrigger,
  cn,
} from "@notrelix/ui-web";
import type {
  CardDetail,
  CardFile,
  CardUpdate,
} from "@notrelix/work-management-core";
import {
  formatItemDate,
  itemDueState,
  toValidDate,
} from "../shared/item-display";
import { ItemDescriptionBody } from "../shared/item-description";
import { fillSheetCopy } from "../card-detail/item-sheet.copy";
import type {
  ItemSheetChildItem,
  ItemSheetLink,
  ItemSheetLinkCandidate,
  TaskDetailCallbacks,
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
        <ItemDescriptionBody
          text={text}
          workspaceId={card.workspaceId}
          className="text-item-prose"
        />
      ) : (
        <p className="m-0 text-item-text text-muted-foreground">
          {copy.description.empty}
        </p>
      )}
    </section>
  );
}

/**
 * "Việc con": the count and the roll-up from the item's children, the rows
 * with their key, due day and assignee, a checkbox that ticks a child and the
 * input that adds one (Enter saves). A viewer who cannot edit sees the rows
 * without the controls.
 */
export function ItemPageChildItems({
  card,
  items,
  capabilities,
  callbacks,
  copy,
  now,
  timeZone,
}: {
  card: CardDetail;
  items: readonly ItemSheetChildItem[] | undefined;
  capabilities: TaskDetailCapabilities;
  callbacks: Pick<TaskDetailCallbacks, "onToggleChildItem" | "onAddChildItem">;
  copy: ItemPageCopy;
  now: Date;
  timeZone?: string;
}) {
  const t = copy.children;
  const rows = items ?? [];
  const [draft, setDraft] = useState("");
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
  const canAdd = showAdd && Boolean(callbacks.onAddChildItem);

  const submit = () => {
    const title = draft.trim();
    if (!title) return;
    callbacks.onAddChildItem?.(title);
    setDraft("");
  };

  const addInput = (
    <label
      className={cn(
        "grid min-h-10.5 grid-cols-[20px_1fr] items-center gap-2.5 px-3 text-item-text text-meta-foreground",
        rows.length > 0 && "min-h-10.75 border-t border-line",
      )}
    >
      <Plus aria-hidden="true" className="size-4" />
      <input
        type="text"
        readOnly={!canAdd}
        value={draft}
        onChange={(event) => setDraft(event.target.value)}
        onKeyDown={(event) => {
          if (event.key !== "Enter") return;
          event.preventDefault();
          submit();
        }}
        aria-label={t.heading}
        placeholder={t.add}
        className="min-w-0 border-0 bg-transparent p-0 text-item-text text-foreground outline-none placeholder:text-meta-foreground"
      />
    </label>
  );

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
              onToggle={
                capabilities.canEditFields
                  ? callbacks.onToggleChildItem
                  : undefined
              }
            />
          ))}
          {showAdd ? (
            canAdd ? (
              addInput
            ) : (
              <DisabledControl reason={t.addUnavailable}>
                {addInput}
              </DisabledControl>
            )
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
  onToggle,
}: {
  row: ItemSheetChildItem;
  copy: ItemPageCopy;
  now: Date;
  timeZone?: string;
  onToggle?: (childId: string, done: boolean) => void;
}) {
  const due = itemDueState(row.dueAt, now, timeZone);
  const overdue = due?.kind === "overdue" && !row.done;
  const box = (
    <button
      type="button"
      aria-disabled={onToggle ? undefined : "true"}
      role="checkbox"
      aria-checked={row.done}
      aria-label={row.title}
      onClick={onToggle ? () => onToggle(row.id, !row.done) : undefined}
      className={cn(
        "grid size-4 flex-none place-items-center rounded-[4px] border-[1.5px] p-0 text-background outline-none focus-visible:ring-2 focus-visible:ring-focus-ring",
        onToggle ? "cursor-pointer" : "cursor-default",
        row.done
          ? "border-work-status-done bg-work-status-done"
          : "border-line-strong bg-transparent",
      )}
    >
      {row.done ? (
        <Check aria-hidden="true" className="size-2.75" strokeWidth={3} />
      ) : null}
    </button>
  );
  return (
    <div
      role="listitem"
      className="grid min-h-10.5 grid-cols-[20px_minmax(0,1fr)_92px_84px_28px] items-center gap-2.5 border-t border-line px-3 text-item-text first:border-t-0 not-first:min-h-10.75"
    >
      {onToggle ? (
        box
      ) : (
        <DisabledControl reason={copy.children.toggleUnavailable}>
          {box}
        </DisabledControl>
      )}
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

const LINK_GROUPS = ["blocked-by", "blocks", "related"] as const;

const LINK_DOT_CLASS: Readonly<
  Record<NonNullable<ItemSheetLink["item"]["statusCategory"]>, string>
> = {
  done: "bg-work-status-done",
  in_progress: "bg-work-status-doing",
  review: "bg-work-status-review",
  todo: "shadow-[inset_0_0_0_2px_var(--work-status-todo)]",
};

/**
 * "Liên kết": the items this one waits for, the items it blocks and the
 * related ones, each with its status; "Thêm" links another item of the board.
 */
export function ItemPageLinks({
  links,
  candidates,
  capabilities,
  callbacks,
  copy,
}: {
  links: readonly ItemSheetLink[] | undefined;
  candidates: readonly ItemSheetLinkCandidate[] | undefined;
  capabilities: TaskDetailCapabilities;
  callbacks: Pick<TaskDetailCallbacks, "onAddLink" | "onRemoveLink">;
  copy: ItemPageCopy;
}) {
  const t = copy.links;
  const canEdit = capabilities.canEditFields && Boolean(callbacks.onAddLink);
  const rows = links ?? [];
  return (
    <section aria-labelledby="item-page-links">
      <SectionHeading
        id="item-page-links"
        count={links && rows.length > 0 ? rows.length : undefined}
        action={
          canEdit ? (
            <LinkPicker
              candidates={candidates ?? []}
              linked={new Set(rows.map((link) => link.item.id))}
              copy={copy}
              onPick={(itemId, relation) =>
                callbacks.onAddLink?.(itemId, relation)
              }
            />
          ) : !capabilities.canEditFields ? null : (
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
          )
        }
      >
        {t.heading}
      </SectionHeading>
      {!links ? (
        <p className="m-0 text-item-text text-muted-foreground">{t.empty}</p>
      ) : rows.length === 0 ? (
        <p className="m-0 text-item-text text-muted-foreground">{t.none}</p>
      ) : (
        LINK_GROUPS.map((group) => {
          const inGroup = rows.filter((link) => link.relation === group);
          if (inGroup.length === 0) return null;
          return (
            <div key={group} className="mb-3 last:mb-0">
              <div className="mb-1 text-item-note font-semibold text-meta-foreground">
                {t.groups[group]}
              </div>
              {inGroup.map((link) => (
                <div
                  key={link.id}
                  className="group/link flex min-h-9 items-center gap-2.5 rounded-lg pl-2 text-item-small"
                >
                  <span
                    aria-hidden="true"
                    className={cn(
                      "size-2.25 shrink-0 rounded-full",
                      link.item.statusCategory
                        ? LINK_DOT_CLASS[link.item.statusCategory]
                        : "bg-muted-foreground",
                    )}
                  />
                  <span className="font-mono text-item-note text-meta-foreground">
                    {link.item.key}
                  </span>
                  <span className="min-w-0 flex-1 truncate text-foreground">
                    {link.item.title}
                  </span>
                  <span className="text-item-note text-meta-foreground">
                    {link.item.statusName ?? t.statusNone}
                  </span>
                  {capabilities.canEditFields && callbacks.onRemoveLink ? (
                    <button
                      type="button"
                      aria-label={fillSheetCopy(t.remove, {
                        key: link.item.key ?? link.item.title,
                      })}
                      onClick={() => callbacks.onRemoveLink?.(link.id)}
                      className={cn(
                        ICON_BUTTON_CLASS,
                        "size-6 w-0 overflow-hidden opacity-0 group-hover/link:w-6 group-hover/link:opacity-100 focus-visible:w-6 focus-visible:opacity-100",
                      )}
                    >
                      <X aria-hidden="true" className="size-3.5" />
                    </button>
                  ) : null}
                </div>
              ))}
            </div>
          );
        })
      )}
    </section>
  );
}

function LinkPicker({
  candidates,
  linked,
  copy,
  onPick,
}: {
  candidates: readonly ItemSheetLinkCandidate[];
  linked: ReadonlySet<string>;
  copy: ItemPageCopy;
  onPick: (itemId: string, relation: ItemSheetLink["relation"]) => void;
}) {
  const t = copy.links;
  const [open, setOpen] = useState(false);
  const [relation, setRelation] =
    useState<ItemSheetLink["relation"]>("blocked-by");
  return (
    <Popover open={open} onOpenChange={setOpen}>
      <PopoverTrigger asChild>
        <button type="button" className={BUTTON_SM_VARIANT_CLASS.ghost}>
          <Plus aria-hidden="true" className="size-4" />
          {t.add}
        </button>
      </PopoverTrigger>
      <PopoverContent
        align="end"
        aria-label={t.picker.label}
        className="w-80 gap-2 p-2"
      >
        <div
          role="radiogroup"
          aria-label={t.picker.relation}
          className="flex gap-1"
        >
          {LINK_GROUPS.map((value) => (
            <button
              key={value}
              type="button"
              role="radio"
              aria-checked={relation === value}
              onClick={() => setRelation(value)}
              className={cn(
                "h-7 flex-1 cursor-pointer rounded-[7px] border-0 px-2 text-crumb font-medium",
                relation === value
                  ? "bg-state-selected text-state-selected-foreground"
                  : "bg-transparent text-muted-foreground hover:bg-state-hover-strong",
              )}
            >
              {t.picker.relations[value]}
            </button>
          ))}
        </div>
        <Command>
          <CommandInput placeholder={t.picker.find} />
          <CommandList>
            <CommandEmpty>{t.picker.empty}</CommandEmpty>
            <CommandGroup>
              {candidates
                .filter((candidate) => !linked.has(candidate.id))
                .map((candidate) => (
                  <CommandItem
                    key={candidate.id}
                    value={`${candidate.key ?? ""} ${candidate.title}`}
                    onSelect={() => {
                      setOpen(false);
                      onPick(candidate.id, relation);
                    }}
                  >
                    <span className="font-mono text-item-note text-meta-foreground">
                      {candidate.key}
                    </span>
                    <span className="truncate">{candidate.title}</span>
                  </CommandItem>
                ))}
            </CommandGroup>
          </CommandList>
        </Command>
      </PopoverContent>
    </Popover>
  );
}

const FILE_KIND_CLASS: Readonly<Record<FileKind, string>> = {
  sheet: "bg-item-file-sheet text-item-file-sheet-foreground",
  pdf: "bg-item-file-pdf text-item-file-pdf-foreground",
  other: "bg-state-hover-strong text-muted-foreground",
};

export function ItemPageFiles({
  files,
  updates,
  capabilities,
  callbacks,
  copy,
  locale,
  now,
  timeZone,
}: {
  files: readonly CardFile[];
  /** The comments, to tell which file came with one. */
  updates: readonly CardUpdate[];
  capabilities: TaskDetailCapabilities;
  callbacks: Pick<TaskDetailCallbacks, "onAddFile">;
  copy: ItemPageCopy;
  locale: string;
  now: Date;
  timeZone?: string;
}) {
  const t = copy.files;
  const input = useRef<HTMLInputElement>(null);
  const [over, setOver] = useState(false);
  const canAdd = capabilities.canEditFields && Boolean(callbacks.onAddFile);
  const withComment = new Set(
    updates.flatMap((update) => update.attachmentIds),
  );
  const add = (list: FileList | null) => {
    for (const file of Array.from(list ?? [])) callbacks.onAddFile?.(file);
  };
  const zone = (
    <div
      role="button"
      tabIndex={0}
      onClick={canAdd ? () => input.current?.click() : undefined}
      onKeyDown={
        canAdd
          ? (event) => {
              if (event.key === "Enter" || event.key === " ") {
                event.preventDefault();
                input.current?.click();
              }
            }
          : undefined
      }
      onDragOver={
        canAdd
          ? (event) => {
              event.preventDefault();
              setOver(true);
            }
          : undefined
      }
      onDragLeave={canAdd ? () => setOver(false) : undefined}
      onDrop={
        canAdd
          ? (event) => {
              event.preventDefault();
              setOver(false);
              add(event.dataTransfer.files);
            }
          : undefined
      }
      className={cn(
        "mt-1.5 flex h-10.5 items-center justify-center gap-2 rounded-[8px] border border-dashed border-line-strong text-item-small text-meta-foreground outline-none focus-visible:ring-2 focus-visible:ring-focus-ring",
        canAdd && "cursor-pointer",
        over && "border-primary bg-state-selected",
      )}
    >
      <Upload aria-hidden="true" className="size-4" />
      <span>{t.drop}</span>
      <u className="font-medium text-brand-subtle-foreground no-underline">
        {t.choose}
      </u>
    </div>
  );
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
        const created = toValidDate(file.createdAt);
        const sameDay =
          created !== null &&
          formatItemDate(created, now, timeZone) ===
            formatItemDate(now, now, timeZone);
        const date = sameDay
          ? t.today
          : (formatItemDate(file.createdAt, now, timeZone) ?? "");
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
                {fillSheetCopy(
                  withComment.has(file.id) ? t.metaWithComment : t.meta,
                  {
                    size: formatFileSize(file.size, locale),
                    author: file.createdBy.name,
                    date,
                  },
                )}
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
        <>
          <input
            ref={input}
            type="file"
            multiple
            hidden
            aria-label={t.choose}
            onChange={(event) => {
              add(event.target.files);
              event.target.value = "";
            }}
          />
          {canAdd ? (
            zone
          ) : (
            <DisabledControl reason={t.dropUnavailable}>{zone}</DisabledControl>
          )}
        </>
      ) : null}
    </section>
  );
}

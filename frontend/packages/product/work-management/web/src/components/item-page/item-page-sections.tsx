import { useEffect, useRef, useState, type DragEvent } from "react";
import {
  AlertTriangle,
  Check,
  Download,
  Plus,
  Search,
  Trash2,
  Upload,
  X,
} from "lucide-react";
import { Popover, PopoverContent, PopoverTrigger, cn } from "@notrelix/ui-web";
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
import { ItemDescriptionEditor } from "../shared/item-description-editor";
import { fillSheetCopy } from "../card-detail/item-sheet.copy";
import { PendingMark } from "./item-page-states";
import { foldForSearch } from "./item-pickers";
import type { ItemPageUpload } from "./item-page-types";
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
  AddRow,
  BUTTON_SM_VARIANT_CLASS,
  DisabledControl,
  ICON_BUTTON_CLASS,
  PersonMark,
  SectionHeading,
} from "./item-page-parts";

/**
 * Lines the description shows before "Xem toàn bộ mô tả". The States mockup
 * says six; the Detail mockup shows ERP-142's seven-line description whole,
 * so the cut is made at eight.
 */
const DESCRIPTION_LINES = 8;

export function ItemPageDescription({
  card,
  copy,
  editable = false,
  onSave,
}: {
  card: CardDetail;
  copy: ItemPageCopy;
  /** The viewer may edit the item: an empty description invites writing one. */
  editable?: boolean;
  /** Saves the description (an empty text clears it). Without it the section reads only. */
  onSave?: (descriptionMd: string) => void;
}) {
  const text = card.descriptionMd?.trim();
  const [editing, setEditing] = useState(false);
  const canEdit = editable && onSave !== undefined;
  const bodyRef = useRef<HTMLDivElement>(null);
  const [expanded, setExpanded] = useState(false);
  const [overflows, setOverflows] = useState(false);
  useEffect(() => {
    const node = bodyRef.current;
    if (!node) return;
    // Measured on the clamped box: a longer scroll height means lines are cut.
    setOverflows(node.scrollHeight > node.clientHeight + 1);
  }, [text, expanded]);
  const t = copy.states.long;
  return (
    <section aria-labelledby="item-page-description">
      <SectionHeading id="item-page-description">
        {copy.description.heading}
      </SectionHeading>
      {editing && canEdit ? (
        <ItemDescriptionEditor
          initial={card.descriptionMd ?? ""}
          labels={{
            heading: copy.description.heading,
            placeholder: copy.states.empty.description,
            save: copy.description.save,
            cancel: copy.description.cancel,
            tooLong: copy.description.tooLong,
          }}
          onSave={onSave}
          onClose={() => setEditing(false)}
        />
      ) : text ? (
        <>
          <div
            ref={bodyRef}
            style={
              expanded
                ? undefined
                : {
                    display: "-webkit-box",
                    WebkitBoxOrient: "vertical",
                    WebkitLineClamp: DESCRIPTION_LINES,
                    overflow: "hidden",
                  }
            }
          >
            <ItemDescriptionBody
              text={text}
              workspaceId={card.workspaceId}
              className="text-item-prose"
            />
          </div>
          <div className="mt-2 flex items-center gap-4">
            {overflows || expanded ? (
              <button
                type="button"
                aria-expanded={expanded}
                onClick={() => setExpanded((open) => !open)}
                className="border-0 bg-transparent p-0 text-item-ui font-semibold text-brand-subtle-foreground"
              >
                {expanded ? t.hideDescription : t.showDescription}
              </button>
            ) : null}
            {canEdit ? (
              <button
                type="button"
                onClick={() => setEditing(true)}
                className="border-0 bg-transparent p-0 text-item-ui font-semibold text-brand-subtle-foreground"
              >
                {copy.description.edit}
              </button>
            ) : null}
          </div>
        </>
      ) : canEdit ? (
        <button
          type="button"
          onClick={() => setEditing(true)}
          className="m-0 w-full cursor-text rounded-[6px] border-0 bg-transparent p-0 text-left text-item-text text-meta-foreground hover:text-muted-foreground"
        >
          {copy.states.empty.description}
        </button>
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
/** Sub-items shown before "Hiện N việc con còn lại". */
const CHILD_ROWS_SHOWN = 8;

export function ItemPageChildItems({
  card,
  items,
  capabilities,
  callbacks,
  copy,
  now,
  timeZone,
  pending = false,
  collapseEmpty = false,
}: {
  /** Nothing yet folds into one "+ Thêm việc con" row; a reader sees nothing. */
  collapseEmpty?: boolean;
  card: CardDetail;
  items: readonly ItemSheetChildItem[] | undefined;
  capabilities: TaskDetailCapabilities;
  callbacks: Pick<TaskDetailCallbacks, "onToggleChildItem" | "onAddChildItem">;
  copy: ItemPageCopy;
  now: Date;
  timeZone?: string;
  /** A sub-item write waits for the network. */
  pending?: boolean;
}) {
  const t = copy.children;
  const rows = items ?? [];
  const [draft, setDraft] = useState("");
  const [adding, setAdding] = useState(false);
  const [showAll, setShowAll] = useState(false);
  const visibleRows =
    showAll || rows.length <= CHILD_ROWS_SHOWN + 2
      ? rows
      : rows.slice(0, CHILD_ROWS_SHOWN);
  const hiddenRows = rows.length - visibleRows.length;
  const progress =
    rows.length > 0
      ? { done: rows.filter((row) => row.done).length, total: rows.length }
      : card.childProgress && card.childProgress.total > 0
        ? card.childProgress
        : null;
  const percent = progress
    ? Math.round((progress.done / progress.total) * 100)
    : null;
  const canAdd =
    capabilities.canEditFields && Boolean(callbacks.onAddChildItem);
  const showAdd = canAdd;

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
        autoFocus={adding}
        className="min-w-0 border-0 bg-transparent p-0 text-item-text text-foreground outline-none placeholder:text-meta-foreground"
      />
    </label>
  );

  if (collapseEmpty && rows.length === 0 && !progress) {
    if (!canAdd) return null;
    return (
      <section aria-label={t.heading}>
        {adding ? (
          <div className="overflow-hidden rounded-[10px] border border-line bg-popover">
            {addInput}
          </div>
        ) : (
          <AddRow onClick={() => setAdding(true)}>
            {copy.states.empty.addChild}
          </AddRow>
        )}
      </section>
    );
  }

  return (
    <section aria-labelledby="item-page-children">
      <SectionHeading
        id="item-page-children"
        count={
          progress ? (
            <>
              {fillSheetCopy(t.progress, progress)}
              {pending ? <PendingMark copy={copy} /> : null}
            </>
          ) : undefined
        }
      >
        {t.heading}
      </SectionHeading>
      {percent !== null ? (
        <div className="-mt-0.5 mb-2 flex items-center gap-2.5 text-crumb text-meta-foreground tabular-nums">
          <span
            role="progressbar"
            aria-label={t.heading}
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
      {rows.length === 0 && showAdd && !adding ? (
        <AddRow onClick={() => setAdding(true)}>
          {copy.states.empty.addChild}
        </AddRow>
      ) : rows.length > 0 || showAdd ? (
        <div className="overflow-hidden rounded-[10px] border border-line bg-popover">
          <div role="list">
            {visibleRows.map((row) => (
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
          </div>
          {hiddenRows > 0 || (showAll && rows.length > CHILD_ROWS_SHOWN + 2) ? (
            <button
              type="button"
              aria-expanded={showAll}
              onClick={() => setShowAll((open) => !open)}
              className="block w-full border-0 border-t border-line bg-transparent px-3 py-2.5 text-left text-item-ui font-semibold text-brand-subtle-foreground"
            >
              {showAll
                ? copy.states.long.hideChildren
                : fillSheetCopy(copy.states.long.showChildren, {
                    count: hiddenRows,
                  })}
            </button>
          ) : null}
          {showAdd ? addInput : null}
        </div>
      ) : (
        <p className="m-0 text-item-text text-muted-foreground">
          {copy.states.empty.children}
        </p>
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
  itemKey,
  place,
  named = false,
  collapseEmpty = false,
}: {
  /** Nothing yet folds into one "+ Thêm liên kết" row; a reader sees nothing. */
  collapseEmpty?: boolean;
  itemKey?: string | null;
  place?: string | null;
  /** Names the add button in full ("Add a link"): set where another "Add" shares the screen. */
  named?: boolean;
  links: readonly ItemSheetLink[] | undefined;
  candidates: readonly ItemSheetLinkCandidate[] | undefined;
  capabilities: TaskDetailCapabilities;
  callbacks: Pick<TaskDetailCallbacks, "onAddLink" | "onRemoveLink">;
  copy: ItemPageCopy;
}) {
  const t = copy.links;
  const canEdit = capabilities.canEditFields && Boolean(callbacks.onAddLink);
  const rows = links ?? [];
  if (collapseEmpty && rows.length === 0) {
    if (!canEdit || !links) return null;
    return (
      <section aria-label={t.heading}>
        <LinkPicker
          candidates={candidates ?? []}
          links={rows}
          itemKey={itemKey ?? null}
          place={place ?? null}
          copy={copy}
          named
          row
          onPick={(itemId, relation) => callbacks.onAddLink?.(itemId, relation)}
        />
      </section>
    );
  }
  return (
    <section aria-labelledby="item-page-links">
      <SectionHeading
        id="item-page-links"
        count={links && rows.length > 0 ? rows.length : undefined}
        action={
          canEdit ? (
            <LinkPicker
              candidates={candidates ?? []}
              links={rows}
              itemKey={itemKey ?? null}
              place={place ?? null}
              copy={copy}
              named={named}
              onPick={(itemId, relation) =>
                callbacks.onAddLink?.(itemId, relation)
              }
            />
          ) : null
        }
      >
        {t.heading}
      </SectionHeading>
      {!links ? null : rows.length === 0 ? (
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

/**
 * "Thêm liên kết": the relation first (waiting, blocking, related), then the
 * item to link, found by key or title. An item already linked is listed and
 * cannot be chosen again; one that would make the two wait for each other is
 * listed with the reason.
 */
function LinkPicker({
  candidates,
  links,
  itemKey,
  place,
  copy,
  named = false,
  row = false,
  onPick,
}: {
  /** The trigger is the dashed "+ …" row of a collapsed section. */
  row?: boolean;
  /** The trigger carries its full name (the side sheet has another "Add" beside it). */
  named?: boolean;
  candidates: readonly ItemSheetLinkCandidate[];
  links: readonly ItemSheetLink[];
  /** Key of the item the link is added to. */
  itemKey: string | null;
  /** Where the candidates live, "CN · Triển khai ERP Q4". */
  place: string | null;
  copy: ItemPageCopy;
  onPick: (itemId: string, relation: ItemSheetLink["relation"]) => void;
}) {
  const t = copy.links;
  const [open, setOpen] = useState(false);
  const [relation, setRelation] =
    useState<ItemSheetLink["relation"]>("blocked-by");
  const [query, setQuery] = useState("");
  const [highlightId, setHighlightId] = useState<string | null>(null);
  // Taken: already linked under this relation (another relation is a new link).
  const linked = new Set(
    links
      .filter((link) => link.relation === relation)
      .map((link) => link.item.id),
  );
  const needle = foldForSearch(query);
  const matches = candidates
    .filter(
      (candidate) =>
        !needle ||
        foldForSearch(`${candidate.key ?? ""} ${candidate.title}`).includes(
          needle,
        ),
    )
    .slice(0, 8);
  // Waiting for X while X waits for this item (or the reverse) is a loop.
  const loopsWith = (candidateId: string) =>
    (relation === "blocked-by" &&
      links.some(
        (link) => link.relation === "blocks" && link.item.id === candidateId,
      )) ||
    (relation === "blocks" &&
      links.some(
        (link) =>
          link.relation === "blocked-by" && link.item.id === candidateId,
      ));
  const choosable = matches.filter(
    (candidate) => !linked.has(candidate.id) && !loopsWith(candidate.id),
  );
  const highlighted =
    choosable.find((candidate) => candidate.id === highlightId) ??
    choosable[0] ??
    null;
  const choose = (candidateId: string) => {
    setOpen(false);
    onPick(candidateId, relation);
  };
  return (
    <Popover
      open={open}
      onOpenChange={(next) => {
        setOpen(next);
        if (next) setQuery("");
      }}
    >
      {row ? (
        <PopoverTrigger asChild>
          <AddRow>{t.picker.label}</AddRow>
        </PopoverTrigger>
      ) : (
        <PopoverTrigger asChild>
          <button
            type="button"
            aria-label={named ? t.picker.label : undefined}
            className={BUTTON_SM_VARIANT_CLASS.ghost}
          >
            <Plus aria-hidden="true" className="size-4" />
            {t.add}
          </button>
        </PopoverTrigger>
      )}
      <PopoverContent
        align="end"
        aria-label={t.picker.label}
        className="max-h-(--radix-popover-content-available-height) w-96 gap-0 overflow-x-hidden overflow-y-auto rounded-[10px] border border-line-strong bg-popover p-0 shadow-lg ring-0"
      >
        <div
          role="radiogroup"
          aria-label={t.picker.relation}
          className="m-2 flex gap-0.5 rounded-[8px] bg-state-hover-strong p-0.5"
        >
          {LINK_GROUPS.map((value) => (
            <button
              key={value}
              type="button"
              role="radio"
              aria-checked={relation === value}
              onClick={() => setRelation(value)}
              className={cn(
                "h-7 flex-1 cursor-pointer rounded-[6px] border-0 px-2 text-crumb font-medium",
                relation === value
                  ? "bg-popover font-semibold text-foreground shadow-sm"
                  : "bg-transparent text-muted-foreground",
              )}
            >
              {t.picker.relations[value]}
            </button>
          ))}
        </div>
        <div className="flex h-10 items-center gap-2 border-y border-line px-3 text-meta-foreground">
          <Search aria-hidden="true" className="size-3.5 shrink-0" />
          <input
            data-picker-search
            autoFocus
            type="text"
            role="combobox"
            aria-expanded="true"
            aria-label={t.picker.find}
            placeholder={t.picker.find}
            value={query}
            onChange={(event) => setQuery(event.target.value)}
            onKeyDown={(event) => {
              if (event.key === "ArrowDown" || event.key === "ArrowUp") {
                event.preventDefault();
                if (choosable.length === 0) return;
                const index = highlighted
                  ? choosable.findIndex((entry) => entry.id === highlighted.id)
                  : -1;
                const step = event.key === "ArrowDown" ? 1 : -1;
                const next =
                  choosable[
                    (index + step + choosable.length) % choosable.length
                  ];
                if (next) setHighlightId(next.id);
              } else if (event.key === "Enter") {
                event.preventDefault();
                if (highlighted) choose(highlighted.id);
              }
            }}
            className="min-w-0 flex-1 border-0 bg-transparent p-0 text-item-ui text-foreground outline-none placeholder:text-meta-foreground"
          />
        </div>
        <div role="listbox" aria-label={t.picker.label} className="p-1.5">
          {matches.length === 0 ? (
            <p className="m-0 px-2.5 py-2 text-item-ui text-meta-foreground">
              {t.picker.empty}
            </p>
          ) : (
            matches.map((candidate) => {
              const loop = loopsWith(candidate.id);
              const alreadyLinked = !loop && linked.has(candidate.id);
              const disabled = alreadyLinked || loop;
              return (
                <div
                  key={candidate.id}
                  role="option"
                  aria-selected={candidate.id === highlighted?.id}
                  aria-disabled={disabled || undefined}
                  onMouseEnter={() => !disabled && setHighlightId(candidate.id)}
                  onClick={() => !disabled && choose(candidate.id)}
                  className={cn(
                    "rounded-[7px] px-2.5 py-1.5 text-item-ui",
                    disabled ? "cursor-default" : "cursor-pointer",
                    candidate.id === highlighted?.id && "bg-state-hover-strong",
                  )}
                >
                  <div
                    className={cn(
                      "flex items-center gap-2",
                      disabled && "text-muted-foreground",
                    )}
                  >
                    <span
                      aria-hidden="true"
                      className={cn(
                        "size-2.25 shrink-0 rounded-full",
                        candidate.statusCategory
                          ? LINK_DOT_CLASS[candidate.statusCategory]
                          : "bg-muted-foreground",
                      )}
                    />
                    <span className="font-mono text-item-note text-meta-foreground">
                      {candidate.key}
                    </span>
                    <span className="min-w-0 flex-1 truncate">
                      {candidate.title}
                    </span>
                    {alreadyLinked ? (
                      <span className="shrink-0 text-item-note text-meta-foreground">
                        {t.picker.linked}
                      </span>
                    ) : null}
                  </div>
                  {place ? (
                    <div className="pl-4.25 text-item-note text-meta-foreground">
                      {place}
                    </div>
                  ) : null}
                  {loop ? (
                    <div className="mt-0.5 flex items-start gap-1 pl-4.25 text-item-note leading-snug text-warning-text">
                      <AlertTriangle
                        aria-hidden="true"
                        className="mt-0.5 size-3 shrink-0"
                      />
                      {fillSheetCopy(
                        relation === "blocks"
                          ? t.picker.cycleBlocks
                          : t.picker.cycleWaits,
                        {
                          key: itemKey ?? "",
                          other: candidate.key ?? candidate.title,
                        },
                      )}
                    </div>
                  ) : null}
                </div>
              );
            })
          )}
        </div>
        <div className="border-t border-line px-3.5 py-2.5 text-item-note text-meta-foreground">
          {t.picker.footer}
        </div>
      </PopoverContent>
    </Popover>
  );
}

const FILE_KIND_CLASS: Readonly<Record<FileKind, string>> = {
  sheet: "bg-item-file-sheet text-item-file-sheet-foreground",
  pdf: "bg-item-file-pdf text-item-file-pdf-foreground",
  other: "bg-state-hover-strong text-muted-foreground",
};

/** A file being sent, or refused: its kind chip, what it is doing, and what can be done about it. */
function UploadRow({
  upload,
  copy,
  locale,
  onRetry,
  onDismiss,
}: {
  upload: ItemPageUpload;
  copy: ItemPageCopy;
  locale: string;
  onRetry?: (uploadId: string) => void;
  onDismiss?: (uploadId: string) => void;
}) {
  const t = copy.files;
  const kind = describeFile({ name: upload.name, contentType: "" });
  const size = formatFileSize(upload.size, locale);
  const failed = upload.status === "error";
  const percent =
    upload.progress === null || upload.progress === undefined
      ? null
      : Math.max(0, Math.min(100, Math.round(upload.progress)));
  return (
    <div
      data-upload-status={upload.status}
      className={cn(
        "mb-1.5 flex items-center gap-2.5 rounded-[10px] border px-2.5 py-2",
        failed
          ? "border-danger-text/40 bg-danger-subtle"
          : "border-line bg-popover",
      )}
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
      <span className="min-w-0 flex-1">
        <span className="block truncate text-item-ui font-medium text-foreground">
          {upload.name}
        </span>
        <small
          className={cn(
            "block text-item-note",
            failed ? "text-danger-text" : "text-meta-foreground",
          )}
        >
          {failed
            ? upload.error === "tooBig"
              ? fillSheetCopy(t.tooBig, { limit: upload.limit ?? "" })
              : t[upload.error ?? "failed"]
            : upload.status === "done"
              ? fillSheetCopy(t.uploaded, { size })
              : percent === null
                ? fillSheetCopy(t.uploadingNoPercent, { size })
                : fillSheetCopy(t.uploading, {
                    percent,
                    done: formatFileSize(
                      Math.round((upload.size * percent) / 100),
                      locale,
                    ),
                    size,
                  })}
        </small>
        {upload.status === "uploading" ? (
          <span
            role="progressbar"
            aria-label={upload.name}
            aria-valuemin={0}
            aria-valuemax={100}
            aria-valuenow={percent ?? undefined}
            className="mt-1.5 block h-1.25 overflow-hidden rounded-[3px] bg-state-hover-strong"
          >
            <span
              className={cn(
                "block h-full rounded-[3px] bg-primary",
                percent === null && "w-2/5 animate-pulse",
              )}
              style={percent === null ? undefined : { width: `${percent}%` }}
            />
          </span>
        ) : null}
      </span>
      {failed ? (
        <>
          {onRetry && upload.retryable !== false ? (
            <button
              type="button"
              className={BUTTON_SM_VARIANT_CLASS.outline}
              onClick={() => onRetry(upload.id)}
            >
              {t.retry}
            </button>
          ) : null}
          {onDismiss ? (
            <button
              type="button"
              className={BUTTON_SM_VARIANT_CLASS.ghost}
              onClick={() => onDismiss(upload.id)}
            >
              {t.skip}
            </button>
          ) : null}
        </>
      ) : null}
      {onDismiss ? (
        <button
          type="button"
          aria-label={fillSheetCopy(
            upload.status === "uploading" ? t.cancel : t.dismiss,
            { name: upload.name },
          )}
          onClick={() => onDismiss(upload.id)}
          className={cn(ICON_BUTTON_CLASS, "size-7")}
        >
          <X aria-hidden="true" />
        </button>
      ) : null}
    </div>
  );
}

export function ItemPageFiles({
  files,
  updates,
  capabilities,
  callbacks,
  copy,
  locale,
  now,
  timeZone,
  uploads = [],
  onRetryUpload,
  onDismissUpload,
  collapseEmpty = false,
  notice = null,
}: {
  /** Nothing yet folds into one "+ Thêm tệp" row (still a drop target); a reader sees nothing. */
  collapseEmpty?: boolean;
  /** A download that did not go through, said once under the list. */
  notice?: string | null;
  uploads?: readonly ItemPageUpload[];
  onRetryUpload?: (uploadId: string) => void;
  onDismissUpload?: (uploadId: string) => void;
  files: readonly CardFile[];
  /** The comments, to tell which file came with one. */
  updates: readonly CardUpdate[];
  capabilities: TaskDetailCapabilities;
  callbacks: Pick<
    TaskDetailCallbacks,
    "onAddFile" | "onDownloadFile" | "onDeleteFile"
  >;
  copy: ItemPageCopy;
  locale: string;
  now: Date;
  timeZone?: string;
}) {
  const t = copy.files;
  const input = useRef<HTMLInputElement>(null);
  const [over, setOver] = useState(false);
  // One file at a time asks to be deleted; a failed delete stays on its row with a retry.
  const [confirmId, setConfirmId] = useState<string | null>(null);
  const [deletingId, setDeletingId] = useState<string | null>(null);
  const [failedId, setFailedId] = useState<string | null>(null);
  const canAdd = capabilities.canEditFields && Boolean(callbacks.onAddFile);
  const canDelete =
    capabilities.canEditFields && Boolean(callbacks.onDeleteFile);
  const withComment = new Set(
    updates.flatMap((update) => update.attachmentIds),
  );
  const add = (list: FileList | null) => {
    for (const file of Array.from(list ?? [])) callbacks.onAddFile?.(file);
  };
  const remove = async (file: CardFile) => {
    setDeletingId(file.id);
    setFailedId(null);
    const ok = await callbacks.onDeleteFile?.(file);
    setDeletingId(null);
    if (ok) setConfirmId(null);
    else setFailedId(file.id);
  };
  const empty = files.length === 0 && uploads.length === 0;
  if (collapseEmpty && empty && !canAdd) return null;
  const picker = canAdd ? (
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
  ) : null;
  const zoneProps = canAdd
    ? {
        onClick: () => input.current?.click(),
        onDragOver: (event: DragEvent<HTMLElement>) => {
          event.preventDefault();
          setOver(true);
        },
        onDragLeave: () => setOver(false),
        onDrop: (event: DragEvent<HTMLElement>) => {
          event.preventDefault();
          setOver(false);
          add(event.dataTransfer.files);
        },
      }
    : {};
  const zone =
    collapseEmpty && empty ? (
      <AddRow
        {...zoneProps}
        className={cn(over && "border-primary bg-state-selected")}
      >
        {t.add}
      </AddRow>
    ) : (
      <div
        role="button"
        tabIndex={0}
        {...zoneProps}
        onKeyDown={(event) => {
          if (event.key === "Enter" || event.key === " ") {
            event.preventDefault();
            input.current?.click();
          }
        }}
        className={cn(
          "mt-1.5 flex h-10.5 cursor-pointer items-center justify-center gap-2 rounded-[8px] border border-dashed border-line-strong text-item-small text-meta-foreground outline-none focus-visible:ring-2 focus-visible:ring-focus-ring",
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
    <section
      aria-labelledby="item-page-files"
      // A file pasted while focus is in the section is attached.
      onPaste={
        canAdd
          ? (event) => {
              if (event.clipboardData.files.length === 0) return;
              event.preventDefault();
              add(event.clipboardData.files);
            }
          : undefined
      }
    >
      {collapseEmpty && empty ? null : (
        <SectionHeading
          id="item-page-files"
          count={files.length > 0 ? files.length : undefined}
        >
          {t.heading}
        </SectionHeading>
      )}
      {uploads.map((upload) => (
        <UploadRow
          key={upload.id}
          upload={upload}
          copy={copy}
          locale={locale}
          onRetry={onRetryUpload}
          onDismiss={onDismissUpload}
        />
      ))}
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
        if (confirmId === file.id) {
          return (
            <div
              key={file.id}
              role="alertdialog"
              aria-label={fillSheetCopy(t.deleteTitle, { name: file.name })}
              className="mb-1 flex flex-wrap items-center gap-2 rounded-[10px] border border-danger-text/40 bg-danger-subtle px-2.5 py-2"
            >
              <p className="m-0 min-w-0 flex-1 text-item-ui text-foreground">
                {fillSheetCopy(t.deleteAsk, { name: file.name })}
                {failedId === file.id ? (
                  <span
                    role="alert"
                    className="block text-item-note text-danger-text"
                  >
                    {t.deleteFailed}
                  </span>
                ) : null}
              </p>
              <button
                type="button"
                autoFocus
                className={BUTTON_SM_VARIANT_CLASS.outline}
                onClick={() => {
                  setConfirmId(null);
                  setFailedId(null);
                }}
              >
                {t.deleteKeep}
              </button>
              <button
                type="button"
                disabled={deletingId === file.id}
                className={BUTTON_SM_VARIANT_CLASS.primary}
                onClick={() => void remove(file)}
              >
                {failedId === file.id ? t.retry : t.deleteConfirm}
              </button>
            </div>
          );
        }
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
            <span className="ml-auto flex items-center">
              {file.kind === "file" && callbacks.onDownloadFile ? (
                <button
                  type="button"
                  aria-label={fillSheetCopy(t.download, { name: file.name })}
                  onClick={() => callbacks.onDownloadFile?.(file)}
                  className={cn(ICON_BUTTON_CLASS, "size-7")}
                >
                  <Download aria-hidden="true" />
                </button>
              ) : file.url ? (
                <a
                  href={file.url}
                  download={file.name}
                  aria-label={fillSheetCopy(t.download, { name: file.name })}
                  className={cn(ICON_BUTTON_CLASS, "size-7")}
                >
                  <Download aria-hidden="true" />
                </a>
              ) : (
                <DisabledControl reason={t.downloadUnavailable}>
                  <button
                    type="button"
                    aria-disabled="true"
                    aria-label={fillSheetCopy(t.download, { name: file.name })}
                    className={cn(ICON_BUTTON_CLASS, "size-7")}
                  >
                    <Download aria-hidden="true" />
                  </button>
                </DisabledControl>
              )}
              {canDelete ? (
                <button
                  type="button"
                  aria-label={fillSheetCopy(t.delete, { name: file.name })}
                  onClick={() => {
                    setFailedId(null);
                    setConfirmId(file.id);
                  }}
                  className={cn(ICON_BUTTON_CLASS, "size-7")}
                >
                  <Trash2 aria-hidden="true" />
                </button>
              ) : null}
            </span>
          </div>
        );
      })}
      {notice ? (
        <p role="alert" className="m-0 mt-1 text-item-note text-danger-text">
          {notice}
        </p>
      ) : null}
      {picker}
      {canAdd ? zone : null}
    </section>
  );
}

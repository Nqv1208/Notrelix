import { useState } from "react";
import { AlertCircle, ChevronDown, Plus, Search, X } from "lucide-react";
import { PopoverTrigger, cn } from "@notrelix/ui-web";
import { useCard } from "@notrelix/work-management-state";
import type { ItemSheetLink } from "../../card-detail/task-detail-types";
import { foldForSearch } from "../../item-page/item-pickers";
import { fillPopupCopy } from "../card-popup.copy";
import { useCardPopup, useCanEdit } from "../card-popup-context";
import {
  EmptySection,
  IconBtn,
  LinkBtn,
  PanelBtn,
  PanelHead,
  PanelPopover,
  PanelInput,
  PersonMark,
  PopSection,
  SECTION_ICONS,
  SectionHead,
  SmallBtn,
} from "../popup-ui";

type Category = "todo" | "in_progress" | "review" | "done";

const DOT: Readonly<Record<Category, string>> = {
  done: "bg-work-status-done",
  in_progress: "bg-work-status-doing",
  review: "bg-work-status-review",
  todo: "shadow-[inset_0_0_0_2px_var(--work-status-todo)]",
};

function Dot({ category }: { category: Category | null }) {
  return (
    <span
      aria-hidden="true"
      className={cn(
        "inline-block size-[9px] shrink-0 rounded-full",
        category ? DOT[category] : "bg-meta-foreground",
      )}
    />
  );
}

/** `.lrow`: key, title, status, who does it and the unlink button. */
function LinkRow({
  itemKey,
  title,
  status,
  category,
  person,
  unlinkLabel,
  onUnlink,
}: {
  itemKey: string | null;
  title: string;
  status: string;
  category: Category | null;
  person?: { userId: string; name: string; initials: string } | null;
  unlinkLabel: string;
  onUnlink?: () => void;
}) {
  return (
    <div className="mb-1.5 box-border flex min-h-11 items-center gap-2.5 rounded-[8px] border border-line bg-popover pr-1.5 pl-3">
      {itemKey ? (
        <span className="font-mono text-[12px] text-meta-foreground">
          {itemKey}
        </span>
      ) : null}
      <span className="min-w-0 flex-1 truncate text-[14px]">{title}</span>
      <span className="inline-flex items-center gap-1.5 text-[12.5px] whitespace-nowrap text-muted-foreground">
        <Dot category={category} />
        {status}
      </span>
      {person ? <PersonMark member={person} size={20} /> : null}
      {onUnlink ? (
        <IconBtn small aria-label={unlinkLabel} onClick={onUnlink}>
          <X aria-hidden="true" className="size-4" />
        </IconBtn>
      ) : null}
    </div>
  );
}

function Group({
  title,
  hint,
  children,
}: {
  title: string;
  hint?: string;
  children: React.ReactNode;
}) {
  return (
    <div className="mt-3.5 first:mt-3.5">
      <div className="mb-1.5 flex items-center gap-1.5 text-[12.5px] font-semibold text-muted-foreground">
        {title}
        {hint ? (
          <span className="font-normal text-meta-foreground"> · {hint}</span>
        ) : null}
      </div>
      {children}
    </div>
  );
}

function ParentRow() {
  const { card, copy } = useCardPopup();
  const t = copy.related.links;
  const parent = useCard(card.parentItemId ?? "", card.workspaceId).card;
  if (!parent) return null;
  return (
    <Group title={t.groupParent}>
      <LinkRow
        itemKey={parent.itemKey ?? null}
        title={parent.title}
        status={parent.statusCategory === "done" ? t.statusDone : t.statusOpen}
        category={parent.statusCategory ?? null}
        person={parent.members[0] ?? null}
        unlinkLabel=""
      />
    </Group>
  );
}

/** "Thêm liên kết": pick the relation, find an item of this board, add. */
export function AddLinkPanel({ onClose }: { onClose: () => void }) {
  const { card, board, data, callbacks, copy } = useCardPopup();
  const t = copy.related.links;
  const [relation, setRelation] =
    useState<ItemSheetLink["relation"]>("related");
  const [query, setQuery] = useState("");
  const [picked, setPicked] = useState<string | null>(null);
  const links = data.links ?? [];
  const needle = foldForSearch(query);
  const results = (data.linkCandidates ?? [])
    .filter((c) => c.id !== card.id)
    .filter(
      (c) =>
        !needle || foldForSearch(`${c.key ?? ""} ${c.title}`).includes(needle),
    )
    .slice(0, 8);
  const chosen = results.find((c) => c.id === picked) ?? null;
  const keyOf = (c: { key: string | null; title: string }) => c.key ?? c.title;
  // Waiting for X while X waits for this card (or the reverse) is a loop.
  const loop =
    chosen &&
    (relation === "blocked-by" &&
    links.some((l) => l.relation === "blocks" && l.item.id === chosen.id)
      ? t.cycleBlocks
      : relation === "blocks" &&
          links.some(
            (l) => l.relation === "blocked-by" && l.item.id === chosen.id,
          )
        ? t.cycleBlockedBy
        : null);
  const taken =
    chosen &&
    links.some((l) => l.relation === relation && l.item.id === chosen.id);
  const error = chosen
    ? loop
      ? fillPopupCopy(loop, { key: keyOf(chosen) })
      : taken
        ? fillPopupCopy(t.already, { key: keyOf(chosen) })
        : null
    : null;
  const relations: ReadonlyArray<readonly [ItemSheetLink["relation"], string]> =
    [
      ["blocks", t.groupBlocks],
      ["blocked-by", t.groupBlockedBy],
      ["related", t.groupRelated],
    ];
  return (
    <div role="dialog" aria-label={t.popTitle}>
      <PanelHead
        title={t.popTitle}
        onClose={onClose}
        closeLabel={t.close}
        icons={{
          back: null,
          close: <X aria-hidden="true" className="size-4" />,
        }}
      />
      <label
        className="mb-1.5 block text-[12px] font-semibold text-muted-foreground"
        htmlFor="popup-link-type"
      >
        {t.type}
      </label>
      <span className="relative block">
        <select
          id="popup-link-type"
          value={relation}
          onChange={(e) =>
            setRelation(e.target.value as ItemSheetLink["relation"])
          }
          className="box-border h-9 w-full appearance-none rounded-[8px] border border-line-strong bg-popover px-3 pr-9 text-[14px] text-foreground focus:border-primary focus:ring-[3px] focus:ring-brand-subtle focus:outline-none"
        >
          {relations.map(([value, label]) => (
            <option key={value} value={value}>
              {label}
            </option>
          ))}
        </select>
        <ChevronDown
          aria-hidden="true"
          className="pointer-events-none absolute top-2.5 right-3 size-4 text-meta-foreground"
        />
      </span>
      <label
        className="mt-3 mb-1.5 block text-[12px] font-semibold text-muted-foreground"
        htmlFor="popup-link-find"
      >
        {t.findLabel}
      </label>
      <span className="relative block">
        <Search
          aria-hidden="true"
          className="pointer-events-none absolute top-2.5 left-3 size-4 text-meta-foreground"
        />
        <PanelInput
          id="popup-link-find"
          data-picker-search
          autoFocus
          value={query}
          onChange={(e) => setQuery(e.target.value)}
          className="pl-9"
        />
      </span>
      <div className="mt-3 mb-1.5 text-[12px] font-semibold text-muted-foreground">
        {t.results}
      </div>
      <div role="listbox" aria-label={t.results} className="flex flex-col">
        {results.length === 0 ? (
          <p className="m-0 px-2.5 py-2 text-[13px] text-meta-foreground">
            {t.noResults}
          </p>
        ) : null}
        {results.map((c) => (
          <button
            key={c.id}
            type="button"
            role="option"
            aria-selected={picked === c.id}
            onClick={() => setPicked(c.id)}
            className={cn(
              "box-border flex min-h-9 w-full items-center gap-2.5 rounded-[8px] border-0 bg-transparent px-2.5 text-left text-[14px] hover:bg-state-hover-strong",
              picked === c.id &&
                "bg-brand-subtle font-semibold text-brand-subtle-foreground",
            )}
          >
            {c.key ? (
              <span className="font-mono text-[12px] text-meta-foreground">
                {c.key}
              </span>
            ) : null}
            <span className="min-w-0 flex-1 truncate">{c.title}</span>
            <Dot category={c.statusCategory ?? null} />
          </button>
        ))}
      </div>
      {error ? (
        <p
          role="alert"
          className="mx-0.5 mt-2 mb-0 flex items-start gap-1.5 text-[12.5px] leading-[1.45] font-medium text-danger-text"
        >
          <AlertCircle
            aria-hidden="true"
            className="mt-0.5 size-3.5 shrink-0"
          />
          <span>{error}</span>
        </p>
      ) : null}
      <p className="mx-0.5 mt-2 mb-0 text-[12.5px] leading-[1.45] text-meta-foreground">
        {fillPopupCopy(t.boardOnly, { board: board.title })}
      </p>
      <PanelBtn
        variant="primary"
        className="mt-3 w-full"
        disabled={!chosen || Boolean(error)}
        onClick={() => {
          if (!chosen) return;
          callbacks.onAddLink?.(chosen.id, relation);
          onClose();
        }}
      >
        {t.submit}
      </PanelBtn>
    </div>
  );
}

/** The "+ Thêm liên kết" button with the panel that links another card; `quiet` is the link-coloured one of a collapsed section. */
function AddLinkButton({ quiet }: { quiet?: boolean }) {
  const { copy } = useCardPopup();
  const [open, setOpen] = useState(false);
  const Btn = quiet ? LinkBtn : SmallBtn;
  return (
    <PanelPopover
      open={open}
      onOpenChange={setOpen}
      width={380}
      trigger={
        <PopoverTrigger asChild>
          <Btn className="max-md:h-11" aria-haspopup="dialog">
            <Plus aria-hidden="true" className="size-[13px]" />
            {copy.related.links.add}
          </Btn>
        </PopoverTrigger>
      }
    >
      <AddLinkPanel onClose={() => setOpen(false)} />
    </PanelPopover>
  );
}

/** Blocks, blocked by, related, parent and sub-items of the card. */
export function LinksSection() {
  const { card, data, callbacks, copy, slots, mode } = useCardPopup();
  const t = copy.related.links;
  const canEdit = useCanEdit() && Boolean(callbacks.onAddLink);
  const links = data.links ?? [];
  const kids = data.childItems ?? [];
  const groups: ReadonlyArray<
    readonly [ItemSheetLink["relation"], string, string | undefined]
  > = [
    ["blocks", t.groupBlocks, t.hintBlocks],
    ["blocked-by", t.groupBlockedBy, t.hintBlockedBy],
    ["related", t.groupRelated, undefined],
  ];
  if (mode === "create" && links.length === 0) return null;
  const empty = links.length === 0 && kids.length === 0 && !card.parentItemId;
  if (empty)
    return (
      <EmptySection
        label={t.title}
        icon={SECTION_ICONS.links}
        none={copy.shell.noneYet}
      >
        {canEdit ? <AddLinkButton quiet /> : null}
      </EmptySection>
    );

  return (
    <PopSection label={t.title} icon={SECTION_ICONS.links}>
      <SectionHead title={t.title}>
        {canEdit ? slots.linksExtras : null}
      </SectionHead>
      {groups.map(([relation, title, hint]) => {
        const rows = links.filter((l) => l.relation === relation);
        if (rows.length === 0) return null;
        return (
          <Group key={relation} title={title} hint={hint}>
            {rows.map((link) => (
              <LinkRow
                key={link.id}
                itemKey={link.item.key}
                title={link.item.title}
                status={link.item.statusName ?? t.statusNone}
                category={link.item.statusCategory}
                unlinkLabel={fillPopupCopy(t.unlinkOne, {
                  key: link.item.key ?? link.item.title,
                })}
                onUnlink={
                  canEdit && callbacks.onRemoveLink
                    ? () => callbacks.onRemoveLink?.(link.id)
                    : undefined
                }
              />
            ))}
          </Group>
        );
      })}
      {card.parentItemId ? <ParentRow /> : null}
      {kids.length > 0 ? (
        <Group title={t.groupChildren}>
          {kids.map((child) => (
            <LinkRow
              key={child.id}
              itemKey={child.itemKey ?? null}
              title={child.title}
              status={child.done ? t.statusDone : t.statusOpen}
              category={child.done ? "done" : "todo"}
              person={child.assignee}
              unlinkLabel=""
            />
          ))}
        </Group>
      ) : null}
      {canEdit ? (
        <div className="mt-3">
          <AddLinkButton />
        </div>
      ) : null}
    </PopSection>
  );
}

import { useMemo, useState } from "react";
import { AlertCircle, Check, Lock, Search } from "lucide-react";
import { cn } from "@notrelix/ui-web";
import {
  RELATION_LINK_LIMIT,
  type RelationLink,
} from "@notrelix/work-management-core";
import { useFullBoard } from "@notrelix/work-management-state";
import { foldForSearch } from "../../item-page/item-pickers";
import { fillPopupCopy } from "../card-popup.copy";
import { useCardPopup } from "../card-popup-context";
import { PanelBtn } from "../popup-ui";

/** A locked link names nothing: it never carries the target's key or title, so the chip shows the lock and one fixed word. */
export function RelationChips({
  links,
  withTitle,
}: {
  links: readonly RelationLink[];
  withTitle?: boolean;
}) {
  const { copy } = useCardPopup();
  const t = copy.sections.relation;
  return (
    <span className="inline-flex min-w-0 flex-wrap items-center gap-1">
      {links.map((link) =>
        link.locked ? (
          <span
            key={link.itemId}
            data-relation-chip="locked"
            className="inline-flex items-center gap-1 text-meta-foreground"
          >
            <Lock aria-hidden="true" className="size-3" />
            {t.locked}
          </span>
        ) : (
          <span
            key={link.itemId}
            data-relation-chip="item"
            className="inline-flex max-w-[220px] items-center gap-1 truncate"
          >
            {link.itemKey ? (
              <span className="font-mono text-[12px]">{link.itemKey}</span>
            ) : null}
            {withTitle || !link.itemKey ? (
              <span className="truncate">{link.title}</span>
            ) : null}
          </span>
        ),
      )}
    </span>
  );
}

/**
 * The item picker of a Relation field: the items of the target board to tick, and the links the reader
 * cannot see, listed without a title and kept on save. The server replaces the whole set, so the locked
 * ids go back with the ticked ones. A target the reader cannot read (or that is gone) lists nothing and
 * only lets the visible links be removed.
 */
export function RelationPickerPanel({
  targetBoardId,
  links,
  onSave,
  onCancel,
}: {
  targetBoardId: string | undefined;
  links: readonly RelationLink[];
  onSave: (itemIds: string[]) => void;
  onCancel: () => void;
}) {
  const { card, board, copy } = useCardPopup();
  const t = copy.sections.relation;
  const target = useFullBoard(targetBoardId, board.workspaceId);
  const locked = links.filter((link) => link.locked);
  const [ticked, setTicked] = useState<readonly string[]>(
    links.filter((link) => !link.locked).map((link) => link.itemId),
  );
  const [query, setQuery] = useState("");

  const items = useMemo(
    () =>
      target.groups
        .flatMap((group) => group.cards)
        // An item never links to itself.
        .filter((item) => item.id !== card.id),
    [target.groups, card.id],
  );
  const needle = foldForSearch(query);
  const rows = items.filter(
    (item) =>
      !needle ||
      foldForSearch(`${item.itemKey ?? ""} ${item.title}`).includes(needle),
  );
  const total = ticked.length + locked.length;
  const full = total >= RELATION_LINK_LIMIT;
  const unavailable = Boolean(target.error);
  // A target that is gone or unreadable answers NotFound or Forbidden; any other failure may pass on a retry.
  const kind = (target.error as { kind?: string } | null)?.kind;
  const gone = kind === "not_found" || kind === "forbidden";
  // Visible links stay removable even when the target cannot be listed.
  const shownVisible = unavailable
    ? links.filter((link) => !link.locked)
    : null;

  function toggle(id: string) {
    setTicked((now) =>
      now.includes(id) ? now.filter((x) => x !== id) : [...now, id],
    );
  }

  return (
    <div
      role="dialog"
      aria-label={fillPopupCopy(t.title, { board: target.board?.title ?? "" })}
    >
      <b className="mb-2 block text-[14px] font-[650]">
        {fillPopupCopy(t.title, { board: target.board?.title ?? "…" })}
      </b>
      {locked.length > 0 ? (
        <p className="m-0 mb-2 text-[12.5px] leading-[1.45] text-muted-foreground">
          {fillPopupCopy(t.summary, {
            total: String(total),
            visible: String(ticked.length),
            hidden: String(locked.length),
          })}
        </p>
      ) : null}
      {!targetBoardId ? (
        <p role="alert" className="m-0 text-[13px] text-meta-foreground">
          {t.noTarget}
        </p>
      ) : unavailable ? (
        <p
          role="alert"
          className="m-0 flex items-start gap-1.5 text-[12.5px] font-medium text-danger-text"
        >
          <AlertCircle
            aria-hidden="true"
            className="mt-0.5 size-3.5 shrink-0"
          />
          <span>{gone ? t.unavailable : t.loadFailed}</span>
        </p>
      ) : target.isLoading ? (
        <p role="status" className="m-0 text-[13px] text-meta-foreground">
          {t.loading}
        </p>
      ) : (
        <>
          <span className="relative mb-1.5 block">
            <Search
              aria-hidden="true"
              className="pointer-events-none absolute top-2.5 left-3 size-4 text-meta-foreground"
            />
            <input
              value={query}
              onChange={(e) => setQuery(e.target.value)}
              aria-label={t.search}
              placeholder={t.search}
              className="box-border h-9 w-full rounded-[8px] border border-line-strong bg-popover pr-3 pl-9 text-[14px] text-foreground focus:border-primary focus:outline-none"
            />
          </span>
          <div className="mb-1 text-[12px] font-semibold text-muted-foreground">
            {t.visibleGroup}
          </div>
        </>
      )}
      <div
        role="listbox"
        aria-multiselectable="true"
        aria-label={t.visibleGroup}
        className="flex max-h-64 flex-col overflow-auto"
      >
        {!unavailable &&
        !target.isLoading &&
        targetBoardId &&
        rows.length === 0 ? (
          <p className="m-0 px-2.5 py-2 text-[13px] text-meta-foreground">
            {items.length === 0
              ? t.nothing
              : fillPopupCopy(t.noMatch, { query })}
          </p>
        ) : null}
        {(shownVisible
          ? shownVisible.map((link) => ({
              id: link.itemId,
              itemKey: link.itemKey,
              title: link.title ?? "",
            }))
          : rows.map((item) => ({
              id: item.id,
              itemKey: item.itemKey ?? null,
              title: item.title,
            }))
        ).map((item) => {
          const on = ticked.includes(item.id);
          return (
            <button
              key={item.id}
              type="button"
              role="option"
              aria-selected={on}
              disabled={!on && full}
              onClick={() => toggle(item.id)}
              className={cn(
                "box-border flex min-h-9 w-full items-center gap-2.5 rounded-[8px] border-0 bg-transparent px-2.5 text-left text-[14px] hover:bg-state-hover-strong disabled:cursor-not-allowed disabled:opacity-55",
                on &&
                  "bg-brand-subtle font-semibold text-brand-subtle-foreground",
              )}
            >
              {item.itemKey ? (
                <span className="font-mono text-[12px] text-meta-foreground">
                  {item.itemKey}
                </span>
              ) : null}
              <span className="min-w-0 flex-1 truncate">{item.title}</span>
              {on ? <Check aria-hidden="true" className="size-4" /> : null}
            </button>
          );
        })}
      </div>
      {locked.length > 0 ? (
        <>
          <div className="mt-2 mb-1 text-[12px] font-semibold text-muted-foreground">
            {t.lockedGroup}
          </div>
          <ul
            aria-label={t.lockedGroup}
            className="m-0 flex list-none flex-col p-0"
          >
            {locked.map((link) => (
              <li
                key={link.itemId}
                data-relation-row="locked"
                className="flex min-h-9 items-center gap-2.5 px-2.5 text-[14px] text-meta-foreground"
              >
                <Lock aria-hidden="true" className="size-3.5" />
                <span className="min-w-0 flex-1">
                  <b className="block font-semibold">{t.locked}</b>
                  <small className="block text-[12px]">{t.lockedHint}</small>
                </span>
              </li>
            ))}
          </ul>
        </>
      ) : null}
      {full ? (
        <p
          role="status"
          className="mx-0.5 mt-2 mb-0 text-[12.5px] text-meta-foreground"
        >
          {fillPopupCopy(t.limit, { max: String(RELATION_LINK_LIMIT) })}
        </p>
      ) : null}
      {unavailable && !gone ? (
        <PanelBtn small className="mt-2" onClick={() => void target.refetch()}>
          {t.retry}
        </PanelBtn>
      ) : null}
      <div className="mt-3 flex justify-end gap-2">
        <PanelBtn onClick={onCancel}>{t.cancel}</PanelBtn>
        <PanelBtn
          variant="primary"
          onClick={() => onSave([...locked.map((l) => l.itemId), ...ticked])}
        >
          {t.save}
        </PanelBtn>
      </div>
    </div>
  );
}

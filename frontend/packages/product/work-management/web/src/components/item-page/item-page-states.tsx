import { Archive, ArrowLeft, Check, SearchX, WifiOff } from "lucide-react";
import { Skeleton, cn } from "@notrelix/ui-web";
import type { Board } from "@notrelix/work-management-core";
import { fillSheetCopy } from "../card-detail/item-sheet.copy";
import type { ItemPageCopy } from "./item-page.copy";
import {
  BUTTON_SM_VARIANT_CLASS,
  DisabledControl,
  ICON_BUTTON_CLASS,
  MARK_TONE_CLASS,
  PageLink,
} from "./item-page-parts";
import type {
  ItemPageLifecycle,
  ItemPageNavigation,
  ItemPageSpace,
} from "./item-page-types";

/**
 * The page's bar before an item is known: the way back and where the page
 * sits (department, board, the item key when the link carried it).
 */
export function ItemPageStateBar({
  board,
  space,
  navigation,
  itemKey,
  copy,
}: {
  board: Board;
  space?: ItemPageSpace;
  navigation: ItemPageNavigation;
  itemKey?: string | null;
  copy: ItemPageCopy;
}) {
  return (
    <header className="flex h-13.25 shrink-0 items-center gap-1.5 border-b border-line bg-background pr-4 pl-3 sm:pl-5">
      <PageLink
        href={navigation.boardHref}
        onNavigate={navigation.onBack}
        aria-label={copy.topbar.back}
        className={ICON_BUTTON_CLASS}
      >
        <ArrowLeft aria-hidden="true" />
      </PageLink>
      <nav
        aria-label={copy.topbar.crumb}
        className="flex min-w-0 items-center gap-2 text-item-small text-meta-foreground"
      >
        {space ? (
          <>
            <span
              aria-hidden="true"
              className={cn(
                "grid size-4.5 shrink-0 place-items-center rounded-[6px] text-mark-3xs",
                MARK_TONE_CLASS[space.mark.tone],
              )}
            >
              {space.mark.initials}
            </span>
            <span className="truncate">{space.name}</span>
            <span aria-hidden="true" className="text-line-strong">
              /
            </span>
          </>
        ) : null}
        <span className="truncate">{board.title}</span>
        {itemKey ? (
          <>
            <span aria-hidden="true" className="text-line-strong">
              /
            </span>
            <span className="shrink-0 font-mono font-medium text-foreground">
              {itemKey}
            </span>
          </>
        ) : null}
      </nav>
    </header>
  );
}

/**
 * The page's frame while the item loads: the same two columns the item takes,
 * so nothing jumps when it arrives. After a while it says it is slow.
 */
export function ItemPageSkeleton({
  board,
  space,
  navigation,
  itemKey,
  slow,
  copy,
}: {
  board: Board;
  space?: ItemPageSpace;
  navigation: ItemPageNavigation;
  itemKey?: string | null;
  slow?: boolean;
  copy: ItemPageCopy;
}) {
  return (
    <>
      <ItemPageStateBar
        board={board}
        space={space}
        navigation={navigation}
        itemKey={itemKey}
        copy={copy}
      />
      <div
        role="status"
        aria-label={copy.loading}
        className="flex min-h-0 flex-1 flex-col overflow-hidden lg:flex-row"
      >
        <div className="min-w-0 flex-1">
          <div className="mx-auto flex max-w-215 flex-col gap-3 px-4 pt-7 pb-10 sm:px-10">
            <Skeleton className="h-3 w-32 rounded-md" />
            <Skeleton className="h-6 w-[88%] rounded-md" />
            <Skeleton className="h-6 w-[60%] rounded-md" />
            <div className="mt-6 flex flex-col gap-3">
              <Skeleton className="h-3.5 w-full rounded-md" />
              <Skeleton className="h-3.5 w-[94%] rounded-md" />
              <Skeleton className="h-3.5 w-[62%] rounded-md" />
            </div>
            <Skeleton className="mt-6 h-11 w-full rounded-[10px]" />
            <Skeleton className="h-11 w-full rounded-[10px]" />
            {slow ? (
              <p className="m-0 mt-2 text-item-small text-meta-foreground">
                {copy.states.loadingSlow}
              </p>
            ) : null}
          </div>
        </div>
        <div
          aria-hidden="true"
          className="hidden w-80 shrink-0 flex-col gap-4 border-l border-line bg-popover px-4.5 pt-5 lg:flex"
        >
          {[0, 1, 2, 3, 4, 5].map((index) => (
            <div key={index} className="flex items-center gap-3">
              <Skeleton className="h-3 w-20 rounded-md" />
              <Skeleton className="h-4 w-28 rounded-md" />
            </div>
          ))}
        </div>
      </div>
    </>
  );
}

/**
 * An item that is not there, or that the viewer may not see: the same words
 * for both, so nobody can probe for keys they cannot open.
 */
export function ItemPageNotFound({
  board,
  space,
  navigation,
  itemKey,
  copy,
  onOpenSearch,
  onGoHome,
}: {
  board: Board;
  space?: ItemPageSpace;
  navigation: ItemPageNavigation;
  itemKey?: string | null;
  copy: ItemPageCopy;
  onOpenSearch?: () => void;
  onGoHome?: () => void;
}) {
  const t = copy.states.notFound;
  return (
    <>
      <ItemPageStateBar
        board={board}
        space={space}
        navigation={navigation}
        itemKey={itemKey}
        copy={copy}
      />
      <div className="flex min-h-0 flex-1 flex-col items-center justify-center px-6 pb-16 text-center">
        <span
          aria-hidden="true"
          className="mb-4 grid size-12 place-items-center rounded-[12px] bg-state-hover-strong text-meta-foreground"
        >
          <SearchX className="size-5.5" />
        </span>
        <h1 className="m-0 text-item-heading text-foreground">
          {itemKey ? fillSheetCopy(t.title, { key: itemKey }) : t.titleNoKey}
        </h1>
        <p className="mt-2 max-w-[38ch] text-item-text leading-normal text-muted-foreground">
          {t.body}
        </p>
        <div className="mt-4 flex gap-2">
          <button
            type="button"
            className={BUTTON_SM_VARIANT_CLASS.outline}
            onClick={onOpenSearch}
          >
            {t.search}
          </button>
          <button
            type="button"
            className={BUTTON_SM_VARIANT_CLASS.outline}
            onClick={onGoHome ?? navigation.onBack}
          >
            {t.home}
          </button>
        </div>
      </div>
    </>
  );
}

const BANNER_CLASS =
  "flex items-start gap-3 rounded-[10px] border px-3.5 py-3 text-item-ui leading-normal text-foreground";

/** "Hoàn thành ngày 06/10 bởi Vũ Thanh." with the way to reopen the item. */
export function ItemPageCompletedBanner({
  date,
  completed,
  canReopen,
  copy,
  onReopen,
}: {
  /** The completion day as shown. */
  date: string;
  completed: NonNullable<ItemPageLifecycle["completed"]>;
  canReopen: boolean;
  copy: ItemPageCopy;
  onReopen?: () => void;
}) {
  const t = copy.states.completed;
  const reopen = (
    <button
      type="button"
      aria-disabled={canReopen && onReopen ? undefined : "true"}
      className={cn("ml-auto", BUTTON_SM_VARIANT_CLASS.outline)}
      onClick={canReopen ? onReopen : undefined}
    >
      {t.reopen}
    </button>
  );
  return (
    <div
      role="status"
      data-item-banner="completed"
      className={cn(
        BANNER_CLASS,
        "items-center border-transparent bg-success-subtle",
      )}
    >
      <Check aria-hidden="true" className="size-4 shrink-0 text-success-text" />
      <div className="min-w-0 flex-1">
        <b className="font-semibold text-success-text">
          {fillSheetCopy(t.title, { date })}
        </b>
        {completed.byName
          ? fillSheetCopy(t.by, { name: completed.byName })
          : null}
      </div>
      {canReopen && onReopen ? (
        reopen
      ) : (
        <DisabledControl reason={t.reopenUnavailable}>{reopen}</DisabledControl>
      )}
    </div>
  );
}

/** "Đã lưu trữ ngày 08/10 bởi …. Chỉ xem, không sửa." with the way to restore it. */
export function ItemPageArchivedBanner({
  date,
  archived,
  canRestore,
  copy,
  onRestore,
}: {
  date: string;
  archived: NonNullable<ItemPageLifecycle["archived"]>;
  canRestore: boolean;
  copy: ItemPageCopy;
  onRestore?: () => void;
}) {
  const t = copy.states.archived;
  return (
    <div
      role="status"
      data-item-banner="archived"
      className={cn(BANNER_CLASS, "border-line-strong bg-popover")}
    >
      <Archive
        aria-hidden="true"
        className="mt-0.5 size-4 shrink-0 text-meta-foreground"
      />
      <div className="min-w-0 flex-1">
        <b className="font-semibold">{fillSheetCopy(t.title, { date })}</b>
        {archived.byName ? fillSheetCopy(t.by, { name: archived.byName }) : "."}
        {t.readOnly}
      </div>
      {canRestore && onRestore ? (
        <button
          type="button"
          className={cn("ml-auto", BUTTON_SM_VARIANT_CLASS.outline)}
          onClick={onRestore}
        >
          {t.restore}
        </button>
      ) : null}
    </div>
  );
}

/** The page cannot reach the server: what waits, and that it will save by itself. */
export function ItemPageOfflineBanner({
  pendingCount,
  copy,
}: {
  pendingCount: number;
  copy: ItemPageCopy;
}) {
  const t = copy.states.offline;
  return (
    <div
      role="alert"
      data-item-banner="offline"
      className={cn(
        BANNER_CLASS,
        "border-item-conflict-line bg-warning-subtle",
      )}
    >
      <WifiOff
        aria-hidden="true"
        className="mt-0.5 size-4 shrink-0 text-warning-text"
      />
      <div>
        <b className="font-semibold text-warning-text">{t.title}</b>{" "}
        {pendingCount === 0
          ? t.bodyNone
          : pendingCount === 1
            ? t.bodyOne
            : fillSheetCopy(t.body, { count: pendingCount })}
      </div>
    </div>
  );
}

/** The amber "chờ lưu" that follows a value written while offline. */
export function PendingMark({ copy }: { copy: ItemPageCopy }) {
  return (
    <span className="ml-2 inline-flex items-center gap-1 text-item-note font-semibold whitespace-nowrap text-warning-text">
      <span
        aria-hidden="true"
        className="size-1.25 rounded-full bg-warning-text"
      />
      {copy.states.offline.pending}
    </span>
  );
}

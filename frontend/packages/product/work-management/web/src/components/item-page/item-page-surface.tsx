import { useEffect, useId, useRef, useState, type KeyboardEvent } from "react";
import { AlertCircle } from "lucide-react";
import { Skeleton } from "@notrelix/ui-web";
import { ItemPageActivity } from "./item-page-activity";
import {
  ItemPageConflictNotice,
  ItemPageReadOnlyNotice,
  ItemPageTitleBlock,
} from "./item-page-header";
import { ItemPageRail } from "./item-page-rail";
import {
  ItemPageChildItems,
  ItemPageDescription,
  ItemPageFiles,
  ItemPageLinks,
} from "./item-page-sections";
import { ItemPageTopbar } from "./item-page-topbar";
import { BUTTON_SM_VARIANT_CLASS } from "./item-page-parts";
import type { ItemPageSurfaceProps, ItemPageTab } from "./item-page-types";

function isTypingTarget(target: EventTarget | null): boolean {
  if (!(target instanceof HTMLElement)) return false;
  return (
    target.isContentEditable ||
    target.tagName === "INPUT" ||
    target.tagName === "TEXTAREA" ||
    target.tagName === "SELECT"
  );
}

/**
 * Item page: the full-width view of one item at desktop width. A bar with the
 * way back, the item's place in its lane and K/J stepping; the title, the
 * description, sub-items, links, files and the activity on the left; the
 * properties, the views that show the item and the system facts on the right.
 * The side sheet and the page share the property editors, the title editor
 * and the comment behaviour. Pure: data, copy, clock and callbacks are
 * injected. Where the contract has no producer (sub-items, links, uploads,
 * follow, reply, react) the control stays and says why it is disabled.
 */
export function ItemPageSurface({
  status,
  board,
  card,
  capabilities,
  detailData,
  callbacks,
  navigation,
  copy,
  sheetCopy,
  now,
  timeZone,
  space,
  viewer,
  creatorName,
  access,
  conflict,
  onResolveConflict,
  activeTab = "all",
  onSelectTab,
}: ItemPageSurfaceProps) {
  const rootRef = useRef<HTMLElement>(null);
  const titleId = useId();
  const [localTab, setLocalTab] = useState<ItemPageTab>(activeTab);
  useEffect(() => setLocalTab(activeTab), [activeTab]);
  const tab = onSelectTab ? activeTab : localTab;
  const selectTab = (next: ItemPageTab) => {
    setLocalTab(next);
    onSelectTab?.(next);
  };

  const stepRef = useRef({
    previous: callbacks.onPrevious,
    next: callbacks.onNext,
  });
  useEffect(() => {
    stepRef.current = {
      previous: callbacks.onPrevious,
      next: callbacks.onNext,
    };
  });

  /** K and J step through the lane; typing and modified keys are left alone. */
  function stepFromKey(
    event: Pick<
      globalThis.KeyboardEvent,
      "key" | "metaKey" | "ctrlKey" | "altKey" | "target" | "preventDefault"
    >,
  ) {
    if (event.metaKey || event.ctrlKey || event.altKey) return;
    if (isTypingTarget(event.target)) return;
    const key = event.key.toLowerCase();
    const step =
      key === "k"
        ? stepRef.current.previous
        : key === "j"
          ? stepRef.current.next
          : undefined;
    if (step) {
      event.preventDefault();
      step();
    }
  }

  function handleKeyDown(event: KeyboardEvent<HTMLElement>) {
    // Keys from portalled menus bubble through React but are not ours.
    if (!rootRef.current?.contains(event.target as Node)) return;
    stepFromKey(event);
  }

  // With nothing focused the keys still step, as on the board.
  useEffect(() => {
    function handleWindowKeyDown(event: globalThis.KeyboardEvent) {
      if (event.target !== document.body) return;
      stepFromKey(event);
    }
    window.addEventListener("keydown", handleWindowKeyDown);
    return () => window.removeEventListener("keydown", handleWindowKeyDown);
  });

  function viewChanges() {
    selectTab("history");
    document
      .getElementById("item-page-activity")
      ?.scrollIntoView?.({ block: "start" });
  }

  const ready = status === "ready" && card && detailData;
  // The last change of the item counts its history: a child ticked is an edit.
  const editedAt = card ? latestChange(card, detailData) : null;

  return (
    <section
      ref={rootRef}
      aria-labelledby={ready ? titleId : undefined}
      aria-label={ready ? undefined : copy.loading}
      aria-busy={status === "loading" || undefined}
      onKeyDown={handleKeyDown}
      className="flex h-full min-h-0 min-w-0 flex-col bg-background text-item-text text-foreground"
    >
      {status === "loading" ? <ItemPageSkeleton copy={copy} /> : null}
      {status === "error" ? (
        <ItemPageError copy={copy} onBack={navigation.onBack} />
      ) : null}
      {ready ? (
        <>
          <ItemPageTopbar
            board={board}
            card={card}
            space={space}
            navigation={navigation}
            capabilities={capabilities}
            callbacks={callbacks}
            copy={copy}
          />
          {/* Below the desktop width the two columns stack and the page scrolls as one. */}
          <div className="flex min-h-0 flex-1 flex-col overflow-y-auto lg:flex-row lg:overflow-hidden">
            <div className="min-w-0 flex-1 lg:overflow-auto">
              <div className="mx-auto flex max-w-215 flex-col gap-7.5 px-4 pt-7 pb-10 sm:px-10">
                <ItemPageTitleBlock
                  card={card}
                  creatorName={creatorName}
                  editedAt={editedAt}
                  editable={capabilities.canEditFields}
                  titleId={titleId}
                  copy={copy}
                  sheetCopy={sheetCopy}
                  now={now}
                  timeZone={timeZone}
                  onRenameTitle={callbacks.onRenameTitle}
                />
                {conflict ? (
                  <ItemPageConflictNotice
                    conflict={conflict}
                    copy={copy}
                    onResolve={onResolveConflict}
                    onViewChanges={viewChanges}
                  />
                ) : null}
                {!capabilities.canEditFields ? (
                  <ItemPageReadOnlyNotice
                    access={access}
                    copy={copy}
                    timeZone={timeZone}
                  />
                ) : null}
                <ItemPageDescription card={card} copy={copy} />
                <ItemPageChildItems
                  card={card}
                  items={detailData.childItems}
                  capabilities={capabilities}
                  callbacks={callbacks}
                  copy={copy}
                  now={now}
                  timeZone={timeZone}
                />
                <div className="grid grid-cols-[repeat(auto-fit,minmax(300px,1fr))] gap-6">
                  <ItemPageLinks
                    links={detailData.links}
                    candidates={detailData.linkCandidates}
                    capabilities={capabilities}
                    callbacks={callbacks}
                    copy={copy}
                  />
                  <ItemPageFiles
                    files={detailData.files}
                    updates={detailData.updates}
                    capabilities={capabilities}
                    callbacks={callbacks}
                    copy={copy}
                    locale={sheetCopy.locale}
                    now={now}
                    timeZone={timeZone}
                  />
                </div>
                <div id="item-page-activity">
                  <ItemPageActivity
                    key={card.id}
                    card={card}
                    data={detailData}
                    capabilities={capabilities}
                    callbacks={callbacks}
                    copy={copy}
                    now={now}
                    timeZone={timeZone}
                    tab={tab}
                    onSelectTab={selectTab}
                    members={
                      card.members.length > 0 ? card.members : card.watchers
                    }
                    viewer={viewer}
                  />
                </div>
              </div>
            </div>
            <ItemPageRail
              board={board}
              card={card}
              capabilities={capabilities}
              callbacks={callbacks}
              navigation={navigation}
              copy={copy}
              sheetCopy={sheetCopy}
              now={now}
              timeZone={timeZone}
              creatorName={creatorName}
              conflict={conflict}
              data={detailData}
              editedAt={editedAt}
            />
          </div>
        </>
      ) : null}
    </section>
  );
}

/** The later of the item's own update time and its newest history line. */
function latestChange(
  card: NonNullable<ItemPageSurfaceProps["card"]>,
  data: ItemPageSurfaceProps["detailData"],
): string {
  const own = card.updatedAt ?? card.createdAt;
  const newest = (data?.activity ?? []).reduce<string | null>(
    (latest, entry) =>
      latest === null || entry.createdAt > latest ? entry.createdAt : latest,
    null,
  );
  return newest !== null && newest > own ? newest : own;
}

export function ItemPageSkeleton({
  copy,
}: {
  copy: ItemPageSurfaceProps["copy"];
}) {
  return (
    <div
      role="status"
      aria-label={copy.loading}
      className="flex h-full w-full flex-col gap-4 p-6"
    >
      <Skeleton className="h-8 rounded-lg" />
      <Skeleton className="h-16 rounded-lg" />
      <Skeleton className="h-8 rounded-lg" />
      <Skeleton className="h-40 rounded-lg" />
    </div>
  );
}

export function ItemPageError({
  copy,
  onBack,
}: {
  copy: ItemPageSurfaceProps["copy"];
  onBack: () => void;
}) {
  return (
    <div className="flex h-full w-full flex-col items-center justify-center p-6 text-center">
      <AlertCircle
        aria-hidden="true"
        className="mb-3 size-8 text-danger-text"
      />
      <h1 className="m-0 text-item-heading text-foreground">
        {copy.unavailable.title}
      </h1>
      <p className="mt-2 max-w-xs text-item-text text-muted-foreground">
        {copy.unavailable.body}
      </p>
      <button
        type="button"
        className={`${BUTTON_SM_VARIANT_CLASS.outline} mt-4`}
        onClick={onBack}
      >
        {copy.unavailable.back}
      </button>
    </div>
  );
}

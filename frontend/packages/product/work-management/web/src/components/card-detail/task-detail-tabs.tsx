import { useId, useState, type KeyboardEvent } from "react";
import { Check } from "lucide-react";
import { cn } from "@notrelix/ui-web";
import type { CardDetail, CardDetailTab } from "@notrelix/work-management-core";
import { TaskActivityTab } from "./task-activity-tab";
import { TaskUpdatesTab } from "./task-updates-tab";
import { fillSheetCopy, type ItemSheetCopy } from "./item-sheet.copy";
import type {
  ItemSheetChildItem,
  TaskDetailCallbacks,
  TaskDetailCapabilities,
  TaskDetailData,
} from "./task-detail-types";

type ActivityTab = "updates" | "activity";

/**
 * Body sections of the item sheet: "Mô tả", "Việc con {done}/{total}" and the
 * "Bình luận {n}" / "Lịch sử" subtabs. The composer sits in the sheet footer.
 */
export function TaskDetailTabs({
  card,
  data,
  capabilities,
  callbacks,
  copy,
  now,
  timeZone,
  activeTab,
}: {
  card: CardDetail;
  data: TaskDetailData;
  capabilities: TaskDetailCapabilities;
  callbacks: TaskDetailCallbacks;
  copy: ItemSheetCopy;
  now: Date;
  timeZone?: string;
  activeTab?: CardDetailTab;
}) {
  const t = copy.sections;
  return (
    <>
      <section>
        <SectionHeading>{t.description}</SectionHeading>
        {card.descriptionMd?.trim() ? (
          <p className="m-0 max-w-prose text-sm leading-relaxed whitespace-pre-wrap text-foreground">
            {card.descriptionMd}
          </p>
        ) : (
          <p className="m-0 text-sm text-muted-foreground">{t.noDescription}</p>
        )}
      </section>

      <ChildItemsSection
        progress={card.childProgress ?? null}
        items={data.childItems}
        copy={copy}
      />

      <ActivitySection
        card={card}
        data={data}
        capabilities={capabilities}
        callbacks={callbacks}
        copy={copy}
        now={now}
        timeZone={timeZone}
        initialTab={activeTab === "activity" ? "activity" : "updates"}
      />
    </>
  );
}

function SectionHeading({ children }: { children: React.ReactNode }) {
  return (
    <h3 className="m-0 mb-2 flex items-center gap-2 text-sm font-semibold text-muted-foreground">
      {children}
    </h3>
  );
}

function ChildItemsSection({
  progress,
  items,
  copy,
}: {
  progress: { readonly done: number; readonly total: number } | null;
  items: readonly ItemSheetChildItem[] | undefined;
  copy: ItemSheetCopy;
}) {
  const t = copy.sections;
  const counted =
    items && items.length > 0
      ? {
          done: items.filter((item) => item.done).length,
          total: items.length,
        }
      : progress && progress.total > 0
        ? progress
        : null;

  return (
    <section>
      <SectionHeading>
        {t.childItems}
        {counted ? (
          <span className="font-normal text-muted-foreground">
            {fillSheetCopy(t.childProgress, counted)}
          </span>
        ) : null}
      </SectionHeading>
      {items && items.length > 0 ? (
        <ul className="m-0 flex list-none flex-col p-0">
          {items.map((item) => (
            <li
              key={item.id}
              className="-mx-1.5 flex min-h-8.5 items-center gap-2.5 rounded-md px-1.5 text-sm"
            >
              <span
                aria-hidden="true"
                className={cn(
                  "grid size-4 shrink-0 place-items-center rounded border",
                  item.done
                    ? "border-work-status-done bg-work-status-done text-background"
                    : "border-border",
                )}
              >
                {item.done ? <Check className="size-3" strokeWidth={3} /> : null}
              </span>
              <span
                className={
                  item.done ? "text-muted-foreground line-through" : undefined
                }
              >
                {item.title}
              </span>
              {item.done ? <span className="sr-only">{t.childDone}</span> : null}
            </li>
          ))}
        </ul>
      ) : counted ? null : (
        <p className="m-0 text-sm text-muted-foreground">{t.noChildItems}</p>
      )}
    </section>
  );
}

function ActivitySection({
  card,
  data,
  capabilities,
  callbacks,
  copy,
  now,
  timeZone,
  initialTab,
}: {
  card: CardDetail;
  data: TaskDetailData;
  capabilities: TaskDetailCapabilities;
  callbacks: TaskDetailCallbacks;
  copy: ItemSheetCopy;
  now: Date;
  timeZone?: string;
  initialTab: ActivityTab;
}) {
  const t = copy.sections;
  const baseId = useId();
  const [tab, setTab] = useState<ActivityTab>(initialTab);
  const tabs: ReadonlyArray<readonly [ActivityTab, string]> = [
    ["updates", fillSheetCopy(t.comments, { count: data.updates.length })],
    ["activity", t.history],
  ];

  function select(next: ActivityTab) {
    setTab(next);
    callbacks.onSelectTab(next);
  }

  function handleKeyDown(event: KeyboardEvent<HTMLDivElement>) {
    if (event.key !== "ArrowRight" && event.key !== "ArrowLeft") return;
    event.preventDefault();
    const next = tab === "updates" ? "activity" : "updates";
    select(next);
    document.getElementById(`${baseId}-${next}-tab`)?.focus();
  }

  return (
    <section>
      <div
        role="tablist"
        aria-label={t.activity}
        onKeyDown={handleKeyDown}
        className="mb-3 flex gap-0.5 border-b border-border"
      >
        {tabs.map(([value, label]) => {
          const selected = tab === value;
          return (
            <button
              key={value}
              id={`${baseId}-${value}-tab`}
              type="button"
              role="tab"
              aria-selected={selected}
              aria-controls={`${baseId}-${value}-panel`}
              tabIndex={selected ? 0 : -1}
              onClick={() => select(value)}
              className={cn(
                "-mb-px h-8 border-b-2 px-2.5 text-sm font-medium transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-focus-ring",
                selected
                  ? "border-primary font-semibold text-foreground"
                  : "border-transparent text-muted-foreground hover:text-foreground",
              )}
            >
              {label}
            </button>
          );
        })}
      </div>
      <div
        role="tabpanel"
        id={`${baseId}-${tab}-panel`}
        aria-labelledby={`${baseId}-${tab}-tab`}
      >
        {tab === "updates" ? (
          <TaskUpdatesTab
            card={card}
            updates={data.updates}
            isLoading={data.updatesLoading}
            capabilities={capabilities}
            copy={copy}
            now={now}
            timeZone={timeZone}
            onCreateUpdate={callbacks.onCreateUpdate}
            onUpdateUpdate={callbacks.onUpdateUpdate}
            onDeleteUpdate={callbacks.onDeleteUpdate}
          />
        ) : (
          <TaskActivityTab
            activity={data.activity}
            isLoading={data.activityLoading}
            isFetching={data.activityFetching}
            copy={copy}
            now={now}
            timeZone={timeZone}
            onRefresh={callbacks.onRefreshActivity}
          />
        )}
      </div>
    </section>
  );
}

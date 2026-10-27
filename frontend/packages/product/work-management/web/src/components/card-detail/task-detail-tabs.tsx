import { useId, useState, type KeyboardEvent } from "react";
import { Check } from "lucide-react";
import { cn } from "@notrelix/ui-web";
import type { CardDetail, CardDetailTab } from "@notrelix/work-management-core";
import { ItemDescriptionBody } from "../shared/item-description";
import { ItemDescriptionEditor } from "../shared/item-description-editor";
import { TaskActivityTab } from "./task-activity-tab";
import { TaskUpdatesTab } from "./task-updates-tab";
import { ItemPageLinks } from "../item-page/item-page-sections";
import type { ItemPageCopy } from "../item-page/item-page.copy";
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
  return (
    <>
      <TaskDetailDescription
        card={card}
        copy={copy}
        canEdit={capabilities.canEditFields}
        onSave={(descriptionMd) =>
          callbacks.onRenameTitle(card.id, { descriptionMd })
        }
      />

      <ChildItemsSection
        progress={card.childProgress ?? null}
        items={data.childItems}
        copy={copy}
        onToggle={
          capabilities.canEditFields ? callbacks.onToggleChildItem : undefined
        }
      />

      {callbacks.onAddLink && data.linksCopy ? (
        <SheetLinks
          card={card}
          data={data}
          linksCopy={data.linksCopy}
          capabilities={capabilities}
          callbacks={callbacks}
        />
      ) : null}

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
    <h3 className="m-0 mb-2 flex items-center gap-2 text-compact font-semibold text-muted-foreground">
      {children}
    </h3>
  );
}

/** "Links" of the sheet: the item page's section, with the item page's words. */
function SheetLinks({
  card,
  data,
  linksCopy,
  capabilities,
  callbacks,
}: {
  card: CardDetail;
  data: TaskDetailData;
  linksCopy: ItemPageCopy;
  capabilities: TaskDetailCapabilities;
  callbacks: TaskDetailCallbacks;
}) {
  return (
    <ItemPageLinks
      links={data.links}
      candidates={data.linkCandidates}
      itemKey={card.itemKey ?? null}
      named
      capabilities={capabilities}
      callbacks={callbacks}
      copy={linksCopy}
    />
  );
}

function ChildItemsSection({
  progress,
  items,
  copy,
  onToggle,
}: {
  progress: { readonly done: number; readonly total: number } | null;
  items: readonly ItemSheetChildItem[] | undefined;
  copy: ItemSheetCopy;
  /** Ticks or unticks a child item; the checkboxes are inert without it. */
  onToggle?: (childId: string, done: boolean) => void;
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
          <span className="font-normal text-meta-foreground">
            {fillSheetCopy(t.childProgress, counted)}
          </span>
        ) : null}
      </SectionHeading>
      {items && items.length > 0 ? (
        <ul className="m-0 flex list-none flex-col p-0">
          {items.map((item) => (
            <li
              key={item.id}
              className="-mx-1.5 flex min-h-8.5 items-center gap-2.5 rounded-[7px] px-1.5 text-body hover:bg-state-hover-strong"
            >
              {onToggle ? (
                <button
                  type="button"
                  role="checkbox"
                  aria-checked={item.done}
                  aria-label={item.title}
                  onClick={() => onToggle(item.id, !item.done)}
                  className={cn(
                    "grid size-4 shrink-0 cursor-pointer place-items-center rounded-[4px] border-[1.5px] p-0 outline-none focus-visible:ring-2 focus-visible:ring-focus-ring",
                    item.done
                      ? "border-work-status-done bg-work-status-done text-background"
                      : "border-line-strong",
                  )}
                >
                  {item.done ? (
                    <Check className="size-2.75" strokeWidth={3} />
                  ) : null}
                </button>
              ) : (
                <span
                  aria-hidden="true"
                  className={cn(
                    "grid size-4 shrink-0 place-items-center rounded-[4px] border-[1.5px]",
                    item.done
                      ? "border-work-status-done bg-work-status-done text-background"
                      : "border-line-strong",
                  )}
                >
                  {item.done ? (
                    <Check className="size-2.75" strokeWidth={3} />
                  ) : null}
                </span>
              )}
              <span
                className={
                  item.done ? "text-meta-foreground line-through" : undefined
                }
              >
                {item.title}
              </span>
              {item.done ? (
                <span className="sr-only">{t.childDone}</span>
              ) : null}
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
        className="mb-3 flex gap-0.5 border-b border-line"
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
                "-mb-px h-8 border-b-2 px-2.5 text-compact font-medium transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-focus-ring",
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
            events={data.activity.filter(
              (entry) =>
                entry.type === "updated" ||
                entry.type === "system" ||
                entry.type === "automation",
            )}
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

/** The sheet's description: read as written, or edited in place when the viewer may edit. */
function TaskDetailDescription({
  card,
  copy,
  canEdit,
  onSave,
}: {
  card: CardDetail;
  copy: ItemSheetCopy;
  canEdit: boolean;
  onSave: (descriptionMd: string) => void;
}) {
  const t = copy.sections;
  const [editing, setEditing] = useState(false);
  const text = card.descriptionMd?.trim();
  return (
    <section>
      <SectionHeading>{t.description}</SectionHeading>
      {editing && canEdit ? (
        <ItemDescriptionEditor
          initial={card.descriptionMd ?? ""}
          labels={{
            heading: t.description,
            placeholder: t.descriptionAdd,
            save: t.descriptionSave,
            cancel: t.descriptionCancel,
            tooLong: t.descriptionTooLong,
          }}
          onSave={onSave}
          onClose={() => setEditing(false)}
        />
      ) : text ? (
        <>
          <ItemDescriptionBody
            workspaceId={card.workspaceId}
            text={text}
            className="text-body leading-[1.65]"
          />
          {canEdit ? (
            <button
              type="button"
              onClick={() => setEditing(true)}
              className="mt-2 border-0 bg-transparent p-0 text-item-ui font-semibold text-brand-subtle-foreground"
            >
              {t.descriptionEdit}
            </button>
          ) : null}
        </>
      ) : canEdit ? (
        <button
          type="button"
          onClick={() => setEditing(true)}
          className="m-0 w-full cursor-text border-0 bg-transparent p-0 text-left text-sm text-muted-foreground hover:text-foreground"
        >
          {t.descriptionAdd}
        </button>
      ) : (
        <p className="m-0 text-sm text-muted-foreground">
          {t.descriptionEmpty}
        </p>
      )}
    </section>
  );
}

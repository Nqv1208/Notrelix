import { useEffect, useRef, type ReactNode } from "react";
import { Plus } from "lucide-react";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
  cn,
} from "@notrelix/ui-web";
import type { BoardSettingsViewDto } from "@notrelix/work-management-core";
import { useSettings } from "./board-settings-context";
import { fillCopy } from "./board-settings-format";
import {
  BUTTON_CLASS,
  SectionHeader,
  SectionLine,
  Tag_,
  TypeMark,
  VIEW_ICON,
} from "./board-settings-parts";
import { SortableList, SortableRow } from "./board-settings-sortable";
import { ViewPanel } from "./board-settings-view-panel";

const KINDS: readonly BoardSettingsViewDto["kind"][] = [
  "table",
  "kanban",
  "calendar",
  "timeline",
];

/** The "Chế độ xem" section: the saved views of the board and the panel that edits one. */
export function ViewsSection({
  viewId,
  onSelectView,
  kanbanSettings,
}: {
  viewId: string | null;
  onSelectView: (viewId: string | null) => void;
  kanbanSettings?: ReactNode;
}) {
  const { copy, settings, actions, run } = useSettings();
  const t = copy.views;
  const selected = settings.views.find((view) => view.id === viewId) ?? null;
  const shared = settings.views.filter((view) => view.visibility === "board");
  const mine = settings.views.filter((view) => view.visibility === "me");
  const knownIds = useRef<readonly string[] | null>(null);

  // A view that no longer exists closes the panel; a new one opens it.
  useEffect(() => {
    if (viewId && !selected) onSelectView(null);
    if (knownIds.current) {
      const fresh = settings.views.find(
        (view) => !knownIds.current!.includes(view.id),
      );
      knownIds.current = null;
      if (fresh) onSelectView(fresh.id);
    }
  }, [onSelectView, selected, settings.views, viewId]);

  async function create(kind: BoardSettingsViewDto["kind"]) {
    knownIds.current = settings.views.map((view) => view.id);
    const result = await run(actions.createView(kind));
    if (!result.ok) knownIds.current = null;
  }

  function reorderShared(ids: string[]) {
    void run(actions.reorderViews([...ids, ...mine.map((view) => view.id)]));
  }

  return (
    <>
      <section
        aria-labelledby="bs-views-h"
        className="min-w-0 flex-1 overflow-auto px-6 pt-5 pb-6"
      >
        <SectionHeader
          id="bs-views-h"
          title={t.title}
          intro={t.intro}
          actions={
            <DropdownMenu>
              <DropdownMenuTrigger asChild>
                <button type="button" className={BUTTON_CLASS.primary}>
                  <Plus aria-hidden="true" />
                  {t.add}
                </button>
              </DropdownMenuTrigger>
              <DropdownMenuContent align="end">
                {KINDS.map((kind) => {
                  const Icon = VIEW_ICON[kind];
                  return (
                    <DropdownMenuItem
                      key={kind}
                      onSelect={() => void create(kind)}
                    >
                      <Icon aria-hidden="true" />
                      {t.kinds[kind]}
                    </DropdownMenuItem>
                  );
                })}
              </DropdownMenuContent>
            </DropdownMenu>
          }
        />

        <SectionLine
          title={t.shared}
          count={shared.length}
          note={t.sharedNote}
        />
        {shared.length === 0 ? (
          <p className="m-0 text-item-small text-meta-foreground">{t.empty}</p>
        ) : (
          <div role="list" className="flex flex-col gap-0.5">
            <SortableList
              ids={shared.map((view) => view.id)}
              onReorder={reorderShared}
            >
              {(order) =>
                order.map((id) => {
                  const view = shared.find((entry) => entry.id === id);
                  return view ? (
                    <ViewRow
                      key={view.id}
                      view={view}
                      selected={view.id === viewId}
                      sortable
                      onOpen={() => onSelectView(view.id)}
                    />
                  ) : null;
                })
              }
            </SortableList>
          </div>
        )}

        {mine.length > 0 ? (
          <>
            <SectionLine title={t.mine} count={mine.length} note={t.mineNote} />
            <div role="list" className="flex flex-col gap-0.5">
              {mine.map((view) => (
                <ViewRow
                  key={view.id}
                  view={view}
                  selected={view.id === viewId}
                  onOpen={() => onSelectView(view.id)}
                />
              ))}
            </div>
          </>
        ) : null}
      </section>
      {selected ? (
        <ViewPanel
          key={selected.id}
          view={selected}
          onClose={() => onSelectView(null)}
          kanbanSettings={kanbanSettings}
        />
      ) : null}
    </>
  );
}

const ROW_CLASS =
  "group relative flex min-h-[62px] items-center gap-2.5 rounded-[9px] border border-transparent px-3 py-1.5";

function ViewRow({
  view,
  selected,
  sortable,
  onOpen,
}: {
  view: BoardSettingsViewDto;
  selected: boolean;
  sortable?: boolean;
  onOpen: () => void;
}) {
  const { copy } = useSettings();
  const body = (handle?: React.ReactNode) => (
    <>
      {handle ? (
        <span className="absolute top-1/2 -left-0.5 -translate-y-1/2 opacity-0 group-hover:opacity-100 focus-within:opacity-100">
          {handle}
        </span>
      ) : null}
      <button
        type="button"
        aria-label={fillCopy(copy.views.open, { name: view.name })}
        aria-current={selected ? "true" : undefined}
        onClick={onOpen}
        className="flex min-w-0 flex-1 cursor-pointer items-center gap-2.5 border-0 bg-transparent p-0 text-left outline-none focus-visible:ring-2 focus-visible:ring-focus-ring"
      >
        <TypeMark
          icon={VIEW_ICON[view.kind]}
          size="md"
          className={cn(view.needsFix && "text-warning-text")}
        />
        <span className="min-w-0 flex-1">
          <b className="block text-item-ui font-semibold">{view.name}</b>
          <small className="block truncate text-metadata text-meta-foreground">
            {view.summary}
          </small>
        </span>
        {view.isDefault ? <Tag_>{copy.views.default}</Tag_> : null}
        {view.needsFix ? (
          <Tag_ tone="warning">{copy.views.needsFix}</Tag_>
        ) : null}
      </button>
    </>
  );
  const rowClass = cn(
    ROW_CLASS,
    selected &&
      "border-primary bg-popover shadow-[0_0_0_3px_var(--brand-subtle)]",
  );
  if (!sortable)
    return (
      <div role="listitem" className={rowClass}>
        {body()}
      </div>
    );
  return (
    <SortableRow
      id={view.id}
      handleLabel={`${copy.common.dragHandle}: ${view.name}`}
      role="listitem"
      className={rowClass}
    >
      {(handle) => body(handle)}
    </SortableRow>
  );
}

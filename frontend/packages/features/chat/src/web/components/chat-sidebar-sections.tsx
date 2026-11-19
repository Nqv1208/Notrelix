import { useState, type HTMLAttributes, type ReactNode } from "react";
import {
  DndContext,
  DragOverlay,
  MouseSensor,
  TouchSensor,
  closestCenter,
  pointerWithin,
  useDraggable,
  useDroppable,
  useSensor,
  useSensors,
  type CollisionDetection,
  type DragEndEvent,
  type DragMoveEvent,
  type DragStartEvent,
} from "@dnd-kit/core";
import {
  ChevronDown,
  ChevronRight,
  Hash,
  MoreHorizontal,
  Pencil,
  Trash2,
  ArrowDown,
  ArrowUp,
} from "lucide-react";
import {
  Button,
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
  cn,
} from "@notrelix/ui-web";
import {
  channelDropFor,
  edgeOf,
  groupDropFor,
  placeChannel,
  placeGroup,
  stepChannel,
  stepGroup,
  type ChannelDrop,
  type DropOver,
  type GroupMove,
  type SidebarSection,
} from "../../core/model/sidebar-order-model";
import type {
  ChatChannel,
  ChatChannelGroup,
  ChatChannelPlacement,
} from "../../core/types/chat";
import type { ChatCopy } from "./chat.copy";
import { fillCopy } from "./use-chat-copy";

const HEADING =
  "min-w-0 flex-1 pt-3.5 pb-1 pl-2.5 pr-1 text-micro leading-[1.45] font-[650] tracking-[.04em] text-muted-foreground uppercase";

/** What an Owner or an Admin can do to a category (the default baseline of chat.group.manage). */
export interface SidebarCategoryActions {
  readonly onRename: (group: ChatChannelGroup) => void;
  readonly onDelete: (group: ChatChannelGroup) => void;
}

export interface ChatSidebarSectionsProps {
  readonly sections: readonly SidebarSection[];
  /** Every channel of the caller in sidebar order, and the category list: the order model reads both. */
  readonly channels: readonly ChatChannel[];
  readonly groups: readonly ChatChannelGroup[];
  readonly headingOf: (section: SidebarSection, index: number) => string;
  readonly copy: ChatCopy;
  readonly renderChannel: (channel: ChatChannel) => ReactNode;
  /** Channels with a true answer get the drag grip area and the Move up and Move down menu. */
  readonly canReorderChannel?: (channel: ChatChannel) => boolean;
  readonly onReorderChannels?: (
    placements: readonly ChatChannelPlacement[],
  ) => void;
  /** Present for the people who manage categories; absent hides the category menu and the category drag. */
  readonly category?: SidebarCategoryActions;
  readonly onReorderGroups?: (moves: readonly GroupMove[]) => void;
  readonly isCollapsed?: (groupId: string | null) => boolean;
  readonly onToggleCollapse?: (groupId: string | null) => void;
  /** Whether a category holds an unread, unmuted channel; shown as a dot while it is collapsed. */
  readonly hasUnread?: (section: SidebarSection) => boolean;
}

type DragData =
  | { readonly type: "channel"; readonly channelId: string }
  | { readonly type: "group"; readonly groupId: string };

/** What the line shows while a drag is over a place: before a channel row, or before a category. */
type DropLine =
  | { readonly kind: "channel"; readonly drop: ChannelDrop }
  | { readonly kind: "group"; readonly beforeId: string | null };

const channelDragId = (id: string) => `channel:${id}`;
const sectionDropId = (groupId: string | null) =>
  `section:${groupId ?? "none"}`;

const collide: CollisionDetection = (args) => {
  const type = (args.active.data.current as DragData | undefined)?.type;
  const dataOf = (id: string | number) =>
    args.droppableContainers.find((c) => c.id === id)?.data.current as
      DropOver | undefined;
  const containers = args.droppableContainers.filter((container) => {
    const over = container.data.current as DropOver | undefined;
    if (type === "group")
      return over?.type === "section" && over.groupId !== null;
    return true;
  });
  const hits = pointerWithin({ ...args, droppableContainers: containers });
  if (hits.length === 0) {
    return closestCenter({ ...args, droppableContainers: containers });
  }
  // A channel row under the pointer wins over the category body around it.
  return [...hits].sort(
    (a, b) =>
      Number(dataOf(b.id)?.type === "channel") -
      Number(dataOf(a.id)?.type === "channel"),
  );
};

/**
 * The categories of the sidebar with their channels (UC-CHT-A02, A04): a heading that collapses, a menu to rename,
 * delete and move it, and channel rows that drag between and within categories. Every drag has a keyboard path in
 * the row and heading menus (Move up, Move down), and the drop line shows where the dragged item lands.
 */
export function ChatSidebarSections({
  sections,
  channels,
  groups,
  headingOf,
  copy,
  renderChannel,
  canReorderChannel,
  onReorderChannels,
  category,
  onReorderGroups,
  isCollapsed,
  onToggleCollapse,
  hasUnread,
}: ChatSidebarSectionsProps) {
  const text = copy.groups;
  const [dragging, setDragging] = useState<DragData | null>(null);
  const [line, setLine] = useState<DropLine | null>(null);
  const [announcement, setAnnouncement] = useState("");
  const sensors = useSensors(
    useSensor(MouseSensor, { activationConstraint: { distance: 4 } }),
    useSensor(TouchSensor, {
      activationConstraint: { delay: 250, tolerance: 8 },
    }),
  );
  const groupIds = groups.map((group) => group.id);
  const draggingChannel =
    dragging?.type === "channel"
      ? channels.find((channel) => channel.id === dragging.channelId)
      : undefined;

  /** The line and the drop for the current pointer, or null where the item already is. */
  function resolve(event: DragMoveEvent | DragEndEvent): DropLine | null {
    const active = event.active.data.current as DragData | undefined;
    const over = event.over?.data.current as DropOver | undefined;
    if (!active || !over || !event.over) return null;
    const translated = event.active.rect.current.translated;
    const centerY = translated ? translated.top + translated.height / 2 : null;
    const edge = edgeOf(centerY, event.over.rect);
    if (active.type === "channel") {
      if (over.type === "channel" && over.channelId === active.channelId) {
        return null;
      }
      const drop = channelDropFor(over, edge, channels, active.channelId);
      return placeChannel(channels, active.channelId, drop)
        ? { kind: "channel", drop }
        : null;
    }
    if (over.type !== "section" || over.groupId === null) return null;
    if (over.groupId === active.groupId) return null;
    const beforeId = groupDropFor(over.groupId, edge, groups, active.groupId);
    return placeGroup(groups, active.groupId, beforeId)
      ? { kind: "group", beforeId }
      : null;
  }

  function onDragStart(event: DragStartEvent) {
    setDragging((event.active.data.current as DragData | undefined) ?? null);
  }

  function onDragMove(event: DragMoveEvent) {
    const next = resolve(event);
    setLine((current) =>
      JSON.stringify(current) === JSON.stringify(next) ? current : next,
    );
  }

  function onDragEnd(event: DragEndEvent) {
    const target = resolve(event);
    const active = event.active.data.current as DragData | undefined;
    setDragging(null);
    setLine(null);
    if (!target || !active) return;
    if (target.kind === "channel" && active.type === "channel") {
      moveChannel(active.channelId, target.drop);
    } else if (target.kind === "group" && active.type === "group") {
      moveGroup(active.groupId, target.beforeId);
    }
  }

  function moveChannel(channelId: string, drop: ChannelDrop) {
    const placements = placeChannel(channels, channelId, drop);
    if (placements) onReorderChannels?.(placements);
  }

  function moveGroup(groupId: string, beforeId: string | null) {
    const moves = placeGroup(groups, groupId, beforeId);
    if (moves) onReorderGroups?.(moves);
  }

  function stepChannelBy(channel: ChatChannel, delta: -1 | 1) {
    const drop = stepChannel(channels, groupIds, channel.id, delta);
    if (!drop) return;
    moveChannel(channel.id, drop);
    setAnnouncement(
      fillCopy(delta === -1 ? text.movedUp : text.movedDown, {
        name: channel.name,
      }),
    );
  }

  const lastGroupId = groups[groups.length - 1]?.id;
  // While a channel drags, the ungrouped run is a drop target even when it holds nothing.
  const shown =
    draggingChannel && !sections.some((section) => section.groupId === null)
      ? [
          { groupId: null, name: null, version: null, channels: [] },
          ...sections,
        ]
      : sections;

  return (
    <DndContext
      sensors={sensors}
      collisionDetection={collide}
      onDragStart={onDragStart}
      onDragMove={onDragMove}
      onDragEnd={onDragEnd}
      onDragCancel={() => {
        setDragging(null);
        setLine(null);
      }}
    >
      <span role="status" className="sr-only">
        {announcement}
      </span>
      {shown.map((section, index) => {
        const group = groups.find((entry) => entry.id === section.groupId);
        const collapsed =
          section.groupId !== null && (isCollapsed?.(section.groupId) ?? false);
        const heading = headingOf(section, index);
        const lineHere =
          line?.kind === "group" &&
          section.groupId !== null &&
          line.beforeId === section.groupId;
        return (
          <SectionFrame
            key={section.groupId ?? "ungrouped"}
            groupId={section.groupId}
            draggable={category !== undefined && group !== undefined}
          >
            {(headingListeners) => (
              <>
                {lineHere ? <DropLineMark /> : null}
                <div
                  {...headingListeners}
                  className="relative -mx-2 flex items-end"
                >
                  {section.groupId !== null ? (
                    <h3 className={HEADING}>
                      <button
                        type="button"
                        aria-expanded={!collapsed}
                        aria-label={fillCopy(
                          collapsed ? text.expand : text.collapse,
                          { name: heading },
                        )}
                        onClick={() => onToggleCollapse?.(section.groupId)}
                        className="flex w-full min-w-0 items-center gap-1 rounded-sm text-start uppercase outline-none focus-visible:ring-2 focus-visible:ring-focus-ring"
                      >
                        {collapsed ? (
                          <ChevronRight
                            className="size-3 shrink-0"
                            aria-hidden="true"
                          />
                        ) : (
                          <ChevronDown
                            className="size-3 shrink-0"
                            aria-hidden="true"
                          />
                        )}
                        <span className="min-w-0 truncate">{heading}</span>
                        {collapsed && hasUnread?.(section) ? (
                          <span
                            aria-hidden="true"
                            data-testid="chat-group-unread-dot"
                            className="size-2 shrink-0 rounded-full bg-brand-solid"
                          />
                        ) : null}
                      </button>
                    </h3>
                  ) : (
                    <h3 className={cn(HEADING, "pr-3")}>{heading}</h3>
                  )}
                  {category && group ? (
                    <GroupMenu
                      group={group}
                      copy={copy}
                      actions={category}
                      canUp={stepGroup(groups, group.id, -1) !== undefined}
                      canDown={stepGroup(groups, group.id, 1) !== undefined}
                      onStep={(delta) => {
                        const step = stepGroup(groups, group.id, delta);
                        if (step) moveGroup(group.id, step.beforeId);
                      }}
                    />
                  ) : null}
                </div>
                {collapsed ? null : (
                  <ul className="flex flex-col gap-px">
                    {section.channels.map((channel) => (
                      <ChannelSlot
                        key={channel.id}
                        channel={channel}
                        groupId={section.groupId}
                        draggable={canReorderChannel?.(channel) ?? false}
                        dimmed={draggingChannel?.id === channel.id}
                        lineBefore={
                          line?.kind === "channel" &&
                          line.drop.groupId === section.groupId &&
                          line.drop.beforeId === channel.id
                        }
                        menu={
                          canReorderChannel?.(channel) ? (
                            <ChannelMenu
                              copy={copy}
                              canUp={
                                stepChannel(
                                  channels,
                                  groupIds,
                                  channel.id,
                                  -1,
                                ) !== null
                              }
                              canDown={
                                stepChannel(
                                  channels,
                                  groupIds,
                                  channel.id,
                                  1,
                                ) !== null
                              }
                              onStep={(delta) => stepChannelBy(channel, delta)}
                            />
                          ) : null
                        }
                      >
                        {renderChannel(channel)}
                      </ChannelSlot>
                    ))}
                    {line?.kind === "channel" &&
                    line.drop.groupId === section.groupId &&
                    line.drop.beforeId === null &&
                    section.channels.length > 0 ? (
                      <li aria-hidden="true">
                        <DropLineMark />
                      </li>
                    ) : null}
                  </ul>
                )}
                {!collapsed &&
                section.groupId !== null &&
                section.channels.length === 0 ? (
                  <p className="px-2.5 pb-1 text-sm text-muted-foreground">
                    {canReorderChannel ? text.emptyManage : text.empty}
                  </p>
                ) : null}
                {line?.kind === "group" &&
                line.beforeId === null &&
                section.groupId === lastGroupId ? (
                  <DropLineMark />
                ) : null}
              </>
            )}
          </SectionFrame>
        );
      })}
      <DragOverlay dropAnimation={null}>
        {draggingChannel ? (
          <div className="flex h-8 items-center gap-[9px] rounded-[10px] border border-primary bg-popover px-2.5 text-sm text-popover-foreground shadow-md">
            <Hash className="size-3.5 shrink-0" aria-hidden="true" />
            <span className="min-w-0 truncate">{draggingChannel.name}</span>
          </div>
        ) : dragging?.type === "group" ? (
          <div className="rounded-[10px] border border-primary bg-popover px-2.5 py-1.5 text-micro font-[650] tracking-[.04em] text-popover-foreground uppercase shadow-md">
            {groups.find((g) => g.id === dragging.groupId)?.name}
          </div>
        ) : null}
      </DragOverlay>
    </DndContext>
  );
}

/** The drop line of the Chat-Var-Group-Drag board: 2 px in the brand colour, inset 8. */
function DropLineMark() {
  return (
    <div
      aria-hidden="true"
      data-testid="chat-drop-line"
      className="relative mx-2 my-0.5 h-0.5 rounded-sm bg-brand-solid"
    />
  );
}

/** A section: a drop target for the end of its category, and the drag source of its heading when the person manages categories. */
function SectionFrame({
  groupId,
  draggable,
  children,
}: {
  readonly groupId: string | null;
  readonly draggable: boolean;
  /** Receives the drag listeners the heading carries (empty when the section does not drag). */
  readonly children: (listeners: HTMLAttributes<HTMLElement>) => ReactNode;
}) {
  const droppable = useDroppable({
    id: sectionDropId(groupId),
    data: { type: "section", groupId } satisfies DropOver,
  });
  const drag = useDraggable({
    id: `group:${groupId ?? "none"}`,
    data: { type: "group", groupId } satisfies Record<string, unknown>,
    disabled: !draggable || groupId === null,
  });
  return (
    <section
      ref={(node) => {
        droppable.setNodeRef(node);
        drag.setNodeRef(node);
      }}
      data-group={groupId ?? undefined}
      className={cn("flex flex-col", drag.isDragging && "opacity-50")}
    >
      {children(draggable ? (drag.listeners ?? {}) : {})}
    </section>
  );
}

/** A channel row: a drop target (before or after it) and, for a manager, a drag source with its Move menu. */
function ChannelSlot({
  channel,
  groupId,
  draggable,
  dimmed,
  lineBefore,
  menu,
  children,
}: {
  readonly channel: ChatChannel;
  readonly groupId: string | null;
  readonly draggable: boolean;
  readonly dimmed: boolean;
  readonly lineBefore: boolean;
  readonly menu: ReactNode;
  readonly children: ReactNode;
}) {
  const droppable = useDroppable({
    id: channelDragId(channel.id),
    data: {
      type: "channel",
      channelId: channel.id,
      groupId,
    } satisfies DropOver,
  });
  const drag = useDraggable({
    id: channelDragId(channel.id),
    data: { type: "channel", channelId: channel.id } satisfies DragData,
    disabled: !draggable,
  });
  return (
    <>
      {lineBefore ? (
        <li aria-hidden="true">
          <DropLineMark />
        </li>
      ) : null}
      <li
        ref={(node) => {
          droppable.setNodeRef(node);
          drag.setNodeRef(node);
        }}
        {...(draggable ? drag.listeners : {})}
        className={cn(
          "group/row relative max-md:-mx-2",
          dimmed && "opacity-40",
          draggable && "max-md:[&>button]:pe-12",
        )}
      >
        {children}
        {menu}
      </li>
    </>
  );
}

function ChannelMenu({
  copy,
  canUp,
  canDown,
  onStep,
}: {
  readonly copy: ChatCopy;
  readonly canUp: boolean;
  readonly canDown: boolean;
  readonly onStep: (delta: -1 | 1) => void;
}) {
  const text = copy.groups;
  // The name is not in the label: the row's own button already names the channel, and a name here would match every selector for it.
  const label = text.channelMenu;
  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <Button
          type="button"
          variant="ghost"
          size="icon-sm"
          aria-label={label}
          title={label}
          className="absolute end-1 top-1/2 size-6 -translate-y-1/2 bg-sidebar text-muted-foreground opacity-0 group-hover/row:opacity-100 focus-visible:opacity-100 data-[state=open]:opacity-100 max-md:size-11 max-md:opacity-100"
        >
          <MoreHorizontal aria-hidden="true" />
        </Button>
      </DropdownMenuTrigger>
      <DropdownMenuContent align="end" className="min-w-[180px]">
        <DropdownMenuItem disabled={!canUp} onSelect={() => onStep(-1)}>
          <ArrowUp aria-hidden="true" />
          {text.channelMoveUp}
        </DropdownMenuItem>
        <DropdownMenuItem disabled={!canDown} onSelect={() => onStep(1)}>
          <ArrowDown aria-hidden="true" />
          {text.channelMoveDown}
        </DropdownMenuItem>
      </DropdownMenuContent>
    </DropdownMenu>
  );
}

function GroupMenu({
  group,
  copy,
  actions,
  canUp,
  canDown,
  onStep,
}: {
  readonly group: ChatChannelGroup;
  readonly copy: ChatCopy;
  readonly actions: SidebarCategoryActions;
  readonly canUp: boolean;
  readonly canDown: boolean;
  readonly onStep: (delta: -1 | 1) => void;
}) {
  const text = copy.groups;
  const label = fillCopy(text.menu, { name: group.name });
  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <Button
          type="button"
          variant="ghost"
          size="icon-sm"
          aria-label={label}
          title={label}
          className="mb-0.5 size-6 shrink-0 text-muted-foreground max-md:size-11"
        >
          <MoreHorizontal aria-hidden="true" />
        </Button>
      </DropdownMenuTrigger>
      <DropdownMenuContent align="end" className="min-w-[200px]">
        <DropdownMenuItem onSelect={() => actions.onRename(group)}>
          <Pencil aria-hidden="true" />
          {text.rename}
        </DropdownMenuItem>
        <DropdownMenuItem disabled={!canUp} onSelect={() => onStep(-1)}>
          <ArrowUp aria-hidden="true" />
          {text.moveUp}
        </DropdownMenuItem>
        <DropdownMenuItem disabled={!canDown} onSelect={() => onStep(1)}>
          <ArrowDown aria-hidden="true" />
          {text.moveDown}
        </DropdownMenuItem>
        <DropdownMenuItem
          variant="destructive"
          onSelect={() => actions.onDelete(group)}
        >
          <Trash2 aria-hidden="true" />
          {text.delete}
        </DropdownMenuItem>
      </DropdownMenuContent>
    </DropdownMenu>
  );
}

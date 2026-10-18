import { useMemo, useState, type ReactNode } from "react";
import {
  Archive,
  Bell,
  Check,
  CalendarDays,
  CircleCheck,
  Clock,
  CornerDownRight,
  Flag,
  Info,
  Link2,
  ListEnd,
  Lock,
  Mail,
  MessageSquare,
  MousePointerClick,
  Pencil,
  Plus,
  Search,
  User,
  type LucideIcon,
} from "lucide-react";
import { Popover, PopoverContent, PopoverTrigger, cn } from "@notrelix/ui-web";
import type {
  AutomationStepKind,
  AutomationTriggerDto,
} from "@notrelix/work-management-core";
import { fillCopy } from "./board-settings-format";
import { foldForSearch } from "../item-page/item-pickers";
import type { AutomationEditorCopy } from "./board-automation.copy";
import { DisabledControl } from "../item-page/item-page-parts";

/** The event picker and the action picker of a rule (mockup `.pop.ev`, `.pop.ac`). */

export type EventKey =
  | "item-created"
  | "status-changed"
  | "priority-changed"
  | "assignee-changed"
  | "due-passed"
  | "comment-added"
  | "daily"
  | "weekly"
  | "date"
  | "manual";

const EVENT_ICON: Readonly<Record<EventKey | "email", LucideIcon>> = {
  "item-created": Plus,
  "status-changed": CircleCheck,
  "priority-changed": Flag,
  "assignee-changed": User,
  "due-passed": CalendarDays,
  "comment-added": MessageSquare,
  daily: Clock,
  weekly: CalendarDays,
  date: CalendarDays,
  manual: MousePointerClick,
  email: Mail,
};

const ACTION_ICON: Readonly<
  Record<Exclude<AutomationStepKind, "set-today" | "remind">, LucideIcon>
> = {
  "set-status": CircleCheck,
  assign: User,
  "set-field": Pencil,
  "move-group": ListEnd,
  "create-subitem": CornerDownRight,
  "archive-item": Archive,
  notify: Bell,
  email: Mail,
  teams: MessageSquare,
  webhook: Link2,
};

/** The event a trigger is, in the picker's terms. */
export function eventKeyOf(trigger: AutomationTriggerDto): EventKey {
  if (trigger.kind === "schedule")
    return trigger.schedule?.repeat === "weekly"
      ? "weekly"
      : trigger.schedule?.repeat === "date"
        ? "date"
        : "daily";
  return trigger.kind;
}

const PANEL =
  "flex flex-col gap-0 overflow-hidden rounded-xl border border-line-strong bg-popover p-0 shadow-lg ring-0";
const TILE =
  "box-border flex min-h-11 w-full cursor-pointer items-center gap-2.5 rounded-lg border-0 bg-transparent px-2 py-1.5 text-left font-[inherit] text-foreground outline-none hover:bg-state-hover focus-visible:ring-2 focus-visible:ring-focus-ring";
const TILE_ON = "bg-state-selected shadow-[inset_0_0_0_1.5px_var(--primary)]";

function TypeSquare({ icon: Icon }: { icon: LucideIcon }) {
  return (
    <span
      aria-hidden="true"
      className="grid size-6.5 shrink-0 place-items-center rounded-[6px] border border-line bg-background text-muted-foreground"
    >
      <Icon className="size-3.5" />
    </span>
  );
}

function SearchRow({
  value,
  onChange,
  placeholder,
  label,
  esc,
}: {
  value: string;
  onChange: (value: string) => void;
  placeholder: string;
  label: string;
  esc: string;
}) {
  return (
    <div className="flex h-[42px] items-center gap-2 border-b border-line px-3.5 text-item-ui text-meta-foreground">
      <Search aria-hidden="true" className="size-4 shrink-0" />
      <input
        data-picker-search
        type="text"
        value={value}
        onChange={(event) => onChange(event.target.value)}
        placeholder={placeholder}
        aria-label={label}
        className="min-w-0 flex-1 border-0 bg-transparent p-0 text-item-ui text-foreground outline-none placeholder:text-meta-foreground"
      />
      <kbd
        aria-hidden="true"
        className="rounded-[4px] border border-line-strong bg-keycap px-1.25 font-mono text-key leading-[18px] text-muted-foreground"
      >
        {esc}
      </kbd>
    </div>
  );
}

function GroupTitle({
  children,
  note,
}: {
  children: ReactNode;
  note?: string;
}) {
  return (
    <div className="flex items-center gap-2 px-1.5 py-1 text-item-note font-semibold text-meta-foreground">
      {children}
      {note ? <span className="font-normal">{note}</span> : null}
    </div>
  );
}

export function EventPicker({
  copy,
  trigger,
  hasEmailAddress,
  integrationsHref,
  onSelect,
  children,
  open,
  onOpenChange,
}: {
  copy: AutomationEditorCopy;
  trigger: AutomationTriggerDto;
  hasEmailAddress: boolean;
  integrationsHref?: string;
  onSelect: (key: EventKey) => void;
  /** The chip that opens the picker. */
  children: ReactNode;
  open: boolean;
  onOpenChange: (open: boolean) => void;
}) {
  const t = copy.pickers;
  const [query, setQuery] = useState("");
  const needle = foldForSearch(query);
  const current = eventKeyOf(trigger);
  const matches = (key: EventKey | "email") => {
    const { label, hint } = t.events[key];
    return !needle || foldForSearch(`${label} ${hint}`).includes(needle);
  };
  const tile = (key: EventKey) =>
    matches(key) ? (
      <button
        key={key}
        type="button"
        aria-pressed={current === key}
        onClick={() => onSelect(key)}
        className={cn(TILE, current === key && TILE_ON)}
      >
        <TypeSquare icon={EVENT_ICON[key]} />
        <span>
          <b className="block text-item-small font-semibold">
            {t.events[key].label}
          </b>
          <small className="block text-micro leading-[1.35] text-meta-foreground">
            {t.events[key].hint}
          </small>
        </span>
        {current === key ? (
          <Check
            aria-hidden="true"
            className="ml-auto size-3.5 text-brand-subtle-foreground"
          />
        ) : null}
      </button>
    ) : null;
  const groups: {
    id: string;
    title: ReactNode;
    note?: string;
    keys: EventKey[];
  }[] = [
    {
      id: "item",
      title: t.groups.item,
      keys: [
        "item-created",
        "status-changed",
        "priority-changed",
        "assignee-changed",
        "due-passed",
        "comment-added",
      ],
    },
    {
      id: "schedule",
      title: t.groups.schedule,
      keys: ["daily", "weekly", "date"],
    },
  ];
  const foot = fillCopy(
    current === "status-changed" ? t.eventFootStatus : t.eventFoot,
    { event: t.events[current].label },
  );
  const parts = foot.split(t.events[current].label);
  return (
    <Popover
      open={open}
      onOpenChange={(next) => {
        if (!next) setQuery("");
        onOpenChange(next);
      }}
    >
      <PopoverTrigger asChild>{children}</PopoverTrigger>
      <PopoverContent
        align="start"
        sideOffset={9}
        alignOffset={-11}
        role="dialog"
        aria-label={t.eventAria}
        className={cn(PANEL, "w-[582px]")}
        onOpenAutoFocus={(event) => {
          event.preventDefault();
          (
            (event.currentTarget as HTMLElement).querySelector<HTMLElement>(
              "[data-picker-search]",
            ) ?? (event.currentTarget as HTMLElement)
          ).focus();
        }}
      >
        <SearchRow
          value={query}
          onChange={setQuery}
          placeholder={t.eventSearch}
          label={t.eventSearchAria}
          esc={t.esc}
        />
        {groups.map((group) =>
          group.keys.some((key) => matches(key)) ? (
            <div key={group.id} className="px-2.5 pt-1.5 pb-0.5">
              <GroupTitle>{group.title}</GroupTitle>
              <div className="grid grid-cols-2 gap-0.5">
                {group.keys.map(tile)}
              </div>
            </div>
          ) : null,
        )}
        {matches("email") ? (
          <div className="px-2.5 pt-1.5 pb-0.5">
            <GroupTitle note={t.groups.integrationNote}>
              {t.groups.integration}
            </GroupTitle>
            <div className="grid grid-cols-2 gap-0.5">
              <div className="flex min-h-11 w-full items-center gap-2.5 rounded-lg px-2 py-1.5">
                <TypeSquare icon={EVENT_ICON.email} />
                <span>
                  <b className="block text-item-small font-semibold text-muted-foreground">
                    {t.events.email.label}
                  </b>
                  <small className="block text-micro leading-[1.35] text-meta-foreground">
                    {hasEmailAddress ? "" : t.events.email.hint}
                    <ConnectLink
                      href={integrationsHref}
                      label={t.connect}
                      unavailable={copy.integrationsUnavailable}
                    />
                  </small>
                </span>
              </div>
            </div>
          </div>
        ) : null}
        {matches("manual") ? (
          <div className="px-2.5 pt-1.5 pb-0.5">
            <GroupTitle>{t.groups.manual}</GroupTitle>
            <div className="grid grid-cols-2 gap-0.5">{tile("manual")}</div>
          </div>
        ) : null}
        {groups.every((g) => g.keys.every((key) => !matches(key))) &&
        !matches("email") &&
        !matches("manual") ? (
          <p className="m-0 px-3.5 py-3 text-item-small text-meta-foreground">
            {t.none}
          </p>
        ) : null}
        <div className="mt-1.5 flex gap-2 border-t border-line bg-background px-3.5 py-2.5 text-crumb leading-normal text-muted-foreground">
          <Info aria-hidden="true" className="mt-0.5 size-3.5 shrink-0" />
          <span>
            {parts[0]}
            <b className="font-semibold text-foreground">
              {t.events[current].label}
            </b>
            {parts[1]}
          </span>
        </div>
      </PopoverContent>
    </Popover>
  );
}

/** A link into the Integrations page, or the reason there is none yet. */
export function ConnectLink({
  href,
  label,
  unavailable,
  className,
}: {
  href?: string;
  label: string;
  unavailable: string;
  className?: string;
}) {
  const style = cn(
    "font-medium text-brand-subtle-foreground hover:underline",
    className,
  );
  if (href)
    return (
      <a href={href} className={style}>
        {label}
      </a>
    );
  return (
    <DisabledControl reason={unavailable}>
      <a
        role="link"
        aria-disabled="true"
        className={cn(style, "cursor-not-allowed")}
      >
        {label}
      </a>
    </DisabledControl>
  );
}

const ACTION_GROUPS: readonly {
  readonly id: "item" | "notify" | "integrations";
  readonly kinds: readonly (keyof typeof ACTION_ICON)[];
}[] = [
  {
    id: "item",
    kinds: [
      "set-status",
      "assign",
      "set-field",
      "move-group",
      "create-subitem",
      "archive-item",
    ],
  },
  { id: "notify", kinds: ["notify", "email"] },
  { id: "integrations", kinds: ["teams", "webhook"] },
];

export function ActionPicker({
  copy,
  teamsNeedsReauthorization,
  integrationsHref,
  onSelect,
  children,
  open,
  onOpenChange,
  side = "top",
}: {
  copy: AutomationEditorCopy;
  teamsNeedsReauthorization: boolean;
  integrationsHref?: string;
  onSelect: (kind: AutomationStepKind) => void;
  children: ReactNode;
  open: boolean;
  onOpenChange: (open: boolean) => void;
  side?: "top" | "bottom";
}) {
  const t = copy.pickers;
  const [query, setQuery] = useState("");
  const needle = foldForSearch(query);
  const visible = useMemo(
    () => (kind: keyof typeof ACTION_ICON) => {
      const { label, hint } = t.actions[kind];
      return !needle || foldForSearch(`${label} ${hint}`).includes(needle);
    },
    [needle, t.actions],
  );
  const tile = (kind: keyof typeof ACTION_ICON) =>
    visible(kind) ? (
      kind === "teams" ? (
        <div
          key={kind}
          className="col-span-2 flex items-center rounded-lg hover:bg-state-hover"
        >
          <button
            type="button"
            aria-describedby="automation-teams-status"
            onClick={() => onSelect(kind)}
            className="box-border flex min-h-11 min-w-0 flex-1 cursor-pointer items-center gap-2.5 rounded-lg border-0 bg-transparent px-2 py-1.5 text-left font-[inherit] text-foreground outline-none focus-visible:ring-2 focus-visible:ring-focus-ring"
          >
            <TypeSquare icon={ACTION_ICON[kind]} />
            <span className="min-w-0">
              <b className="block text-item-small font-semibold">
                {t.actions[kind].label}
              </b>
              {teamsNeedsReauthorization ? (
                <small
                  id="automation-teams-status"
                  className="block text-micro leading-[1.35] font-semibold text-warning-text"
                >
                  {t.teamsReauth}
                </small>
              ) : null}
            </span>
          </button>
          <ConnectLink
            href={integrationsHref}
            label={t.viewConnection}
            unavailable={copy.integrationsUnavailable}
            className="px-2 text-item-note whitespace-nowrap"
          />
        </div>
      ) : (
        <button
          key={kind}
          type="button"
          onClick={() => onSelect(kind)}
          className={TILE}
        >
          <TypeSquare icon={ACTION_ICON[kind]} />
          <span>
            <b className="block text-item-small font-semibold">
              {t.actions[kind].label}
            </b>
            <small className="block text-micro leading-[1.35] text-meta-foreground">
              {t.actions[kind].hint}
            </small>
          </span>
        </button>
      )
    ) : null;
  return (
    <Popover
      open={open}
      onOpenChange={(next) => {
        if (!next) setQuery("");
        onOpenChange(next);
      }}
    >
      <PopoverTrigger asChild>{children}</PopoverTrigger>
      <PopoverContent
        align="start"
        side={side}
        sideOffset={side === "top" ? 32 : 9}
        alignOffset={side === "top" ? -3 : -11}
        role="dialog"
        aria-label={t.actionAria}
        className={cn(PANEL, "w-[662px]")}
        onOpenAutoFocus={(event) => {
          event.preventDefault();
          (
            (event.currentTarget as HTMLElement).querySelector<HTMLElement>(
              "[data-picker-search]",
            ) ?? (event.currentTarget as HTMLElement)
          ).focus();
        }}
      >
        <SearchRow
          value={query}
          onChange={setQuery}
          placeholder={t.actionSearch}
          label={t.actionSearchAria}
          esc={t.esc}
        />
        {ACTION_GROUPS.map((group) =>
          group.kinds.some(visible) ? (
            <div key={group.id} className="px-2.5 pt-1.5 pb-0.5">
              <GroupTitle>{t.groups[group.id]}</GroupTitle>
              <div className="grid grid-cols-3 gap-0.5">
                {group.kinds.map(tile)}
              </div>
            </div>
          ) : null,
        )}
        {ACTION_GROUPS.every((group) => !group.kinds.some(visible)) ? (
          <p className="m-0 px-3.5 py-3 text-item-small text-meta-foreground">
            {t.none}
          </p>
        ) : null}
        <div className="mt-1.5 flex gap-2 border-t border-line bg-background px-3.5 py-2.5 text-crumb leading-normal text-muted-foreground">
          <Lock aria-hidden="true" className="mt-0.5 size-3.5 shrink-0" />
          <span>{t.actionFoot}</span>
        </div>
      </PopoverContent>
    </Popover>
  );
}

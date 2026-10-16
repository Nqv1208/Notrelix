import * as React from "react";
import { Check, Plus } from "lucide-react";
import {
  Skeleton,
  THEME_ACCENT_OPTIONS,
  THEME_MODES,
  cn,
  type ThemeAccent,
  type ThemeMode,
} from "@notrelix/ui-web";
import {
  LoadError,
  Note,
  PageHead,
  Toast,
  Toggle,
} from "../shared/account-parts";
import {
  fillCopy,
  type AccountLocale,
  type AccountSaveStatus,
  type AccountSection,
} from "../shared/account-surface-model";
import type { AccountAppearanceCopy } from "./account-appearance.copy";

/** The account preference this page loads: the sidebar default. */
export interface AccountSidebarPreference {
  readonly collapsed: boolean;
}

export interface AccountAppearanceSurfaceProps {
  readonly copy: AccountAppearanceCopy;
  /** Device appearance (theme runtime): always available, never loading. */
  readonly mode: ThemeMode;
  readonly accent: ThemeAccent;
  readonly locale: AccountLocale;
  /** The workspace whose default accent the note names. */
  readonly workspaceName: string;
  /** The account's sidebar preference; the only part that loads. */
  readonly sidebar: AccountSection<AccountSidebarPreference>;
  readonly sidebarSave: AccountSaveStatus;
  /** Shows the "Saved" toast at the foot of the page. */
  readonly toast: boolean;
  readonly onModeChange: (mode: ThemeMode) => void;
  readonly onAccentChange: (accent: ThemeAccent) => void;
  readonly onLocaleChange: (locale: AccountLocale) => void;
  readonly onSidebarCollapsedChange: (collapsed: boolean) => void;
  readonly onRetryPreferences: () => void;
}

const MODE_COPY_KEYS = {
  light: "light",
  dark: "dark",
  system: "system",
  "high-contrast": "highContrast",
} as const;

const LOCALE_OPTIONS = ["vi", "en"] as const satisfies readonly AccountLocale[];

const OPTION =
  "relative box-border flex min-w-0 cursor-pointer rounded-[10px] border border-line-strong bg-popover hover:border-meta-foreground has-[:checked]:border-primary has-[:checked]:shadow-[0_0_0_1px_var(--color-primary)] has-[:focus-visible]:outline-2 has-[:focus-visible]:outline-offset-2 has-[:focus-visible]:outline-focus-ring dark:bg-card";
const RADIO = "m-0 size-4 shrink-0 accent-primary focus-visible:outline-0";

/**
 * The miniature page of a mode card. These are fixed pictures of the three
 * looks (light, dark, high contrast), so they do not follow the theme the
 * page is in; only the button takes the chosen accent.
 */
const PANES = {
  light: {
    page: "rgb(247 248 250)",
    side: "rgb(239 241 244)",
    edge: "inset -1px 0 0 rgb(225 229 234)",
    line: "rgb(205 211 219)",
    ring: undefined,
  },
  dark: {
    page: "rgb(17 20 25)",
    side: "rgb(22 26 32)",
    edge: "inset -1px 0 0 rgb(39 46 56)",
    line: "rgb(58 67 80)",
    ring: undefined,
  },
  contrast: {
    page: "rgb(255 255 255)",
    side: "rgb(255 255 255)",
    edge: undefined,
    line: "rgb(0 0 0)",
    ring: "0 0 0 1.5px rgb(0 0 0)",
  },
} as const;

function MiniPane({
  kind,
  accent,
}: {
  kind: keyof typeof PANES;
  accent: ThemeAccent;
}) {
  const pane = PANES[kind];
  return (
    <span className="flex min-w-0 flex-1" style={{ background: pane.page }}>
      <span
        className="w-[24%] flex-none"
        style={{ background: pane.side, boxShadow: pane.edge }}
      />
      <span className="flex min-w-0 flex-1 flex-col gap-[5px] px-[7px] pt-[7px] pb-1.5">
        <span
          className="block h-[5px] w-[72%] rounded-[3px]"
          style={{ background: pane.line }}
        />
        <span
          className="block h-[5px] w-[46%] rounded-[3px]"
          style={{ background: pane.line }}
        />
        <span
          className="mt-auto block h-[9px] w-7 rounded-[3px]"
          style={{
            background: `var(--accent-swatch-${accent})`,
            boxShadow: pane.ring,
          }}
        />
      </span>
    </span>
  );
}

function ModePicture({
  mode,
  accent,
}: {
  mode: ThemeMode;
  accent: ThemeAccent;
}) {
  return (
    <span
      aria-hidden="true"
      className="flex h-14 overflow-hidden rounded-[7px] ring-1 ring-line"
    >
      {mode === "system" ? (
        <>
          <MiniPane kind="light" accent={accent} />
          <MiniPane kind="dark" accent={accent} />
        </>
      ) : (
        <MiniPane
          kind={
            mode === "dark"
              ? "dark"
              : mode === "high-contrast"
                ? "contrast"
                : "light"
          }
          accent={accent}
        />
      )}
    </span>
  );
}

function GroupTitle({ id, children }: { id: string; children: string }) {
  return (
    <h3
      id={id}
      className="m-0 mb-2.5 text-body leading-[1.45] font-semibold text-foreground"
    >
      {children}
    </h3>
  );
}

function SidebarChoice({
  copy,
  sidebar,
  save,
  onChange,
  onRetry,
}: {
  copy: AccountAppearanceCopy;
  sidebar: AccountSection<AccountSidebarPreference>;
  save: AccountSaveStatus;
  onChange: (collapsed: boolean) => void;
  onRetry: () => void;
}) {
  const labelId = React.useId();
  const helpId = React.useId();
  if (sidebar.status === "loading")
    return (
      <div
        role="status"
        aria-label={copy.sidebar.loading}
        className="flex flex-col gap-2 rounded-[10px] border border-line-strong p-3"
      >
        <Skeleton className="h-3.5 w-40" />
        <Skeleton className="h-3 w-56 max-w-full" />
      </div>
    );
  if (sidebar.status === "error")
    return (
      <LoadError
        message={copy.sidebar.loadError}
        retry={copy.sidebar.retry}
        onRetry={onRetry}
      />
    );
  return (
    <>
      <div
        data-f="swbox"
        className="flex items-start gap-3 rounded-[10px] border border-line-strong bg-popover px-3 py-2.5 dark:bg-card"
      >
        <span className="min-w-0 flex-1">
          <b
            id={labelId}
            className="block text-compact leading-[1.45] font-semibold text-foreground"
          >
            {copy.sidebar.label}
          </b>
          <small
            id={helpId}
            className="block text-xs leading-[1.4] text-meta-foreground"
          >
            {copy.sidebar.help}
          </small>
        </span>
        <span className="mt-0.5 flex">
          <Toggle
            checked={sidebar.data.collapsed}
            disabled={save.state === "saving"}
            aria-labelledby={labelId}
            aria-describedby={helpId}
            onChange={onChange}
          />
        </span>
      </div>
      {save.state === "error" ? (
        <p
          role="alert"
          className="m-0 mt-2 rounded-md bg-danger-subtle px-3 py-2 text-metadata text-danger-subtle-foreground"
        >
          {copy.sidebar.saveError}
        </p>
      ) : null}
    </>
  );
}

function Preview({
  copy,
  accentName,
  modeName,
}: {
  copy: AccountAppearanceCopy;
  accentName: string;
  modeName: string;
}) {
  const p = copy.preview;
  const titleId = React.useId();
  return (
    <aside className="flex flex-col gap-3" aria-labelledby={titleId}>
      <section className="overflow-hidden rounded-[12px] border border-line bg-popover dark:bg-card">
        <div className="flex items-center gap-2 border-b border-line px-3.5 py-[11px]">
          <h3
            id={titleId}
            className="m-0 text-compact leading-[1.45] font-semibold text-foreground"
          >
            {p.title}
          </h3>
          <small className="ml-auto text-xs text-meta-foreground">
            {accentName} · {modeName}
          </small>
        </div>
        <div
          aria-hidden="true"
          className="flex flex-col gap-2.5 bg-background px-3.5 pt-3 pb-3.5"
        >
          <div className="flex gap-3.5 border-b border-line text-crumb leading-[1.45] text-meta-foreground">
            <span className="pb-[7px]">{p.tabs.table}</span>
            <span className="pb-[7px] font-semibold text-foreground shadow-[inset_0_-2px_0_var(--color-primary)]">
              {p.tabs.kanban}
            </span>
            <span className="pb-[7px]">{p.tabs.calendar}</span>
          </div>
          <PreviewCard
            selected
            code="ERP-142"
            title={p.cardOne}
            status={p.doing}
            dot="bg-work-status-doing"
            tail={
              <span className="inline-flex h-5 items-center rounded-[5px] bg-danger-subtle px-[7px] font-semibold text-danger-text">
                {p.overdue}
              </span>
            }
          />
          <PreviewCard
            code="ERP-138"
            title={p.cardTwo}
            status={p.review}
            dot="bg-work-status-review"
            tail={<span>{p.dateTwo}</span>}
          />
          <div className="flex items-center gap-3">
            <span className="inline-flex h-[30px] items-center gap-1.5 rounded-[7px] bg-primary px-[11px] text-crumb leading-[1.45] font-semibold whitespace-nowrap text-primary-foreground">
              <Plus className="size-4" />
              {p.addItem}
            </span>
            <span className="text-crumb leading-[1.45] font-semibold whitespace-nowrap text-link underline underline-offset-2">
              {p.openBoard}
            </span>
          </div>
        </div>
        <p className="m-0 border-t border-line px-3.5 pt-2.5 pb-3 text-xs leading-normal text-muted-foreground">
          <b className="font-semibold text-foreground">{p.changes}</b>
          {p.changesTo}
          <b className="font-semibold text-foreground">{p.stays}</b>
          {p.staysTo}
        </p>
      </section>
    </aside>
  );
}

function PreviewCard({
  selected = false,
  code,
  title,
  status,
  dot,
  tail,
}: {
  selected?: boolean;
  code: string;
  title: string;
  status: string;
  dot: string;
  tail: React.ReactNode;
}) {
  return (
    <div
      className={cn(
        "flex flex-col gap-[7px] rounded-[9px] border bg-popover px-2.5 py-[9px] dark:bg-card",
        selected
          ? "border-primary shadow-[0_0_0_1px_var(--color-primary)]"
          : "border-line",
      )}
    >
      <span className="text-crumb leading-[1.4] font-medium text-foreground">
        <span className="mr-[5px] font-mono text-hint text-meta-foreground">
          {code}
        </span>
        {title}
      </span>
      <span className="flex flex-wrap items-center gap-1.5 text-micro leading-[1.45] text-muted-foreground">
        <span className="inline-flex h-5 items-center gap-[5px] rounded-[5px] bg-state-hover-strong px-[7px] font-medium text-foreground">
          <span className={cn("size-[7px] rounded-full", dot)} />
          {status}
        </span>
        {tail}
        <span className="ml-auto grid size-5 place-items-center rounded-full bg-identity-blue text-mark-3xs font-bold text-identity-blue-foreground">
          MQ
        </span>
      </span>
    </div>
  );
}

/**
 * Account appearance (Final-Account `giao-dien`): theme mode as four
 * pictured cards, the accent colour, the language and the sidebar default,
 * with a live preview beside them. Mode, accent and language are device
 * choices handled by the theme and locale runtimes; the sidebar default is
 * the account's own preference.
 */
export function AccountAppearanceSurface({
  copy,
  mode,
  accent,
  locale,
  workspaceName,
  sidebar,
  sidebarSave,
  toast,
  onModeChange,
  onAccentChange,
  onLocaleChange,
  onSidebarCollapsedChange,
  onRetryPreferences,
}: AccountAppearanceSurfaceProps) {
  const prefix = React.useId();
  const accentName = copy.accent[accent].label;
  const modeName = copy.preview.modeName[MODE_COPY_KEYS[mode]];
  const modeId = `${prefix}-mode`;
  const accentId = `${prefix}-accent`;
  const languageId = `${prefix}-language`;
  const sidebarId = `${prefix}-sidebar`;
  return (
    <section
      aria-labelledby={`${prefix}-title`}
      className="flex min-w-0 flex-col gap-4"
    >
      <PageHead
        id={`${prefix}-title`}
        title={copy.title}
        description={copy.description}
      />
      <div className="grid items-start gap-6 min-[1100px]:grid-cols-[minmax(0,1fr)_272px]">
        <div className="flex min-w-0 flex-col gap-[22px]">
          <div>
            <GroupTitle id={modeId}>{copy.mode.legend}</GroupTitle>
            <div
              role="radiogroup"
              aria-labelledby={modeId}
              className="grid grid-cols-4 gap-2.5 max-md:grid-cols-2"
            >
              {THEME_MODES.map((option) => {
                const text = copy.mode[MODE_COPY_KEYS[option]];
                const descId = `${prefix}-mode-${option}-d`;
                const nameId = `${prefix}-mode-${option}-n`;
                return (
                  <label
                    key={option}
                    className={cn(OPTION, "flex-col gap-2 px-2 pt-2 pb-2.5")}
                  >
                    <ModePicture mode={option} accent={accent} />
                    <span className="flex items-center gap-[7px] text-compact leading-[1.45] font-semibold text-foreground">
                      <input
                        type="radio"
                        name={modeId}
                        value={option}
                        checked={mode === option}
                        aria-labelledby={nameId}
                        aria-describedby={descId}
                        onChange={() => onModeChange(option)}
                        className={RADIO}
                      />
                      <span id={nameId}>{text.label}</span>
                    </span>
                    <small
                      id={descId}
                      className="block pl-[23px] text-xs leading-[1.4] text-meta-foreground"
                    >
                      {text.description}
                    </small>
                  </label>
                );
              })}
            </div>
          </div>

          <div>
            <GroupTitle id={accentId}>{copy.accent.legend}</GroupTitle>
            <div
              role="radiogroup"
              aria-labelledby={accentId}
              className="grid grid-cols-2 gap-2 max-sm:grid-cols-1"
            >
              {THEME_ACCENT_OPTIONS.map((option) => {
                const text = copy.accent[option.id];
                const descId = `${prefix}-accent-${option.id}-d`;
                const nameId = `${prefix}-accent-${option.id}-n`;
                return (
                  <label
                    key={option.id}
                    className={cn(OPTION, "items-center gap-2.5 px-2.5 py-2")}
                  >
                    <input
                      type="radio"
                      name={accentId}
                      value={option.id}
                      checked={accent === option.id}
                      aria-labelledby={nameId}
                      aria-describedby={descId}
                      onChange={() => onAccentChange(option.id)}
                      className={RADIO}
                    />
                    <span
                      aria-hidden="true"
                      data-accent-swatch={option.id}
                      className={cn(
                        "grid size-[26px] shrink-0 place-items-center rounded-full shadow-[inset_0_0_0_1px_rgb(0_0_0/0.2)]",
                        option.id === "amber"
                          ? "text-black"
                          : "text-white dark:text-black",
                      )}
                      style={{ background: option.swatch }}
                    >
                      {accent === option.id ? (
                        <Check className="size-3.5" strokeWidth={2.6} />
                      ) : null}
                    </span>
                    <span className="min-w-0">
                      <b
                        id={nameId}
                        className="block text-compact leading-[1.45] font-semibold text-foreground"
                      >
                        {text.label}
                      </b>
                      <small
                        id={descId}
                        className="block text-xs leading-[1.35] text-meta-foreground"
                      >
                        {text.description}
                      </small>
                    </span>
                  </label>
                );
              })}
            </div>
          </div>

          <div className="grid grid-cols-2 gap-4 max-sm:grid-cols-1">
            <div>
              <GroupTitle id={languageId}>{copy.language.legend}</GroupTitle>
              <div
                role="radiogroup"
                aria-labelledby={languageId}
                className="flex gap-2"
              >
                {LOCALE_OPTIONS.map((option) => (
                  <label
                    key={option}
                    lang={option}
                    className={cn(
                      OPTION,
                      "h-10 flex-1 items-center gap-[9px] px-3 text-compact leading-[1.45] font-semibold text-foreground",
                    )}
                  >
                    <input
                      type="radio"
                      name={languageId}
                      value={option}
                      checked={locale === option}
                      onChange={() => onLocaleChange(option)}
                      className={RADIO}
                    />
                    {copy.language[option]}
                  </label>
                ))}
              </div>
            </div>
            <div>
              <GroupTitle id={sidebarId}>{copy.sidebar.legend}</GroupTitle>
              <SidebarChoice
                copy={copy}
                sidebar={sidebar}
                save={sidebarSave}
                onChange={onSidebarCollapsedChange}
                onRetry={onRetryPreferences}
              />
            </div>
          </div>
        </div>

        <div className="flex flex-col gap-3">
          <Preview copy={copy} accentName={accentName} modeName={modeName} />
          <Note>
            {fillCopy(copy.companyDefault, {
              workspace: workspaceName,
              accent: "\u0000",
            })
              .split("\u0000")
              .flatMap((part, index) =>
                index === 0
                  ? [part]
                  : [
                      <b key="accent" className="font-semibold text-foreground">
                        {copy.accent.blue.label}
                      </b>,
                      part,
                    ],
              )}
          </Note>
        </div>
      </div>
      {toast ? <Toast>{copy.sidebar.saved}</Toast> : null}
    </section>
  );
}

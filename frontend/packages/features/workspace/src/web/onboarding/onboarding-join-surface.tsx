import {
  Check,
  Contrast,
  LayoutPanelTop,
  Monitor,
  Moon,
  Sun,
  Upload,
  type LucideIcon,
} from "lucide-react";
import {
  THEME_ACCENT_OPTIONS,
  THEME_MODES,
  cn,
  type ThemeAccent,
  type ThemeMode,
} from "@notrelix/ui-web";
import type { OnboardingCopy } from "./onboarding.copy";
import { fillOnboarding } from "./fill-copy";
import {
  BUTTON_PRIMARY_CLASS,
  FIELD_INPUT_CLASS,
  FieldLabel,
  Hint,
  OPTION_CLASS,
  OnboardingFrame,
} from "./onboarding-frame";
import { OnboardingPreview } from "./onboarding-preview";
import {
  DEPARTMENT_TONE_CLASS,
  departmentInitials,
  type DepartmentColor,
} from "./onboarding-models";
import { DisabledHint } from "./disabled-hint";

/**
 * What an invited person sees on the first visit (the Onboarding mockup):
 * where they can go, their profile, the look of the app and the email they
 * get. The theme and the accent are applied by the host as they are picked,
 * so the page and its picture show them. Pure: everything is injected.
 */
export interface JoinSpace {
  readonly id: string;
  readonly name: string;
  readonly color: DepartmentColor | null;
  /** Open to the whole workspace (not Private). */
  readonly open: boolean;
  /** The person belongs to it (they hold a role there). */
  readonly member: boolean;
  readonly boards: readonly {
    readonly id: string;
    readonly title: string;
    readonly level: "viewer" | "commenter" | "editor";
  }[];
}

export interface OnboardingJoinSurfaceProps {
  readonly copy: OnboardingCopy;
  readonly email: string;
  readonly org: { readonly name: string; readonly initials: string };
  readonly inviterName: string;
  readonly spaces: readonly JoinSpace[];
  readonly displayName: string;
  readonly initials: string;
  readonly mode: ThemeMode;
  readonly accent: ThemeAccent;
  /** The accent the company sets by default. */
  readonly companyAccent: ThemeAccent;
  readonly notifyEmail: "digest" | "mentions";
  readonly pending?: boolean;
  readonly error?: "unreachable" | "name" | null;
  readonly onDisplayName: (value: string) => void;
  readonly onMode: (mode: ThemeMode) => void;
  readonly onAccent: (accent: ThemeAccent) => void;
  readonly onNotifyEmail: (value: "digest" | "mentions") => void;
  readonly onStart: () => void;
  readonly onLater: () => void;
}

const MODE_ICONS: Readonly<Record<ThemeMode, LucideIcon>> = {
  light: Sun,
  dark: Moon,
  system: Monitor,
  "high-contrast": Contrast,
};

export function OnboardingJoinSurface(props: OnboardingJoinSurfaceProps) {
  const { copy, spaces } = props;
  const t = copy.join;
  const mine = spaces.filter((space) => space.member);
  const open = spaces.filter((space) => space.open && !space.member);
  const firstName =
    props.displayName
      .replace(/\s{0,16}\([^)]{0,64}\)\s{0,16}$/, "")
      .trim()
      .split(/\s+/)
      .at(-1) ?? "";
  const accentName = t.accents[props.companyAccent];
  const firstBoard = mine.flatMap((space) => space.boards)[0];
  const aside = (
    <OnboardingPreview
      copy={copy}
      title={copy.preview.join.title}
      caption={copy.preview.captions.join}
      alt={copy.preview.alt.join}
      company={{
        name: props.org.name,
        initials: props.org.initials,
        sub: "",
      }}
      departments={spaces.slice(0, 3).map((space) => ({
        name: space.name,
        color: space.color ?? "blue",
        locked: !space.open,
        boards: space.member
          ? space.boards.slice(0, 3).map((board, index) => ({
              name: board.title,
              isNew: index === 0,
            }))
          : [],
      }))}
      activeBoard={firstBoard?.title ?? null}
      user={{
        name: props.displayName,
        initials: props.initials,
        role: copy.preview.member,
        toneClass: "bg-mark-crimson text-mark-crimson-foreground",
      }}
      main={{
        kind: "board",
        title: firstBoard?.title ?? props.org.name,
        lanes: [
          {
            label: fillOnboarding(copy.preview.join.lane, { count: 3 }),
            card: copy.preview.join.sample.cardOne,
            meta: copy.preview.join.sample.keyOne,
            late: fillOnboarding(copy.preview.join.overdue, { count: 2 }),
          },
          {
            label: fillOnboarding(copy.preview.join.review, { count: 2 }),
            card: copy.preview.join.sample.cardTwo,
            meta: copy.preview.join.sample.keyTwo,
          },
        ],
      }}
    />
  );
  return (
    <OnboardingFrame
      copy={copy}
      stepLabel={copy.frame.joinLabel}
      stepName={fillOnboarding(copy.frame.joinName, { org: props.org.name })}
      email={props.email}
      exit={{ label: copy.frame.later, onClick: props.onLater }}
      aside={aside}
      footer={
        <>
          <span className="text-item-ui text-muted-foreground">{t.foot}</span>
          <button
            type="button"
            disabled={props.pending}
            onClick={props.onStart}
            className={BUTTON_PRIMARY_CLASS}
          >
            {props.pending ? t.starting : t.start}
          </button>
        </>
      }
    >
      <div className="flex items-start gap-3.5">
        <span
          aria-hidden="true"
          className="grid size-11 shrink-0 place-items-center rounded-[11px] bg-primary text-body font-bold text-primary-foreground"
        >
          {props.org.initials}
        </span>
        <div className="flex flex-col gap-1.5">
          <h1 className="m-0 text-item-title leading-tight tracking-tight">
            {fillOnboarding(t.title, { name: firstName, org: props.org.name })}
          </h1>
          <p className="m-0 text-item-prose leading-normal text-muted-foreground">
            {fillOnboarding(t.lead, { inviter: props.inviterName })}
          </p>
        </div>
      </div>
      {props.error === "unreachable" ? (
        <p
          role="alert"
          className="m-0 text-item-ui font-medium text-danger-text"
        >
          {copy.frame.unreachable}
        </p>
      ) : null}

      <section aria-labelledby="join-access" className="flex flex-col gap-2.5">
        <h2 id="join-access" className="m-0 text-item-prose font-semibold">
          {t.accessTitle}
        </h2>
        <div className="overflow-hidden rounded-[12px] border border-line">
          {mine.length === 0 ? (
            <p className="m-0 px-4 py-3 text-item-ui text-muted-foreground">
              {t.noAccess}
            </p>
          ) : (
            mine.map((space) => (
              <div
                key={space.id}
                className="border-b border-line last:border-b-0"
              >
                <div className="flex items-center gap-2.5 px-4 py-3">
                  <span
                    aria-hidden="true"
                    className={cn(
                      "grid size-5.5 place-items-center rounded-[6px] text-mark-xs font-bold",
                      DEPARTMENT_TONE_CLASS[space.color ?? "blue"],
                    )}
                  >
                    {departmentInitials(space.name)}
                  </span>
                  <b className="flex-1 text-item-prose font-semibold">
                    {space.name}
                  </b>
                  <span className="rounded-full bg-state-hover-strong px-2.5 py-0.5 text-item-small font-semibold">
                    {t.memberBadge}
                  </span>
                </div>
                {space.boards.map((board) => (
                  <div
                    key={board.id}
                    className="flex items-center gap-2.5 border-t border-line px-4 py-2.5 pl-5.5 text-item-prose"
                  >
                    <LayoutPanelTop
                      aria-hidden="true"
                      className="size-3.5 shrink-0 text-meta-foreground"
                    />
                    <span className="flex-1">{board.title}</span>
                    <span className="text-item-ui text-muted-foreground">
                      {t.levels[board.level]}
                    </span>
                  </div>
                ))}
              </div>
            ))
          )}
          {open.length > 0 ? (
            <p className="m-0 border-t border-line bg-state-hover px-4 py-3 text-item-ui leading-normal text-muted-foreground">
              {fillOnboarding(t.openNote, {
                names: open.map((space) => space.name).join(", "),
              })}
            </p>
          ) : null}
        </div>
      </section>

      <section aria-labelledby="join-profile" className="flex flex-col gap-3">
        <h2 id="join-profile" className="m-0 text-item-prose font-semibold">
          {t.profileTitle}
        </h2>
        <div className="flex items-end gap-4">
          <span
            aria-hidden="true"
            className="grid size-13 shrink-0 place-items-center rounded-full bg-mark-crimson text-doc-heading font-bold text-mark-crimson-foreground"
          >
            {props.initials}
          </span>
          <div className="flex min-w-0 flex-1 flex-col gap-1.5">
            <FieldLabel htmlFor="join-name">{t.nameLabel}</FieldLabel>
            <input
              id="join-name"
              type="text"
              autoComplete="name"
              value={props.displayName}
              aria-invalid={props.error === "name" ? true : undefined}
              onChange={(event) => props.onDisplayName(event.target.value)}
              className={FIELD_INPUT_CLASS}
            />
            {props.error === "name" ? (
              <p
                role="alert"
                className="m-0 text-item-ui font-medium text-danger-text"
              >
                {t.nameRequired}
              </p>
            ) : null}
          </div>
          <DisabledHint reason={t.uploadUnavailable}>
            <button
              type="button"
              aria-disabled="true"
              className="inline-flex h-10 items-center gap-2 rounded-[8px] border border-line-strong bg-popover px-3.5 text-body font-medium text-foreground opacity-70"
            >
              <Upload aria-hidden="true" className="size-4" />
              {t.upload}
            </button>
          </DisabledHint>
        </div>
      </section>

      <section aria-labelledby="join-look" className="flex flex-col gap-3">
        <h2 id="join-look" className="m-0 text-item-prose font-semibold">
          {t.appearanceTitle}
        </h2>
        <div className="flex flex-wrap items-center gap-2.5">
          <span
            id="join-mode"
            className="w-18 shrink-0 text-item-ui font-semibold text-muted-foreground"
          >
            {t.modeLabel}
          </span>
          <div
            role="radiogroup"
            aria-labelledby="join-mode"
            className="flex flex-wrap gap-2"
          >
            {THEME_MODES.map((mode) => {
              const Icon = MODE_ICONS[mode];
              return (
                <label key={mode} className={cn(OPTION_CLASS, "h-9 px-3")}>
                  <input
                    type="radio"
                    name="join-mode"
                    value={mode}
                    checked={props.mode === mode}
                    onChange={() => props.onMode(mode)}
                    className="size-4 accent-primary"
                  />
                  <Icon aria-hidden="true" className="hidden size-4" />
                  {t.modes[mode]}
                </label>
              );
            })}
          </div>
        </div>
        <div className="flex flex-wrap items-center gap-2.5">
          <span
            id="join-accent"
            className="w-18 shrink-0 text-item-ui font-semibold text-muted-foreground"
          >
            {t.accentLabel}
          </span>
          <div
            role="radiogroup"
            aria-labelledby="join-accent"
            className="flex flex-wrap gap-2"
          >
            {THEME_ACCENT_OPTIONS.map(({ id, swatch }) => {
              const on = props.accent === id;
              return (
                <label
                  key={id}
                  className={cn(
                    OPTION_CLASS,
                    "h-9 gap-2 border-transparent bg-transparent px-2 has-[:checked]:border-line-strong has-[:checked]:bg-state-hover",
                  )}
                >
                  <input
                    type="radio"
                    name="join-accent"
                    value={id}
                    checked={on}
                    onChange={() => props.onAccent(id)}
                    className="sr-only"
                  />
                  <span
                    aria-hidden="true"
                    className="grid size-5 place-items-center rounded-full text-primary-foreground"
                    style={{ backgroundColor: swatch }}
                  >
                    {on ? <Check className="size-3.5" strokeWidth={3} /> : null}
                  </span>
                  {t.accents[id]}
                </label>
              );
            })}
          </div>
        </div>
        <Hint>
          {fillOnboarding(t.accentNote, {
            org: props.org.name,
            accent: accentName,
          })}
        </Hint>
      </section>

      <fieldset className="m-0 flex min-w-0 flex-col gap-2.5 border-0 p-0">
        <legend className="mb-1 p-0 text-item-prose font-semibold">
          {t.emailTitle}
        </legend>
        <div className="flex flex-wrap gap-2.5">
          {(["digest", "mentions"] as const).map((value) => (
            <label key={value} className={OPTION_CLASS}>
              <input
                type="radio"
                name="join-email"
                value={value}
                checked={props.notifyEmail === value}
                onChange={() => props.onNotifyEmail(value)}
                className="size-4 accent-primary"
              />
              {value === "digest" ? t.emailDigest : t.emailMentions}
            </label>
          ))}
        </div>
      </fieldset>
    </OnboardingFrame>
  );
}

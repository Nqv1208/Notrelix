import { useState } from "react";
import {
  ArrowRight,
  Check,
  ChevronLeft,
  SquareCheck,
  Smartphone,
  UserPlus,
} from "lucide-react";
import { cn } from "@notrelix/ui-web";
import type { OnboardingCopy } from "./onboarding.copy";
import { fillOnboarding, pluralOnboarding } from "./fill-copy";
import {
  BUTTON_OUTLINE_CLASS,
  BUTTON_PRIMARY_CLASS,
  BUTTON_QUIET_CLASS,
  OnboardingFrame,
  StepBar,
  StepTitle,
} from "./onboarding-frame";
import {
  OnboardingPreview,
  type PreviewDepartment,
} from "./onboarding-preview";
import {
  BoardStep,
  DepartmentsStep,
  InviteStep,
  WorkspaceStep,
  inviteRowProblem,
} from "./onboarding-steps";
import {
  ONBOARDING_STEPS,
  companyInitials,
  shortCompanyName,
  type AddressCheck,
  type BoardTemplateId,
  type OnboardingCatalog,
  type OnboardingDraftData,
  type OnboardingStepId,
  type OnboardingSummary,
} from "./onboarding-models";

/**
 * The company setup (the Onboarding mockup): four steps and a last page, each
 * with the small picture of the product beside it. Pure: the draft, the
 * address check, the suggestions, the summary and the callbacks are injected;
 * the checks it makes before "Tiếp tục" are on the draft alone.
 */
export interface OnboardingSurfaceProps {
  readonly copy: OnboardingCopy;
  readonly step: OnboardingStepId;
  readonly draft: OnboardingDraftData;
  /** The signed-in email, top right. */
  readonly email: string;
  readonly ownerName: string;
  readonly address: AddressCheck;
  readonly catalog: OnboardingCatalog | null;
  readonly summary: OnboardingSummary | null;
  /** A call is on its way: the buttons wait. */
  readonly pending?: boolean;
  readonly error?: "unreachable" | "emailNotConfirmed" | null;
  /** Asks for a new confirmation link (the "emailNotConfirmed" notice offers it). */
  readonly onResendConfirmation?: () => void;
  /** Where the last "send again" stands; `secondsLeft` counts down to the next one. */
  readonly resend?: {
    readonly status: "idle" | "sending" | "sent" | "limited" | "failed";
    readonly secondsLeft: number;
  };
  /** The workspace and its departments exist already (a step revisited). */
  readonly created?: {
    readonly workspace: boolean;
    readonly departments: boolean;
  };
  /** Shown in the preview greeting ("Thứ Hai, 12 tháng 10"). */
  readonly today: string;
  readonly onDraft: (next: OnboardingDraftData) => void;
  readonly onUseSuggestion: (slug: string) => void;
  /** "Tiếp tục" and its kin: the container writes what the step made. */
  readonly onNext: () => void;
  readonly onBack: () => void;
  readonly onSkip: () => void;
  readonly onExit: () => void;
  readonly onEnterHome: () => void;
}

export function OnboardingSurface(props: OnboardingSurfaceProps) {
  const {
    copy,
    step,
    draft,
    email,
    pending,
    error,
    catalog,
    address,
    summary,
    onNext,
    onBack,
    onSkip,
    onExit,
    onEnterHome,
  } = props;
  const [tried, setTried] = useState(false);
  const resend = props.resend ?? { status: "idle", secondsLeft: 0 };
  const t = copy.frame;
  const index =
    step === "done" ? ONBOARDING_STEPS.length : ONBOARDING_STEPS.indexOf(step);

  const workspaceReady =
    props.created?.workspace === true ||
    (draft.workspace.name.trim().length > 0 && address.kind === "free");
  const departmentsReady =
    draft.departments.every((entry) => entry.name.trim()) &&
    new Set(draft.departments.map((entry) => entry.name.trim().toLowerCase()))
      .size === draft.departments.length;
  const filledInvites = draft.invites.filter((row) => row.email.trim());
  const invitesReady = draft.invites.every(
    (row, i) => inviteRowProblem(draft.invites, row, i) === null,
  );
  const boardReady = draft.board.name.trim().length > 0;

  const next = (ready: boolean) => () => {
    setTried(true);
    if (ready) onNext();
  };

  // ── Footer ────────────────────────────────────────────────────────────────
  let footer: React.ReactNode;
  if (step === "workspace") {
    footer = (
      <>
        <span className="text-item-ui text-muted-foreground">
          {address.kind === "taken"
            ? copy.workspace.footTaken
            : address.kind === "invalid"
              ? copy.workspace.footInvalid
              : copy.workspace.footNote}
        </span>
        <button
          type="button"
          disabled={pending || (tried && !workspaceReady)}
          aria-disabled={!workspaceReady || undefined}
          onClick={next(workspaceReady)}
          className={BUTTON_PRIMARY_CLASS}
        >
          {pending ? t.saving : t.next}
        </button>
      </>
    );
  } else if (step === "done") {
    footer = (
      <>
        <span className="text-item-ui text-muted-foreground">
          {copy.done.foot}
        </span>
        <button
          type="button"
          onClick={onEnterHome}
          className={BUTTON_PRIMARY_CLASS}
        >
          {copy.done.enter}
          <ArrowRight aria-hidden="true" className="size-4" />
        </button>
      </>
    );
  } else {
    const count = filledInvites.length;
    footer = (
      <>
        <button
          type="button"
          disabled={pending}
          onClick={onBack}
          className={BUTTON_OUTLINE_CLASS}
        >
          <ChevronLeft aria-hidden="true" className="size-4" />
          {t.back}
        </button>
        <span className="flex items-center gap-2">
          {step === "invite" || step === "board" ? (
            <button
              type="button"
              disabled={pending}
              onClick={onSkip}
              className={BUTTON_QUIET_CLASS}
            >
              {step === "invite" ? copy.invite.skip : copy.board.skip}
            </button>
          ) : null}
          {step === "departments" ? (
            <button
              type="button"
              disabled={pending || draft.departments.length > 0}
              onClick={onSkip}
              className={cn(
                BUTTON_QUIET_CLASS,
                draft.departments.length > 0 && "hidden",
              )}
            >
              {copy.departments.skip}
            </button>
          ) : null}
          <button
            type="button"
            disabled={pending}
            onClick={
              step === "departments"
                ? next(departmentsReady && draft.departments.length > 0)
                : step === "invite"
                  ? next(invitesReady)
                  : next(boardReady)
            }
            className={BUTTON_PRIMARY_CLASS}
          >
            {pending
              ? t.saving
              : step === "departments"
                ? pluralOnboarding(
                    copy.departments.create_one,
                    copy.departments.create_other,
                    draft.departments.length,
                  )
                : step === "invite"
                  ? count > 0
                    ? pluralOnboarding(
                        copy.invite.send_one,
                        copy.invite.send_other,
                        count,
                      )
                    : copy.invite.sendNone
                  : copy.board.create}
          </button>
        </span>
      </>
    );
  }

  // ── Preview ───────────────────────────────────────────────────────────────
  const companyName = draft.workspace.name.trim() || summary?.companyName || "";
  const shown = step === "done" || index >= 1;
  const created: PreviewDepartment[] = shown
    ? (step === "done" && summary
        ? summary.departments.map((entry) => ({ ...entry, locked: true }))
        : draft.departments.map((entry) => ({
            name: entry.name,
            color: entry.color,
            locked: true,
          }))
      ).map((entry) => ({
        name: entry.name,
        color: entry.color,
        locked: true,
        boards: [],
      }))
    : [];
  const boardName =
    step === "done" ? (summary?.board?.name ?? null) : draft.board.name.trim();
  const boardDepartment =
    step === "done"
      ? (summary?.board?.departmentName ?? null)
      : (draft.departments.find(
          (entry) => entry.key === draft.board.departmentKey,
        )?.name ?? null);
  const withBoard = created.map((entry) =>
    (step === "board" || step === "done") &&
    boardName &&
    entry.name === boardDepartment
      ? { ...entry, boards: [{ name: boardName, isNew: true }] }
      : entry,
  );
  const invitesCount =
    step === "done" ? (summary?.invitesSent ?? 0) : filledInvites.length;
  const sub =
    step === "workspace" && address.kind === "taken"
      ? copy.preview.noAddress
      : step === "workspace"
        ? draft.workspace.slug
          ? `${draft.workspace.slug}${copy.workspace.suffix}`
          : copy.preview.noAddress
        : index >= 2 || step === "done"
          ? invitesCount > 0
            ? fillOnboarding(copy.preview.peopleInvites, {
                people: 1,
                invites: invitesCount,
              })
            : fillOnboarding(copy.preview.people, { count: 1 })
          : `${draft.workspace.slug}${copy.workspace.suffix}`;
  const caption =
    step === "workspace"
      ? address.kind === "taken"
        ? copy.preview.captions.taken
        : copy.preview.captions.workspace
      : step === "departments"
        ? copy.preview.captions.departments
        : step === "invite"
          ? copy.preview.captions.invite
          : step === "board"
            ? boardDepartment
              ? fillOnboarding(copy.preview.captions.board, {
                  department: boardDepartment,
                })
              : copy.preview.captions.boardRoot
            : copy.preview.captions.done;
  const alt =
    step === "done"
      ? copy.preview.alt.home
      : withBoard.length > 0
        ? pluralOnboarding(
            copy.preview.alt.departments_one,
            copy.preview.alt.departments_other,
            withBoard.length,
          )
        : copy.preview.alt.none;

  const aside = (
    <OnboardingPreview
      copy={copy}
      title={copy.preview.title}
      caption={caption}
      alt={alt}
      company={{
        name: companyName || copy.workspace.namePlaceholder,
        initials: companyInitials(companyName || "?"),
        sub,
      }}
      departments={withBoard}
      user={{
        name: props.ownerName,
        initials: initialsOf(props.ownerName),
        role: copy.preview.owner,
        toneClass: "bg-mark-sage text-mark-sage-foreground",
      }}
      main={
        step === "done"
          ? {
              kind: "home",
              greeting: fillOnboarding(copy.preview.home.greeting, {
                name: props.ownerName.split(" ").at(-1) ?? props.ownerName,
              }),
              date: props.today,
              startTitle: fillOnboarding(copy.preview.home.startTitle, {
                name: shortCompanyName(companyName),
              }),
              items: [
                copy.done.items.firstItem,
                copy.done.items.inviteMore,
                copy.done.items.mobile,
              ],
            }
          : { kind: "stub" }
      }
    />
  );

  return (
    <OnboardingFrame
      copy={copy}
      stepLabel={
        step === "done"
          ? copy.frame.allDone
          : fillOnboarding(copy.frame.stepOf, { n: index + 1 })
      }
      stepName={step === "done" ? copy.frame.doneName : copy.frame.steps[step]}
      email={email}
      exit={
        step === "done"
          ? undefined
          : { label: copy.frame.exit, onClick: onExit, disabled: pending }
      }
      aside={aside}
      footer={footer}
    >
      <StepBar copy={copy} current={step} />
      {error === "unreachable" ? (
        <p
          role="alert"
          className="m-0 text-item-ui font-medium text-danger-text"
        >
          {copy.frame.unreachable}
        </p>
      ) : null}
      {error === "emailNotConfirmed" ? (
        <div
          role="alert"
          className="m-0 flex flex-col items-start gap-1.5 text-item-ui font-medium text-danger-text"
        >
          <p className="m-0">{copy.frame.emailNotConfirmed}</p>
          {props.onResendConfirmation ? (
            <button
              type="button"
              disabled={resend.status === "sending" || resend.secondsLeft > 0}
              onClick={props.onResendConfirmation}
              className="cursor-pointer border-0 bg-transparent p-0 font-semibold text-brand-subtle-foreground underline disabled:cursor-not-allowed disabled:text-state-disabled-foreground disabled:no-underline"
            >
              {resend.status === "sending"
                ? copy.frame.resendSending
                : resend.secondsLeft > 0
                  ? fillOnboarding(copy.frame.resendWait, {
                      time: `${Math.floor(resend.secondsLeft / 60)}:${String(resend.secondsLeft % 60).padStart(2, "0")}`,
                    })
                  : copy.frame.resendConfirmation}
            </button>
          ) : null}
          {resend.status === "sent" ? (
            <p role="status" className="m-0 font-normal text-success-text">
              {copy.frame.resendSent}
            </p>
          ) : null}
          {resend.status === "limited" ? (
            <p className="m-0 font-normal">{copy.frame.resendLimited}</p>
          ) : null}
          {resend.status === "failed" ? (
            <p className="m-0 font-normal">{copy.frame.resendFailed}</p>
          ) : null}
        </div>
      ) : null}
      {step === "workspace" ? (
        <WorkspaceStep
          copy={copy}
          draft={draft}
          address={address}
          onDraft={props.onDraft}
          onUseSuggestion={props.onUseSuggestion}
          showNameError={tried}
          locked={props.created?.workspace === true}
        />
      ) : null}
      {step === "departments" ? (
        <DepartmentsStep
          copy={copy}
          draft={draft}
          catalog={catalog}
          sizeLabel={copy.workspace.sizes[draft.workspace.size]}
          onDraft={props.onDraft}
          locked={props.created?.departments === true}
        />
      ) : null}
      {step === "invite" ? (
        <InviteStep
          copy={copy}
          draft={draft}
          onDraft={props.onDraft}
          showProblems={tried}
        />
      ) : null}
      {step === "board" ? (
        <BoardStep
          copy={copy}
          draft={draft}
          catalog={catalog}
          onDraft={props.onDraft}
          showNameError={tried}
        />
      ) : null}
      {step === "done" && summary ? (
        <DoneStep copy={copy} summary={summary} />
      ) : null}
    </OnboardingFrame>
  );
}

function initialsOf(name: string): string {
  const words = name.trim().split(/\s+/).filter(Boolean);
  return (
    words.length === 0
      ? "?"
      : words.length === 1
        ? (words[0] ?? "").slice(0, 2)
        : `${words[0]?.charAt(0) ?? ""}${words.at(-1)?.charAt(0) ?? ""}`
  ).toUpperCase();
}

function DoneStep({
  copy,
  summary,
}: {
  copy: OnboardingCopy;
  summary: OnboardingSummary;
}) {
  const t = copy.done;
  const boardDepartment = summary.board?.departmentName ?? null;
  const rows = [
    {
      title: fillOnboarding(t.workspace, { address: summary.address }),
      note: t.workspaceNote,
    },
    summary.departments.length > 0
      ? {
          title: pluralOnboarding(
            t.departments_one,
            t.departments_other,
            summary.departments.length,
          ),
          note: fillOnboarding(t.departmentsNote, {
            names: summary.departments.map((entry) => entry.name).join(", "),
          }),
        }
      : { title: t.noDepartments, note: t.noDepartmentsNote },
    summary.invitesSent > 0
      ? {
          title: pluralOnboarding(
            t.invites_one,
            t.invites_other,
            summary.invitesSent,
          ),
          note: t.invitesNote,
        }
      : { title: t.noInvites, note: t.noInvitesNote },
    summary.board
      ? {
          title: fillOnboarding(t.board, { name: summary.board.name }),
          note: boardDepartment
            ? fillOnboarding(t.boardNote, {
                template: copy.board.templates[summary.board.templateId].name,
                department: boardDepartment,
              })
            : fillOnboarding(t.boardNoteRoot, {
                template: copy.board.templates[summary.board.templateId].name,
              }),
        }
      : { title: t.noBoard, note: t.noBoardNote },
  ];
  const firstDepartmentWithoutPeople = summary.departments.at(-1)?.name ?? "";
  const next = [
    {
      title: t.items.firstItem,
      note: summary.board
        ? fillOnboarding(t.items.firstItemNote, { board: summary.board.name })
        : t.items.firstItemNoBoard,
      Icon: SquareCheck,
    },
    {
      title: t.items.inviteMore,
      note:
        summary.invitesSent > 0
          ? fillOnboarding(t.items.inviteMoreNote, {
              count: summary.invitesSent,
              department: firstDepartmentWithoutPeople,
            })
          : t.items.inviteMoreNoneNote,
      Icon: UserPlus,
    },
    { title: t.items.mobile, note: t.items.mobileNote, Icon: Smartphone },
  ];
  return (
    <>
      <div className="flex items-center gap-4">
        <span
          aria-hidden="true"
          className="grid size-13 shrink-0 place-items-center rounded-[14px] bg-success-subtle text-success-text"
        >
          <Check className="size-6" />
        </span>
        <StepTitle
          title={fillOnboarding(t.title, {
            name: shortCompanyName(summary.companyName),
          })}
          lead={fillOnboarding(t.lead, {
            company: summary.companyName,
            address: summary.address,
          })}
        />
      </div>
      <ul
        aria-label={t.summaryLabel}
        className="m-0 flex list-none flex-col overflow-hidden rounded-[12px] border border-line p-0"
      >
        {rows.map((row) => (
          <li
            key={row.title}
            className="flex items-center gap-3 border-b border-line px-4 py-3 text-item-prose last:border-b-0"
          >
            <span
              aria-hidden="true"
              className="grid size-5.5 shrink-0 place-items-center rounded-full bg-success-subtle text-success-text"
            >
              <Check className="size-3.5" strokeWidth={3} />
            </span>
            <span className="min-w-0 flex-1">{row.title}</span>
            <span className="text-item-ui text-muted-foreground">
              {row.note}
            </span>
          </li>
        ))}
      </ul>
      <div className="flex flex-col gap-1">
        <h2 className="m-0 text-item-prose font-semibold">
          {t.continueTitle}{" "}
          <span className="font-normal text-meta-foreground">0/3</span>
        </h2>
        <p className="m-0 text-item-ui text-muted-foreground">
          {t.continueNote}
        </p>
      </div>
      <ul className="m-0 flex list-none flex-col gap-2.5 p-0">
        {next.map(({ title, note, Icon }) => (
          <li
            key={title}
            className="flex items-center gap-3 rounded-[12px] border border-line bg-popover px-4 py-3.5"
          >
            <span
              aria-hidden="true"
              className="size-5.5 shrink-0 rounded-full border border-line-strong"
            />
            <span className="min-w-0 flex-1">
              <b className="block text-item-prose font-semibold">{title}</b>
              <span className="block text-item-ui text-muted-foreground">
                {note}
              </span>
            </span>
            <span
              aria-hidden="true"
              className="grid size-9 shrink-0 place-items-center rounded-[8px] bg-state-hover text-muted-foreground"
            >
              <Icon className="size-4" />
            </span>
          </li>
        ))}
      </ul>
    </>
  );
}

export type { BoardTemplateId };

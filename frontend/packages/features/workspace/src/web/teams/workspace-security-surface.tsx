import { Skeleton, cn } from "@notrelix/ui-web";
import type { SectionState } from "../home/home-models";
import { formatCopy } from "../shell/shell-models";
import { FOCUS, Glyph } from "../spaces/space-page-parts";
import {
  WorkspaceSetupFrame,
  type SetupFrameProps,
} from "./workspace-setup-frame";
import type { WorkspaceTeamsCopy } from "./workspace-teams.copy";

export interface WorkspaceSecurityView {
  /** Whether sharing links are allowed (the workspace policy). */
  readonly linksOn: boolean;
  /** "15/09" while off and the producer knows since when. */
  readonly offSince: string | null;
  readonly guestCount: number | null;
  /** Days a new guest's access lasts; `null` = no end. */
  readonly guestExpiryDays: number | null;
  readonly companyDomain: string | null;
}

export interface WorkspaceSecuritySurfaceProps {
  readonly copy: WorkspaceTeamsCopy;
  readonly frame: SetupFrameProps;
  readonly view: SectionState<WorkspaceSecurityView>;
  readonly canManage: boolean;
  readonly saving: boolean;
  /** A failure to show beside the switch. */
  readonly error: string | null;
  readonly onToggleLinks: (next: boolean) => void;
  readonly onRetry: () => void;
}

function Info({ children }: { children: React.ReactNode }) {
  return (
    <div className="mt-2.5 flex max-w-[64ch] items-start gap-[9px] rounded-[8px] border border-line bg-background px-3 py-2.5 text-crumb leading-normal text-muted-foreground">
      <Glyph
        name="infoCircle"
        className="mt-0.5 size-3.5 text-meta-foreground"
      />
      <span>{children}</span>
    </div>
  );
}

function Row({
  title,
  titleFor,
  text,
  children,
  control,
}: {
  title: string;
  titleFor?: string;
  text: string;
  children?: React.ReactNode;
  control: React.ReactNode;
}) {
  return (
    <div
      data-f="sr-row"
      className="flex items-start gap-[18px] border-t border-line px-[18px] py-4 first:border-t-0"
    >
      <div className="min-w-0 flex-1">
        {titleFor ? (
          <label
            htmlFor={titleFor}
            className="block text-body leading-[1.45] font-semibold text-foreground"
          >
            {title}
          </label>
        ) : (
          <b className="block text-body leading-[1.45] font-semibold text-foreground">
            {title}
          </b>
        )}
        <p className="m-0 mt-1 max-w-[64ch] text-crumb leading-[1.55] text-muted-foreground">
          {text}
        </p>
        {children}
      </div>
      <div className="flex shrink-0 items-center gap-2 pt-0.5">{control}</div>
    </div>
  );
}

/**
 * Sharing and security of the workspace (Final-Teams `chia-se`): the sharing
 * links switch (the workspace policy, a real write), the guest count, the
 * default end of a guest's access and the company domain.
 */
export function WorkspaceSecuritySurface(props: WorkspaceSecuritySurfaceProps) {
  const { copy, view } = props;
  const s = copy.security;
  const expiryKey = (days: number | null) =>
    days === 30 ? "d30" : days === 90 ? "d90" : days === 180 ? "d180" : "never";
  return (
    <WorkspaceSetupFrame copy={copy} frame={props.frame}>
      <section aria-labelledby="security-title" className="flex flex-col gap-4">
        <div data-f="ph">
          <h2
            id="security-title"
            className="m-0 text-lg leading-[1.45] font-[650] text-foreground"
          >
            {s.title}
          </h2>
          <p className="m-0 mt-[3px] max-w-[66ch] text-compact leading-normal text-meta-foreground">
            {formatCopy(s.description, {
              workspace: props.frame.workspaceName,
            })}
          </p>
        </div>
        {view.status === "loading" ? (
          <div
            role="status"
            aria-label={copy.list.loading}
            className="flex flex-col gap-2"
          >
            <Skeleton className="h-24 w-full rounded-[12px]" />
            <Skeleton className="h-24 w-full rounded-[12px]" />
          </div>
        ) : view.status === "error" ? (
          <div
            role="alert"
            className="flex flex-wrap items-center justify-between gap-3 rounded-[12px] border border-line bg-popover px-4 py-3 dark:bg-card"
          >
            <p className="m-0 text-body text-muted-foreground">
              {copy.list.error}
            </p>
            <button
              type="button"
              onClick={props.onRetry}
              className={cn(
                "inline-flex h-7 items-center rounded-[7px] border border-line-strong bg-popover px-2.5 text-crumb font-medium hover:bg-state-hover-strong dark:bg-card",
                FOCUS,
              )}
            >
              {copy.list.retry}
            </button>
          </div>
        ) : (
          <div
            data-f="set"
            className="rounded-[12px] border border-line bg-popover dark:bg-card"
          >
            <Row
              title={s.link.title}
              text={s.link.text}
              control={
                <>
                  <span
                    aria-hidden="true"
                    className="text-crumb font-medium text-meta-foreground"
                  >
                    {view.data.linksOn ? s.link.on : s.link.off}
                  </span>
                  <button
                    type="button"
                    role="switch"
                    aria-checked={view.data.linksOn}
                    aria-label={s.link.switchLabel}
                    aria-disabled={props.canManage ? undefined : true}
                    title={props.canManage ? undefined : s.readOnly}
                    disabled={props.saving}
                    onClick={
                      props.canManage
                        ? () => props.onToggleLinks(!view.data.linksOn)
                        : undefined
                    }
                    className={cn(
                      "relative h-5 w-[34px] shrink-0 rounded-full border-0 p-0 transition-colors",
                      view.data.linksOn ? "bg-primary" : "bg-line-strong",
                      props.canManage
                        ? "cursor-pointer"
                        : "cursor-not-allowed opacity-70",
                      FOCUS,
                    )}
                  >
                    <span
                      className={cn(
                        "absolute top-0.5 size-4 rounded-full bg-white shadow-[0_1px_2px_rgb(0_0_0/0.2)] transition-[left]",
                        view.data.linksOn ? "left-4" : "left-0.5",
                      )}
                    />
                  </button>
                </>
              }
            >
              <span className="mt-1.5 block text-xs text-meta-foreground">
                {view.data.linksOn
                  ? s.link.onNote
                  : formatCopy(s.link.offSince, {
                      date: view.data.offSince ?? "",
                    })}
              </span>
              <Info>
                <b className="font-semibold text-foreground">
                  {s.link.copyNoteLead}
                </b>
                {s.link.copyNote}
              </Info>
              {props.error ? (
                <p
                  role="alert"
                  className="m-0 mt-2 text-crumb text-danger-text"
                >
                  {props.error}
                </p>
              ) : null}
            </Row>
            <Row
              title={s.guests.title}
              text={s.guests.text}
              control={
                view.data.guestCount !== null ? (
                  <span className="inline-flex h-5 items-center rounded-[5px] bg-state-hover-strong px-[7px] text-micro font-semibold whitespace-nowrap text-muted-foreground">
                    {formatCopy(
                      view.data.guestCount === 1
                        ? s.guests.count_one
                        : s.guests.count_other,
                      { count: view.data.guestCount },
                    )}
                  </span>
                ) : null
              }
            >
              <Info>{s.guests.note}</Info>
            </Row>
            <Row
              title={s.expiry.title}
              titleFor="guest-expiry"
              text={s.expiry.text}
              control={
                <select
                  id="guest-expiry"
                  aria-disabled="true"
                  title={s.expiry.readOnly}
                  value={expiryKey(view.data.guestExpiryDays)}
                  onChange={() => undefined}
                  className="h-8 min-w-[150px] rounded-[7px] border border-line-strong bg-popover px-2 text-compact text-foreground dark:bg-card"
                >
                  {(["d30", "d90", "d180", "never"] as const).map((key) => (
                    <option key={key} value={key}>
                      {s.expiry.options[key]}
                    </option>
                  ))}
                </select>
              }
            />
            <Row
              title={s.domain.title}
              titleFor="company-domain"
              text={s.domain.text}
              control={
                <input
                  id="company-domain"
                  readOnly
                  value={view.data.companyDomain ?? ""}
                  className="box-border h-8 min-w-[150px] rounded-[7px] border border-line bg-background px-2.5 font-mono text-compact text-muted-foreground"
                />
              }
            />
          </div>
        )}
      </section>
    </WorkspaceSetupFrame>
  );
}

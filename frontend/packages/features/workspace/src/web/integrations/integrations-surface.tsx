import * as React from "react";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogTitle,
  cn,
} from "@notrelix/ui-web";
import { formatCopy } from "../shell/shell-models";
import type { SectionState } from "../home/home-models";
import { FOCUS } from "../spaces/space-page-parts";
import {
  BILLING_BTN,
  BILLING_BTN_PRIMARY,
  BILLING_BTN_SM,
  BackLink,
  BillingGlyph,
  BOX,
  BOX_TEXT,
  BOX_TITLE,
  CARD,
  DIALOG_OVERLAY,
  DIALOG_PANEL,
  InfoNote,
  LNKB,
  MONO,
  NavAnchor,
  PageHead,
  Rich,
} from "./../billing/billing-parts";
import type {
  IntegrationCatalogDto,
  IntegrationDetailDto,
  IntegrationRowDto,
  IntegrationStatus,
  IntegrationsLinks,
  IntegrationsListDto,
} from "./integrations-models";
import type { IntegrationsCopy } from "./integrations.copy";

const SMALL =
  "block text-caption tracking-normal leading-[1.45] text-meta-foreground";
const TONE = {
  ok: ["text-success-text", "bg-success"],
  warn: ["text-warning-text", "bg-warning"],
  off: [
    "text-muted-foreground",
    "bg-transparent shadow-[inset_0_0_0_2px_var(--color-meta-foreground)]",
  ],
} as const;

function Ind({ status }: { status: IntegrationStatus }) {
  return (
    <span
      className={cn(
        "inline-flex items-center gap-1.5 text-crumb leading-[1.45] font-semibold whitespace-nowrap",
        TONE[status.tone][0],
      )}
    >
      <i
        aria-hidden="true"
        className={cn("size-2 shrink-0 rounded-full", TONE[status.tone][1])}
      />
      {status.label}
    </span>
  );
}

function Pv({
  mono,
  large,
  className,
}: {
  mono: string;
  large?: boolean;
  className?: string;
}) {
  return (
    <span
      aria-hidden="true"
      className={cn(
        "box-border grid shrink-0 place-items-center border border-line-strong bg-background font-bold tracking-[0.02em] text-muted-foreground",
        large
          ? "size-11 rounded-[12px] text-item-heading"
          : "size-[34px] rounded-[9px] text-micro",
        className,
      )}
    >
      {mono}
    </span>
  );
}

type Shared = {
  readonly copy: IntegrationsCopy;
  readonly links: IntegrationsLinks;
};

// ── List ────────────────────────────────────────────────────────────────

export interface IntegrationsListProps extends Shared {
  readonly state: SectionState<IntegrationsListDto>;
  readonly onReload: () => void;
  readonly onConnect: (key: string) => void;
}

function ScopeChip({
  row,
  copy,
  workspaceName,
}: {
  row: IntegrationRowDto;
  copy: IntegrationsCopy;
  workspaceName: string;
}) {
  void workspaceName;
  return (
    <span className="flex flex-wrap gap-1 text-caption tracking-normal text-muted-foreground">
      <span className="inline-flex min-h-[22px] items-center gap-[5px] rounded-[5px] bg-state-hover-strong px-[7px] whitespace-nowrap">
        {row.scope.kind === "workspace" ? (
          <>
            <span className="grid size-3.5 place-items-center rounded-[4px] bg-brand-solid text-mark-2xs font-bold text-brand-solid-foreground">
              MA
            </span>
            {copy.scopeWorkspace}
          </>
        ) : (
          formatCopy(copy.scopeBoard, { name: row.scope.name })
        )}
      </span>
    </span>
  );
}

export function IntegrationsListSurface(props: IntegrationsListProps) {
  const { copy, links, state } = props;
  if (state.status !== "ready") {
    return state.status === "loading" ? (
      <p role="status" className="m-0 text-compact text-meta-foreground">
        {copy.loading}
      </p>
    ) : (
      <div role="alert" className={cn(BOX, "max-w-[520px]")}>
        <p className={BOX_TEXT}>{copy.error}</p>
        <button
          type="button"
          onClick={props.onReload}
          className={cn(BILLING_BTN, "self-start")}
        >
          {copy.retry}
        </button>
      </div>
    );
  }
  const d = state.data;
  const COLS =
    "grid grid-cols-[minmax(0,1fr)_168px_200px_176px_32px] items-center gap-3 px-3.5";
  return (
    <section
      aria-labelledby="int-t"
      data-f="integrations-list"
      className="flex flex-col gap-4"
    >
      <PageHead id="int-t" title={copy.title} lede={copy.lede} />
      {d.canManage ? null : (
        <InfoNote>
          <Rich
            text={formatCopy(copy.memberInfo, {
              names: d.admins.join(" hoặc "),
            })}
          />
        </InfoNote>
      )}
      <div className="-mb-1.5 mt-1.5 mx-0.5 flex items-center gap-2 text-crumb leading-[1.45] font-semibold text-meta-foreground">
        {formatCopy(copy.inUse, { count: d.rows.length })}
        <span className="ml-auto font-normal">
          {formatCopy(copy.updated, { time: d.updatedAt })}
          <button type="button" onClick={props.onReload} className={LNKB}>
            {copy.reload}
          </button>
        </span>
      </div>
      <div
        role="table"
        data-f="list"
        aria-label={formatCopy(copy.tableLabel, { workspace: d.workspaceName })}
        className={CARD}
      >
        <div
          role="row"
          className={cn(
            COLS,
            "h-9 rounded-t-[12px] border-b border-line bg-background text-micro font-semibold text-meta-foreground",
          )}
        >
          <span role="columnheader">{copy.columns.integration}</span>
          <span role="columnheader">{copy.columns.scope}</span>
          <span role="columnheader">{copy.columns.status}</span>
          <span role="columnheader">{copy.columns.last}</span>
          <span role="columnheader">
            <span className="sr-only">{copy.columns.open}</span>
          </span>
        </div>
        {d.rows.map((row) => (
          <div
            key={row.key}
            role="row"
            data-f="ir"
            className={cn(
              COLS,
              "box-border min-h-[72px] border-t border-line py-2.5 first-of-type:border-t-0 hover:bg-background",
            )}
          >
            <span role="cell" className="flex min-w-0 items-start gap-[11px]">
              <Pv mono={row.mono} />
              <span className="min-w-0">
                <b className="block text-body leading-[1.45] font-semibold text-foreground">
                  {row.name}
                </b>
                <small className={SMALL}>{row.sub}</small>
              </span>
            </span>
            <span role="cell">
              <ScopeChip
                row={row}
                copy={copy}
                workspaceName={d.workspaceName}
              />
            </span>
            <span
              role="cell"
              className="grid grid-cols-[54px_minmax(0,1fr)] items-center gap-x-1.5 gap-y-1.5 text-caption tracking-normal"
            >
              <span className="text-meta-foreground">{copy.conn}</span>
              <Ind status={row.conn} />
              <span className="text-meta-foreground">{copy.sync}</span>
              <Ind status={row.sync} />
            </span>
            <span role="cell" className="text-crumb leading-[1.4]">
              {row.last}
              <small className="block text-caption tracking-normal text-meta-foreground">
                {row.lastSub}
              </small>
            </span>
            <span role="cell" className="justify-self-end">
              {d.canManage ? (
                <NavAnchor
                  link={links.detail(row.key)}
                  aria-label={formatCopy(copy.openAria, { name: row.name })}
                  className={cn(
                    "grid size-8 place-items-center rounded-[7px] text-meta-foreground hover:bg-state-hover-strong hover:text-foreground",
                    FOCUS,
                  )}
                >
                  <BillingGlyph name="go" />
                </NavAnchor>
              ) : null}
            </span>
          </div>
        ))}
      </div>
      {d.canManage ? (
        <>
          <div className="-mb-1.5 mt-1.5 mx-0.5 flex items-center gap-2 text-crumb leading-[1.45] font-semibold text-meta-foreground">
            {copy.catalogTitle}
            <span className="ml-auto font-normal">{copy.catalogNote}</span>
          </div>
          <div
            role="list"
            aria-label={copy.catalogLabel}
            className={cn(CARD, "grid grid-cols-2")}
          >
            {d.catalog.map((item, index) => (
              <div
                key={item.key}
                role="listitem"
                className={cn(
                  "flex min-w-0 items-center gap-[11px] border-t border-line px-3.5 py-3",
                  index < 2 && "border-t-0",
                  index % 2 === 0 && "border-r border-line",
                )}
              >
                <Pv mono={item.mono} />
                <span className="min-w-0 flex-1">
                  <b className="flex items-center gap-1.5 text-nav leading-[1.45] font-semibold text-foreground">
                    {item.name}
                    {item.enterprise ? (
                      <span className="inline-flex h-5 items-center rounded-[5px] bg-state-selected px-[7px] text-micro font-semibold text-brand-ink">
                        {copy.enterprise}
                      </span>
                    ) : null}
                  </b>
                  <small className="block text-caption tracking-normal leading-[1.45] text-meta-foreground">
                    {item.desc}
                  </small>
                </span>
                {item.configured === false ? (
                  <span
                    data-f="not-configured"
                    className="inline-flex h-8 items-center rounded-[7px] bg-state-hover-strong px-3 text-compact leading-[1.45] font-semibold whitespace-nowrap text-meta-foreground"
                  >
                    {copy.notConfigured}
                  </span>
                ) : (
                  <button
                    type="button"
                    aria-label={formatCopy(
                      item.enterprise ? copy.viewAria : copy.connectAria,
                      { name: item.name },
                    )}
                    onClick={() => props.onConnect(item.key)}
                    className={BILLING_BTN_SM}
                  >
                    {item.enterprise ? copy.view : copy.connect}
                  </button>
                )}
              </div>
            ))}
          </div>
          <p className="m-0 -mt-1 mx-0.5 text-caption tracking-normal leading-[1.5] text-meta-foreground">
            {copy.foot}
          </p>
        </>
      ) : null}
    </section>
  );
}

// ── Detail ──────────────────────────────────────────────────────────────

export interface IntegrationDetailProps extends Shared {
  readonly state: SectionState<IntegrationDetailDto>;
  readonly busy: boolean;
  readonly onReload: () => void;
  readonly onReauthorize: () => void;
  readonly onDisconnect: () => void;
  readonly onTest: () => void;
  readonly onPause: (paused: boolean) => void;
  readonly onRegenerate: () => void;
}

const LIST_T =
  "flex items-center gap-2 border-b border-line px-3.5 py-3 text-compact leading-[1.45] font-semibold";
function PermList({
  items,
  no,
}: {
  items: readonly { t: string; sub?: string }[];
  no?: boolean;
}) {
  return (
    <ul className="m-0 flex list-none flex-col gap-2 p-0 text-compact leading-[1.45]">
      {items.map((item) => (
        <li key={item.t} className="flex items-start gap-2">
          <BillingGlyph
            name={no ? "x" : "check"}
            className={cn(
              "mt-0.5 size-[15px]",
              no ? "text-meta-foreground" : "text-success",
            )}
          />
          <span>
            {item.t}
            {item.sub ? <small className={SMALL}>{item.sub}</small> : null}
          </span>
        </li>
      ))}
    </ul>
  );
}

export function IntegrationDetailSurface(props: IntegrationDetailProps) {
  const { copy, links, state } = props;
  const back = (
    <BackLink href={links.list.href} onNavigate={links.list.onNavigate}>
      {copy.back}
    </BackLink>
  );
  if (state.status !== "ready") {
    return (
      <div className="flex flex-col gap-4">
        {back}
        {state.status === "loading" ? (
          <p role="status" className="m-0 text-compact text-meta-foreground">
            {copy.loading}
          </p>
        ) : (
          <div role="alert" className={cn(BOX, "max-w-[520px]")}>
            <p className={BOX_TEXT}>{copy.error}</p>
            <button
              type="button"
              onClick={props.onReload}
              className={cn(BILLING_BTN, "self-start")}
            >
              {copy.retry}
            </button>
          </div>
        )}
      </div>
    );
  }
  const d = state.data;
  const provider = d.name.split(" ")[0] ?? d.name;
  const C = d.callout;
  return (
    <div className="flex flex-col gap-4" data-f="integration-detail">
      {back}
      <div className="flex items-center gap-3.5">
        <Pv mono={d.mono} large />
        <div>
          <h2 className="m-0 text-sheet-title leading-[1.45] font-[650] tracking-[-0.01em] text-foreground">
            {d.name}
          </h2>
          <small className="mt-0.5 block text-crumb leading-[1.45] text-meta-foreground">
            {d.subtitle}
          </small>
        </div>
        <div className="ml-auto flex items-center gap-2">
          {d.actions.reauthorize ? (
            <button
              type="button"
              onClick={props.onReauthorize}
              className={BILLING_BTN}
            >
              {copy.reauthorize}
            </button>
          ) : null}
          {d.actions.test ? (
            <button
              type="button"
              onClick={props.onTest}
              className={BILLING_BTN}
            >
              {copy.test}
            </button>
          ) : null}
          {d.actions.pause ? (
            <button
              type="button"
              onClick={() => props.onPause(!d.actions.paused)}
              className={BILLING_BTN}
            >
              {d.actions.paused ? copy.resume : copy.pause}
            </button>
          ) : null}
          {d.actions.disconnect ? (
            <button
              type="button"
              aria-haspopup="dialog"
              onClick={props.onDisconnect}
              className={cn(
                BILLING_BTN,
                "border-transparent bg-transparent text-danger-text",
              )}
            >
              {copy.disconnect}
            </button>
          ) : null}
        </div>
      </div>
      <div
        role="group"
        aria-label={formatCopy(copy.healthLabel, { name: d.name })}
        data-f="health"
        className={cn(
          CARD,
          "flex flex-wrap items-center gap-6 px-4 py-3 text-crumb leading-[1.45]",
        )}
      >
        {[
          [copy.conn, d.health.conn],
          [copy.sync, d.health.sync],
        ].map(([label, status], index) => (
          <React.Fragment key={String(label)}>
            {index === 1 ? (
              <span aria-hidden="true" className="h-5 w-px bg-line" />
            ) : null}
            <span>
              <span className="mr-2 text-meta-foreground">
                {label as string}
              </span>
              <Ind status={status as IntegrationStatus} />
              {(status as IntegrationStatus).note ? (
                <span className="ml-1.5 text-caption tracking-normal text-meta-foreground">
                  {(status as IntegrationStatus).note}
                </span>
              ) : null}
            </span>
          </React.Fragment>
        ))}
        <span className="ml-auto flex items-center gap-2 text-caption tracking-normal text-meta-foreground">
          {formatCopy(copy.updated, { time: d.updatedAt }).replace(/ · $/, "")}
          <button type="button" onClick={props.onReload} className={LNKB}>
            {copy.reload}
          </button>
        </span>
      </div>
      <div className="grid grid-cols-[minmax(0,1fr)_300px] items-start gap-5">
        <div className="flex min-w-0 flex-col gap-4">
          {C ? (
            <div
              role="status"
              data-f="callw"
              className="flex items-start gap-3 rounded-[12px] border border-warning-text/40 bg-warning-subtle px-4 py-3.5"
            >
              <BillingGlyph
                name="warn"
                className="mt-px size-[18px] text-warning-text"
              />
              <div>
                <b className="block text-body leading-[1.45] font-semibold text-foreground">
                  {C.title}
                </b>
                <p className="mt-1 mb-0 text-crumb leading-[1.55] text-muted-foreground">
                  {C.body}
                </p>
                {C.cta ? (
                  <div className="mt-3 flex flex-wrap items-center gap-2.5">
                    <button
                      type="button"
                      onClick={props.onReauthorize}
                      className={BILLING_BTN_PRIMARY}
                    >
                      {C.cta}
                      <BillingGlyph name="ext" />
                    </button>
                  </div>
                ) : null}
                {C.hint ? (
                  <p className="mt-2 mb-0 max-w-[62ch] text-caption tracking-normal leading-[1.5] text-meta-foreground">
                    {C.hint}
                  </p>
                ) : null}
              </div>
            </div>
          ) : null}
          {d.pending ? (
            <section data-f="list" aria-labelledby="pd-t" className={CARD}>
              <div className={LIST_T}>
                <span id="pd-t">
                  {copy.pendingTitle}{" "}
                  <span className="font-normal text-meta-foreground">1</span>
                </span>
              </div>
              <div className="flex items-center gap-3 px-3.5 py-3 text-compact">
                <Pv mono="⚡" className="size-[34px]" />
                <span className="min-w-0 flex-1">
                  <b className="block text-nav leading-[1.45] font-semibold">
                    {d.pending.title}
                  </b>
                  <small className={SMALL}>{d.pending.sub}</small>
                </span>
                {links.automation ? (
                  <NavAnchor
                    link={links.automation(d.pending.boardId)}
                    className={BILLING_BTN_SM}
                  >
                    {copy.openRun}
                  </NavAnchor>
                ) : null}
              </div>
              <div className="flex flex-col gap-1.5 rounded-b-[12px] border-t border-line bg-background px-3.5 pt-3 pb-3.5 text-caption tracking-normal leading-[1.5] text-meta-foreground">
                {d.pending.note}
              </div>
            </section>
          ) : null}
          {d.overview.length > 0 ? (
            <section data-f="list" aria-labelledby="ov-t" className={CARD}>
              <div className={LIST_T}>
                <span id="ov-t">{copy.overview}</span>
              </div>
              {d.overview.map((row) => (
                <div
                  key={row.k}
                  className="grid grid-cols-[170px_minmax(0,1fr)] items-baseline gap-3 border-t border-line px-3.5 py-[11px] text-compact leading-[1.45] first-of-type:border-t-0"
                >
                  <span className="text-meta-foreground">{row.k}</span>
                  <span>
                    {row.v}
                    {row.sub ? (
                      <small className={cn(SMALL, "mt-0.5")}>{row.sub}</small>
                    ) : null}
                  </span>
                </div>
              ))}
            </section>
          ) : null}
          {d.deliveries ? (
            <section data-f="list" aria-labelledby="dl-t" className={CARD}>
              <div className={LIST_T}>
                <span id="dl-t">{copy.deliveries}</span>
                <span className="grow" />
                <span className="text-caption tracking-normal font-normal text-meta-foreground">
                  {formatCopy(copy.retryAt, { time: d.deliveries.retryAt })}
                </span>
              </div>
              <div role="table" aria-labelledby="dl-t">
                <div
                  role="row"
                  className={cn(
                    DL,
                    "h-[34px] border-b border-line bg-background text-micro font-semibold text-meta-foreground",
                  )}
                >
                  {[
                    copy.dl.time,
                    copy.dl.event,
                    copy.dl.item,
                    copy.dl.result,
                    copy.dl.next,
                  ].map((h) => (
                    <span key={h} role="columnheader">
                      {h}
                    </span>
                  ))}
                </div>
                <div
                  role="rowgroup"
                  aria-label={formatCopy(copy.waiting, {
                    count: d.deliveries.waiting.length,
                  })}
                >
                  <div
                    aria-hidden="true"
                    className="flex items-center gap-2 border-t border-line bg-background px-3.5 py-2 text-caption tracking-normal leading-[1.45] font-semibold text-meta-foreground"
                  >
                    {formatCopy(copy.waiting, {
                      count: d.deliveries.waiting.length,
                    })}
                  </div>
                  {d.deliveries.waiting.map((r) => (
                    <div
                      key={r.ref}
                      role="row"
                      className={cn(
                        DL,
                        "min-h-[42px] border-t border-line text-compact leading-[1.45]",
                      )}
                    >
                      <span
                        role="cell"
                        className="text-crumb text-muted-foreground"
                      >
                        {r.t}
                      </span>
                      <span role="cell">{d.config?.event}</span>
                      <span
                        role="cell"
                        className="font-semibold text-brand-ink"
                      >
                        {r.ref}
                      </span>
                      <span role="cell">
                        <Rc tone="warn">{r.result}</Rc>
                      </span>
                      <span
                        role="cell"
                        className="text-crumb text-meta-foreground"
                      >
                        {r.next}
                      </span>
                    </div>
                  ))}
                </div>
                <div role="rowgroup" aria-label={copy.sent}>
                  <div
                    aria-hidden="true"
                    className="flex items-center gap-2 border-t border-line bg-background px-3.5 py-2 text-caption tracking-normal leading-[1.45] font-semibold text-meta-foreground"
                  >
                    {copy.sent}
                  </div>
                  {d.deliveries.sent.map((r) => (
                    <div
                      key={r.ref}
                      role="row"
                      className={cn(
                        DL,
                        "min-h-[42px] border-t border-line text-compact leading-[1.45]",
                      )}
                    >
                      <span
                        role="cell"
                        className="text-crumb text-muted-foreground"
                      >
                        {r.t}
                      </span>
                      <span role="cell">{d.config?.event}</span>
                      <span
                        role="cell"
                        className="font-semibold text-brand-ink"
                      >
                        {r.ref}
                      </span>
                      <span role="cell">
                        <Rc tone="ok">{r.result}</Rc>
                      </span>
                      <span
                        role="cell"
                        className="text-crumb text-meta-foreground"
                      >
                        {copy.done}
                      </span>
                    </div>
                  ))}
                </div>
              </div>
            </section>
          ) : null}
          {d.activity.length > 0 ? (
            <section data-f="list" aria-labelledby="act-t" className={CARD}>
              <div className={LIST_T}>
                <span id="act-t">{copy.activity}</span>
              </div>
              {d.activity.map((a) => (
                <div
                  key={a.t + a.text}
                  className="grid grid-cols-[118px_minmax(0,1fr)] gap-3 border-t border-line px-3.5 py-[11px] text-compact leading-[1.45] first-of-type:border-t-0"
                >
                  <span className="text-crumb text-meta-foreground">{a.t}</span>
                  <span>
                    {a.text}
                    {a.ref ? (
                      <>
                        {" · "}
                        <span className="font-semibold text-brand-ink">
                          {a.ref}
                        </span>
                      </>
                    ) : null}
                  </span>
                </div>
              ))}
            </section>
          ) : null}
        </div>
        <div className="flex flex-col gap-3.5">
          {d.who ? (
            <section data-f="box" aria-labelledby="who-t" className={BOX}>
              <h3 id="who-t" className={BOX_TITLE}>
                {copy.whoTitle}
              </h3>
              <p className={BOX_TEXT}>
                <Rich text={d.who} />
              </p>
            </section>
          ) : null}
          {d.usedIn.length > 0 ? (
            <section data-f="box" aria-labelledby="use-t" className={BOX}>
              <h3 id="use-t" className={BOX_TITLE}>
                {copy.usedIn}
              </h3>
              {d.usedIn.map((u, i) => (
                <div
                  key={u.title}
                  className="-mx-2 flex items-center gap-2.5 rounded-[8px] p-2"
                >
                  <Pv
                    mono={i === 0 ? "⚡" : "#"}
                    className="size-7 rounded-[8px]"
                  />
                  <span>
                    <b className="block text-nav leading-[1.45] font-semibold">
                      {u.title}
                    </b>
                    <small className={SMALL}>{u.sub}</small>
                  </span>
                  <BillingGlyph
                    name="go"
                    className="ml-auto text-meta-foreground"
                  />
                </div>
              ))}
            </section>
          ) : null}
          {d.config ? (
            <section data-f="box" aria-labelledby="cf-t" className={BOX}>
              <h3 id="cf-t" className={BOX_TITLE}>
                {copy.configTitle}
              </h3>
              {[
                [
                  copy.url,
                  <span
                    key="u"
                    className={cn(MONO, "text-crumb [overflow-wrap:anywhere]")}
                  >
                    {d.config.url}
                  </span>,
                ],
                [
                  copy.event,
                  <span key="e" className="text-compact">
                    {d.config.event}
                  </span>,
                ],
                [
                  copy.scope,
                  <span
                    key="s"
                    className="inline-flex min-h-[22px] items-center rounded-[5px] bg-state-hover-strong px-[7px] text-caption tracking-normal text-muted-foreground"
                  >
                    {d.config.scope}
                  </span>,
                ],
              ].map(([label, node]) => (
                <div key={String(label)} className="flex flex-col gap-1.5">
                  <span className="text-crumb leading-[1.45] font-semibold text-muted-foreground">
                    {label}
                  </span>
                  {node}
                </div>
              ))}
              <div className="flex flex-col gap-1.5">
                <span className="text-crumb leading-[1.45] font-semibold text-muted-foreground">
                  {copy.secret}
                </span>
                <span className="flex flex-wrap items-center gap-2">
                  <BillingGlyph
                    name="lock"
                    className="size-3.5 text-meta-foreground"
                  />
                  <span className="text-compact">{d.config.secret}</span>
                  <button
                    type="button"
                    aria-haspopup="dialog"
                    onClick={props.onRegenerate}
                    className={BILLING_BTN_SM}
                  >
                    {copy.regenerate}
                  </button>
                </span>
                <span className="text-caption tracking-normal leading-[1.5] text-meta-foreground">
                  {copy.secretNote}
                </span>
              </div>
            </section>
          ) : null}
          {d.dedupe.length > 0 ? (
            <section data-f="box" aria-labelledby="dd-t" className={BOX}>
              <h3 id="dd-t" className={BOX_TITLE}>
                {copy.dedupe}
              </h3>
              {d.dedupe.map((line) => (
                <p key={line} className={BOX_TEXT}>
                  {line}
                </p>
              ))}
            </section>
          ) : null}
          {d.perms.length > 0 ? (
            <section data-f="box" aria-labelledby="pm-t" className={BOX}>
              <h3 id="pm-t" className={BOX_TITLE}>
                {formatCopy(copy.granted, { provider })}
              </h3>
              <PermList items={d.perms} />
              {d.permNote ? <p className={BOX_TEXT}>{d.permNote}</p> : null}
            </section>
          ) : null}
          {d.nots.length > 0 ? (
            <section data-f="box" aria-labelledby="no-t" className={BOX}>
              <h3 id="no-t" className={BOX_TITLE}>
                {copy.cannot}
              </h3>
              <PermList items={d.nots} no />
              {d.notNote ? <p className={BOX_TEXT}>{d.notNote}</p> : null}
            </section>
          ) : null}
        </div>
      </div>
    </div>
  );
}

const DL =
  "grid grid-cols-[100px_minmax(0,1fr)_72px_120px_84px] items-center gap-3 px-3.5";
function Rc({
  tone,
  children,
}: {
  tone: "ok" | "warn";
  children: React.ReactNode;
}) {
  return (
    <span
      className={cn(
        "inline-flex h-[22px] items-center justify-self-start rounded-[5px] px-2 text-caption tracking-normal font-semibold whitespace-nowrap",
        tone === "ok"
          ? "bg-success-subtle text-success-text"
          : "bg-warning-subtle text-warning-text",
      )}
    >
      {children}
    </span>
  );
}

// ── Dialogs ─────────────────────────────────────────────────────────────

function DialogHead({
  id,
  title,
  mono,
  closeLabel,
  onClose,
}: {
  id: string;
  title: string;
  mono?: string;
  closeLabel: string;
  onClose: () => void;
}) {
  return (
    <div className="flex items-center gap-2.5 px-5 pt-[18px] pb-1.5">
      {mono ? <Pv mono={mono} /> : null}
      <DialogTitle
        id={id}
        className="m-0 text-dialog-title leading-[1.45] font-[650] text-foreground"
      >
        {title}
      </DialogTitle>
      <button
        type="button"
        aria-label={closeLabel}
        onClick={onClose}
        className={cn(
          "ml-auto grid size-8 shrink-0 cursor-pointer place-items-center rounded-[7px] border-0 bg-transparent text-meta-foreground hover:bg-state-hover-strong",
          FOCUS,
        )}
      >
        <BillingGlyph name="x" />
      </button>
    </div>
  );
}
const FOOT =
  "flex items-center gap-2 border-t border-line bg-background px-5 py-3.5";
const H3 =
  "m-0 mb-2 text-crumb leading-[1.45] font-semibold text-muted-foreground";

export interface IntegrationConnectDialogProps extends Shared {
  readonly open: boolean;
  readonly item: IntegrationCatalogDto | null;
  readonly workspaceName: string;
  readonly scope: "workspace" | "board";
  readonly submitting: boolean;
  readonly onScope: (scope: "workspace" | "board") => void;
  readonly onNext: () => void;
  readonly onClose: () => void;
  /** Workspace-wide only: leaves out the scope choice and the step markers. */
  readonly hideScope?: boolean;
  /** An address to type (an outgoing webhook); the primary button then connects. */
  readonly url?: {
    readonly value: string;
    readonly error: string | null;
    readonly onChange: (value: string) => void;
  };
}

export function IntegrationConnectDialog(props: IntegrationConnectDialogProps) {
  const { copy, item } = props;
  const c = copy.connectDialog;
  const provider = item?.provider ?? "";
  return (
    <Dialog
      open={props.open && item !== null}
      onOpenChange={(next) => (next ? undefined : props.onClose())}
    >
      <DialogContent
        showCloseButton={false}
        overlayClassName={DIALOG_OVERLAY}
        aria-labelledby="gd-t"
        className={cn(DIALOG_PANEL, "top-[110px] w-[600px]")}
      >
        {item ? (
          <>
            <DialogHead
              id="gd-t"
              mono={item.mono}
              title={formatCopy(c.title, { name: item.name })}
              closeLabel={c.close}
              onClose={props.onClose}
            />
            <DialogDescription className="sr-only">
              {item.desc}
            </DialogDescription>
            {props.hideScope ? null : (
              <ol
                aria-label={c.stepsLabel}
                className="m-0 mt-0.5 flex list-none gap-2.5 p-0 px-5"
              >
                {[c.step1, c.step2, c.step3].map((label, index) => (
                  <li
                    key={index}
                    aria-current={index === 1 ? "step" : undefined}
                    className={cn(
                      "flex flex-1 flex-col gap-[7px] text-caption tracking-normal leading-[1.45]",
                      index === 1
                        ? "font-semibold text-foreground"
                        : index === 0
                          ? "text-muted-foreground"
                          : "text-meta-foreground",
                    )}
                  >
                    <span
                      className={cn(
                        "h-[3px] rounded-[2px]",
                        index === 0
                          ? "bg-success"
                          : index === 1
                            ? "bg-primary"
                            : "bg-line-strong",
                      )}
                    />
                    {formatCopy(label, { provider })}
                  </li>
                ))}
              </ol>
            )}
            <div className="flex flex-col gap-3.5 px-5 pt-3.5 pb-4">
              {props.url ? (
                <div className="flex flex-col gap-1.5">
                  <label
                    htmlFor="int-url"
                    className="text-crumb leading-[1.45] font-semibold text-muted-foreground"
                  >
                    {c.urlLabel}
                  </label>
                  <input
                    id="int-url"
                    type="url"
                    inputMode="url"
                    autoComplete="off"
                    spellCheck={false}
                    value={props.url.value}
                    placeholder="https://example.com/notrelix"
                    aria-invalid={props.url.error ? true : undefined}
                    aria-describedby="int-url-hint"
                    onChange={(event) =>
                      props.url?.onChange(event.target.value)
                    }
                    className={cn(
                      "h-9 rounded-[7px] border border-line-strong bg-background px-3 text-nav outline-none focus-visible:ring-2 focus-visible:ring-focus-ring",
                      props.url.error && "border-destructive",
                    )}
                  />
                  <span
                    id="int-url-hint"
                    role={props.url.error ? "alert" : undefined}
                    className={cn(
                      "text-caption tracking-normal leading-[1.5]",
                      props.url.error
                        ? "text-danger-text"
                        : "text-meta-foreground",
                    )}
                  >
                    {props.url.error ?? c.urlHint}
                  </span>
                </div>
              ) : null}
              {props.hideScope ? null : (
                <fieldset className="m-0 flex flex-col gap-2 border-0 p-0">
                  <legend className="mb-1.5 p-0 text-crumb leading-[1.45] font-semibold text-muted-foreground">
                    {c.legend}
                  </legend>
                  {(["workspace", "board"] as const).map((scope) => (
                    <label
                      key={scope}
                      className={cn(
                        "flex cursor-pointer items-start gap-2.5 rounded-[9px] border px-3 py-2.5 text-nav leading-[1.45]",
                        props.scope === scope
                          ? "border-primary bg-state-selected"
                          : "border-line-strong",
                      )}
                    >
                      <input
                        type="radio"
                        name="int-scope"
                        checked={props.scope === scope}
                        onChange={() => props.onScope(scope)}
                        className="mt-[3px] shrink-0 accent-primary"
                      />
                      <span>
                        <b className="block font-semibold">
                          {scope === "workspace"
                            ? formatCopy(c.workspace, {
                                workspace: props.workspaceName,
                              })
                            : c.board}
                        </b>
                        <small className="block text-caption tracking-normal leading-[1.45] text-meta-foreground">
                          {scope === "workspace"
                            ? c.workspaceHint
                            : c.boardHint}
                        </small>
                      </span>
                    </label>
                  ))}
                </fieldset>
              )}
              <div className="grid grid-cols-2 gap-3.5">
                <div>
                  <h3 className={H3}>{c.will}</h3>
                  <PermList items={item.will.map((t) => ({ t }))} />
                </div>
                <div>
                  <h3 className={H3}>{c.wont}</h3>
                  <PermList items={item.wont.map((t) => ({ t }))} no />
                </div>
              </div>
              <InfoNote>
                <Rich text={formatCopy(c.info, { provider })} />
              </InfoNote>
            </div>
            <div className={FOOT}>
              {props.hideScope ? null : (
                <small className="text-caption tracking-normal text-meta-foreground">
                  {c.step}
                </small>
              )}
              <span className="grow" />
              <button
                type="button"
                onClick={props.onClose}
                className={BILLING_BTN}
              >
                {c.back}
              </button>
              <button
                type="button"
                disabled={
                  props.submitting ||
                  (props.url !== undefined && props.url.value.trim() === "")
                }
                onClick={props.onNext}
                className={BILLING_BTN_PRIMARY}
              >
                {props.url
                  ? c.connectWebhook
                  : formatCopy(c.next, { provider })}
                {props.url ? null : <BillingGlyph name="ext" />}
              </button>
            </div>
          </>
        ) : null}
      </DialogContent>
    </Dialog>
  );
}

export interface IntegrationSecretDialogProps extends Shared {
  readonly open: boolean;
  /** The signing secret, known only now: it is not shown again. */
  readonly secret: string | null;
  readonly onClose: () => void;
}

/** Shows a new webhook's signing secret once. */
export function IntegrationSecretDialog(props: IntegrationSecretDialogProps) {
  const c = props.copy.secretDialog;
  const [copied, setCopied] = React.useState(false);
  const copy = () => {
    if (!props.secret) return;
    void navigator.clipboard
      ?.writeText(props.secret)
      .then(() => setCopied(true))
      .catch(() => undefined);
  };
  return (
    <Dialog
      open={props.open && props.secret !== null}
      onOpenChange={(next) => (next ? undefined : props.onClose())}
    >
      <DialogContent
        showCloseButton={false}
        overlayClassName={DIALOG_OVERLAY}
        aria-labelledby="sd-t"
        className={cn(DIALOG_PANEL, "top-[110px] w-[600px]")}
      >
        <DialogHead
          id="sd-t"
          title={c.title}
          closeLabel={props.copy.connectDialog.close}
          onClose={props.onClose}
        />
        <div className="flex flex-col gap-3.5 px-5 pt-3.5 pb-4">
          <DialogDescription className="m-0 text-nav leading-[1.55] text-foreground">
            {c.body}
          </DialogDescription>
          <div className="flex items-center gap-2 rounded-[9px] border border-line-strong bg-background px-3 py-2.5">
            <BillingGlyph
              name="lock"
              className="size-3.5 text-meta-foreground"
            />
            <code
              data-f="signing-secret"
              className={cn("min-w-0 flex-1 break-all text-compact", MONO)}
            >
              {props.secret}
            </code>
            <button type="button" onClick={copy} className={BILLING_BTN_SM}>
              {copied ? c.copied : c.copy}
            </button>
          </div>
          <InfoNote>{c.note}</InfoNote>
        </div>
        <div className={FOOT}>
          <span className="grow" />
          <button
            type="button"
            onClick={props.onClose}
            className={BILLING_BTN_PRIMARY}
          >
            {c.done}
          </button>
        </div>
      </DialogContent>
    </Dialog>
  );
}

export interface IntegrationDisconnectDialogProps extends Shared {
  readonly open: boolean;
  readonly detail: IntegrationDetailDto | null;
  readonly submitting: boolean;
  readonly onConfirm: () => void;
  readonly onClose: () => void;
}

export function IntegrationDisconnectDialog(
  props: IntegrationDisconnectDialogProps,
) {
  const { copy, detail } = props;
  const c = copy.disconnectDialog;
  const x = detail?.disconnect ?? null;
  return (
    <Dialog
      open={props.open && x !== null}
      onOpenChange={(next) => (next ? undefined : props.onClose())}
    >
      <DialogContent
        showCloseButton={false}
        role="alertdialog"
        overlayClassName={DIALOG_OVERLAY}
        aria-labelledby="dc-t"
        aria-describedby="dc-d"
        className={cn(DIALOG_PANEL, "top-[110px] w-[600px]")}
      >
        {x && detail ? (
          <>
            <DialogHead
              id="dc-t"
              title={formatCopy(c.title, { name: detail.name })}
              closeLabel={copy.connectDialog.close}
              onClose={props.onClose}
            />
            <div className="flex flex-col gap-3.5 px-5 pt-3.5 pb-4">
              <DialogDescription
                id="dc-d"
                className="m-0 text-nav leading-[1.55] text-foreground"
              >
                {x.intro}
              </DialogDescription>
              <div className="grid grid-cols-2 gap-3.5">
                <div>
                  <h3 className={H3}>{c.stops}</h3>
                  <PermList items={x.stops.map((t) => ({ t }))} no />
                </div>
                <div>
                  <h3 className={H3}>{c.keeps}</h3>
                  <PermList items={x.keeps.map((t) => ({ t }))} />
                </div>
              </div>
              <InfoNote>{x.note}</InfoNote>
            </div>
            <div className={FOOT}>
              <small className="text-caption tracking-normal text-meta-foreground">
                {x.foot}
              </small>
              <span className="grow" />
              <button
                type="button"
                onClick={props.onClose}
                className={BILLING_BTN}
              >
                {c.cancel}
              </button>
              <button
                type="button"
                disabled={props.submitting}
                onClick={props.onConfirm}
                className="inline-flex h-8 items-center gap-1.5 rounded-[7px] border border-destructive bg-destructive px-3 text-compact leading-[1.45] font-semibold whitespace-nowrap text-white outline-none focus-visible:ring-2 focus-visible:ring-focus-ring disabled:opacity-60"
              >
                {c.confirm}
              </button>
            </div>
          </>
        ) : null}
      </DialogContent>
    </Dialog>
  );
}

export interface IntegrationUpgradeSheetProps extends Shared {
  readonly open: boolean;
  readonly item: IntegrationCatalogDto | null;
  readonly plan: IntegrationsListDto["plan"];
  readonly onContact: () => void;
  readonly onClose: () => void;
}

export function IntegrationUpgradeSheet(props: IntegrationUpgradeSheetProps) {
  const { copy, item, plan } = props;
  const c = copy.sheet;
  const planName = copy.plans[plan.name];
  return (
    <Dialog
      open={props.open && item !== null && item.gate !== null}
      onOpenChange={(next) => (next ? undefined : props.onClose())}
    >
      <DialogContent
        showCloseButton={false}
        overlayClassName={DIALOG_OVERLAY}
        aria-labelledby="pb-t"
        className="fixed top-0 right-0 left-auto flex h-full w-[460px] max-w-full translate-x-0 translate-y-0 flex-col gap-0 rounded-none border-0 border-l border-line bg-popover p-0 shadow-dialog sm:max-w-[460px] dark:bg-card"
      >
        {item?.gate ? (
          <>
            <div className="flex items-center gap-3 border-b border-line px-5 pt-5 pb-3.5">
              <Pv mono={item.mono} large />
              <div>
                <DialogTitle
                  id="pb-t"
                  className="m-0 text-section-heading leading-[1.45] font-[650] text-foreground"
                >
                  {item.name}
                </DialogTitle>
                <DialogDescription className="mt-0.5 block text-crumb leading-[1.45] text-meta-foreground">
                  {item.desc}
                </DialogDescription>
              </div>
              <button
                type="button"
                aria-label={c.close}
                onClick={props.onClose}
                className={cn(
                  "ml-auto grid size-8 cursor-pointer place-items-center self-start rounded-[7px] border-0 bg-transparent text-meta-foreground hover:bg-state-hover-strong",
                  FOCUS,
                )}
              >
                <BillingGlyph name="x" />
              </button>
            </div>
            <div className="flex flex-1 flex-col gap-4 overflow-auto px-5 py-[18px] text-nav leading-[1.55]">
              <div className="rounded-[10px] border border-line bg-background p-3.5">
                <b className="block text-body leading-[1.45] font-semibold">
                  {formatCopy(c.gate, { plan: planName })}
                </b>
                <p className="mt-1 mb-0 text-crumb leading-[1.55] text-muted-foreground">
                  {formatCopy(c.gateDetail, {
                    account: plan.account,
                    plan: planName,
                    count: plan.workspaces.length,
                    names: plan.workspaces.join(", "),
                  })}
                </p>
              </div>
              <div>
                <h3 className={H3}>{c.can}</h3>
                <PermList items={item.gate.can.map((t) => ({ t }))} />
              </div>
              <div>
                <h3 className={H3}>{c.also}</h3>
                <PermList items={item.gate.alsoHas.map((t) => ({ t }))} />
              </div>
              <InfoNote>
                {formatCopy(c.info, { account: plan.account })}
              </InfoNote>
            </div>
            <div className="flex items-center gap-2 border-t border-line bg-background px-5 py-3.5">
              <span className="grow" />
              <button
                type="button"
                onClick={props.onContact}
                className={BILLING_BTN}
              >
                {c.contact}
              </button>
              <NavAnchor
                link={props.links.billing}
                className={BILLING_BTN_PRIMARY}
              >
                {c.plans}
              </NavAnchor>
            </div>
          </>
        ) : null}
      </DialogContent>
    </Dialog>
  );
}

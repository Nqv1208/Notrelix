import { cn } from "@notrelix/ui-web";
import type { FeatureAvailabilityCopy } from "../unavailable/feature-availability.copy";
import { formatCopy } from "../shell/shell-models";
import {
  BILLING_BTN,
  BOX,
  BOX_TEXT,
  BOX_TITLE,
  CARD,
  InfoNote,
  Meter,
  PageHead,
} from "./billing-parts";

/** One capability the workspace's billing grants, as the backend reports it. */
export interface BillingCapabilityDto {
  readonly code: string;
  readonly isGranted: boolean;
  readonly isUnlimited: boolean;
  /** `null` when unlimited or not granted. */
  readonly limit: number | null;
  readonly used: number;
  /** `null` when unlimited. */
  readonly remaining: number | null;
}

export type BillingCapabilitiesState =
  | { readonly status: "loading" }
  | { readonly status: "error" }
  | { readonly status: "forbidden" }
  | {
      readonly status: "ready";
      readonly data: readonly BillingCapabilityDto[];
    };

/** The plan the account is on and the catalog it can switch within. */
export interface BillingPlanCardState {
  readonly currentCode: string;
  readonly plans: readonly { readonly code: string; readonly name: string }[];
  /** True for the account owner in an environment that allows switching. */
  readonly canChange: boolean;
  readonly changing: boolean;
  readonly failed: boolean;
}

export interface BillingCapabilitiesSurfaceProps {
  readonly copy: FeatureAvailabilityCopy;
  readonly state: BillingCapabilitiesState;
  readonly onRetry: () => void;
  /** The plan card; omitted while the plan is not known. */
  readonly plan?: BillingPlanCardState;
  readonly onChangePlan?: (code: string) => void;
}

const STORAGE_CODE = "STORAGE_GB";

function Row({
  copy,
  item,
}: {
  copy: FeatureAvailabilityCopy["billing"];
  item: BillingCapabilityDto;
}) {
  const name = copy.capabilities[item.code] ?? item.code;
  const limit = item.limit;
  const full = item.isGranted && limit !== null && item.used >= limit;
  const isStorage = item.code === STORAGE_CODE;
  const detail = !item.isGranted
    ? copy.notGranted
    : item.isUnlimited || limit === null
      ? copy.unlimited
      : formatCopy(isStorage ? copy.usedGb : copy.used, {
          used: item.used,
          limit,
        });
  return (
    <li className={cn(CARD, "flex flex-col gap-2 p-3.5")}>
      <div className="flex items-baseline gap-3">
        <span className="text-compact leading-[1.45] font-semibold text-foreground">
          {name}
        </span>
        <span className="grow" />
        <span className="text-crumb text-meta-foreground">{detail}</span>
      </div>
      {item.isGranted && limit !== null ? (
        <Meter
          max={limit}
          now={Math.min(item.used, limit)}
          label={name}
          segments={[
            {
              share: limit === 0 ? 0 : Math.min(100, (item.used / limit) * 100),
              tone: "one",
            },
          ]}
        />
      ) : null}
      {item.isGranted && item.isUnlimited ? (
        <span className="text-crumb text-meta-foreground">
          {formatCopy(isStorage ? copy.usedUnlimitedGb : copy.usedUnlimited, {
            used: item.used,
          })}
        </span>
      ) : null}
      {full ? (
        <span className="text-crumb font-semibold text-foreground">
          {copy.full}
        </span>
      ) : item.isGranted && item.remaining !== null ? (
        <span className="text-crumb text-meta-foreground">
          {formatCopy(isStorage ? copy.remainingGb : copy.remaining, {
            count: item.remaining,
          })}
        </span>
      ) : null}
    </li>
  );
}

function PlanCard({
  copy,
  plan,
  onChangePlan,
}: {
  copy: FeatureAvailabilityCopy["billing"];
  plan: BillingPlanCardState;
  onChangePlan?: (code: string) => void;
}) {
  const text = copy.plan;
  const current = plan.plans.find((p) => p.code === plan.currentCode);
  return (
    <div
      role="group"
      aria-label={text.title}
      className={cn(BOX, "max-w-[560px]")}
    >
      <h3 className={BOX_TITLE}>{text.current}</h3>
      <p className="m-0 text-compact leading-[1.45] font-semibold text-foreground">
        {current?.name ?? plan.currentCode}
      </p>
      <p className={BOX_TEXT}>{text.lede}</p>
      {plan.canChange ? (
        <>
          <h3 className={BOX_TITLE}>{text.switchTitle}</h3>
          <div className="flex flex-wrap gap-2">
            {plan.plans
              .filter((p) => p.code !== plan.currentCode)
              .map((p) => (
                <button
                  key={p.code}
                  type="button"
                  disabled={plan.changing}
                  onClick={() => onChangePlan?.(p.code)}
                  className={BILLING_BTN}
                >
                  {formatCopy(text.switchTo, { plan: p.name })}
                </button>
              ))}
          </div>
          <p className={BOX_TEXT}>
            {plan.changing ? text.changing : text.switchNote}
          </p>
        </>
      ) : null}
      {plan.failed ? (
        <p role="alert" className={BOX_TEXT}>
          {text.failed}
        </p>
      ) : null}
    </div>
  );
}

/**
 * What the workspace's billing grants, with each capability's limit and use.
 * Buying or changing a plan, invoices and payment methods are said to be not
 * available yet instead of being drawn from invented data.
 */
export function BillingCapabilitiesSurface({
  copy,
  state,
  onRetry,
  plan,
  onChangePlan,
}: BillingCapabilitiesSurfaceProps) {
  const text = copy.billing;
  let body: React.ReactNode;
  if (state.status === "loading") {
    body = (
      <p role="status" className="m-0 text-compact text-meta-foreground">
        {text.loading}
      </p>
    );
  } else if (state.status === "error") {
    body = (
      <div role="alert" className={cn(BOX, "max-w-[520px]")}>
        <p className={BOX_TEXT}>{text.error}</p>
        <button
          type="button"
          onClick={onRetry}
          className={cn(BILLING_BTN, "self-start")}
        >
          {text.retry}
        </button>
      </div>
    );
  } else if (state.status === "forbidden") {
    body = (
      <div role="status" className={cn(BOX, "max-w-[520px]")}>
        <h3 className={BOX_TITLE}>{text.forbiddenTitle}</h3>
        <p className={BOX_TEXT}>{text.forbiddenBody}</p>
      </div>
    );
  } else {
    body = (
      <ul
        aria-label={text.listLabel}
        className="m-0 flex max-w-[560px] list-none flex-col gap-2.5 p-0"
      >
        {state.data.map((item) => (
          <Row key={item.code} copy={text} item={item} />
        ))}
      </ul>
    );
  }
  return (
    <section
      aria-labelledby="billing-capabilities"
      className="flex flex-col gap-4"
    >
      <PageHead id="billing-capabilities" title={text.title} lede={text.lede} />
      {state.status === "ready" && plan ? (
        <PlanCard copy={text} plan={plan} onChangePlan={onChangePlan} />
      ) : null}
      {body}
      {state.status === "ready" ? (
        <InfoNote className="max-w-[560px]">
          <b className="font-semibold text-foreground">
            {text.unavailableTitle}.
          </b>{" "}
          {text.unavailableBody}
        </InfoNote>
      ) : null}
    </section>
  );
}

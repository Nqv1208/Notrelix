import type {
  OperationRequestBody,
  OperationResponse,
  operations,
} from "@notrelix/contracts";

/**
 * The operator API client. The operator token lives in module memory only (never storage, never a cookie):
 * a reload signs the operator out, and no other page or tab can read it. Every call is bearer-authenticated
 * against /operator/v1; the tenant client is not used here.
 */
const BASE = (import.meta.env.VITE_OPERATOR_API_URL ?? "/operator/v1").replace(
  /\/$/,
  "",
);

let token: string | null = null;
let onSignedOut: (() => void) | null = null;

export function setToken(value: string | null): void {
  token = value;
}
export function hasToken(): boolean {
  return token !== null;
}
export function onSessionEnded(handler: () => void): void {
  onSignedOut = handler;
}

export class ApiFailure extends Error {
  constructor(
    readonly status: number,
    message: string,
    readonly ruleCode?: string,
  ) {
    super(message);
  }
}

async function call<T>(
  method: string,
  path: string,
  body?: unknown,
): Promise<T> {
  const response = await fetch(`${BASE}${path}`, {
    method,
    headers: {
      Accept: "application/json",
      ...(body === undefined ? {} : { "Content-Type": "application/json" }),
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
    },
    body: body === undefined ? undefined : JSON.stringify(body),
  });
  if (response.status === 401 && token) {
    token = null;
    onSignedOut?.();
  }
  if (!response.ok) {
    let message = `Request failed (${response.status}).`;
    let ruleCode: string | undefined;
    try {
      const problem = (await response.json()) as {
        detail?: string;
        title?: string;
        ruleCode?: string;
      };
      message = problem.detail ?? problem.title ?? message;
      ruleCode = problem.ruleCode;
    } catch {
      /* a body that is not a problem document keeps the generic message */
    }
    throw new ApiFailure(response.status, message, ruleCode);
  }
  return (await response.json()) as T;
}

type Op = keyof operations;
/**
 * The generated schema marks every property optional and nullable (the producer does not publish required
 * members). The console reads answers it has just received, so it narrows them to always present; the few
 * members that can be absent (a missing subscription, no expiry) are handled where they are read.
 */
type Strict<T> = T extends readonly (infer U)[]
  ? Strict<U>[]
  : T extends object
    ? { [K in keyof T]-?: Strict<NonNullable<T[K]>> }
    : NonNullable<T>;
type Out<K extends Op> = Strict<NonNullable<OperationResponse<K>>>;
type In<K extends Op> = NonNullable<OperationRequestBody<K>>;

export type Session = Out<"Operator.Auth.SignIn">;
export type AccountPage = Out<"Operator.Account.List">;
export type AccountDto = AccountPage["items"][number];
export type AccountDetail = Out<"Operator.Account.Get">;
export type AccountBilling = Out<"Operator.Billing.Get">;
export type OverrideDto = AccountBilling["overrides"][number];
export type PlanDto = Out<"Operator.Billing.Plans.List">[number];
export type AuditEntry = Out<"Operator.Audit.List">[number];
export type AccountPlan = Out<"Operator.Billing.AccountPlans">[number];
export type NewRevision = In<"Operator.Billing.Plans.CreateRevision">;
export type NewOverride = In<"Operator.Billing.GrantOverride">;

const enc = encodeURIComponent;

export const operatorApi = {
  signIn: (email: string, password: string) =>
    call<Session>("POST", "/auth/sign-in", { email, password }),
  accounts: (search: string, skip: number, take: number) =>
    call<AccountPage>(
      "GET",
      `/accounts?search=${enc(search)}&skip=${skip}&take=${take}`,
    ),
  account: (id: string) => call<AccountDetail>("GET", `/accounts/${enc(id)}`),
  suspend: (id: string, reason: string, expectedVersion: number) =>
    call<AccountDto>("POST", `/accounts/${enc(id)}/suspend`, {
      reason,
      expectedVersion,
    }),
  liftSuspension: (id: string, reason: string, expectedVersion: number) =>
    call<AccountDto>("POST", `/accounts/${enc(id)}/lift-suspension`, {
      reason,
      expectedVersion,
    }),
  accountPlans: (accountIds: string[]) =>
    call<AccountPlan[]>("POST", "/billing/account-plans", { accountIds }),
  billing: (id: string) =>
    call<AccountBilling>("GET", `/accounts/${enc(id)}/billing`),
  changePlan: (
    id: string,
    planId: string,
    reason: string,
    expectedVersion: number | null,
  ) =>
    call<AccountBilling>("PUT", `/accounts/${enc(id)}/billing/plan`, {
      planId,
      reason,
      expectedVersion,
    }),
  terminate: (id: string, reason: string, expectedVersion: number) =>
    call<AccountBilling>("POST", `/accounts/${enc(id)}/billing/terminate`, {
      reason,
      expectedVersion,
    }),
  grantOverride: (id: string, input: NewOverride) =>
    call<AccountBilling>(
      "POST",
      `/accounts/${enc(id)}/billing/overrides`,
      input,
    ),
  revokeOverride: (
    entitlementId: string,
    reason: string,
    expectedVersion: number,
  ) =>
    call<AccountBilling>(
      "POST",
      `/billing/overrides/${enc(entitlementId)}/revoke`,
      { reason, expectedVersion },
    ),
  plans: () => call<PlanDto[]>("GET", "/billing/plans"),
  createRevision: (input: NewRevision) =>
    call<PlanDto>("POST", "/billing/plans", input),
  deprecatePlan: (id: string, reason: string, expectedVersion: number) =>
    call<PlanDto>("POST", `/billing/plans/${enc(id)}/deprecate`, {
      reason,
      expectedVersion,
    }),
  archivePlan: (id: string, reason: string, expectedVersion: number) =>
    call<PlanDto>("POST", `/billing/plans/${enc(id)}/archive`, {
      reason,
      expectedVersion,
    }),
  audit: (accountId: string | null, take: number) =>
    call<AuditEntry[]>(
      "GET",
      `/audit?take=${take}${accountId ? `&accountId=${enc(accountId)}` : ""}`,
    ),
};

export const FEATURE_CODES = [
  "AUTOMATION_RULE",
  "MEMBER_SEAT",
  "GUEST_SEAT",
  "STORAGE_GB",
  "CUSTOM_ROLES",
] as const;

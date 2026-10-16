import type { BillingLink } from "../billing/billing-models";

/** What the Integrations surfaces read (gap `CTR-GAP-INTEGRATIONS`). Free-text lines arrive composed; time labels are Hanoi time. */
export type IntegrationTone = "ok" | "warn" | "off";
export interface IntegrationStatus {
  readonly tone: IntegrationTone;
  readonly label: string;
  readonly note?: string;
}
export interface IntegrationRowDto {
  readonly key: string;
  readonly mono: string;
  readonly name: string;
  readonly sub: string;
  readonly scope: {
    readonly kind: "workspace" | "board";
    readonly name: string;
  };
  readonly conn: IntegrationStatus;
  readonly sync: IntegrationStatus;
  readonly last: string;
  readonly lastSub: string;
}
export interface IntegrationCatalogDto {
  readonly key: string;
  readonly mono: string;
  readonly name: string;
  readonly desc: string;
  readonly enterprise: boolean;
  readonly provider: string;
  readonly will: readonly string[];
  readonly wont: readonly string[];
  readonly gate: { can: readonly string[]; alsoHas: readonly string[] } | null;
}
export interface IntegrationsListDto {
  readonly serverTime: string;
  readonly updatedAt: string;
  readonly canManage: boolean;
  readonly workspaceName: string;
  readonly admins: readonly string[];
  readonly rows: readonly IntegrationRowDto[];
  readonly catalog: readonly IntegrationCatalogDto[];
  readonly plan: {
    name: "free" | "pro" | "business" | "enterprise";
    account: string;
    workspaces: readonly string[];
  };
}
export interface IntegrationDetailDto {
  readonly key: string;
  readonly mono: string;
  readonly name: string;
  readonly subtitle: string;
  readonly kind: "storage" | "chat" | "webhook";
  readonly updatedAt: string;
  readonly actions: {
    readonly reauthorize: boolean;
    readonly disconnect: boolean;
    readonly test: boolean;
    readonly pause: boolean;
    readonly paused: boolean;
  };
  readonly health: {
    readonly conn: IntegrationStatus;
    readonly sync: IntegrationStatus;
  };
  readonly overview: readonly { k: string; v: string; sub?: string }[];
  readonly activity: readonly { t: string; text: string; ref: string | null }[];
  readonly callout: {
    title: string;
    body: string;
    hint: string | null;
    cta: string | null;
  } | null;
  readonly pending: {
    title: string;
    sub: string;
    note: string;
    boardId: string;
  } | null;
  readonly who: string | null;
  readonly usedIn: readonly { title: string; sub: string }[];
  readonly perms: readonly { t: string; sub?: string }[];
  readonly nots: readonly { t: string }[];
  readonly permNote: string | null;
  readonly notNote: string | null;
  readonly deliveries: {
    readonly retryAt: string;
    readonly waiting: readonly {
      t: string;
      ref: string;
      result: string;
      next: string;
    }[];
    readonly sent: readonly { t: string; ref: string; result: string }[];
  } | null;
  readonly config: {
    url: string;
    event: string;
    scope: string;
    secret: string;
  } | null;
  readonly dedupe: readonly string[];
  readonly disconnect: {
    intro: string;
    stops: readonly string[];
    keeps: readonly string[];
    note: string;
    foot: string;
  } | null;
}
export interface IntegrationsLinks {
  readonly list: BillingLink;
  readonly detail: (key: string) => BillingLink;
  readonly billing: BillingLink;
  /** The automation settings of a board (where a held run is reopened). */
  readonly automation: ((boardId: string) => BillingLink) | null;
}

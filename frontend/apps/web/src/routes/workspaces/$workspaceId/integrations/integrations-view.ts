import type {
  IntegrationCatalogDto,
  IntegrationDetailDto,
  IntegrationRowDto,
  IntegrationStatus,
  IntegrationsCopy,
  IntegrationsListDto,
} from "@notrelix/features-workspace/web";
import { formatCopy } from "@notrelix/features-workspace/web";
import type {
  IntegrationConnection,
  IntegrationsOverview,
} from "./integrations-api";

/** The providers in the order the catalog lists them. */
const PROVIDERS = ["google-calendar", "slack", "webhook"] as const;
type ProviderKey = (typeof PROVIDERS)[number];

const isProvider = (value: string | null | undefined): value is ProviderKey =>
  PROVIDERS.includes(value as ProviderKey);

/** Time labels are Hanoi time, the way the rest of the page shows them. */
function timeLabel(iso: string | null | undefined, locale: string): string {
  if (!iso) return "";
  return new Intl.DateTimeFormat(locale, {
    timeZone: "Asia/Ho_Chi_Minh",
    day: "2-digit",
    month: "2-digit",
    hour: "2-digit",
    minute: "2-digit",
    hour12: false,
  }).format(new Date(iso));
}

function connStatus(
  connection: IntegrationConnection,
  copy: IntegrationsCopy,
): IntegrationStatus {
  switch (connection.status) {
    case "paused":
      return { tone: "off", label: copy.real.status.paused };
    case "error":
      return { tone: "warn", label: copy.real.status.error };
    default:
      return { tone: "ok", label: copy.real.status.active };
  }
}

function syncStatus(
  connection: IntegrationConnection,
  copy: IntegrationsCopy,
): IntegrationStatus {
  if (connection.provider !== "webhook") {
    return { tone: "ok", label: copy.real.syncOauth };
  }
  if (connection.status === "error") {
    return { tone: "warn", label: copy.real.syncFailed };
  }
  return connection.lastSuccessAt
    ? { tone: "ok", label: copy.real.syncOk }
    : { tone: "off", label: copy.real.syncNever };
}

function nameOf(
  provider: string | null | undefined,
  copy: IntegrationsCopy,
): string {
  return isProvider(provider) ? copy.real.providers[provider].name : "";
}

function rowOf(
  connection: IntegrationConnection,
  copy: IntegrationsCopy,
  locale: string,
  members: ReadonlyMap<string, string>,
): IntegrationRowDto {
  const provider = isProvider(connection.provider) ? connection.provider : null;
  const who =
    members.get(connection.connectedBy ?? "") ?? copy.real.unknownMember;
  return {
    key: connection.id ?? "",
    mono: provider ? copy.real.providers[provider].mono : "?",
    name: nameOf(connection.provider, copy),
    sub: `${copy.real.connectedBy} ${who} · ${timeLabel(connection.connectedAt, locale)}`,
    scope: { kind: "workspace", name: "" },
    conn: connStatus(connection, copy),
    sync: syncStatus(connection, copy),
    last: connection.lastSuccessAt
      ? timeLabel(connection.lastSuccessAt, locale)
      : copy.real.lastNever,
    lastSub: connection.lastError
      ? formatCopy(copy.real.lastError, {
          time: timeLabel(connection.lastErrorAt, locale),
        }) + ` · ${connection.lastError}`
      : "",
  };
}

function catalogOf(
  entry: { provider?: string | null; configured?: boolean },
  copy: IntegrationsCopy,
): IntegrationCatalogDto | null {
  if (!isProvider(entry.provider)) return null;
  const text = copy.real.providers[entry.provider];
  return {
    key: entry.provider,
    mono: text.mono,
    name: text.name,
    desc: text.desc,
    enterprise: false,
    provider: text.name,
    will: text.will.split("|"),
    wont: text.wont.split("|"),
    gate: null,
    configured: entry.configured === true,
  };
}

/** What a member who may not manage integrations sees: the page, no list. */
export function memberListOf(
  workspaceName: string,
  now: Date,
  locale: string,
): IntegrationsListDto {
  return {
    serverTime: now.toISOString(),
    updatedAt: timeLabel(now.toISOString(), locale),
    canManage: false,
    workspaceName,
    admins: [],
    rows: [],
    catalog: [],
    plan: { name: "free", account: "", workspaces: [] },
  };
}

export function listOf(
  overview: IntegrationsOverview,
  copy: IntegrationsCopy,
  options: {
    readonly workspaceName: string;
    readonly now: Date;
    readonly locale: string;
    readonly members: ReadonlyMap<string, string>;
  },
): IntegrationsListDto {
  return {
    serverTime: options.now.toISOString(),
    updatedAt: timeLabel(options.now.toISOString(), options.locale),
    canManage: true,
    workspaceName: options.workspaceName,
    admins: [],
    rows: (overview.connections ?? []).map((connection) =>
      rowOf(connection, copy, options.locale, options.members),
    ),
    catalog: (overview.catalog ?? [])
      .map((entry) => catalogOf(entry, copy))
      .filter((entry): entry is IntegrationCatalogDto => entry !== null),
    plan: { name: "free", account: "", workspaces: [] },
  };
}

export function detailOf(
  connection: IntegrationConnection,
  copy: IntegrationsCopy,
  options: {
    readonly now: Date;
    readonly locale: string;
    readonly members: ReadonlyMap<string, string>;
  },
): IntegrationDetailDto {
  const provider = isProvider(connection.provider) ? connection.provider : null;
  const text = provider ? copy.real.providers[provider] : null;
  const name = text?.name ?? "";
  const who =
    options.members.get(connection.connectedBy ?? "") ??
    copy.real.unknownMember;
  const webhook = connection.provider === "webhook";
  const overview = [
    { k: copy.real.connectedBy, v: who },
    {
      k: copy.real.connectedAt,
      v: timeLabel(connection.connectedAt, options.locale),
    },
    ...(connection.targetUrl
      ? [{ k: copy.real.receivingAddress, v: connection.targetUrl }]
      : []),
    {
      k: copy.real.lastSuccess.replace("{{time}}", "").trim(),
      v: connection.lastSuccessAt
        ? timeLabel(connection.lastSuccessAt, options.locale)
        : copy.real.lastNever,
    },
  ];
  return {
    key: connection.id ?? "",
    mono: text?.mono ?? "?",
    name,
    subtitle: text?.subtitle ?? "",
    kind: webhook ? "webhook" : "chat",
    updatedAt: timeLabel(options.now.toISOString(), options.locale),
    actions: {
      reauthorize: !webhook,
      disconnect: true,
      test: webhook,
      pause: true,
      paused: connection.status === "paused",
    },
    health: {
      conn: connStatus(connection, copy),
      sync: syncStatus(connection, copy),
    },
    overview,
    activity: [],
    callout:
      connection.status === "error" && connection.lastError
        ? {
            title: copy.real.errorTitle,
            body: formatCopy(copy.real.errorBody, {
              error: connection.lastError,
            }),
            hint: null,
            cta: webhook ? null : copy.reauthorize,
          }
        : null,
    pending: null,
    who: null,
    usedIn: [],
    perms: (text?.will.split("|") ?? []).map((t) => ({ t })),
    nots: (text?.wont.split("|") ?? []).map((t) => ({ t })),
    permNote: null,
    notNote: null,
    deliveries: null,
    config: null,
    dedupe: [],
    disconnect: {
      intro: formatCopy(copy.real.disconnect.intro, { name }),
      stops: copy.real.disconnect.stops.split("|"),
      keeps: copy.real.disconnect.keeps.split("|"),
      note: copy.real.disconnect.note,
      foot: copy.real.disconnect.foot,
    },
  };
}

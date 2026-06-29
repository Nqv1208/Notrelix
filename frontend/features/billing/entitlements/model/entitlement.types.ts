export type EntitlementFeature =
  | "boards.limit"
  | "docs.collaboration"
  | "automation"
  | "governance.custom-roles"
  | "governance.audit-logs"

export type EntitlementValue = boolean | number

export type WorkspaceEntitlements = Record<EntitlementFeature, EntitlementValue>

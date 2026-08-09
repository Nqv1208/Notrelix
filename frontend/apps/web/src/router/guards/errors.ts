export type RouteGuardFailureKind =
  | "unauthenticated"
  | "invalid-workspace"
  | "not-member"
  | "forbidden"
  | "missing-entitlement"
  | "feature-unavailable";

export class RouteGuardError extends Error {
  constructor(
    readonly kind: RouteGuardFailureKind,
    message: string,
    readonly status: 401 | 403 | 404 | 426 = 403,
  ) {
    super(message);
    this.name = "RouteGuardError";
  }
}

import { RouteGuardError } from './errors';

export function requireEntitlement(input: {
  readonly entitlement: string;
  readonly hasEntitlement: (entitlement: string) => boolean;
}): void {
  if (input.hasEntitlement(input.entitlement)) return;
  throw new RouteGuardError('missing-entitlement', 'Workspace entitlement is required.', 426);
}

import { describe, it, expect } from 'vitest';
import { billingQueryKeys } from '../core/query/keys';

describe('billingQueryKeys', () => {
  it('should generate plan subscription key for workspace', () => {
    expect(billingQueryKeys.subscription('ws-999')).toEqual(['billing', 'subscription', 'ws-999']);
  });

  it('should generate invoices query key', () => {
    expect(billingQueryKeys.invoices('ws-999')).toEqual(['billing', 'invoices', 'ws-999']);
  });
});

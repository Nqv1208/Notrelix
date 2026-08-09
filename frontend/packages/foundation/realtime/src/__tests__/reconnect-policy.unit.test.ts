import { describe, expect, test } from "vitest";
import { ReconnectPolicy } from "../connection/reconnect-policy";

describe("ReconnectPolicy", () => {
  test("respects maxRetries threshold", () => {
    const policy = new ReconnectPolicy({ maxRetries: 3 });

    expect(policy.shouldRetry(0)).toBe(true);
    expect(policy.shouldRetry(1)).toBe(true);
    expect(policy.shouldRetry(2)).toBe(true);
    expect(policy.shouldRetry(3)).toBe(false);
  });

  test("calculates exponential backoff capped by maxDelayMs", () => {
    const policy = new ReconnectPolicy({
      minDelayMs: 100,
      maxDelayMs: 500,
      backoffFactor: 2,
      jitter: false,
    });

    expect(policy.getNextDelay(0)).toBe(100);
    expect(policy.getNextDelay(1)).toBe(200);
    expect(policy.getNextDelay(2)).toBe(400);
    expect(policy.getNextDelay(3)).toBe(500); // capped at maxDelayMs
    expect(policy.getNextDelay(4)).toBe(500);
  });

  test("applies jitter when jitter option is true", () => {
    const policy = new ReconnectPolicy({
      minDelayMs: 1000,
      maxDelayMs: 5000,
      jitter: true,
    });

    const delay = policy.getNextDelay(0);
    expect(delay).toBeGreaterThanOrEqual(500);
    expect(delay).toBeLessThanOrEqual(1500);
  });
});

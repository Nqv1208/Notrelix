import { describe, expect, it } from "vitest";
import { env } from "../env";

describe("mobile app env config", () => {
  it("defines all required service URLs as valid absolute URLs", () => {
    expect(new URL(env.apiUrl).protocol).toMatch(/^https?:$/);
    expect(new URL(env.realtimeUrl).protocol).toMatch(/^(https?|wss?):$/);
    expect(new URL(env.webUrl).protocol).toMatch(/^https?:$/);
  });

  it("is a resolved runtime environment object with expected service URLs", () => {
    expect(env.apiUrl).toBeDefined();
    expect(env.realtimeUrl).toBeDefined();
    expect(env.webUrl).toBeDefined();
    expect(env.appUrl).toBeDefined();
  });
});

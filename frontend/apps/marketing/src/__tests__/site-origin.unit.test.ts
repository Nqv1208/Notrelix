import { describe, expect, it } from "vitest";

import { serializeJsonLd } from "../config/json-ld";
import { resolveSiteOrigin } from "../config/site-origin";

const env = {
  SITE_URL: "https://demo.notrelix.me/ignored/path",
  ALLOWED_HOSTS: "Demo.Notrelix.me, www.notrelix.me",
};

describe("resolveSiteOrigin", () => {
  it("accepts an allowlisted host", () => {
    expect(resolveSiteOrigin(env, "WWW.notrelix.me")).toBe(
      "https://www.notrelix.me",
    );
  });
  it("ignores a non-allowlisted host and uses SITE_URL origin", () => {
    expect(resolveSiteOrigin(env, "evil.example")).toBe(
      "https://demo.notrelix.me",
    );
  });
  it("falls back to SITE_URL without a host (forwarded headers are never an input)", () => {
    expect(resolveSiteOrigin(env, null)).toBe("https://demo.notrelix.me");
  });
  it("rejects a bad proto in SITE_URL", () => {
    expect(
      resolveSiteOrigin({ SITE_URL: "javascript://x/%0aalert(1)" }, "a.com"),
    ).toBeUndefined();
    expect(
      resolveSiteOrigin({ SITE_URL: "ftp://a.com" }, null),
    ).toBeUndefined();
  });
  it("returns undefined when nothing is configured", () => {
    expect(resolveSiteOrigin({}, "evil.example")).toBeUndefined();
  });
  it("never emits a path-bearing origin from an odd allowlist entry", () => {
    expect(resolveSiteOrigin({ ALLOWED_HOSTS: "a.com/x" }, "a.com/x")).toBe(
      "https://a.com",
    );
  });
});

describe("serializeJsonLd", () => {
  it("escapes a script-closing payload", () => {
    const sep = String.fromCharCode(0x2028, 0x2029);
    const payload = `</script><script>alert(1)</script>&${sep}`;
    const out = serializeJsonLd({ url: payload });
    expect(out).not.toMatch(/[<>&]/);
    expect(out.includes(sep[0]!) || out.includes(sep[1]!)).toBe(false);
    expect(JSON.parse(out).url).toBe(payload);
  });
});

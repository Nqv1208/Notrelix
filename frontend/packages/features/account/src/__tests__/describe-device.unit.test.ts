import { describe, expect, it } from "vitest";
import { describeDevice } from "../core";

describe("describeDevice", () => {
  it("classifies tablets, phones and desktops from the user agent", () => {
    expect(
      describeDevice("Mozilla/5.0 (iPad; CPU OS 17_0) Safari/604.1").kind,
    ).toBe("tablet");
    expect(
      describeDevice("Mozilla/5.0 (iPhone; CPU iPhone OS 17_0) Safari/604.1")
        .kind,
    ).toBe("phone");
    expect(
      describeDevice(
        "Mozilla/5.0 (Linux; Android 14; Pixel 8) Chrome/120.0 Mobile Safari/537.36",
      ).kind,
    ).toBe("phone");
    expect(
      describeDevice(
        "Mozilla/5.0 (Linux; Android 14; Pixel Tablet) Chrome/120.0 Safari/537.36",
      ).kind,
    ).toBe("tablet");
    expect(
      describeDevice("Mozilla/5.0 (Windows NT 10.0) Chrome/120.0 Safari/537.36")
        .kind,
    ).toBe("desktop");
    expect(describeDevice(null).kind).toBe("desktop");
  });

  it("labels the browser and the system", () => {
    expect(
      describeDevice("Mozilla/5.0 (Windows NT 10.0) Chrome/120.0 Safari/537.36")
        .device,
    ).toBe("Chrome · Windows");
  });

  it("classifies a long adversarial user agent in linear time", () => {
    const hostile = "Android".repeat(50_000);
    const started = performance.now();
    const result = describeDevice(hostile);
    expect(performance.now() - started).toBeLessThan(200);
    expect(result.kind).toBe("desktop");
    expect(describeDevice(`${hostile}Mobile`).kind).toBe("phone");
  });
});

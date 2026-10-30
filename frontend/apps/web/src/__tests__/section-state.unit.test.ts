import { describe, expect, it } from "vitest";
import { sectionState } from "../shell/workspace/section-state";

const pending = { isError: false, isSuccess: false };

describe("sectionState", () => {
  it("is loading until the query has data", () => {
    expect(sectionState(pending, undefined)).toEqual({ status: "loading" });
    expect(
      sectionState({ isError: false, isSuccess: true }, undefined),
    ).toEqual({ status: "loading" });
  });

  it("is ready with the data once the query succeeded", () => {
    expect(sectionState({ isError: false, isSuccess: true }, [1])).toEqual({
      status: "ready",
      data: [1],
    });
  });

  it("says a refused read was refused, so the page does not offer a retry that cannot help", () => {
    expect(
      sectionState(
        { isError: true, isSuccess: false, error: { status: 403 } },
        undefined,
      ),
    ).toEqual({ status: "error", reason: "forbidden" });
  });

  it("says an unreachable server was unreachable", () => {
    expect(
      sectionState(
        { isError: true, isSuccess: false, error: { kind: "network" } },
        undefined,
      ),
    ).toEqual({ status: "error", reason: "offline" });
  });

  it("gives no reason for any other failure", () => {
    expect(
      sectionState(
        { isError: true, isSuccess: false, error: { status: 500 } },
        undefined,
      ),
    ).toEqual({ status: "error" });
    expect(
      sectionState({ isError: true, isSuccess: false }, undefined),
    ).toEqual({ status: "error" });
  });
});

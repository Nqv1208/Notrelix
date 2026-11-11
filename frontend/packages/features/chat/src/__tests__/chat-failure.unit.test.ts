import { describe, expect, it } from "vitest";
import { chatFailureOf, isStaleFailure } from "../core/api/chat-failure";

describe("chatFailureOf", () => {
  it("reads a stable backend code before the status", () => {
    expect(chatFailureOf({ status: 409, code: "channel-archived" }).kind).toBe(
      "archived",
    );
    expect(chatFailureOf({ status: 409, code: "name-conflict" }).kind).toBe(
      "name-conflict",
    );
    expect(chatFailureOf({ status: 422, code: "last-manager" }).kind).toBe(
      "last-manager",
    );
    expect(chatFailureOf({ status: 409, code: "message-deleted" }).kind).toBe(
      "message-locked",
    );
    expect(
      chatFailureOf({ status: 409, code: "system-message-immutable" }).kind,
    ).toBe("message-locked");
  });

  it("maps a 412 to stale, and reports stale as the stale failure", () => {
    const failure = chatFailureOf({
      status: 412,
      code: "common.precondition-failed",
    });
    expect(failure.kind).toBe("stale");
    expect(isStaleFailure(failure)).toBe(true);
    expect(isStaleFailure(chatFailureOf({ status: 403 }))).toBe(false);
  });

  it("maps a 409 without a stable code to conflict, not to a message lock", () => {
    expect(chatFailureOf({ status: 409 }).kind).toBe("conflict");
  });

  it("falls back to the status for the other refusals", () => {
    expect(chatFailureOf({ status: 404 }).kind).toBe("not-found");
    expect(chatFailureOf({ status: 403 }).kind).toBe("forbidden");
    expect(chatFailureOf({ status: 400 }).kind).toBe("invalid");
    expect(chatFailureOf({ status: 422 }).kind).toBe("invalid");
    expect(chatFailureOf({ status: 429 }).kind).toBe("rate-limited");
    expect(chatFailureOf({ status: 503 }).kind).toBe("server");
  });

  it("treats a dropped connection as a retryable network failure", () => {
    const failure = chatFailureOf({ kind: "network" });
    expect(failure).toMatchObject({
      kind: "network",
      retryable: true,
      status: undefined,
    });
    expect(chatFailureOf({ kind: "aborted" }).kind).toBe("network");
  });

  it("marks only network, rate-limited and server failures as retryable", () => {
    expect(chatFailureOf({ status: 429 }).retryable).toBe(true);
    expect(chatFailureOf({ status: 500 }).retryable).toBe(true);
    expect(
      chatFailureOf({ status: 409, code: "channel-archived" }).retryable,
    ).toBe(false);
    expect(chatFailureOf({ status: 412 }).retryable).toBe(false);
  });

  it("classifies a non-AppError value as a server failure", () => {
    expect(chatFailureOf(new Error("boom")).kind).toBe("server");
    expect(chatFailureOf(undefined).kind).toBe("server");
  });
});

import { describe, expect, it, vi } from "vitest";
import { createOwnershipStepUp } from "../ownership-step-up";

const endpoints = {
  auth: {
    stepUpRequirement: "/req",
    stepUpCompleteMfa: "/mfa",
    stepUpCompletePassword: "/pw",
  },
} as never;

function setup(requirement: object) {
  const post = vi.fn(async (url: string) => {
    if (url === "/req") return requirement;
    return { proofToken: "proof-1" };
  });
  const stepUp = createOwnershipStepUp({ post } as never, endpoints);
  return { post, stepUp };
}

describe("The host's answer to the ownership handover's proof", () => {
  it.each([
    ["MfaChallenge", "code", "challenge-1"],
    ["Password", "password", null],
    ["OAuth", "provider", null],
  ] as const)(
    "asks for the TransferWorkspaceOwnership purpose and maps %s to %s",
    async (requiredFactor, factor, challengeToken) => {
      const { post, stepUp } = setup({ requiredFactor, challengeToken });
      expect(await stepUp.request()).toEqual({ factor, challengeToken });
      expect(post).toHaveBeenCalledWith(
        "/req",
        { purpose: "TransferWorkspaceOwnership" },
        expect.anything(),
      );
    },
  );

  it("completes a code and a password for that purpose only and returns the single-use proof", async () => {
    const { post, stepUp } = setup({});
    expect(await stepUp.withCode("challenge-1", "123456")).toBe("proof-1");
    expect(await stepUp.withPassword("secret")).toBe("proof-1");
    expect(post).toHaveBeenCalledWith(
      "/mfa",
      {
        purpose: "TransferWorkspaceOwnership",
        challengeToken: "challenge-1",
        code: "123456",
      },
      expect.anything(),
    );
    expect(post).toHaveBeenCalledWith(
      "/pw",
      { purpose: "TransferWorkspaceOwnership", password: "secret" },
      expect.anything(),
    );
  });
});

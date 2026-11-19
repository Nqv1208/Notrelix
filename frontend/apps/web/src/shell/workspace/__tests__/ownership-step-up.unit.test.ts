import { afterEach, describe, expect, it, vi } from "vitest";
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

describe("The host's provider round trip for the ownership handover", () => {
  const providerEndpoints = {
    auth: {
      stepUpRequirement: "/req",
      oauthStepUpStart: (provider: string) =>
        `/oauth/${provider}/step-up/start`,
      stepUpClaimProvider: "/claim",
      securitySummary: "/summary",
    },
  } as never;

  function provider(env = { mockApi: false, apiUrl: "https://api.test" }) {
    const post = vi.fn(async () => ({ proofToken: "proof-cookie" }));
    const get = vi.fn(async () => ({ linkedProviders: ["Google"] }));
    const stepUp = createOwnershipStepUp(
      { post, get } as never,
      providerEndpoints,
      env,
    );
    return { post, get, stepUp: stepUp.provider! };
  }

  /** A browser address the host can read, assign to and rewrite, in the node environment. */
  function browserAt(href: string) {
    const location = {
      href,
      get pathname() {
        return new URL(this.href).pathname;
      },
      get search() {
        return new URL(this.href).search;
      },
      origin: new URL(href).origin,
      assign: vi.fn(),
    };
    const history = {
      state: null,
      replaceState: (_state: unknown, _title: string, url: string) => {
        location.href = new URL(url, location.href).href;
      },
    };
    vi.stubGlobal("window", { location, history });
    return location;
  }

  afterEach(() => vi.unstubAllGlobals());

  it("leaves for the provider with this purpose and the chosen person in the way back", async () => {
    const { assign } = browserAt("https://app.test/workspaces/w-1/settings");
    await provider().stepUp.start("Google", "u-ha");
    const target = String(assign.mock.calls[0]?.[0]);
    expect(
      target.startsWith("https://api.test/oauth/Google/step-up/start?"),
    ).toBe(true);
    const query = new URL(target).searchParams;
    expect(query.get("purpose")).toBe("TransferWorkspaceOwnership");
    expect(query.get("returnUrl")).toBe(
      "/workspaces/w-1/settings?stepUp=TransferWorkspaceOwnership&newOwner=u-ha",
    );
  });

  it("reads the return once and clears it from the address", () => {
    const location = browserAt(
      "https://app.test/workspaces/w-1/settings?stepUp=TransferWorkspaceOwnership&newOwner=u-ha&tab=x",
    );
    const { stepUp } = provider();
    expect(stepUp.resume()).toEqual({ newOwnerId: "u-ha", failed: false });
    expect(location.search).toBe("?tab=x");
    expect(stepUp.resume()).toBeNull();
  });

  it("reports a refusal from the provider, and ignores another purpose's return", () => {
    browserAt(
      "https://app.test/s?stepUp=TransferWorkspaceOwnership&newOwner=u-ha&stepUpResult=failed",
    );
    expect(provider().stepUp.resume()).toEqual({
      newOwnerId: "u-ha",
      failed: true,
    });
    browserAt("https://app.test/s?stepUp=IssueApiToken&newOwner=u-ha");
    expect(provider().stepUp.resume()).toBeNull();
  });

  it("claims the single-use proof the return left and lists the linked providers", async () => {
    const { post, stepUp } = provider();
    expect(await stepUp.claim()).toBe("proof-cookie");
    expect(post).toHaveBeenCalledWith("/claim", {}, expect.anything());
    expect(await stepUp.providers()).toEqual(["Google"]);
  });

  it("offers no provider path without a runtime environment", () => {
    const stepUp = createOwnershipStepUp(
      { post: vi.fn() } as never,
      providerEndpoints,
    );
    expect(stepUp.provider).toBeUndefined();
  });
});

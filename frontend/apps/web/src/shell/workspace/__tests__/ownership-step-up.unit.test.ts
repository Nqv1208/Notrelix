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
  const stepUp = createOwnershipStepUp({ post } as never, endpoints, "w-1");
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

  it("completes a code and a password for that purpose, this workspace and the chosen person only, and returns the single-use proof", async () => {
    const { post, stepUp } = setup({});
    expect(await stepUp.withCode("challenge-1", "123456", "u-ha")).toBe(
      "proof-1",
    );
    expect(await stepUp.withPassword("secret", "u-ha")).toBe("proof-1");
    expect(post).toHaveBeenCalledWith(
      "/mfa",
      {
        purpose: "TransferWorkspaceOwnership",
        challengeToken: "challenge-1",
        code: "123456",
        workspaceId: "w-1",
        newOwnerId: "u-ha",
      },
      expect.anything(),
    );
    expect(post).toHaveBeenCalledWith(
      "/pw",
      {
        purpose: "TransferWorkspaceOwnership",
        password: "secret",
        workspaceId: "w-1",
        newOwnerId: "u-ha",
      },
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
    const post = vi.fn(async (url: string) =>
      url.endsWith("/step-up/start")
        ? { authorizationUrl: "https://accounts.google.test/authorize?s=1" }
        : { proofToken: "proof-cookie" },
    );
    const get = vi.fn(async () => ({ linkedProviders: ["Google"] }));
    const stepUp = createOwnershipStepUp(
      { post, get } as never,
      providerEndpoints,
      "w-1",
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

  it("starts the provider round trip with a POST naming this purpose, workspace and person, then leaves for the URL it answers", async () => {
    const { assign } = browserAt("https://app.test/workspaces/w-1/settings");
    const { post, stepUp } = provider();
    await stepUp.start("Google", "u-ha");
    expect(post).toHaveBeenCalledWith(
      "/oauth/Google/step-up/start",
      {
        purpose: "TransferWorkspaceOwnership",
        returnUrl:
          "/workspaces/w-1/settings?stepUp=TransferWorkspaceOwnership&newOwner=u-ha",
        workspaceId: "w-1",
        newOwnerId: "u-ha",
      },
      expect.anything(),
    );
    expect(assign).toHaveBeenCalledWith(
      "https://accounts.google.test/authorize?s=1",
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
      "w-1",
    );
    expect(stepUp.provider).toBeUndefined();
  });
});

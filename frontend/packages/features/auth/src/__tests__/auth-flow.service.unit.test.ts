import { describe, expect, it, vi } from "vitest";
import {
  AUTH_FLOW_PATHS,
  createAuthFlowService,
  type AuthFlowApiClient,
} from "../core/api/auth-flow.service";
import { IDENTITY_ERROR, readFlowError } from "../core/utils/flow-error";

function setup(answer: unknown = undefined) {
  const post = vi.fn(async () => answer);
  const get = vi.fn(async () => answer);
  const api = { get, post } as unknown as AuthFlowApiClient;
  return { post, get, service: createAuthFlowService(api) };
}

describe("auth flow adapter: identify", () => {
  it("posts the email to the backend route and reads a password answer", async () => {
    const { post, service } = setup({ method: "password", sso: null });
    await expect(service.identify("ten@congty.vn")).resolves.toEqual({
      method: "password",
      sso: null,
    });
    expect(post).toHaveBeenCalledWith(
      AUTH_FLOW_PATHS.identify,
      { email: "ten@congty.vn" },
      expect.objectContaining({ skipAuthRefresh: true }),
    );
    expect(AUTH_FLOW_PATHS.identify).toBe("/auth/identify");
  });

  it("keeps the company card when the answer carries an sso block", async () => {
    const { service } = setup({
      method: "sso",
      sso: {
        orgName: "Minh An",
        orgInitials: "MA",
        provider: "microsoft-entra",
        providerLabel: "Microsoft Entra",
      },
    });
    const result = await service.identify("a@minhan.vn");
    expect(result.method).toBe("sso");
    expect(result.sso?.provider).toBe("microsoft-entra");
  });

  it("falls back to a password when sso is announced without a block", async () => {
    const { service } = setup({ method: "sso", sso: null });
    expect((await service.identify("a@b.vn")).method).toBe("password");
  });
});

describe("auth flow adapter: provider sign-in", () => {
  it("builds the backend start path for the browser to navigate to, without calling it", () => {
    const { post, get, service } = setup();
    expect(service.oauthStartPath("google")).toBe("/auth/oauth/google/start");
    expect(service.oauthStartPath("microsoft", "/workspaces/a?b=1")).toBe(
      "/auth/oauth/microsoft/start?returnUrl=%2Fworkspaces%2Fa%3Fb%3D1",
    );
    expect(post).not.toHaveBeenCalled();
    expect(get).not.toHaveBeenCalled();
  });
});

describe("auth flow adapter: invitation preview", () => {
  const wire = {
    state: "valid",
    inviterName: "Nguyễn Thu Hà",
    workspaceName: "Minh An Group",
    role: "Guest",
    resourceKind: "work-management.board",
    resourceTitle: "Triển khai ERP Q4",
    resourceLevel: "Editor",
    expiresOn: "2027-01-31",
    email: "tuan@doitac.vn",
    hasAccount: false,
  };

  it("posts the token in the body and maps the backend shape to the page's model", async () => {
    const { post, service } = setup(wire);
    const preview = await service.getInvite("tok/1");
    expect(post).toHaveBeenCalledWith(
      AUTH_FLOW_PATHS.invitationPreview,
      { token: "tok/1" },
      expect.anything(),
    );
    expect(preview).toEqual({
      state: "valid",
      inviterName: "Nguyễn Thu Hà",
      inviterInitials: "NH",
      orgName: "Minh An Group",
      boardTitle: "Triển khai ERP Q4",
      level: "editor",
      expiresOn: "2027-01-31",
      email: "tuan@doitac.vn",
      name: null,
      hasAccount: false,
    });
  });

  it("names the workspace when the invitation is not for a resource and defaults to viewer", async () => {
    const { service } = setup({
      ...wire,
      resourceKind: null,
      resourceTitle: null,
      resourceLevel: null,
      state: "expired",
      email: null,
    });
    const preview = await service.getInvite("t");
    expect(preview.boardTitle).toBe("Minh An Group");
    expect(preview.level).toBe("viewer");
    expect(preview.state).toBe("expired");
    expect(preview.email).toBeNull();
  });
});

describe("auth flow adapter: accepting and declining", () => {
  it("accepts with registration, reads the session and lands on the invited board", async () => {
    const { post, service } = setup({
      workspaceId: "w1",
      resourceKind: "work-management.board",
      resourceId: "b1",
      alreadyAccepted: false,
      expiresAt: "2026-10-24T10:00:00Z",
      user: { id: "u1", email: "a@b.vn", name: "An", emailConfirmed: true },
    });
    const result = await service.acceptInviteWithRegistration("tok", {
      name: "An",
      password: "mot-mat-khau-du-dai",
    });
    expect(post).toHaveBeenCalledWith(
      AUTH_FLOW_PATHS.invitationAcceptWithRegistration,
      { token: "tok", name: "An", password: "mot-mat-khau-du-dai" },
      expect.anything(),
    );
    expect(result).toEqual({
      redirectTo: "/workspaces/w1/boards/b1",
      alreadyAccepted: false,
      session: {
        user: { id: "u1", email: "a@b.vn", name: "An", avatarUrl: null },
        expiresAt: "2026-10-24T10:00:00Z",
      },
    });
  });

  it("a second press has no session", async () => {
    const { service } = setup({
      workspaceId: "w1",
      resourceKind: null,
      resourceId: null,
      alreadyAccepted: true,
      expiresAt: null,
      user: null,
    });
    const result = await service.acceptInviteWithRegistration("tok", {
      name: "An",
      password: "mot-mat-khau-du-dai",
    });
    expect(result.alreadyAccepted).toBe(true);
    expect(result.session).toBeNull();
    expect(result.redirectTo).toBe("/workspaces/w1");
  });

  it("accepts with the signed-in session by token only", async () => {
    const { post, service } = setup({
      workspaceId: "w1",
      resourceKind: "work-management.board",
      resourceId: "b1",
    });
    const result = await service.acceptInvite("tok");
    expect(post).toHaveBeenCalledWith(
      AUTH_FLOW_PATHS.invitationAccept,
      { token: "tok" },
      expect.anything(),
    );
    expect(result.redirectTo).toBe("/workspaces/w1/boards/b1");
    expect(result.session).toBeNull();
  });

  it("declines with the token in the body", async () => {
    const { post, service } = setup();
    await service.declineInvite("tok");
    expect(post).toHaveBeenCalledWith(
      AUTH_FLOW_PATHS.invitationDecline,
      { token: "tok" },
      expect.anything(),
    );
  });
});

describe("flow errors", () => {
  it("reads the producer's property names as page field names", () => {
    const failure = readFlowError({
      status: 400,
      details: {
        errorCode: "validation.failed",
        errors: { Password: ["Too short"], Name: ["Required"] },
      },
    });
    expect(failure.fieldErrors).toEqual({
      password: "Too short",
      name: "Required",
    });
    expect(failure.code).toBe("validation.failed");
  });

  it("names the provider sign-in stopped by a second factor", () => {
    expect(IDENTITY_ERROR.oauthMfaRequired).toBe("identity.oauth.mfa-required");
  });
});

/**
 * The wire shape of two Members-page writes: another member's job title (F08, `PUT .../job-title` quoting the
 * member's version) and an invitation with initial Team and Space entries (G02, `initialEntries`).
 */
import { describe, expect, it, vi } from "vitest";
import { createMembersService } from "../core/api/members.service";
import { createWorkspaceInvitationsService } from "../core/api/invitations.service";
import type { WorkspaceApiClient } from "../core/api/workspace.service";

function client() {
  return {
    get: vi.fn(),
    put: vi.fn().mockResolvedValue(undefined),
    post: vi.fn().mockResolvedValue("inv-1"),
    patch: vi.fn(),
    delete: vi.fn(),
  } satisfies WorkspaceApiClient;
}

describe("member job title write", () => {
  it("FUI[workspace.members:job-title-wire] puts the trimmed title with the member's version", async () => {
    const api = client();
    await createMembersService(api).updateJobTitle("w1", "u1", "  Lead ", 4);
    expect(api.put).toHaveBeenCalledWith(
      "/workspaces/w1/members/u1/job-title",
      {
        jobTitle: "Lead",
        expectedVersion: 4,
      },
    );
  });

  it("sends null for a blank title, and never writes a member whose version is unknown", async () => {
    const api = client();
    const service = createMembersService(api);
    await service.updateJobTitle("w1", "u1", "   ", 4);
    expect(api.put).toHaveBeenCalledWith(
      "/workspaces/w1/members/u1/job-title",
      {
        jobTitle: null,
        expectedVersion: 4,
      },
    );
    await expect(
      service.updateJobTitle("w1", "u1", "Lead", undefined),
    ).rejects.toThrow("version is not known");
    expect(api.put).toHaveBeenCalledTimes(1);
  });
});

describe("invitation with initial entries", () => {
  const endpoints = {
    workspaces: {
      members: (id: string) => `/workspaces/${id}/members`,
      invitations: (id: string) => `/workspaces/${id}/invitations`,
      invitation: (id: string, inv: string) =>
        `/workspaces/${id}/invitations/${inv}`,
      invitationRole: (id: string, inv: string) =>
        `/workspaces/${id}/invitations/${inv}/role`,
      resendInvitation: (id: string, inv: string) =>
        `/workspaces/${id}/invitations/${inv}/resend`,
    },
  };

  it("FUI[workspace.members:invite-entries-wire] sends each entry with the producer's kind and role names", async () => {
    const api = client();
    await createWorkspaceInvitationsService(api, endpoints).create("w1", {
      email: " an.do@minhan.vn ",
      role: "member",
      initialEntries: [
        { kind: "team", targetId: "t1", role: "lead" },
        { kind: "space", targetId: "s1", role: "viewer" },
      ],
    });
    expect(api.post).toHaveBeenCalledWith(
      "/workspaces/w1/members",
      {
        email: "an.do@minhan.vn",
        role: "Member",
        initialEntries: [
          { kind: "Team", targetId: "t1", role: "Lead" },
          { kind: "Space", targetId: "s1", role: "Viewer" },
        ],
      },
      undefined,
    );
  });

  it("sends no entries field for a plain invitation", async () => {
    const api = client();
    await createWorkspaceInvitationsService(api, endpoints).create("w1", {
      email: "a@b.vn",
      role: "guest",
    });
    expect(api.post).toHaveBeenCalledWith(
      "/workspaces/w1/members",
      { email: "a@b.vn", role: "Guest" },
      undefined,
    );
  });
});

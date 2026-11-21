/**
 * Workspace settings read and write (Workspaces.Settings.*): a save sends every setting the producer holds, so a
 * write made for another field (the sharing switch, the defaults form) never clears the default end of a Guest's
 * access, which the producer reads a missing value as "none" for.
 */
import { describe, expect, it, vi } from "vitest";
import {
  createWorkspaceSettingsService,
  parseWorkspaceSettings,
  type WorkspaceApiClient,
  type WorkspaceSettingsEndpoints,
} from "../core/api/workspace.service";

const endpoints: WorkspaceSettingsEndpoints = {
  workspaces: {
    profile: (id) => `/workspaces/${id}/profile`,
    slug: (id) => `/workspaces/${id}/slug`,
    settings: (id) => `/workspaces/${id}/settings`,
    archive: (id) => `/workspaces/${id}/archive`,
    unarchive: (id) => `/workspaces/${id}/archive/unarchive`,
    remove: (id, version) => `/workspaces/${id}?expectedVersion=${version}`,
    transferOwnership: (id) => `/workspaces/${id}/transfer-ownership`,
  },
};

function client(read: unknown) {
  const api = {
    get: vi.fn().mockResolvedValue(read),
    put: vi.fn().mockResolvedValue(undefined),
    post: vi.fn(),
    patch: vi.fn(),
    delete: vi.fn(),
  } satisfies WorkspaceApiClient;
  return api;
}

const stored = {
  allowPublicSharing: false,
  enforceMfa: false,
  allowGuestInvites: true,
  defaultMemberRole: "Guest",
  invitationExpiryDays: 14,
  guestDefaultExpiryDays: 90,
  size: "11-50",
  language: "en",
  timeZone: "Asia/Tokyo",
  weekStart: "sunday",
};

describe("workspace settings service", () => {
  it("FUI[workspace.settings:guest-expiry-kept] sends the Guest default expiry it read, so a save never clears it", async () => {
    const api = client(stored);
    const service = createWorkspaceSettingsService(api, endpoints);

    const settings = await service.getSettings("w1");
    expect(settings.guestDefaultExpiryDays).toBe(90);
    await service.updateSettings(
      "w1",
      { ...settings, allowPublicSharing: true },
      5,
    );

    expect(api.put).toHaveBeenCalledWith("/workspaces/w1/settings", {
      allowPublicSharing: true,
      allowGuestInvites: true,
      defaultMemberRole: "Guest",
      invitationExpiryDays: 14,
      guestDefaultExpiryDays: 90,
      timeZone: "Asia/Tokyo",
      weekStart: "sunday",
      expectedVersion: 5,
    });
  });

  it("reads no Guest default expiry as none and sends none", async () => {
    const api = client({ ...stored, guestDefaultExpiryDays: null });
    const service = createWorkspaceSettingsService(api, endpoints);

    const settings = await service.getSettings("w1");
    await service.updateSettings("w1", settings, 6);

    expect(settings.guestDefaultExpiryDays).toBeNull();
    expect(api.put.mock.calls[0]?.[1]).toMatchObject({
      guestDefaultExpiryDays: null,
      expectedVersion: 6,
    });
  });

  it("keeps only the producer's three Guest expiry choices when reading", () => {
    for (const days of [30, 90, 180])
      expect(
        parseWorkspaceSettings({ guestDefaultExpiryDays: days })
          .guestDefaultExpiryDays,
      ).toBe(days);
    for (const bad of [7, "90", undefined, null])
      expect(
        parseWorkspaceSettings({ guestDefaultExpiryDays: bad })
          .guestDefaultExpiryDays,
      ).toBeNull();
  });

  it("FUI[workspace.unarchive:write] posts the version the member last saw to the unarchive route", async () => {
    const api = client(stored);
    api.post.mockResolvedValue(undefined);
    const service = createWorkspaceSettingsService(api, endpoints);

    await service.unarchive("w1", 7);

    expect(api.post).toHaveBeenCalledWith("/workspaces/w1/archive/unarchive", {
      expectedVersion: 7,
    });
  });

  it("FUI[workspace.settings:address-write] patches the normalised address with the version it read", async () => {
    const api = client(stored);
    const service = createWorkspaceSettingsService(api, endpoints);

    await service.changeSlug("w1", "  Acme-Team ", 7);

    expect(api.patch).toHaveBeenCalledWith("/workspaces/w1/slug", {
      slug: "acme-team",
      expectedVersion: 7,
    });
  });
});

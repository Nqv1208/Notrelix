import { describe, expect, test } from "bun:test"
import { mapInvitationDto } from "./invitations.api"
import { mapMemberDto } from "./members.api"
import { mapWorkspaceDto } from "./workspace.api"

describe("workspace API mappers", () => {
  test("maps workspace DTO into a frontend workspace summary", () => {
    expect(mapWorkspaceDto({
      id: "workspace-a",
      name: "Product Team",
      slug: "product-team",
      description: null,
      isPersonal: false,
      plan: "BUSINESS",
      iconType: null,
      iconValue: null,
      coverUrl: null,
      isArchived: false,
      memberCount: 4,
      createdAt: "2026-01-01T00:00:00.000Z",
      settings: "{\"customViews\":[]}",
    })).toEqual({
      id: "workspace-a",
      slug: "product-team",
      name: "Product Team",
      description: undefined,
      icon: "P",
      plan: "business",
      memberCount: 4,
      isPersonal: false,
      settings: "{\"customViews\":[]}",
    })
  })

  test("maps member DTO into normalized workspace member fields", () => {
    expect(mapMemberDto({
      userId: "user-a",
      name: "Minh Nguyen",
      avatar: null,
      role: "OWNER",
      joinedAt: "2026-01-01T00:00:00.000Z",
    }, 0)).toMatchObject({
      id: "wm-user-a",
      userId: "user-a",
      initials: "MN",
      role: "owner",
      status: "active",
      workload: 0,
    })
  })

  test("maps invitation DTO without using any-shaped results", () => {
    expect(mapInvitationDto({
      id: "inv-a",
      email: "teammate@example.com",
      role: "admin",
      expiresAt: "2026-01-07T00:00:00.000Z",
      isAccepted: false,
      createdAt: "2026-01-01T00:00:00.000Z",
    })).toEqual({
      id: "inv-a",
      email: "teammate@example.com",
      role: "admin",
      expiresAt: "2026-01-07T00:00:00.000Z",
      isAccepted: false,
      createdAt: "2026-01-01T00:00:00.000Z",
    })
  })
})

/**
 * The invitations addressed to the signed-in person (`Workspaces.Invitations.GetUserPendingInvitations`,
 * `AcceptInvitationById`, `DeclineInvitation`). They name workspaces the person does not belong to yet, so
 * they live apart from a workspace's own invitation list. Small per-user state: the first read seeds two
 * waiting invitations; accepting one adds the workspace through the store.
 */

export interface MockInviteeInvitation {
  id: string;
  workspaceName: string;
  workspaceSlug: string;
  inviterName: string;
  role: "Member" | "Guest";
  /** Days from the first read until the invitation runs out. */
  expiresInDays: number;
  status: "pending" | "accepted" | "declined";
}

const SEED: readonly Omit<MockInviteeInvitation, "status">[] = [
  {
    id: "mock-invitee-invitation-1",
    workspaceName: "Studio Sao Mai",
    workspaceSlug: "studio-sao-mai",
    inviterName: "Lê Thu Hà",
    role: "Member",
    expiresInDays: 6,
  },
  {
    id: "mock-invitee-invitation-2",
    workspaceName: "Kế toán Minh An",
    workspaceSlug: "ke-toan-minh-an",
    inviterName: "Phạm Quốc Bảo",
    role: "Guest",
    expiresInDays: 2,
  },
];

export class MockInviteeInvitations {
  private byUser = new Map<string, MockInviteeInvitation[]>();

  reset(): void {
    this.byUser.clear();
  }

  private of(userId: string): MockInviteeInvitation[] {
    let list = this.byUser.get(userId);
    if (!list) {
      list = SEED.map((row) => ({ ...row, status: "pending" as const }));
      this.byUser.set(userId, list);
    }
    return list;
  }

  /** The invitations still waiting, newest first. */
  pendingOf(userId: string): MockInviteeInvitation[] {
    return this.of(userId).filter((row) => row.status === "pending");
  }

  /** One waiting invitation; `null` when it is unknown, answered or not this person's. */
  findPending(userId: string, id: string): MockInviteeInvitation | null {
    return this.pendingOf(userId).find((row) => row.id === id) ?? null;
  }

  answer(userId: string, id: string, status: "accepted" | "declined"): void {
    const row = this.findPending(userId, id);
    if (row) row.status = status;
  }
}

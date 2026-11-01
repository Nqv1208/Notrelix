import { expect, test, type Page } from "@playwright/test";
import {
  apiRead,
  apiWrite,
  openBoard,
  realUsers,
  signIn,
} from "./support/real-session";

const KIND = "work-management.board";

interface CreatedLink {
  readonly shareLink: { readonly id: string };
}
interface Settings {
  readonly enforceMfa: boolean;
  readonly allowGuestInvites: boolean;
  readonly allowPublicSharing: boolean;
  readonly defaultMemberRole: string;
  readonly invitationExpiryDays: number;
  readonly guestDefaultExpiryDays?: number | null;
  readonly size?: string | null;
  readonly language?: string | null;
  readonly timeZone?: string | null;
  readonly weekStart?: string | null;
}

/** The workspace's share-link switch, set as its owner; returns what it was. */
async function setShareLinks(page: Page, workspaceId: string, allow: boolean) {
  const settings = await apiRead<Settings>(
    page,
    `/workspaces/${workspaceId}/settings`,
  );
  const workspaces = await apiRead<{ id: string; version: number }[]>(
    page,
    "/workspaces",
  );
  const version = workspaces.find((w) => w.id === workspaceId)!.version;
  const saved = await apiWrite(
    page,
    "PUT",
    `/workspaces/${workspaceId}/settings`,
    { ...settings, allowPublicSharing: allow, expectedVersion: version },
  );
  expect(saved.status, JSON.stringify(saved.data)).toBeLessThan(300);
  return settings.allowPublicSharing;
}

test("a board manager turns a share link off on the shared board, and a link of another board stays out of reach", async ({
  browser,
  page,
}) => {
  const ownerContext = await browser.newContext();
  const ownerPage = await ownerContext.newPage();
  await signIn(ownerPage, realUsers.workspaceOwner);
  await signIn(page, realUsers.workspaceAdmin);
  const { workspace, board } = await openBoard(
    page,
    "Workspace 3",
    "Engineering Tasks",
  );
  const boards = await apiRead<{ id: string; title: string }[]>(
    page,
    `/workspaces/${workspace.id}/boards`,
  );
  const other = boards.find((candidate) => candidate.id !== board.id);
  expect(other, "a second board in Workspace 3").toBeDefined();
  const linksPath = `/resources/${KIND}/${board.id}/share-links`;

  const wasAllowed = await setShareLinks(ownerPage, workspace.id, false);
  try {
    // While the workspace has links off, none is made.
    const refusedWhileOff = await apiWrite(page, "POST", linksPath, {
      level: "viewer",
    });
    expect(refusedWhileOff.status).toBe(400);

    await setShareLinks(ownerPage, workspace.id, true);
    const link = await apiWrite<CreatedLink>(page, "POST", linksPath, {
      level: "viewer",
    });
    expect(link.status, JSON.stringify(link.data)).toBeLessThan(300);
    const linkId = link.data!.shareLink.id;

    // The link belongs to this board only: named under another board it is not found.
    const wrongBoard = await apiWrite(
      page,
      "DELETE",
      `/resources/${KIND}/${other!.id}/share-links/${linkId}`,
    );
    expect(wrongBoard.status).toBe(404);

    // A person who manages nothing on the board cannot turn it off.
    const guestContext = await browser.newContext();
    const guestPage = await guestContext.newPage();
    await signIn(guestPage, realUsers.workspaceGuest);
    const refused = await apiWrite(
      guestPage,
      "DELETE",
      `${linksPath}/${linkId}`,
    );
    expect([403, 404]).toContain(refused.status);
    await guestContext.close();

    // The manager turns it off on the board it was made for; doing it again changes nothing.
    const off = await apiWrite(page, "DELETE", `${linksPath}/${linkId}`);
    expect(off.status).toBe(204);
    const again = await apiWrite(page, "DELETE", `${linksPath}/${linkId}`);
    expect(again.status).toBe(204);
  } finally {
    await setShareLinks(ownerPage, workspace.id, wasAllowed);
    await ownerContext.close();
  }
});

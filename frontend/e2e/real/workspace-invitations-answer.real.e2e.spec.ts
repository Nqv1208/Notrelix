import { expect, test, type Page } from "@playwright/test";
import {
  apiRead,
  apiWrite,
  findWorkspace,
  realUsers,
  signIn,
} from "./support/real-session";

const apiUrl = process.env.VITE_API_URL ?? "http://127.0.0.1:58000/api/v1";

interface Pending {
  readonly id: string;
  readonly workspaceName?: string;
}
interface PendingPage {
  readonly items?: readonly Pending[];
}

async function pending(page: Page): Promise<readonly Pending[]> {
  const read = await apiRead<PendingPage | readonly Pending[]>(
    page,
    "/invitations/pending",
  );
  return Array.isArray(read) ? read : ((read as PendingPage).items ?? []);
}

async function openInvites(page: Page) {
  await page.goto("/");
  await page
    .getByRole("button", { name: /open the Notrelix system rail/ })
    .click();
  await page
    .getByRole("button", { name: "Pending workspace invitations" })
    .click();
}

test.setTimeout(120_000);

test("an invited person declines, is invited again and accepts from the invitations list; someone else cannot answer for them", async ({
  browser,
  page,
}) => {
  // The Owner of Workspace 3 invites a person who has an account.
  await signIn(page, realUsers.workspaceOwner);
  const workspace = await findWorkspace(page, "Workspace 3");
  const invite = () =>
    apiWrite<string>(page, "POST", `/workspaces/${workspace.id}/members`, {
      email: realUsers.guest.email,
      role: "Member",
    });

  const invitee = await browser.newContext();
  const other = await browser.newContext();
  {
    const inviteePage = await invitee.newPage();
    await signIn(inviteePage, realUsers.guest);
    // The invitee is a seeded Member of the workspace; the Owner removes them,
    // which is the state an invitation is for.
    const me = await apiRead<{ userId: string; version: number }>(
      inviteePage,
      `/workspaces/${workspace.id}/members/me`,
    );
    expect(
      (
        await apiWrite(
          page,
          "DELETE",
          `/workspaces/${workspace.id}/members/${me.userId}?expectedVersion=${me.version}`,
        )
      ).status,
    ).toBeLessThan(300);
    expect((await invite()).status).toBe(201);
    const waiting = await pending(inviteePage);
    expect(waiting).toHaveLength(1);
    const invitationId = waiting[0]!.id;

    // Another signed-in person neither sees it nor can answer it.
    const otherPage = await other.newPage();
    await signIn(otherPage, realUsers.workspaceGuest);
    expect(await pending(otherPage)).toHaveLength(0);
    expect(
      (await apiWrite(otherPage, "POST", `/invitations/${invitationId}/accept`))
        .status,
    ).toBeGreaterThanOrEqual(400);
    expect(
      (
        await apiWrite(
          otherPage,
          "POST",
          `/invitations/${invitationId}/decline`,
        )
      ).status,
    ).toBeGreaterThanOrEqual(400);
    expect(await pending(inviteePage)).toHaveLength(1);

    // The invitee declines in the list: it leaves the list and grants nothing.
    await openInvites(inviteePage);
    await inviteePage
      .getByRole("button", { name: /^Decline the invitation to / })
      .click();
    await expect(inviteePage.getByText("No pending invitations")).toBeVisible();
    expect(await pending(inviteePage)).toHaveLength(0);
    expect(
      (
        await inviteePage.request.get(
          `${apiUrl}/workspaces/${workspace.id}/members/me`,
        )
      ).status(),
    ).toBeGreaterThanOrEqual(400);

    // Invited again, they accept in the list and land in the workspace.
    expect((await invite()).status).toBe(201);
    await openInvites(inviteePage);
    await inviteePage
      .getByRole("button", { name: /^Accept the invitation to / })
      .click();
    await expect(inviteePage).toHaveURL(
      new RegExp(`/workspaces/${workspace.id}`),
    );
    expect(await pending(inviteePage)).toHaveLength(0);
    expect(
      (
        await inviteePage.request.get(
          `${apiUrl}/workspaces/${workspace.id}/members/me`,
        )
      ).ok(),
    ).toBe(true);
  }
});

import { expect, test, type Browser, type Page } from "@playwright/test";
import {
  confirmEmailOf,
  freshMember,
  registerPerson,
} from "./support/fresh-person";
import { apiRead, apiWrite } from "./support/real-session";
import {
  createOwnedWorkspace,
  invitationsOf,
  membersOf,
} from "./support/real-workspace";

/**
 * The one real plan limit of release 0.1.0 (UC-BIL-F01): a new account is on the Free plan, which holds ten member
 * seats. The owner and nine invited people take them; the eleventh person who accepts is refused with the seat
 * message in the pending-invitations menu and nothing is added; a person who already holds a seat in the account is
 * admitted to a second workspace at the limit; a repeated accept takes no second seat. The people are registered
 * through the public API of the stack, the same way each spec makes its own.
 */
test.setTimeout(420_000);
test.use({ viewport: { width: 1440, height: 900 } });

const MEMBER_SEATS = 10;

async function person(browser: Browser, label: string) {
  const context = await browser.newContext();
  const page = await context.newPage();
  const registered = await registerPerson(page, label);
  // An unconfirmed address cannot accept an invitation.
  await confirmEmailOf(page, registered.email, "2m");
  return { context, page, ...registered };
}

/** The owner invites `email` to the workspace and returns the pending invitation's id. */
async function invite(owner: Page, workspaceId: string, email: string) {
  const invited = await apiWrite(
    owner,
    "POST",
    `/workspaces/${workspaceId}/members`,
    { email, role: "Member" },
  );
  expect(invited.status, `invite ${email}`).toBe(201);
  const pending = (await invitationsOf(owner, workspaceId)).find(
    (row) =>
      row.email.toLowerCase() === email.toLowerCase() &&
      row.status === "Pending",
  );
  expect(pending, `pending invitation of ${email}`).toBeDefined();
  return pending!.id;
}

test("the Free plan holds ten member seats: the eleventh person is refused with the seat message and nothing is added, a person already seated in the account is admitted at the limit, and a repeated accept takes no second seat", async ({
  browser,
  page,
}) => {
  const owner = await freshMember(page, "seatowner");
  const workspaceId = owner.workspaceId;
  const seated: Awaited<ReturnType<typeof person>>[] = [];
  const cleanup: (() => Promise<void>)[] = [];

  try {
    // The owner holds one seat; nine invited people take the rest.
    for (let index = 1; index < MEMBER_SEATS; index += 1) {
      const member = await person(browser, `seat${index}`);
      cleanup.push(() => member.context.close());
      const invitationId = await invite(page, workspaceId, member.email);
      const accepted = await apiWrite(
        member.page,
        "POST",
        `/invitations/${invitationId}/accept`,
      );
      expect(accepted.status, `accept #${index}`).toBeLessThan(300);
      seated.push(member);
    }
    expect(await membersOf(page, workspaceId)).toHaveLength(MEMBER_SEATS);

    // The eleventh person is invited (the invitation is not a seat) and refused when they accept.
    const eleventhContext = await browser.newContext();
    cleanup.push(() => eleventhContext.close());
    const eleventhPage = await eleventhContext.newPage();
    const eleventh = await freshMember(eleventhPage, "seateleven");
    const refusedInvitation = await invite(page, workspaceId, eleventh.email);

    await eleventhPage.goto(`/workspaces/${eleventh.workspaceId}`);
    await eleventhPage
      .getByRole("button", { name: "Pending workspace invitations" })
      .click();
    const accept = eleventhPage.getByRole("button", {
      name: /^Accept the invitation to/,
    });
    await accept.click();
    await expect(
      eleventhPage.getByRole("alert").filter({ hasText: /member limit/ }),
    ).toContainText("member limit of this account's plan has been reached");
    // The invitation stays, so it can be accepted once a seat is free; no membership was made.
    await expect(accept).toBeVisible();
    expect(await membersOf(page, workspaceId)).toHaveLength(MEMBER_SEATS);
    expect(
      (await invitationsOf(page, workspaceId)).find(
        (row) => row.id === refusedInvitation,
      )?.status,
    ).toBe("Pending");

    // The same refusal on a retry through the API: a 4xx, never a 5xx, and still ten members.
    const retried = await apiWrite(
      eleventhPage,
      "POST",
      `/invitations/${refusedInvitation}/accept`,
    );
    expect(retried.status).toBeGreaterThanOrEqual(400);
    expect(retried.status).toBeLessThan(500);
    expect(await membersOf(page, workspaceId)).toHaveLength(MEMBER_SEATS);

    // A person who already holds a seat in the account joins a second workspace of it at the limit.
    const { accountId } = await apiRead<{ accountId: string }>(
      page,
      "/auth/bootstrap",
    );
    const second = await createOwnedWorkspace(page, "SeatSecond");
    expect(second.accountId).toBe(accountId);
    const veteran = seated[0]!;
    const secondInvitation = await invite(page, second.id, veteran.email);
    const admitted = await apiWrite(
      veteran.page,
      "POST",
      `/invitations/${secondInvitation}/accept`,
    );
    expect(admitted.status, "a seated person is admitted").toBeLessThan(300);
    expect((await membersOf(page, second.id)).map((m) => m.role)).toContain(
      "Member",
    );
    expect(await membersOf(page, second.id)).toHaveLength(2);

    // A repeated accept of an accepted invitation takes no second seat.
    const again = await apiWrite(
      veteran.page,
      "POST",
      `/invitations/${secondInvitation}/accept`,
    );
    expect(again.status).toBeLessThan(500);
    expect(await membersOf(page, second.id)).toHaveLength(2);
    expect(await membersOf(page, workspaceId)).toHaveLength(MEMBER_SEATS);
  } finally {
    for (const close of cleanup) await close();
  }
});

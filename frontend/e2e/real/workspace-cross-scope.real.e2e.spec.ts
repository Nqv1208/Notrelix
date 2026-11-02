import { expect, test } from "@playwright/test";
import { apiWrite, realUsers, signIn } from "./support/real-session";
import {
  createOwnedWorkspace,
  invitationsOf,
  joinAs,
  membersOf,
  uniqueEmail,
} from "./support/real-workspace";

test.setTimeout(180_000);

// UC-WSP-F02, G04, G05, G06 cross-tenant negatives: the ids of another workspace's invitation and member
// reached through this workspace's routes, and a signed-in stranger on this workspace's routes, answer
// like a missing target and change nothing.
test("another workspace's invitation and member are not reachable through this workspace's routes, and a stranger gets the answer a made-up workspace gets", async ({
  browser,
  page,
}) => {
  await signIn(page, realUsers.admin);
  const here = await createOwnedWorkspace(page, "Scope here");
  const there = await createOwnedWorkspace(page, "Scope there");

  const email = uniqueEmail("elsewhere");
  expect(
    (
      await apiWrite(page, "POST", `/workspaces/${there.id}/members`, {
        email,
        role: "Member",
      })
    ).status,
  ).toBe(201);
  const invitation = (await invitationsOf(page, there.id)).find(
    (row) => row.email === email,
  )!;
  const joined = await joinAs(
    browser,
    page,
    there.id,
    realUsers.workspaceAdmin,
    "Member",
  );
  const member = (await membersOf(page, there.id)).find(
    (row) => row.role === "Member",
  )!;

  // The owner of `here` aims `there`'s ids at `here`'s routes.
  const aimed = [
    await apiWrite(
      page,
      "PATCH",
      `/workspaces/${here.id}/invitations/${invitation.id}/role`,
      { newRole: "Guest", expectedVersion: invitation.version },
    ),
    await apiWrite(
      page,
      "POST",
      `/workspaces/${here.id}/invitations/${invitation.id}/resend`,
    ),
    await apiWrite(
      page,
      "DELETE",
      `/workspaces/${here.id}/invitations/${invitation.id}`,
    ),
    await apiWrite(
      page,
      "PATCH",
      `/workspaces/${here.id}/members/${member.userId}`,
      { role: "Guest", expectedVersion: member.version },
    ),
    await apiWrite(
      page,
      "DELETE",
      `/workspaces/${here.id}/members/${member.userId}?expectedVersion=${member.version}`,
    ),
  ];
  expect(aimed.map((answer) => answer.status)).toEqual([
    404, 404, 404, 404, 404,
  ]);
  expect(
    (await invitationsOf(page, there.id)).find(
      (row) => row.id === invitation.id,
    ),
  ).toEqual(invitation);
  expect(
    (await membersOf(page, there.id)).find(
      (row) => row.userId === member.userId,
    ),
  ).toEqual(member);

  // A signed-in stranger of `here` gets what a made-up workspace id gets, on the same writes.
  const strangerContext = await browser.newContext();
  const stranger = await strangerContext.newPage();
  await signIn(stranger, realUsers.guest);
  const madeUp = "00000000-0000-4000-8000-00000000f02a";
  const writes = (workspaceId: string) => [
    () =>
      apiWrite(
        stranger,
        "PATCH",
        `/workspaces/${workspaceId}/members/${member.userId}`,
        { role: "Guest", expectedVersion: 1 },
      ),
    () =>
      apiWrite(
        stranger,
        "PATCH",
        `/workspaces/${workspaceId}/invitations/${invitation.id}/role`,
        { newRole: "Guest", expectedVersion: 1 },
      ),
    () =>
      apiWrite(
        stranger,
        "POST",
        `/workspaces/${workspaceId}/invitations/${invitation.id}/resend`,
      ),
    () =>
      apiWrite(
        stranger,
        "DELETE",
        `/workspaces/${workspaceId}/invitations/${invitation.id}`,
      ),
  ];
  // One after the other: every CSRF read rotates the token, so concurrent writes would refuse each other.
  const real = [];
  for (const send of writes(here.id)) real.push(await send());
  const missing = [];
  for (const send of writes(madeUp)) missing.push(await send());
  expect(real.map((answer) => answer.status)).toEqual(
    missing.map((answer) => answer.status),
  );
  for (const answer of real) expect([403, 404]).toContain(answer.status);
  expect((await invitationsOf(page, here.id)).length).toBe(0);

  await joined.context.close();
  await strangerContext.close();
});

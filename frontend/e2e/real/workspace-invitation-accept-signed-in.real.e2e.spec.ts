import { expect, test } from "@playwright/test";
import { apiRead, apiWrite, realUsers, signIn } from "./support/real-session";
import { freshMember } from "./support/fresh-person";
import {
  createOwnedWorkspace,
  invitationTokenFor,
  invitationsOf,
  membersOf,
} from "./support/real-workspace";

test.setTimeout(180_000);

test("a signed-in invitee accepts from the link and lands in the workspace; another person's session and a made-up token get nothing", async ({
  browser,
  page,
}) => {
  await signIn(page, realUsers.admin);
  const workspace = await createOwnedWorkspace(page, "Accept signed in");

  // The invitee already has an account and a session in their own browser.
  const inviteeContext = await browser.newContext();
  const invitee = await inviteeContext.newPage();
  const person = await freshMember(invitee, "joiner");
  expect(
    (
      await apiWrite(page, "POST", `/workspaces/${workspace.id}/members`, {
        email: person.email,
        role: "Member",
      })
    ).status,
  ).toBe(201);
  const { token } = await invitationTokenFor(person.email);
  const pendingRow = () =>
    invitationsOf(page, workspace.id).then((rows) =>
      rows.find((row) => row.email === person.email),
    );

  // Rejection: another signed-in person holding the token is refused and changes nothing.
  const otherContext = await browser.newContext();
  const other = await otherContext.newPage();
  await signIn(other, realUsers.guest);
  const wrongPerson = await apiWrite(other, "POST", "/invitations/accept", {
    token,
  });
  expect(wrongPerson.status).toBe(400);
  const madeUp = await apiWrite(other, "POST", "/invitations/accept", {
    token: `${token.slice(0, -4)}AAAA`,
  });
  expect(madeUp.status).toBe(404);
  expect((await pendingRow())?.status).toBe("Pending");
  expect(await membersOf(page, workspace.id)).toHaveLength(1);

  // Main flow: the link opens the join page for the signed-in invitee; one press accepts it.
  await invitee.goto(`/invite#token=${encodeURIComponent(token)}`);
  await expect(
    invitee.getByText(new RegExp(workspace.name)).first(),
  ).toBeVisible();
  await invitee.getByRole("button", { name: /^Accept/ }).click();
  await expect(invitee).toHaveURL(new RegExp(`/workspaces/${workspace.id}`), {
    timeout: 30_000,
  });
  expect(
    await apiRead<{ role: string; status: string }>(
      invitee,
      `/workspaces/${workspace.id}/members/me`,
    ),
  ).toMatchObject({ role: "Member", status: "Active" });
  expect((await pendingRow())?.status).toBe("Accepted");
  expect(await membersOf(page, workspace.id)).toHaveLength(2);

  // The link is spent: pressing it again changes nothing.
  const again = await apiWrite(invitee, "POST", "/invitations/accept", {
    token,
  });
  expect(again.status).toBeGreaterThanOrEqual(400);
  expect(await membersOf(page, workspace.id)).toHaveLength(2);
  await otherContext.close();
  await inviteeContext.close();
});

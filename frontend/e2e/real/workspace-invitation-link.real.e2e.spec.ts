import { expect, test } from "@playwright/test";
import { apiRead, apiWrite, realUsers, signIn } from "./support/real-session";
import {
  createOwnedWorkspace,
  invitationTokenFor,
  invitationsOf,
  membersOf,
  uniqueEmail,
} from "./support/real-workspace";

test.setTimeout(180_000);

interface Preview {
  readonly state: string;
  readonly workspaceName: string;
  readonly role: string;
  readonly email: string | null;
  readonly hasAccount: boolean;
}

test("a person with no account opens the link from the e-mail, sees the invitation and joins in one step; a weak password, a reused link and a made-up link get nothing", async ({
  browser,
  page,
}) => {
  await signIn(page, realUsers.admin);
  const workspace = await createOwnedWorkspace(page, "Join by link");
  const email = uniqueEmail("newcomer");
  expect(
    (
      await apiWrite(page, "POST", `/workspaces/${workspace.id}/members`, {
        email,
        role: "Member",
      })
    ).status,
  ).toBe(201);
  const { token } = await invitationTokenFor(email);

  const anonymousContext = await browser.newContext();
  const anonymous = await anonymousContext.newPage();
  await anonymous.goto("/sign-in");
  const preview = (value: string) =>
    apiWrite<Preview>(anonymous, "POST", "/invitations/preview", {
      token: value,
    });

  // H01: the holder of the token reads the workspace, the role and the address, signed out.
  const read = await preview(token);
  expect(read.status).toBe(200);
  expect(read.data).toMatchObject({
    state: "valid",
    workspaceName: workspace.name,
    role: "Member",
    email,
    hasAccount: false,
  });

  // H01 rejection: a made-up token and a malformed one answer the same, and say nothing of any invitation.
  const unknown = await preview(`${token.slice(0, -4)}AAAA`);
  const malformed = await preview("not-a-token");
  expect(unknown.status).toBe(404);
  expect(malformed.status).toBe(404);
  const withoutTrace = (data: unknown) => ({
    ...(data as Record<string, unknown>),
    traceId: undefined,
  });
  expect(withoutTrace(unknown.data)).toEqual(withoutTrace(malformed.data));

  // The link of the e-mail opens the join page, not a dead end.
  await anonymous.goto(`/invite#token=${encodeURIComponent(token)}`);
  await expect(
    anonymous.getByText(new RegExp(workspace.name)).first(),
  ).toBeVisible();
  await expect(anonymous.getByLabel("Invited email")).toHaveValue(email);

  // H03 rejection: a password below the policy creates nothing.
  await anonymous.getByLabel("Full name").fill("Casey Newcomer");
  await anonymous.getByLabel("Create a password").fill("short");
  await anonymous.getByRole("button", { name: /Accept and create/ }).click();
  await expect(
    anonymous.getByText(/Use at least \d+ characters/),
  ).toBeVisible();
  expect(
    (await invitationsOf(page, workspace.id)).find((row) => row.email === email)
      ?.status,
  ).toBe("Pending");

  // H03 main flow: a valid name and password create the account, the membership, and land in the workspace.
  await anonymous
    .getByLabel("Create a password")
    .fill(`Pw-${crypto.randomUUID()}-Aa1!`);
  await anonymous.getByRole("button", { name: /Accept and create/ }).click();
  await expect(anonymous).toHaveURL(new RegExp(`/workspaces/${workspace.id}`), {
    timeout: 30_000,
  });
  const mine = await apiRead<{ role: string; status: string }>(
    anonymous,
    `/workspaces/${workspace.id}/members/me`,
  );
  expect(mine).toMatchObject({ role: "Member", status: "Active" });
  expect(
    (await invitationsOf(page, workspace.id)).find((row) => row.email === email)
      ?.status,
  ).toBe("Accepted");
  expect(await membersOf(page, workspace.id)).toHaveLength(2);

  // The link cannot be used twice: it reads as used and withholds the address.
  const reused = await preview(token);
  expect(reused.data).toMatchObject({ state: "used", email: null });
  await anonymousContext.close();
});

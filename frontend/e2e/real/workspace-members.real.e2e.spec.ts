import { expect, test } from "@playwright/test";
import {
  apiRead,
  apiWrite,
  findWorkspace,
  realUsers,
  signIn,
} from "./support/real-session";

interface Invitation {
  readonly email: string;
}

test("inviting a person who is already an active member is refused, says so in the dialog and sends nothing", async ({
  page,
}) => {
  // user4 is an Admin of Workspace 3 and may invite; its Owner is already an Active member.
  await signIn(page, realUsers.workspaceAdmin);
  const workspace = await findWorkspace(page, "Workspace 3");
  const member = realUsers.workspaceOwner.email;
  const invitationsOf = async () =>
    (
      await apiRead<Invitation[]>(
        page,
        `/workspaces/${workspace.id}/invitations`,
      )
    ).filter((invitation) => invitation.email === member);
  const before = (await invitationsOf()).length;

  // The producer answers 409 with its own code and creates nothing.
  const refused = await apiWrite<{ errorCode?: string }>(
    page,
    "POST",
    `/workspaces/${workspace.id}/members`,
    { email: member, role: "Member" },
  );
  expect(refused.status).toBe(409);
  expect(refused.data?.errorCode).toBe("workspaces.invitation.already-member");

  // The invite form shows the catalog copy next to the field and keeps what was typed.
  await page.goto(`/workspaces/${workspace.id}/members?tab=invitations`);
  const email = page.getByLabel("E-mail address");
  await email.fill(member);
  await page.getByRole("button", { name: "Send invitation" }).click();
  await expect(
    page.getByText(
      `${member} is already a member of this workspace, so no invitation was sent.`,
    ),
  ).toBeVisible();
  await expect(email).toHaveValue(member);

  expect((await invitationsOf()).length).toBe(before);
});

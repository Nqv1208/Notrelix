import { expect, test, type Page } from "@playwright/test";
import { apiRead, apiWrite, realUsers, signIn } from "./support/real-session";
import {
  createOwnedWorkspace,
  invitationTokenFor,
  invitationsOf,
  joinAs,
  uniqueEmail,
  anonymousApi,
} from "./support/real-workspace";

test.setTimeout(240_000);

const apiUrl = process.env.VITE_API_URL ?? "http://127.0.0.1:58000/api/v1";

interface Problem {
  readonly errorCode?: string;
}
interface Preview {
  readonly state: string;
  readonly workspaceName: string;
  readonly role: string;
  readonly email: string | null;
}
interface InvitationsPage {
  readonly items: readonly Record<string, unknown>[];
  readonly nextCursor: string | null;
}

async function rowFor(page: Page, workspaceId: string, email: string) {
  const row = (await invitationsOf(page, workspaceId)).find(
    (invitation) => invitation.email === email,
  );
  expect(row, `invitation of ${email}`).toBeDefined();
  return row!;
}

/** The owner's workspace plus a Member (user4) and a Guest (user6) who belong to it. */
async function scene(
  browser: Parameters<typeof joinAs>[0],
  page: Page,
  label: string,
) {
  await signIn(page, realUsers.admin);
  const workspace = await createOwnedWorkspace(page, label);
  const member = await joinAs(
    browser,
    page,
    workspace.id,
    realUsers.workspaceAdmin,
    "Member",
  );
  const guest = await joinAs(
    browser,
    page,
    workspace.id,
    realUsers.workspaceGuest,
    "Guest",
  );
  return { workspace, member, guest };
}

test("an Owner invites by e-mail, lists and re-roles the invitations; a retry, a duplicate, an Admin role, a Member and a stale version each change nothing", async ({
  browser,
  page,
}) => {
  const { workspace, member, guest } = await scene(browser, page, "Invite");
  const invite = (email: string, role: string, key?: string) =>
    apiWrite<string>(
      page,
      "POST",
      `/workspaces/${workspace.id}/members`,
      { email, role },
      key,
    );

  // G01 main flow: the form sends one invitation, the list shows it, one e-mail is written.
  const email = uniqueEmail("invitee");
  await page.goto(`/workspaces/${workspace.id}/members?tab=invitations`);
  await page.getByLabel("E-mail address").fill(email);
  await page.getByRole("button", { name: "Send invitation" }).click();
  await expect(page.getByText(`Invitation sent to ${email}.`)).toBeVisible();
  const pending = await rowFor(page, workspace.id, email);
  expect(pending).toMatchObject({ role: "Member", status: "Pending" });
  await expect(
    page.getByRole("cell", { name: email, exact: true }),
  ).toBeVisible();
  const first = await invitationTokenFor(email);
  expect(first.count).toBe(1);

  // G01 rejections: a pending one for the address, an Admin role and a Member as the inviter.
  const duplicate = await invite(email, "Member");
  expect(duplicate.status).toBe(409);
  expect((duplicate.data as Problem | null)?.errorCode).toBe(
    "workspaces.invitation.pending-exists",
  );
  const asAdmin = uniqueEmail("as-admin");
  expect((await invite(asAdmin, "Admin")).status).toBe(400);
  expect(
    (await invitationsOf(page, workspace.id)).some(
      (row) => row.email === asAdmin,
    ),
  ).toBe(false);
  const byMember = await apiWrite(
    member.page,
    "POST",
    `/workspaces/${workspace.id}/members`,
    { email: uniqueEmail("by-member"), role: "Member" },
  );
  expect(byMember.status).toBe(403);

  // G01 retry: the same key returns the first invitation and writes no second e-mail.
  const retried = uniqueEmail("retry");
  const key = crypto.randomUUID();
  const once = await invite(retried, "Member", key);
  const twice = await invite(retried, "Member", key);
  expect(once.status).toBe(201);
  expect(twice.status).toBeLessThan(300);
  expect(
    (await invitationsOf(page, workspace.id)).filter(
      (row) => row.email === retried,
    ),
  ).toHaveLength(1);
  const sent = await invitationTokenFor(retried);
  await page.waitForTimeout(4000);
  expect((await invitationTokenFor(retried)).count).toBe(sent.count);
  expect(sent.count).toBe(1);

  // G03: rows carry no token; invited addresses are for the people who may invite, so a Member and a Guest are
  // refused; pages cover each row once.
  const all = await invitationsOf(page, workspace.id);
  expect(all.length).toBeGreaterThanOrEqual(2);
  for (const row of all) {
    expect(Object.keys(row).join(",")).not.toMatch(/token/i);
  }
  expect(
    (
      await member.page.request.get(
        `${apiUrl}/workspaces/${workspace.id}/invitations`,
      )
    ).status(),
  ).toBe(403);
  expect(
    (
      await guest.page.request.get(
        `${apiUrl}/workspaces/${workspace.id}/invitations`,
      )
    ).status(),
  ).toBe(403);
  const seen: unknown[] = [];
  let cursor: string | null = null;
  let pages = 0;
  do {
    const query: string = `limit=1${cursor ? `&cursor=${encodeURIComponent(cursor)}` : ""}`;
    const read: InvitationsPage = await apiRead<InvitationsPage>(
      page,
      `/workspaces/${workspace.id}/invitations?${query}`,
    );
    seen.push(...read.items.map((row) => row.id));
    cursor = read.nextCursor;
    pages += 1;
  } while (cursor);
  expect(pages).toBe(all.length);
  expect(new Set(seen).size).toBe(all.length);
  for (const bad of ["limit=0", "limit=201", "cursor=not-a-cursor"]) {
    expect(
      (
        await page.request.get(
          `${apiUrl}/workspaces/${workspace.id}/invitations?${bad}`,
        )
      ).status(),
    ).toBe(400);
  }

  // G04 main flow: the role select on the row offers Guest.
  await page.reload();
  await page
    .getByRole("combobox", {
      name: `Role offered in the invitation to ${email}`,
    })
    .click();
  await page.getByRole("option", { name: "Guest" }).click();
  await expect(
    page.getByText(`The invitation to ${email} now offers Guest.`),
  ).toBeVisible();
  const guestOffer = await rowFor(page, workspace.id, email);
  expect(guestOffer.role).toBe("Guest");
  expect(guestOffer.version).toBeGreaterThan(pending.version);

  // G04 rejections: a stale version, a raise to Admin; the same role is a no-op.
  const stale = await apiWrite(
    page,
    "PATCH",
    `/workspaces/${workspace.id}/invitations/${pending.id}/role`,
    { newRole: "Member", expectedVersion: pending.version },
  );
  expect(stale.status).toBe(412);
  const raise = await apiWrite(
    page,
    "PATCH",
    `/workspaces/${workspace.id}/invitations/${pending.id}/role`,
    { newRole: "Admin", expectedVersion: guestOffer.version },
  );
  expect(raise.status).toBe(400);
  const same = await apiWrite(
    page,
    "PATCH",
    `/workspaces/${workspace.id}/invitations/${pending.id}/role`,
    { newRole: "Guest", expectedVersion: guestOffer.version },
  );
  expect(same.status).toBeLessThan(300);
  expect(await rowFor(page, workspace.id, email)).toMatchObject({
    role: "Guest",
    version: guestOffer.version,
  });
});

test("an Owner resends then revokes an invitation; the resend waits a minute and replaces the link, the revoked link is dead, and an answered invitation is left as it is", async ({
  browser,
  page,
}) => {
  const { workspace, member } = await scene(browser, page, "Resend revoke");
  const link = await anonymousApi();
  const preview = (token: string) =>
    link.post<Preview>("/invitations/preview", { token });

  const email = uniqueEmail("resend");
  expect(
    (
      await apiWrite(page, "POST", `/workspaces/${workspace.id}/members`, {
        email,
        role: "Member",
      })
    ).status,
  ).toBe(201);
  const original = await invitationTokenFor(email);
  expect((await preview(original.token)).data?.state).toBe("valid");
  const row = await rowFor(page, workspace.id, email);

  // G05 rejection: held for a minute after the last send; nothing changes, no e-mail.
  const early = await apiWrite(
    page,
    "POST",
    `/workspaces/${workspace.id}/invitations/${row.id}/resend`,
  );
  expect(early.status).toBeGreaterThanOrEqual(400);
  expect(early.status).toBeLessThan(500);
  expect((await invitationTokenFor(email)).count).toBe(1);
  expect((await rowFor(page, workspace.id, email)).version).toBe(row.version);

  // G05 main flow: after the minute the row's button sends a new link; the old one stops working.
  await page.waitForTimeout(62_000);
  await page.goto(`/workspaces/${workspace.id}/members?tab=invitations`);
  await page
    .getByRole("button", { name: `Resend invitation to ${email}` })
    .click();
  await expect(
    page.getByText(`Invitation sent again to ${email}.`),
  ).toBeVisible();
  const renewed = await invitationTokenFor(email, 1);
  expect(renewed.token).not.toBe(original.token);
  expect((await preview(renewed.token)).data?.state).toBe("valid");
  const old = await preview(original.token);
  expect(old.data?.state === "valid").toBe(false);

  // G06 rejections: a Member cannot revoke; an accepted invitation cannot be revoked.
  const current = await rowFor(page, workspace.id, email);
  expect(
    (
      await apiWrite(
        member.page,
        "DELETE",
        `/workspaces/${workspace.id}/invitations/${current.id}`,
      )
    ).status,
  ).toBe(403);
  const accepted = await joinAs(
    browser,
    page,
    workspace.id,
    realUsers.guest,
    "Member",
  );
  // An answered invitation is not revoked: the call changes nothing (the person is removed as a member instead).
  const acceptedBefore = await rowFor(
    page,
    workspace.id,
    realUsers.guest.email,
  );
  expect(acceptedBefore.status).toBe("Accepted");
  const refused = await apiWrite(
    page,
    "DELETE",
    `/workspaces/${workspace.id}/invitations/${accepted.invitationId}`,
  );
  expect(refused.status).toBeLessThan(500);
  expect(await rowFor(page, workspace.id, realUsers.guest.email)).toMatchObject(
    { status: "Accepted", version: acceptedBefore.version },
  );
  const joined = await apiRead<{ status: string }>(
    accepted.page,
    `/workspaces/${workspace.id}/members/me`,
  );
  expect(joined.status).toBe("Active");

  // G06 main flow: the trash button asks, then the row is gone and the link answers used.
  await page.reload();
  await page
    .getByRole("button", { name: `Revoke invitation to ${email}` })
    .click();
  await page
    .getByRole("alertdialog", { name: `Revoke the invitation to ${email}?` })
    .getByRole("button", { name: "Revoke invitation" })
    .click();
  await expect(
    page.getByText(`The invitation to ${email} was revoked.`),
  ).toBeVisible();
  await expect(
    page.getByRole("cell", { name: email, exact: true }),
  ).toHaveCount(0);
  expect((await rowFor(page, workspace.id, email)).status).toBe("Revoked");
  const dead = await preview(renewed.token);
  expect(dead.data?.state).toBe("used");
  expect(dead.data?.email).toBeNull();
  const register = await link.post("/invitations/accept-with-registration", {
    token: renewed.token,
    name: "Late Arrival",
    password: `Pw-${crypto.randomUUID()}`,
  });
  expect(register.status).toBeGreaterThanOrEqual(400);
  expect(
    (
      await apiWrite(
        page,
        "POST",
        `/workspaces/${workspace.id}/invitations/${current.id}/resend`,
      )
    ).status,
  ).toBeGreaterThanOrEqual(400);
  await link.dispose();
});

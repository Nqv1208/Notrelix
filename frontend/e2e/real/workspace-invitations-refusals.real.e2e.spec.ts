import { expect, test, type Browser, type Page } from "@playwright/test";
import { openTenantInConsole } from "./support/operator-console";
import {
  confirmEmailOf,
  freshMember,
  registerPerson,
} from "./support/fresh-person";
import {
  apiRead,
  apiWrite,
  findWorkspace,
  realUsers,
  signIn,
} from "./support/real-session";
import {
  createOwnedWorkspace,
  invitationsOf,
  membersOf,
} from "./support/real-workspace";

test.setTimeout(360_000);

const apiUrl = process.env.VITE_API_URL ?? "http://127.0.0.1:58000/api/v1";

interface Pending {
  readonly id: string;
  readonly workspaceName?: string;
}

async function pendingOf(page: Page): Promise<readonly Pending[]> {
  const read = await apiRead<
    { items?: readonly Pending[] } | readonly Pending[]
  >(page, "/invitations/pending");
  return Array.isArray(read)
    ? read
    : ((read as { items?: readonly Pending[] }).items ?? []);
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

/** A person with a confirmed address and a workspace of their own, in their own browser context. */
async function newPerson(browser: Browser, label: string) {
  const context = await browser.newContext();
  const page = await context.newPage();
  const person = await freshMember(page, label);
  return { context, page, email: person.email };
}

async function invite(owner: Page, workspaceId: string, email: string) {
  const sent = await apiWrite(
    owner,
    "POST",
    `/workspaces/${workspaceId}/members`,
    {
      email,
      role: "Member",
    },
  );
  expect(sent.status, `invite ${email}`).toBe(201);
  const row = (await invitationsOf(owner, workspaceId)).find(
    (invitation) =>
      invitation.email === email && invitation.status === "Pending",
  );
  expect(row, `pending invitation of ${email}`).toBeDefined();
  return row!.id;
}

test("an invitation is accepted once: the flyout lands in the workspace, a repeat answers NotFound and the roster holds the person once; an unconfirmed address sees nothing and cannot answer", async ({
  browser,
  page,
}) => {
  await signIn(page, realUsers.admin);
  const workspace = await createOwnedWorkspace(page, "Invite refusals");
  const invitee = await newPerson(browser, "once");
  const invitationId = await invite(page, workspace.id, invitee.email);

  // Main flow in the browser: Accept in the flyout leads into the workspace.
  await openInvites(invitee.page);
  await invitee.page
    .getByRole("button", { name: /^Accept the invitation to / })
    .click();
  await expect(invitee.page).toHaveURL(
    new RegExp(`/workspaces/${workspace.id}`),
  );
  expect(await pendingOf(invitee.page)).toHaveLength(0);

  // Repeat: the same answer again is refused as a missing invitation and adds no second membership.
  const again = await apiWrite(
    invitee.page,
    "POST",
    `/invitations/${invitationId}/accept`,
  );
  expect(again.status).toBe(404);
  const decline = await apiWrite(
    invitee.page,
    "POST",
    `/invitations/${invitationId}/decline`,
  );
  expect(decline.status).toBe(404);
  const roster = await membersOf(page, workspace.id);
  const mine = await apiRead<{ userId: string }>(
    invitee.page,
    `/workspaces/${workspace.id}/members/me`,
  );
  expect(roster.filter((member) => member.userId === mine.userId)).toHaveLength(
    1,
  );

  // An address nobody confirmed: the list is empty, and accepting or declining is refused the same way
  // for this invitation as for an id that never existed (the pipeline asks for a confirmed address first).
  const unconfirmedContext = await browser.newContext();
  const unconfirmedPage = await unconfirmedContext.newPage();
  const stranger = await registerPerson(unconfirmedPage, "unconfirmed");
  const waitingId = await invite(page, workspace.id, stranger.email);
  expect(await pendingOf(unconfirmedPage)).toHaveLength(0);
  for (const verb of ["accept", "decline"]) {
    const real = await apiWrite(
      unconfirmedPage,
      "POST",
      `/invitations/${waitingId}/${verb}`,
    );
    const made = await apiWrite(
      unconfirmedPage,
      "POST",
      `/invitations/${crypto.randomUUID()}/${verb}`,
    );
    expect(real.status, `${verb} before confirming`).toBeGreaterThanOrEqual(
      400,
    );
    expect(
      real.status,
      `${verb}: an existing id answers like a made-up one`,
    ).toBe(made.status);
  }
  // The invitation is untouched: still Pending for its owner.
  expect(
    (await invitationsOf(page, workspace.id)).find(
      (row) => row.id === waitingId,
    )?.status,
  ).toBe("Pending");
  await confirmEmailOf(unconfirmedPage, stranger.email);
  expect(await pendingOf(unconfirmedPage)).toHaveLength(1);

  await invitee.context.close();
  await unconfirmedContext.close();
});

test("when the plan has no free seat, accepting in the flyout is refused with the plan-limit reason and admits nobody", async ({
  browser,
  page,
}) => {
  await signIn(page, realUsers.admin);
  const workspace = await createOwnedWorkspace(page, "Invite seats");
  const operator = await (await browser.newContext()).newPage();
  const people: Awaited<ReturnType<typeof newPerson>>[] = [];
  let planChanged = false;
  try {
    // The Free plan holds 10 members; read how many of them the account already uses.
    await openTenantInConsole(operator);
    await operator
      .getByLabel("Move to plan revision")
      .selectOption({ label: "Free r1" });
    await operator
      .getByRole("button", { name: "Change plan", exact: true })
      .click();
    const dialog = operator.getByRole("dialog");
    await dialog.getByLabel(/Reason/).fill("real e2e seat refusal");
    await dialog.getByRole("button", { name: "Change plan" }).click();
    await expect(dialog).toBeHidden();
    planChanged = true;

    const limitsPage = await findWorkspace(page, "Workspace 1");
    await page.goto(`/workspaces/${limitsPage.id}/billing`);
    const used = page
      .getByRole("list", { name: "Capabilities granted by billing" })
      .getByText(/^\d+ of 10 used$/);
    await expect(used).toBeVisible();
    const taken = Number(
      /^(\d+) of 10 used$/.exec((await used.textContent()) ?? "")![1],
    );

    // Fill the free seats with people who accept through the API, then one more asks for a seat.
    for (let seat = taken; seat < 10; seat += 1) {
      const person = await newPerson(browser, `seat${seat}`);
      people.push(person);
      const id = await invite(page, workspace.id, person.email);
      const accepted = await apiWrite(
        person.page,
        "POST",
        `/invitations/${id}/accept`,
      );
      expect(accepted.status, `fill seat ${seat}`).toBeLessThan(300);
    }
    const last = await newPerson(browser, "lastseat");
    people.push(last);
    const invitationId = await invite(page, workspace.id, last.email);

    await openInvites(last.page);
    await last.page
      .getByRole("button", { name: /^Accept the invitation to / })
      .click();
    await expect(
      last.page.getByRole("alert").filter({ hasText: /member limit/i }),
    ).toBeVisible();
    // Nobody was admitted: the invitation still waits and the person has no membership.
    expect(
      (await invitationsOf(page, workspace.id)).find(
        (row) => row.id === invitationId,
      )?.status,
    ).toBe("Pending");
    expect(
      (
        await last.page.request.get(
          `${apiUrl}/workspaces/${workspace.id}/members/me`,
        )
      ).status(),
    ).toBeGreaterThanOrEqual(400);
  } finally {
    if (planChanged) {
      await operator
        .getByLabel("Move to plan revision")
        .selectOption({ label: "Pro r1" });
      await operator
        .getByRole("button", { name: "Change plan", exact: true })
        .click();
      const dialog = operator.getByRole("dialog");
      await dialog.getByLabel(/Reason/).fill("real e2e seat refusal restored");
      await dialog.getByRole("button", { name: "Change plan" }).click();
      await expect(dialog).toBeHidden();
    }
    for (const person of people) await person.context.close();
    await operator.context().close();
  }
});

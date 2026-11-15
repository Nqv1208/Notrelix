import { expect, test, type Page } from "@playwright/test";
import { apiRead, apiWrite, realUsers, signIn } from "./support/real-session";
import {
  createOwnedWorkspace,
  invitationsOf,
  joinAs,
  uniqueEmail,
} from "./support/real-workspace";

test.setTimeout(300_000);

interface Paged<T> {
  readonly items: readonly T[];
}
interface WorkspaceRow {
  readonly id: string;
  readonly name: string;
  readonly slug: string;
}
interface SpaceRow {
  readonly name: string;
  readonly visibility: string;
}
interface CardRow {
  readonly visible: boolean;
  readonly dismissed: boolean;
  readonly items: readonly { key: string; done: boolean }[];
}
interface DraftRead {
  readonly draft: {
    readonly step: string;
    readonly workspaceId: string | null;
  } | null;
}
interface SlugCheck {
  readonly status: "free" | "taken" | "invalid";
  readonly suggestion: string | null;
  readonly reason: string | null;
}

const draftOf = (page: Page) =>
  apiRead<DraftRead>(page, "/workspaces/onboarding-draft");
const cardOf = (page: Page, workspaceId: string) =>
  apiRead<CardRow>(page, `/workspaces/${workspaceId}/getting-started`);
const slugCheck = (page: Page, accountId: string, slug: string) =>
  apiRead<SlugCheck>(
    page,
    `/accounts/${accountId}/workspaces/slug-check?slug=${encodeURIComponent(slug)}`,
  );

test("a new company is set up in four steps: a taken or reserved address is refused, the draft resumes after Save and exit, the workspace, departments, invitation and board exist, and the card on Home is dismissed for this person only", async ({
  browser,
  page,
}) => {
  // The seeded admin: confirmed, and allowed to create workspaces in its Account.
  const person = realUsers.admin;
  await signIn(page, person);
  const { accountId } = await apiRead<{ accountId: string }>(
    page,
    "/auth/bootstrap",
  );
  // The Account already holds an address: the check is unique inside the Account.
  const decoy = await createOwnedWorkspace(page, "decoy");
  const decoySlug = (
    await apiRead<WorkspaceRow>(page, `/workspaces/${decoy.id}`)
  ).slug;
  const stamp = `${Date.now()}`.slice(-9);
  const company = `Onboard Co ${stamp}`;
  const address = `onboard-${stamp}`;

  // M03 on the producer: free, taken with the next free address, and invalid with a rule code.
  expect((await slugCheck(page, accountId, address)).status).toBe("free");
  const taken = await slugCheck(page, accountId, decoySlug);
  expect(taken.status).toBe("taken");
  expect(taken.suggestion).toBe(`${decoySlug}-2`);
  const reserved = await slugCheck(page, accountId, "admin");
  expect([reserved.status, reserved.reason]).toEqual([
    "invalid",
    "Workspaces_Workspace_SlugReserved",
  ]);
  // A name of 40 characters is a valid address (3 to 63), as the producer rules.
  const long = "a".repeat(40);
  expect((await slugCheck(page, accountId, long)).status).toBe("free");

  // M03 and M04 step 1 on the page: a taken address blocks Continue and offers the next one.
  await page.goto("/onboarding");
  await page.getByLabel("Company name").fill(company);
  const slugField = page.getByLabel("Workspace address");
  await slugField.fill(decoySlug);
  await expect(page.getByText(`${decoySlug} is taken`)).toBeVisible();
  await expect(
    page.getByRole("button", { name: "Continue", exact: true }),
  ).toHaveAttribute("aria-disabled", "true");
  // A reserved word is refused with the invalid line.
  await slugField.fill("admin");
  await expect(page.getByText(/^Use 3 to 63 lowercase letters/)).toBeVisible();
  // A 40-character address is accepted by the page as well.
  await slugField.fill(long);
  await expect(
    page.getByText(`${long}.notrelix.vn is available`),
  ).toBeVisible();
  await slugField.fill(decoySlug);
  await page.getByRole("button", { name: `Use ${decoySlug}-2` }).click();
  await expect(
    page.getByText(`${decoySlug}-2.notrelix.vn is available`),
  ).toBeVisible();
  await slugField.fill(address);
  await expect(
    page.getByText(`${address}.notrelix.vn is available`),
  ).toBeVisible();
  await page.getByRole("button", { name: "Continue", exact: true }).click();
  await expect(
    page.getByRole("heading", { level: 1, name: "Your departments" }),
  ).toBeVisible();
  const workspaces = await apiRead<WorkspaceRow[]>(page, "/workspaces");
  const made = workspaces.find((row) => row.slug === address);
  expect(made?.name, "the workspace of step 1").toBe(company);
  const workspaceId = made!.id;

  // M01: Save and exit keeps the draft and signs out; the next sign-in resumes at the same step.
  await page.getByRole("button", { name: "Save and exit" }).click();
  await expect(page).toHaveURL(/\/sign-in/);
  await signIn(page, person);
  expect((await draftOf(page)).draft).toMatchObject({
    step: "departments",
    workspaceId,
  });
  await page.goto("/onboarding");
  await expect(
    page.getByRole("heading", { level: 1, name: "Your departments" }),
  ).toBeVisible();

  // Step 2: the suggested departments are created once, Private.
  await page.getByRole("button", { name: /^Create \d+ departments?$/ }).click();
  const invitee = uniqueEmail("onb-invitee");
  await expect(
    page.getByRole("textbox", { name: /^Email \d/ }).first(),
  ).toBeVisible();
  const departments = (
    await apiRead<Paged<SpaceRow>>(page, `/workspaces/${workspaceId}/spaces`)
  ).items.filter((space) => space.name !== undefined);
  expect(departments.length).toBeGreaterThan(0);
  expect(new Set(departments.map((row) => row.visibility))).toEqual(
    new Set(["Private"]),
  );

  // Step 3: one invitation goes out.
  await page
    .getByRole("textbox", { name: /^Email \d/ })
    .first()
    .fill(invitee);
  await page.getByRole("button", { name: /^Send 1 invitation$/ }).click();
  await expect(page.getByLabel("Board name")).toBeVisible();
  const pending = (await invitationsOf(page, workspaceId)).filter(
    (row) => row.email === invitee,
  );
  expect(pending.map((row) => row.status)).toEqual(["Pending"]);

  // Step 4: the first board; the draft is cleared and the last page opens.
  await page.getByLabel("Board name").fill(`Launch ${stamp}`);
  await page.getByRole("button", { name: "Create the board" }).click();
  await expect(page.getByRole("heading", { level: 1 })).toContainText(
    "is ready",
  );
  expect((await draftOf(page)).draft).toBeNull();
  const boards = await apiRead<{ title: string }[]>(
    page,
    `/workspaces/${workspaceId}/boards`,
  );
  expect(boards.map((board) => board.title)).toContain(`Launch ${stamp}`);

  // M06: Home shows the card with its two derived items; the invitation is done, the first item is not.
  await page.getByRole("button", { name: "Go to Home" }).click();
  await expect(page).toHaveURL(new RegExp(`/workspaces/${workspaceId}`));
  const card = page.getByRole("region", { name: /Get started with/ });
  await expect(card).toBeVisible();
  const before = await cardOf(page, workspaceId);
  expect(before.visible).toBe(true);
  expect(before.items.map((item) => [item.key, item.done])).toEqual([
    ["first-item", false],
    ["invite-more", true],
  ]);

  // M06 rejection: a person outside the workspace is told it does not exist.
  const outsiderContext = await browser.newContext();
  const outsider = await outsiderContext.newPage();
  await signIn(outsider, realUsers.guest);
  const refused = await outsider.request.get(
    `${process.env.VITE_API_URL ?? "http://127.0.0.1:58000/api/v1"}/workspaces/${workspaceId}/getting-started`,
  );
  expect(refused.status()).toBe(404);
  await outsiderContext.close();

  // M07: Hide the card; it stays hidden after a reload, a repeat changes nothing, and it is per person.
  await card.getByRole("button", { name: "Hide this list" }).click();
  await expect(card).toBeHidden();
  const after = await cardOf(page, workspaceId);
  expect([after.visible, after.dismissed]).toEqual([false, true]);
  await page.reload();
  await expect(page.getByRole("heading", { level: 1 })).toBeVisible();
  await expect(card).toBeHidden();
  const again = await apiWrite(
    page,
    "POST",
    `/workspaces/${workspaceId}/getting-started/dismiss`,
  );
  expect(again.status, "dismissing again is a no-op").toBe(204);

  const member = await joinAs(
    browser,
    page,
    workspaceId,
    realUsers.workspaceAdmin,
    "Member",
  );
  const theirs = await cardOf(member.page, workspaceId);
  expect(
    [theirs.visible, theirs.dismissed],
    "the dismissal is the owner's only",
  ).toEqual([true, false]);
  await member.context.close();
});

test("an invited person joins, sets a name and the mail choice, and the profile and the notification matrix hold them; an empty name is refused", async ({
  browser,
  page,
}) => {
  await signIn(page, realUsers.admin);
  const workspace = await createOwnedWorkspace(page, "Join page");
  const joined = await joinAs(
    browser,
    page,
    workspace.id,
    realUsers.workspaceOwner,
    "Member",
  );
  const guest = joined.page;
  const original = (await apiRead<{ name: string }>(guest, "/auth/me")).name;

  await guest.goto(`/welcome/${workspace.id}`);
  await expect(guest.getByRole("heading", { level: 1 })).toContainText(
    workspace.name,
  );
  const name = guest.getByLabel("Display name");
  await name.fill("");
  await guest.getByRole("button", { name: "Start", exact: true }).click();
  await expect(guest.getByRole("alert")).toBeVisible();
  await expect(guest).toHaveURL(/\/welcome\//);

  type Matrix = {
    version: number;
    rows: { type: string; email: boolean }[];
  };
  const digestOf = (m: Matrix) =>
    m.rows.find((row) => row.type === "digest")!.email;
  const start = await apiRead<Matrix>(
    guest,
    "/profile/notification-preferences",
  );
  const choice = (digest: boolean) =>
    digest ? "Daily summary" : "Only when someone mentions me";

  const newName = `Joiner ${Date.now()}`.slice(0, 40);
  await name.fill(newName);
  // Pick the choice the person does not have yet, so the save changes something.
  await guest.getByRole("radio", { name: choice(!digestOf(start)) }).click();
  await guest.getByRole("button", { name: "Start", exact: true }).click();
  await expect(guest).not.toHaveURL(/\/welcome\//);

  const me = await apiRead<{ name: string }>(guest, "/auth/me");
  expect(me.name).toBe(newName);
  const matrix = await apiRead<Matrix>(
    guest,
    "/profile/notification-preferences",
  );
  expect(digestOf(matrix)).toBe(!digestOf(start));
  expect(matrix.version).toBeGreaterThan(start.version);
  expect(matrix.rows.find((row) => row.type === "mention")?.email).toBe(true);

  // Back to what it was: a second save carries the version the first one made.
  await guest.goto(`/welcome/${workspace.id}`);
  await guest.getByRole("radio", { name: choice(digestOf(start)) }).click();
  await guest.getByRole("button", { name: "Start", exact: true }).click();
  await expect(guest).not.toHaveURL(/\/welcome\//);
  const second = await apiRead<Matrix>(
    guest,
    "/profile/notification-preferences",
  );
  expect(second.version).toBeGreaterThan(matrix.version);
  expect(digestOf(second)).toBe(digestOf(start));
  // The seeded account is shared with other specs: give it its name back.
  const restored = await apiWrite(guest, "PATCH", "/profile", {
    name: original,
  });
  expect(restored.status).toBeLessThan(300);
  await joined.context.close();
});

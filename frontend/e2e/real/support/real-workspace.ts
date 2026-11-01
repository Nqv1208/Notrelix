import { execFileSync } from "node:child_process";
import { expect, type Browser, type Page } from "@playwright/test";
import { apiRead, apiWrite, realUsers, signIn } from "./real-session";

const backendContainer = "notrelix-fe-real-e2e-backend";

export type RealUser = (typeof realUsers)[keyof typeof realUsers];

export interface InvitationRow {
  readonly id: string;
  readonly email: string;
  readonly role: string;
  readonly status: string;
  readonly version: number;
}
export interface MemberRow {
  readonly userId: string;
  readonly name: string;
  readonly role: string;
  readonly status: string;
  readonly version: number;
}
interface Paged<T> {
  readonly items: readonly T[];
  readonly nextCursor: string | null;
}

/** A workspace the signed-in person owns, created through the API under a name no other spec uses. */
export async function createOwnedWorkspace(page: Page, label: string) {
  const { accountId } = await apiRead<{ accountId: string }>(
    page,
    "/auth/bootstrap",
  );
  const stamp = `${Date.now()}${Math.floor(Math.random() * 1000)}`;
  const name = `${label} ${stamp}`;
  const created = await apiWrite<string>(
    page,
    "POST",
    `/accounts/${accountId}/workspaces`,
    {
      name,
      slug: `${label.toLowerCase().replace(/\W+/g, "-")}-${stamp}`,
      size: "1-10",
      language: "en",
    },
  );
  expect(created.status, "create workspace").toBe(201);
  return { id: created.data as string, name, accountId };
}

export async function membersOf(page: Page, workspaceId: string) {
  return (
    await apiRead<Paged<MemberRow>>(page, `/workspaces/${workspaceId}/members`)
  ).items;
}

export async function invitationsOf(page: Page, workspaceId: string) {
  return (
    await apiRead<Paged<InvitationRow>>(
      page,
      `/workspaces/${workspaceId}/invitations`,
    )
  ).items;
}

/**
 * The owner invites `user`, who answers from their own session: the
 * invitation is the only way into a workspace, so this is the scene every
 * member spec starts from. Returns the invitee's open page.
 */
export async function joinAs(
  browser: Browser,
  ownerPage: Page,
  workspaceId: string,
  user: RealUser,
  role: "Member" | "Guest",
) {
  const invited = await apiWrite(
    ownerPage,
    "POST",
    `/workspaces/${workspaceId}/members`,
    { email: user.email, role },
  );
  expect(invited.status, `invite ${user.email}`).toBe(201);
  const context = await browser.newContext();
  const page = await context.newPage();
  await signIn(page, user);
  const invitation = (await invitationsOf(ownerPage, workspaceId)).find(
    (row) => row.email === user.email && row.status === "Pending",
  );
  expect(invitation, `pending invitation of ${user.email}`).toBeDefined();
  const accepted = await apiWrite(
    page,
    "POST",
    `/invitations/${invitation!.id}/accept`,
  );
  expect(accepted.status, `accept as ${user.email}`).toBeLessThan(300);
  return { context, page, invitationId: invitation!.id };
}

/** The address as the development mail log writes it. */
function masked(email: string) {
  const at = email.indexOf("@");
  return `${email[0]}***${email.slice(at - 1)}`;
}

/**
 * The raw token of the newest invitation e-mail to `email`: no provider runs on
 * a developer machine, so the development backend logs the message text.
 * `after` skips messages already read (a resend writes a second one).
 */
export async function invitationTokenFor(
  email: string,
  after = 0,
): Promise<{ token: string; count: number }> {
  const escaped = masked(email).replace(/[.*+?^${}()|[\]\\]/g, "\\$&");
  const pattern = new RegExp(
    `Development email to ${escaped}: [\\s\\S]{0,1500}?/invite#token=([^\\s"<>]+)`,
    "g",
  );
  for (let attempt = 0; attempt < 60; attempt += 1) {
    const logs = execFileSync("docker", ["logs", backendContainer], {
      encoding: "utf8",
      maxBuffer: 128 * 1024 * 1024,
    });
    const found = [...logs.matchAll(pattern)];
    if (found.length > after) {
      return {
        token: decodeURIComponent(found[found.length - 1]![1]!),
        count: found.length,
      };
    }
    await new Promise((resolve) => setTimeout(resolve, 1000));
  }
  throw new Error(`no invitation e-mail was logged for ${email}`);
}

export function uniqueEmail(label: string) {
  return `${label}-${Date.now()}${Math.floor(Math.random() * 1000)}@example.com`;
}

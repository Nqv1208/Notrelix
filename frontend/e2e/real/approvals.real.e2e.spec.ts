import { expect, test } from "@playwright/test";
import {
  apiRead,
  apiWrite,
  findWorkspace,
  openBoard,
  realUsers,
  signIn,
} from "./support/real-session";

test.use({ viewport: { width: 1440, height: 900 } });
test.setTimeout(180_000);

interface InboxEntry {
  readonly id: string;
  readonly type: string;
  readonly excerpt: string | null;
  readonly target: { readonly id: string } | null;
  readonly approval: { requestId: string; status: string } | null;
}

test("a named approver is asked, decides once in the Inbox, and the requester is told", async ({
  browser,
  page,
}) => {
  const unique = Date.now().toString(36);
  const approverContext = await browser.newContext();
  const approver = await approverContext.newPage();
  await signIn(approver, realUsers.admin);
  const approverWorkspace = await findWorkspace(approver, "Workspace 3");
  const me = await apiRead<{ userId: string }>(
    approver,
    `/workspaces/${approverWorkspace.id}/members/me`,
  );

  // The requester asks the admin to approve a new item, with a note.
  await signIn(page, realUsers.workspaceAdmin);
  const { workspace, board } = await openBoard(
    page,
    "Workspace 3",
    "Engineering Tasks",
  );
  const groupId = (
    await apiRead<{ lists: { id: string }[] }>(page, `/boards/${board.id}/full`)
  ).lists[0]!.id;
  const itemTitle = `Approve me ${unique}`;
  const item = await apiWrite<{ id: string }>(
    page,
    "POST",
    `/boards/${board.id}/items`,
    { groupId, title: itemTitle },
  );
  const itemId = item.data!.id;
  const asked = await apiWrite<{ id: string }>(
    page,
    "POST",
    `/board-items/${itemId}/approval-requests`,
    { approverUserId: me.userId, note: `Please look ${unique}` },
  );
  expect(asked.status).toBeLessThan(300);
  const requestId = asked.data!.id;

  // The same person is not asked twice about the same item while it is pending.
  expect(
    (
      await apiWrite(page, "POST", `/board-items/${itemId}/approval-requests`, {
        approverUserId: me.userId,
      })
    ).status,
  ).toBe(409);

  // The approver is told, sees it pending in Home and My work, and in the Inbox.
  const inbox = () =>
    apiRead<{ items: InboxEntry[] }>(
      approver,
      `/workspaces/${approverWorkspace.id}/inbox?limit=50`,
    );
  await expect
    .poll(
      async () =>
        (await inbox()).items.find(
          (entry) =>
            entry.type === "approval.requested" && entry.target?.id === itemId,
        )?.approval?.status,
      { timeout: 60_000 },
    )
    .toBe("Pending");
  const requested = (await inbox()).items.find(
    (entry) =>
      entry.type === "approval.requested" && entry.target?.id === itemId,
  )!;
  expect(requested.excerpt).toBe(`Please look ${unique}`);
  expect(requested.approval?.requestId).toBe(requestId);

  const home = await apiRead<{
    replies: { id: string; kind: string }[];
    summary: { approvalsPending: number };
  }>(approver, `/workspaces/${approverWorkspace.id}/home/feed`);
  expect(home.summary.approvalsPending).toBeGreaterThan(0);
  expect(
    home.replies.some((r) => r.kind === "approval" && r.id === requestId),
  ).toBe(true);
  const myWork = await apiRead<{
    items: {
      id: string;
      relations: string[];
      approval: { requestId: string } | null;
    }[];
  }>(approver, `/workspaces/${approverWorkspace.id}/my-work/extended`);
  expect(
    myWork.items.find((entry) => entry.id === itemId)?.relations,
  ).toContain("approver");
  expect(
    myWork.items.find((entry) => entry.id === itemId)?.approval?.requestId,
  ).toBe(requestId);

  // Someone who is not named may not decide it.
  const strangerContext = await browser.newContext();
  const stranger = await strangerContext.newPage();
  await signIn(stranger, realUsers.workspaceOwner);
  expect(
    (
      await apiWrite(
        stranger,
        "POST",
        `/workspaces/${workspace.id}/approvals/${requestId}/decision`,
        { decision: "approve" },
      )
    ).status,
  ).toBe(403);
  await strangerContext.close();

  // The approver approves from the Inbox reading pane.
  await approver.goto(`/workspaces/${approverWorkspace.id}/inbox`);
  await approver.getByText(itemTitle).first().click();
  const decided = approver.waitForResponse(
    (response) =>
      response.request().method() === "POST" &&
      response.url().endsWith(`/approvals/${requestId}/decision`),
  );
  await approver.getByRole("button", { name: "Approve", exact: true }).click();
  expect((await decided).status()).toBeLessThan(300);

  // A second decision is refused, and the entry no longer offers one.
  expect(
    (
      await apiWrite(
        approver,
        "POST",
        `/workspaces/${approverWorkspace.id}/approvals/${requestId}/decision`,
        { decision: "reject" },
      )
    ).status,
  ).toBe(409);
  await expect
    .poll(
      async () =>
        (await inbox()).items.find((entry) => entry.id === requested.id)
          ?.approval?.status,
    )
    .toBe("Approved");
  const homeAfter = await apiRead<{ replies: { id: string }[] }>(
    approver,
    `/workspaces/${approverWorkspace.id}/home/feed`,
  );
  expect(homeAfter.replies.some((r) => r.id === requestId)).toBe(false);

  // The requester is told it was approved.
  await expect
    .poll(
      async () =>
        (
          await apiRead<{ items: InboxEntry[] }>(
            page,
            `/workspaces/${workspace.id}/inbox?limit=50`,
          )
        ).items.some(
          (entry) =>
            entry.type === "approval.approved" && entry.target?.id === itemId,
        ),
      { timeout: 60_000 },
    )
    .toBe(true);
  await page.goto(`/workspaces/${workspace.id}/inbox`);
  await page.getByRole("tab", { name: /^All/ }).click();
  await expect(page.getByText(`approved ${itemTitle}`).first()).toBeVisible();
  await approverContext.close();
});

test("an approver can reject with a comment and the requester hears it", async ({
  browser,
  page,
}) => {
  const unique = Date.now().toString(36);
  const approverContext = await browser.newContext();
  const approver = await approverContext.newPage();
  await signIn(approver, realUsers.admin);
  const approverWorkspace = await findWorkspace(approver, "Workspace 3");
  const me = await apiRead<{ userId: string }>(
    approver,
    `/workspaces/${approverWorkspace.id}/members/me`,
  );

  await signIn(page, realUsers.workspaceAdmin);
  const { workspace, board } = await openBoard(
    page,
    "Workspace 3",
    "Engineering Tasks",
  );
  const groupId = (
    await apiRead<{ lists: { id: string }[] }>(page, `/boards/${board.id}/full`)
  ).lists[0]!.id;
  const itemId = (
    await apiWrite<{ id: string }>(page, "POST", `/boards/${board.id}/items`, {
      groupId,
      title: `Reject me ${unique}`,
    })
  ).data!.id;
  const requestId = (
    await apiWrite<{ id: string }>(
      page,
      "POST",
      `/board-items/${itemId}/approval-requests`,
      { approverUserId: me.userId },
    )
  ).data!.id;

  expect(
    (
      await apiWrite(
        approver,
        "POST",
        `/workspaces/${approverWorkspace.id}/approvals/${requestId}/decision`,
        { decision: "maybe" },
      )
    ).status,
  ).toBe(400);
  expect(
    (
      await apiWrite(
        approver,
        "POST",
        `/workspaces/${approverWorkspace.id}/approvals/${requestId}/decision`,
        { decision: "reject", comment: `Needs a budget ${unique}` },
      )
    ).status,
  ).toBeLessThan(300);

  await expect
    .poll(
      async () =>
        (
          await apiRead<{ items: InboxEntry[] }>(
            page,
            `/workspaces/${workspace.id}/inbox?limit=50`,
          )
        ).items.find(
          (entry) =>
            entry.type === "approval.rejected" && entry.target?.id === itemId,
        )?.excerpt,
      { timeout: 60_000 },
    )
    .toBe(`Needs a budget ${unique}`);
  await approverContext.close();
});

test("a requester asks a board member to approve from the card popup, the approver answers there, and the requester sees each answer", async ({
  browser,
  page,
}) => {
  const unique = Date.now().toString(36);
  const approverContext = await browser.newContext();
  const approver = await approverContext.newPage();
  await signIn(approver, realUsers.admin);
  const approverWorkspace = await findWorkspace(approver, "Workspace 3");
  const approverSelf = await apiRead<{ userId: string }>(
    approver,
    `/workspaces/${approverWorkspace.id}/members/me`,
  );

  await signIn(page, realUsers.workspaceAdmin);
  const { workspace, board } = await openBoard(
    page,
    "Workspace 3",
    "Engineering Tasks",
  );
  const members = await apiRead<{
    items: { userId: string; name: string }[];
  }>(page, `/workspaces/${workspace.id}/members`);
  const approverName = members.items.find(
    (member) => member.userId === approverSelf.userId,
  )!.name;
  const groupId = (
    await apiRead<{ lists: { id: string }[] }>(page, `/boards/${board.id}/full`)
  ).lists[0]!.id;
  const itemId = (
    await apiWrite<{ id: string }>(page, "POST", `/boards/${board.id}/items`, {
      groupId,
      title: `Ask from the popup ${unique}`,
    })
  ).data!.id;

  const popup = `/workspaces/${workspace.id}/boards/${board.id}?item=${itemId}`;
  await page.goto(popup);
  const approval = page.getByRole("region", { name: "Approval" });
  // Nothing was asked yet: the side column has no approval block.
  await expect(
    page.getByRole("button", { name: "Card actions" }),
  ).toBeVisible();
  await expect(approval).toHaveCount(0);

  // The card menu offers the board members, not the person who asks.
  const ask = async () => {
    await page.getByRole("button", { name: "Card actions" }).click();
    await page.getByRole("menuitem", { name: "Request approval" }).click();
  };
  await ask();
  await expect(
    page.getByRole("menuitem", { name: approverName }),
  ).toBeVisible();
  const sent = page.waitForResponse(
    (response) =>
      response.request().method() === "POST" &&
      response.url().includes(`/board-items/${itemId}/approval-requests`),
  );
  await page.getByRole("menuitem", { name: approverName }).click();
  expect((await sent).status()).toBeLessThan(300);
  await expect(approval.getByText(`Waiting for ${approverName}`)).toBeVisible();
  // The requester cannot decide their own request, only cancel it.
  await expect(approval.getByRole("button", { name: "Approve" })).toHaveCount(
    0,
  );
  await expect(
    approval.getByRole("button", { name: "Cancel request" }),
  ).toBeVisible();

  // Asking the same person again is refused with a notice, not a second request.
  await ask();
  await page.getByRole("menuitem", { name: approverName }).click();
  await expect(approval.getByRole("alert")).toHaveText(
    "That person was already asked about this item.",
  );
  await expect(approval.getByRole("listitem")).toHaveCount(1);

  // The approver opens the same popup, is asked to decide, and rejects with a reason.
  await approver.goto(
    `/workspaces/${approverWorkspace.id}/boards/${board.id}?item=${itemId}`,
  );
  const approverBlock = approver.getByRole("region", { name: "Approval" });
  await expect(approverBlock.getByText(/asked you to approve/)).toBeVisible();
  await approverBlock
    .getByRole("button", { name: "Reject", exact: true })
    .click();
  await approverBlock
    .getByRole("textbox", { name: "Reason for rejecting (optional)" })
    .fill(`Needs a budget ${unique}`);
  const rejected = approver.waitForResponse(
    (response) =>
      response.request().method() === "POST" &&
      response.url().endsWith("/decision"),
  );
  await approverBlock.getByRole("button", { name: "Reject request" }).click();
  expect((await rejected).status()).toBeLessThan(300);
  await expect(
    approverBlock.getByText(`Rejected by ${approverName}`),
  ).toBeVisible();
  await expect(
    approverBlock.getByRole("button", { name: "Approve" }),
  ).toHaveCount(0);

  // The requester reads the refusal and its reason, asks again, and the approver approves.
  await page.reload();
  await expect(approval.getByText(`Rejected by ${approverName}`)).toBeVisible();
  await expect(approval.getByText(`Needs a budget ${unique}`)).toBeVisible();
  await ask();
  await page.getByRole("menuitem", { name: approverName }).click();
  await expect(approval.getByText(`Waiting for ${approverName}`)).toBeVisible();
  await expect(approval.getByRole("listitem")).toHaveCount(2);

  await approver.reload();
  const decided = approver.waitForResponse(
    (response) =>
      response.request().method() === "POST" &&
      response.url().endsWith("/decision"),
  );
  await approver
    .getByRole("region", { name: "Approval" })
    .getByRole("button", { name: "Approve", exact: true })
    .click();
  expect((await decided).status()).toBeLessThan(300);
  await page.reload();
  await expect(approval.getByText(`Approved by ${approverName}`)).toBeVisible();
  await approverContext.close();
});

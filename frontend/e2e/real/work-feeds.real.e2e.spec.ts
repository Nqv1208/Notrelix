import { expect, test } from "@playwright/test";
import { findWorkspace, realUsers, signIn } from "./support/real-session";

test.use({ viewport: { width: 1440, height: 900 } });

test("Home, My work and the inbox badges load from the backend without an error", async ({
  page,
}) => {
  const statuses = new Map<string, number>();
  page.on("response", (response) => {
    if (response.request().method() !== "GET") return;
    const { pathname } = new URL(response.url());
    for (const suffix of [
      "/home/feed",
      "/my-work/extended",
      "/inbox/summary",
    ]) {
      if (pathname.endsWith(suffix)) statuses.set(suffix, response.status());
    }
  });

  await signIn(page, realUsers.workspaceAdmin);
  const workspace = await findWorkspace(page, "Workspace 3");

  statuses.clear();
  await page.goto(`/workspaces/${workspace.id}`);
  for (const suffix of ["/home/feed", "/inbox/summary"]) {
    await expect.poll(() => statuses.get(suffix)).toBe(200);
  }
  await expect(page.getByText(/couldn.t load/i)).toHaveCount(0);

  await page.goto(`/workspaces/${workspace.id}/my-work`);
  await expect.poll(() => statuses.get("/my-work/extended")).toBe(200);
  await expect(page.getByText(/couldn.t load/i)).toHaveCount(0);
});

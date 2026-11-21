import { expect, test, type Page } from "@playwright/test";
import { apiRead, apiWrite, realUsers, signIn } from "./support/real-session";
import { createOwnedWorkspace } from "./support/real-workspace";

test.setTimeout(300_000);

interface SpaceRead {
  readonly id: string;
  readonly name: string;
  readonly version: number;
}

const apiUrl = process.env.VITE_API_URL ?? "http://127.0.0.1:58000/api/v1";
const statusOf = async (page: Page, path: string) =>
  (await page.request.get(`${apiUrl}${path}`)).status();

test("the department page shows the live record read by id; an outsider, a missing id and another workspace's department all read as not found", async ({
  browser,
  page,
}) => {
  await signIn(page, realUsers.admin);
  const workspace = await createOwnedWorkspace(page, "Space read");
  const other = await createOwnedWorkspace(page, "Space read other");
  const w = `/workspaces/${workspace.id}`;
  const made = await apiWrite<string>(page, "POST", `${w}/spaces`, {
    name: "Secret",
    visibility: "Private",
    spaceType: "Department",
  });
  expect(made.status).toBe(201);
  const id = made.data as string;
  const foreign = await apiWrite<string>(
    page,
    "POST",
    `/workspaces/${other.id}/spaces`,
    { name: "Foreign", visibility: "Workspace", spaceType: "Department" },
  );
  expect(foreign.status).toBe(201);

  // Main flow: a rename done elsewhere shows on the page, which reads the Space by id.
  const before = await apiRead<SpaceRead>(page, `${w}/spaces/${id}`);
  const renamed = await apiWrite(page, "PUT", `${w}/spaces/${id}`, {
    name: "Secret renamed",
    expectedVersion: before.version,
  });
  expect(renamed.status).toBe(204);
  await page.goto(`${w}/spaces/${id}`);
  await expect(
    page.getByRole("heading", { level: 1, name: /^Secret renamed/ }),
  ).toBeVisible();

  // Rejections: an outsider (a private department), a missing id and another workspace's id are the same NotFound.
  const stranger = await browser.newContext();
  const outsider = await stranger.newPage();
  await signIn(outsider, realUsers.workspaceOwner);
  expect(await statusOf(outsider, `${w}/spaces/${id}`)).toBe(404);
  expect(
    await statusOf(page, `${w}/spaces/00000000-0000-4000-8000-000000000000`),
  ).toBe(404);
  expect(await statusOf(page, `${w}/spaces/${foreign.data as string}`)).toBe(
    404,
  );
  await page.goto(`${w}/spaces/${foreign.data as string}`);
  await expect(page.getByText("This department isn't available")).toBeVisible();
  await expect(page.getByRole("link", { name: "Go to Home" })).toBeVisible();
  await stranger.close();
});

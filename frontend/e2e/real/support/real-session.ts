import { expect, type Page } from "@playwright/test";

// The seeded development accounts (`InitDb`): the admin is a Member of
// Workspace 2, user5 is a Guest of it.
export const realUsers = {
  admin: {
    email: process.env.REAL_E2E_ADMIN_EMAIL ?? "admin@notrelix.com",
    password: process.env.REAL_E2E_ADMIN_PASSWORD ?? "Notrelix@123",
  },
  /** Admin of Workspace 3, so a manager of its boards. */
  workspaceAdmin: {
    email: process.env.REAL_E2E_WORKSPACE_ADMIN_EMAIL ?? "user4@notrelix.io",
    password: process.env.REAL_E2E_WORKSPACE_ADMIN_PASSWORD ?? "Notrelix@123",
  },
  /** Owner of Workspace 3: the only member who manages a page's permissions. */
  workspaceOwner: {
    email: process.env.REAL_E2E_WORKSPACE_OWNER_EMAIL ?? "member@notrelix.com",
    password: process.env.REAL_E2E_WORKSPACE_OWNER_PASSWORD ?? "Notrelix@123",
  },
  /** A Guest of Workspace 3. */
  workspaceGuest: {
    email: process.env.REAL_E2E_WORKSPACE_GUEST_EMAIL ?? "user6@notrelix.io",
    password: process.env.REAL_E2E_WORKSPACE_GUEST_PASSWORD ?? "Notrelix@123",
  },
  guest: {
    email: process.env.REAL_E2E_GUEST_EMAIL ?? "user5@notrelix.io",
    password: process.env.REAL_E2E_GUEST_PASSWORD ?? "Notrelix@123",
  },
} as const;

const apiUrl = process.env.VITE_API_URL ?? "http://127.0.0.1:58000/api/v1";

export async function signIn(
  page: Page,
  user: (typeof realUsers)[keyof typeof realUsers],
) {
  await page.goto("/sign-in");
  await page.waitForLoadState("networkidle");
  await page.getByLabel("Email").fill(user.email);
  await page.getByRole("button", { name: "Continue", exact: true }).click();
  await page.locator('input[name="password"]').fill(user.password);
  await page.getByRole("button", { name: "Sign in", exact: true }).click();
  await expect(page).toHaveURL(/\/workspaces\/[^/]+/);
}

interface Workspace {
  readonly id: string;
  readonly name: string;
}
interface Board {
  readonly id: string;
  readonly title: string;
}

async function readJson<T>(page: Page, path: string): Promise<T> {
  const response = await page.request.get(`${apiUrl}${path}`);
  expect(response.ok(), `GET ${path}`).toBe(true);
  return (await response.json()) as T;
}

/** The workspace the signed-in member sees under this name. */
export async function findWorkspace(page: Page, name: string) {
  const workspaces = await readJson<Workspace[]>(page, "/workspaces");
  const found = workspaces.find((workspace) => workspace.name === name);
  expect(found, `workspace ${name}`).toBeDefined();
  return found!;
}

/** Opens a board of the workspace by its title prefix (the boards are not listed in the shell yet). */
export async function openBoard(
  page: Page,
  workspaceName: string,
  titlePrefix: string,
) {
  const workspace = await findWorkspace(page, workspaceName);
  const boards = await readJson<Board[]>(
    page,
    `/workspaces/${workspace.id}/boards`,
  );
  const board = boards.find((candidate) =>
    candidate.title.startsWith(titlePrefix),
  );
  expect(board, `board ${titlePrefix}`).toBeDefined();
  await page.goto(`/workspaces/${workspace.id}/boards/${board!.id}`);
  await expect(
    page.getByRole("heading", { level: 1, name: board!.title }),
  ).toBeVisible();
  return { workspace, board: board! };
}

/**
 * Adds an item to a lane through the lane's composer and returns the create
 * response status. A created item then gets the lane's status; the item is
 * settled (real id and version) once that write is answered.
 */
export async function addItemToLane(page: Page, lane: string, title: string) {
  await page
    .getByRole("button", { name: `Add to ${lane}`, exact: true })
    .click();
  await page
    .getByRole("textbox", { name: `New item title in ${lane}` })
    .fill(title);
  const created = page.waitForResponse(
    (response) =>
      response.request().method() === "POST" &&
      /\/boards\/[^/]+\/items$/.test(response.url()),
  );
  const settled = page.waitForResponse(
    (response) =>
      response.request().method() === "PATCH" &&
      /\/board-items\/[^/]+\/status$/.test(response.url()),
  );
  await page.keyboard.press("Enter");
  const status = (await created).status();
  if (status < 300) await settled;
  return status;
}

/** The lane shows only its first items; opens the rest. */
export async function showAllInLane(page: Page, lane: string) {
  const more = page
    .getByRole("region", { name: lane, exact: true })
    .getByRole("button", { name: /^Show \d+ more items?$/ });
  if (await more.count()) await more.click();
}

/**
 * A write through the API as the signed-in member: the CSRF token the browser
 * client would send, an idempotency key and the JSON body. Specs use it to set a
 * scene up quickly; what the spec is about goes through the page.
 */
export async function apiWrite<T = unknown>(
  page: Page,
  method: "POST" | "PUT" | "PATCH" | "DELETE",
  path: string,
  body?: unknown,
): Promise<{ status: number; data: T | null }> {
  const csrf = (await (
    await page.request.get(`${apiUrl}/auth/csrf`)
  ).json()) as { token: string };
  const response = await page.request.fetch(`${apiUrl}${path}`, {
    method,
    headers: {
      "X-CSRF-Token": csrf.token,
      "Idempotency-Key": crypto.randomUUID(),
      "Content-Type": "application/json",
    },
    data: body === undefined ? undefined : JSON.stringify(body),
  });
  const text = await response.text();
  return {
    status: response.status(),
    data: text ? (JSON.parse(text) as T) : null,
  };
}

/** A read through the API as the signed-in member. */
export async function apiRead<T = unknown>(
  page: Page,
  path: string,
): Promise<T> {
  return readJson<T>(page, path);
}

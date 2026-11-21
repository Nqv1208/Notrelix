import { expect, test, type Page } from "@playwright/test";
import {
  apiRead,
  apiWrite,
  findWorkspace,
  realUsers,
  signIn,
} from "./support/real-session";

// Typing on the sidebar row: A holds a channel open, B types in the A and B direct conversation, and
// A's sidebar row for it swaps the preview for "typing" and the dots, then goes back when B stops.
test.use({ viewport: { width: 1440, height: 900 } });
test.setTimeout(180_000);

async function english(page: Page) {
  await page.addInitScript(() => {
    try {
      window.localStorage.setItem("notrelix-locale", "en");
    } catch {
      // Private mode: the default language applies.
    }
  });
}

test("a direct conversation row shows the dots while the other person types in it", async ({
  page: typer,
  browser,
}) => {
  const unique = Date.now().toString(36);
  await english(typer);
  await signIn(typer, realUsers.workspaceOwner);
  const ws = (await findWorkspace(typer, "Workspace 3")).id;
  const base = `/workspaces/${ws}`;

  const watcherContext = await browser.newContext({
    viewport: { width: 1440, height: 900 },
  });
  const watcher = await watcherContext.newPage();
  await english(watcher);
  await signIn(watcher, realUsers.workspaceAdmin);
  const watcherMe = await apiRead<{ userId: string }>(
    watcher,
    `/workspaces/${ws}/members/me`,
  );

  const channel = await apiWrite<{ id: string }>(
    typer,
    "POST",
    `/workspaces/${ws}/chat/channels`,
    { name: `st-${unique}`, isPrivate: false },
  );
  expect(channel.status).toBeLessThan(300);
  const joined = await apiWrite(
    watcher,
    "POST",
    `/workspaces/${ws}/chat/channels/${channel.data!.id}/members/me`,
  );
  expect(joined.status).toBeLessThan(300);
  const dm = await apiWrite<{ conversation: { id: string } }>(
    typer,
    "POST",
    `/workspaces/${ws}/chat/conversations`,
    { kind: "direct", userId: watcherMe.userId },
  );
  expect(dm.status).toBeLessThan(300);
  const dmId = dm.data!.conversation.id;

  // The watcher has the channel open, not the direct conversation.
  await watcher.goto(`${base}/chat/${channel.data!.id}`);
  const row = watcher
    .getByRole("navigation", { name: "Channels" })
    .getByRole("button", { name: /^Direct message: / });
  await expect(row).toBeVisible({ timeout: 30_000 });
  await expect(row).not.toContainText("typing");

  await typer.goto(`${base}/chat/dm/${dmId}`);
  const composer = typer.getByRole("combobox", { name: /^Message / });
  await expect(composer).toBeVisible({ timeout: 20_000 });
  await composer.click();
  await composer.pressSequentially("typing along", { delay: 120 });
  await expect(row).toContainText("typing", { timeout: 20_000 });
  await expect(row.locator(".chat-typing-wave")).toHaveCount(3);

  await composer.fill("");
  await expect(row).not.toContainText("typing", { timeout: 20_000 });
  await watcherContext.close();
});

import { expect, test } from "@playwright/test";
import { freshMember } from "./support/fresh-person";
import { apiRead, apiWrite } from "./support/real-session";

const apiUrl = process.env.VITE_API_URL ?? "http://127.0.0.1:58000/api/v1";

// A 1x1 PNG: recognised from its bytes, whatever the file is called.
const PNG = Buffer.from(
  "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR4nGP4z8DwHwAFAAH/q842iQAAAABJRU5ErkJggg==",
  "base64",
);

interface Me {
  id: string;
  name: string;
  avatarUrl: string | null;
}
interface Preferences {
  version: number;
  phone: string | null;
}

test.setTimeout(120_000);
test("a person renames themselves, keeps a stale preferences save from overwriting, and the photo is read only by them", async ({
  page,
  browser,
}) => {
  const member = await freshMember(page, "profile");
  await page.goto(`/workspaces/${member.workspaceId}/account/profile`);

  // Name: edit, save through the bar, see it in the session read.
  const name = page.getByLabel("Name", { exact: true });
  await name.fill("Casey Renamed");
  await page
    .getByRole("region", { name: "Unsaved changes" })
    .getByRole("button", { name: "Save changes" })
    .click();
  await expect
    .poll(async () => (await apiRead<Me>(page, "/auth/me")).name)
    .toBe("Casey Renamed");

  // The page saves name, preferences and job title in turn; wait for the last answer.
  await expect(page.getByText("Profile updated.")).toBeVisible();

  // Rejection: a blank name cannot be saved and the stored name stays.
  await name.fill("");
  await expect(
    page
      .getByRole("region", { name: "Unsaved changes" })
      .getByRole("button", { name: "Save changes" }),
  ).toBeDisabled();
  expect((await apiRead<Me>(page, "/auth/me")).name).toBe("Casey Renamed");
  await name.fill("Casey Renamed");

  // Preferences conflict: another device saves first, so this page's version is stale.
  const stored = await apiRead<Preferences>(page, "/profile/preferences");
  const other = await apiWrite(page, "PATCH", "/profile/preferences", {
    expectedVersion: stored.version,
    phone: "+84912345678",
  });
  expect(other.status).toBe(200);
  const afterOther = await apiRead<Preferences>(page, "/profile/preferences");
  expect(afterOther.version).toBeGreaterThan(stored.version);
  await page.getByLabel("Phone number").fill("+84988000111");
  await page.getByRole("button", { name: "Save changes" }).click();
  await expect(page.getByRole("alert")).toContainText(
    /changed on another device/,
  );
  const after = await apiRead<Preferences>(page, "/profile/preferences");
  expect(after.phone).toBe("+84912345678");
  expect(after.version).toBe(afterOther.version);

  // Photo: an SVG is refused, a PNG named .jpg is accepted by its bytes.
  await page.goto(`/workspaces/${member.workspaceId}/account/profile`);
  const input = page.locator('input[type="file"]');
  await input.setInputFiles({
    name: "logo.svg",
    mimeType: "image/svg+xml",
    buffer: Buffer.from("<svg xmlns='http://www.w3.org/2000/svg'/>"),
  });
  await expect(page.getByRole("alert")).toContainText(/PNG, JPEG or WebP/);
  expect((await apiRead<Me>(page, "/auth/me")).avatarUrl).toBeNull();
  await input.setInputFiles({
    name: "face.jpg",
    mimeType: "image/jpeg",
    buffer: PNG,
  });
  await expect(
    page.getByRole("button", { name: "Remove profile photo" }),
  ).toBeVisible();
  const me = await apiRead<Me>(page, "/auth/me");
  expect(me.avatarUrl).not.toBeNull();

  // Read: the person reads their own photo with the hardening headers.
  const own = await page.request.get(`${apiUrl}/users/${me.id}/avatar`);
  expect(own.status()).toBe(200);
  expect(own.headers()["x-content-type-options"]).toBe("nosniff");

  // Read: a stranger who shares no workspace gets the same 404 as "no photo".
  const strangerContext = await browser.newContext();
  const stranger = await strangerContext.newPage();
  await freshMember(stranger, "stranger");
  const denied = await stranger.request.get(`${apiUrl}/users/${me.id}/avatar`);
  const noPhoto = await stranger.request.get(
    `${apiUrl}/users/${(await apiRead<Me>(stranger, "/auth/me")).id}/avatar`,
  );
  expect(denied.status()).toBe(404);
  expect(noPhoto.status()).toBe(404);
  // The answers differ only in the ids and the trace of each request.
  const problem = async (response: typeof denied) => {
    const body = (await response.json()) as Record<string, string>;
    return JSON.stringify([
      body.type,
      body.title,
      body.status,
      body.detail,
    ]).replace(/[0-9a-f]{8}-[0-9a-f-]{27}/g, "<id>");
  };
  expect(await problem(denied)).toBe(await problem(noPhoto));
  await strangerContext.close();

  // Remove: asks first (keeping changes nothing), then the read answers 404.
  await page.getByRole("button", { name: "Remove profile photo" }).click();
  await page.getByRole("button", { name: "Keep it" }).click();
  expect((await apiRead<Me>(page, "/auth/me")).avatarUrl).not.toBeNull();
  await page.getByRole("button", { name: "Remove profile photo" }).click();
  await page.getByRole("button", { name: "Remove", exact: true }).click();
  await expect
    .poll(async () => (await apiRead<Me>(page, "/auth/me")).avatarUrl)
    .toBeNull();
  expect(
    (await page.request.get(`${apiUrl}/users/${me.id}/avatar`)).status(),
  ).toBe(404);
});

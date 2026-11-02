import { expect, test, type Browser, type Page } from "@playwright/test";
import { freshMember, turnOnTwoFactor } from "./support/fresh-person";
import { apiRead, apiWrite } from "./support/real-session";
import { totpCode } from "./support/totp";

const apiUrl = process.env.VITE_API_URL ?? "http://127.0.0.1:58000/api/v1";

/** Signs the person in up to the second step on a device of its own. */
async function toSecondStep(
  browser: Browser,
  person: { email: string; password: string },
) {
  const context = await browser.newContext();
  const page = await context.newPage();
  await page.goto("/sign-in");
  await page.waitForLoadState("networkidle");
  await page.getByLabel("Email").fill(person.email);
  const identified = page.waitForResponse((response) =>
    response.url().includes("/auth/identify"),
  );
  await page.getByRole("button", { name: "Continue", exact: true }).click();
  await identified;
  await page.locator("#auth-password").fill(person.password);
  await page.getByRole("button", { name: "Sign in", exact: true }).click();
  await expect(
    page.getByRole("heading", { name: "Two-step verification" }),
  ).toBeVisible();
  return { context, page };
}

async function typeDigits(page: Page, code: string) {
  // A wrong code empties the boxes once its answer lands: type into empty ones.
  await expect(page.getByLabel("Digit 6 of 6")).toHaveValue("");
  await page.getByLabel("Digit 1 of 6").click();
  await page.keyboard.type(code);
  const verify = page.getByRole("button", { name: "Verify", exact: true });
  try {
    await expect(verify, "six digits make the button ready").toBeEnabled({
      timeout: 5_000,
    });
  } catch (error) {
    await test.info().attach("second-step-page", {
      body: await page.screenshot(),
      contentType: "image/png",
    });
    console.log(
      "SECOND STEP PAGE:",
      (await page.locator("main").innerText()).replace(/\s+/g, " "),
    );
    throw error;
  }
  const answered = page.waitForResponse((response) =>
    response.url().includes("/auth/mfa/verify"),
  );
  await verify.click();
  return answered;
}

test.use({ actionTimeout: 15_000 });

test("the second step: a right code signs in, wrong codes are refused until the challenge ends, a recovery code works once and new codes void the old ones", async ({
  page,
  browser,
}) => {
  test.setTimeout(240_000);
  const member = await freshMember(page, "second");
  const { secret, backupCodes } = await turnOnTwoFactor(
    page,
    member.workspaceId,
    member.password,
  );

  // UC-IDN-H03: the status says it is on and how many recovery codes are left.
  const summary = await apiRead<{
    mfa: { isEnabled: boolean; recoveryCodesRemaining?: number };
  }>(page, "/auth/security/summary");
  expect(summary.mfa.isEnabled).toBe(true);
  await expect(
    page.getByText(new RegExp(`${backupCodes.length} backup codes? left`)),
  ).toBeVisible();

  // UC-IDN-I01: a wrong code is refused in words; a right one opens the session.
  const first = await toSecondStep(browser, member);
  await typeDigits(first.page, "000000");
  await expect(first.page.getByRole("alert")).toContainText(
    "That code is not right",
  );
  await typeDigits(first.page, totpCode(secret, Date.now() + 30_000));
  await expect(first.page).toHaveURL(/\/workspaces\/[^/]+/, {
    timeout: 20_000,
  });
  expect((await first.page.request.get(`${apiUrl}/auth/me`)).status()).toBe(
    200,
  );
  await first.context.close();

  // UC-IDN-I02: wrong codes end the challenge; the right code no longer helps.
  const second = await toSecondStep(browser, member);
  let ended = false;
  for (let attempt = 0; attempt < 10 && !ended; attempt += 1) {
    const answer = await typeDigits(second.page, "000000");
    expect(answer.status()).toBe(400);
    ended =
      (await answer.json()).errorCode === "identity.mfa.challenge-invalid";
  }
  expect(ended, "wrong codes end the challenge").toBe(true);
  await expect(second.page.getByRole("alert")).toContainText(
    "This verification expired",
  );
  await expect(
    second.page.getByRole("button", { name: "Verify", exact: true }),
  ).toBeDisabled();
  const afterEnd = await apiWrite(second.page, "POST", "/auth/mfa/verify", {
    code: totpCode(secret, Date.now() + 60_000),
  });
  expect(afterEnd.status, "a right code after the challenge ended").toBe(400);
  expect((await second.page.request.get(`${apiUrl}/auth/me`)).status()).toBe(
    401,
  );
  await second.context.close();

  // UC-IDN-J01: a recovery code signs in once.
  const [usable, spare] = backupCodes as [string, string];
  const third = await toSecondStep(browser, member);
  await third.page
    .getByRole("button", { name: "Use a backup code", exact: true })
    .click();
  await third.page.getByLabel("Backup code").fill(usable);
  await third.page.getByRole("button", { name: "Verify", exact: true }).click();
  await expect(third.page).toHaveURL(/\/workspaces\/[^/]+/, {
    timeout: 20_000,
  });
  await third.context.close();
  const fourth = await toSecondStep(browser, member);
  await fourth.page
    .getByRole("button", { name: "Use a backup code", exact: true })
    .click();
  await fourth.page.getByLabel("Backup code").fill(usable);
  await fourth.page
    .getByRole("button", { name: "Verify", exact: true })
    .click();
  await expect(fourth.page.getByRole("alert")).toBeVisible();
  await expect(fourth.page).toHaveURL(/\/sign-in/);
  await fourth.context.close();

  // UC-IDN-J02: new codes ask for a proof and void every old code.
  await page.goto(`/workspaces/${member.workspaceId}/account/security`);
  await page.getByRole("button", { name: "Create new codes" }).click();
  const proof = page.getByRole("region", {
    name: "Confirm to create new backup codes",
  });
  // A recovery code is a proof too (and avoids reusing a time step of the app).
  await proof.getByRole("textbox", { name: "Code" }).fill(backupCodes[2]!);
  await proof.getByRole("button", { name: "Confirm", exact: true }).click();
  const fresh = page.getByLabel("Save your backup codes");
  await expect(fresh).toBeVisible();
  const newCodes = (await fresh.innerText()).match(
    /\b[A-Z0-9]{4}(?:-[A-Z0-9]{4}){4}\b/g,
  )!;
  expect(newCodes).not.toContain(spare);
  await page.getByRole("button", { name: "I saved them" }).click();
  const fifth = await toSecondStep(browser, member);
  await fifth.page
    .getByRole("button", { name: "Use a backup code", exact: true })
    .click();
  await fifth.page.getByLabel("Backup code").fill(spare);
  await fifth.page.getByRole("button", { name: "Verify", exact: true }).click();
  await expect(fifth.page.getByRole("alert")).toBeVisible();
  await expect(fifth.page).toHaveURL(/\/sign-in/);
  await fifth.context.close();

  // Without a proof the route refuses.
  const noProof = await apiWrite(
    page,
    "POST",
    "/auth/mfa/recovery-codes/regenerate",
    { stepUpToken: "not-a-proof" },
  );
  expect(noProof.status).toBeGreaterThanOrEqual(400);
});

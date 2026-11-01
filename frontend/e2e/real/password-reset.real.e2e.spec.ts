import { randomUUID } from "node:crypto";
import { expect, test } from "@playwright/test";
import { resetCodeOf } from "./support/dev-mail";
import { registerPerson } from "./support/fresh-person";
import { apiWrite } from "./support/real-session";
import { anonymousApi, uniqueEmail } from "./support/real-workspace";

const apiUrl = process.env.VITE_API_URL ?? "http://127.0.0.1:58000/api/v1";

test("ten wrong passwords lock the account; the emailed code resets the password, ends every device and lifts the lock", async ({
  page,
  browser,
}) => {
  test.setTimeout(240_000);
  // Device A and device B hold sessions of a new person.
  const person = await registerPerson(page, "reset");
  const otherDevice = await browser.newContext();
  const deviceB = await otherDevice.newPage();
  const signedInB = await apiWrite(deviceB, "POST", "/auth/login", {
    email: person.email,
    password: person.password,
  });
  expect(signedInB.status).toBe(200);

  // UC-IDN-B09: ten wrong passwords lock the account, and even the right password is not checked.
  const visitor = await anonymousApi();
  for (let attempt = 1; attempt <= 10; attempt += 1) {
    const wrong = await visitor.post("/auth/login", {
      email: person.email,
      password: `Wrong-${attempt}-${randomUUID()}`,
    });
    expect(wrong.status, `wrong password ${attempt}`).toBeLessThan(500);
    expect(wrong.status).not.toBe(200);
  }
  const locked = await visitor.post("/auth/login", {
    email: person.email,
    password: person.password,
  });
  expect(locked.status, "the right password during the lock").toBe(429);

  // The page says so with the time left; it does not sign the person in.
  await page.context().clearCookies();
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
  await expect(page.getByText(/Try again in \d+ seconds/)).toBeVisible();
  await expect(page).toHaveURL(/\/sign-in/);

  // UC-IDN-B06: "Forgot your password?" asks for the email and says a code went out.
  const forgotAnswer = page.waitForResponse(
    (response) =>
      response.url().includes("/auth/forgot-password") &&
      response.request().method() === "POST",
  );
  await page.getByRole("button", { name: "Forgot your password?" }).click();
  await expect(page).toHaveURL(/\/forgot-password/);
  await expect(page.getByLabel("Email")).toHaveValue(person.email);
  await page
    .getByRole("button", { name: "Send the code", exact: true })
    .click();
  const known = await forgotAnswer;
  await expect(
    page.getByRole("heading", { name: "Check your email" }),
  ).toBeVisible();
  const resendButton = page.getByRole("button", { name: /Send again/ });
  await expect(resendButton).toBeDisabled();

  // The answer is the same for an address with no account, and the request limit is three an hour.
  const ghost = uniqueEmail("ghost");
  const unknown = await visitor.post("/auth/forgot-password", { email: ghost });
  expect(unknown.status).toBe(known.status());
  expect(unknown.data ?? null).toEqual(
    (await known.text()) ? await known.json() : null,
  );
  expect(
    (await visitor.post("/auth/forgot-password", { email: ghost })).status,
  ).toBe(known.status());
  expect(
    (await visitor.post("/auth/forgot-password", { email: ghost })).status,
  ).toBe(known.status());
  expect(
    (await visitor.post("/auth/forgot-password", { email: ghost })).status,
    "the fourth request in the hour",
  ).toBe(429);

  // A second request replaces the first code: only the newest works.
  const first = resetCodeOf(person.email);
  expect(
    (await visitor.post("/auth/forgot-password", { email: person.email }))
      .status,
  ).toBe(known.status());
  const newest = resetCodeOf(person.email);
  expect(newest).not.toBe(first);
  const newPassword = `Pw-${randomUUID()}`;
  const stale = await visitor.post("/auth/reset-password", {
    email: person.email,
    code: first,
    newPassword,
  });
  expect(stale.status, "the replaced code").toBe(400);

  // UC-IDN-B07: the reset screen takes the email, the code and the new password.
  await page.getByRole("button", { name: "Enter the code" }).click();
  await expect(page).toHaveURL(/\/reset-password\?email=/);
  await expect(page.getByLabel("Email", { exact: true })).toHaveValue(
    person.email,
  );
  await page.getByLabel("Code from the email").fill("111111");
  await page.getByLabel("New password").fill(newPassword);
  await page.getByRole("button", { name: "Change password" }).click();
  await expect(page.getByRole("alert")).toContainText(
    "That code is not right or has run out",
  );
  await page.getByLabel("Code from the email").fill(newest);
  await page.getByRole("button", { name: "Change password" }).click();
  await expect(page).toHaveURL(/\/sign-in\?reason=password-reset/);
  await expect(
    page.getByText(/Your password was changed and every device was signed out/),
  ).toBeVisible();

  // Every device signed out, the old password is gone, the code works once.
  expect((await deviceB.request.get(`${apiUrl}/auth/me`)).status()).toBe(401);
  expect(
    (
      await visitor.post("/auth/login", {
        email: person.email,
        password: person.password,
      })
    ).status,
  ).toBe(401);
  expect(
    (
      await visitor.post("/auth/reset-password", {
        email: person.email,
        code: newest,
        newPassword: `Pw-${randomUUID()}`,
      })
    ).status,
    "a code used once",
  ).toBe(400);

  // The reset lifted the account's lock: the new password signs in.
  await page.getByLabel("Email").fill(person.email);
  const identifiedAgain = page.waitForResponse((response) =>
    response.url().includes("/auth/identify"),
  );
  await page.getByRole("button", { name: "Continue", exact: true }).click();
  await identifiedAgain;
  await page.locator("#auth-password").fill(newPassword);
  await page.getByRole("button", { name: "Sign in", exact: true }).click();
  await expect(page).not.toHaveURL(/\/sign-in/, { timeout: 20_000 });
  expect((await page.request.get(`${apiUrl}/auth/me`)).status()).toBe(200);

  // Five wrong codes kill the code: the right one is refused after them.
  expect(
    (await visitor.post("/auth/forgot-password", { email: person.email }))
      .status,
  ).toBe(known.status());
  const third = resetCodeOf(person.email);
  for (let attempt = 0; attempt < 5; attempt += 1) {
    expect(
      (
        await visitor.post("/auth/reset-password", {
          email: person.email,
          code: "000000",
          newPassword: `Pw-${randomUUID()}`,
        })
      ).status,
    ).toBe(400);
  }
  const killed = await visitor.post<{ errorCode?: string }>(
    "/auth/reset-password",
    {
      email: person.email,
      code: third,
      newPassword: `Pw-${randomUUID()}`,
    },
  );
  expect(killed.status).not.toBe(200);
  expect(killed.data?.errorCode).toBe("identity.auth.too-many-attempts");

  await visitor.dispose();
  await otherDevice.close();
});

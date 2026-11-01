import { expect, test } from "@playwright/test";
import { realUsers, signIn } from "./support/real-session";

/**
 * UC-IDN-D01: a dropped connection during the silent refresh keeps the session. The access cookie
 * is gone (as when it ran out), the refresh route is cut off, and the page must not go to sign-in;
 * it says the session cannot be read and offers a retry. With the network back the same session
 * opens, because only the server's 401 on the refresh ends one.
 */
test("a network drop during the refresh keeps the session and the retry opens it", async ({
  page,
}) => {
  await signIn(page, realUsers.admin);
  const at = new URL(page.url()).pathname;

  const cookies = await page.context().cookies();
  const access = cookies.find((cookie) => /access/i.test(cookie.name));
  expect(access, "the access cookie").toBeDefined();
  await page.context().clearCookies({ name: access!.name });

  await page.route(/\/auth\/refresh/, (route) => route.abort("failed"));
  await page.reload();
  await expect(
    page.getByRole("alert").getByRole("button", { name: /retry|try again/i }),
  ).toBeVisible({ timeout: 20_000 });
  await expect(page).not.toHaveURL(/\/sign-in/);
  const stillThere = (await page.context().cookies()).find((cookie) =>
    /refresh/i.test(cookie.name),
  );
  expect(stillThere, "the refresh credential is kept").toBeDefined();

  await page.unroute(/\/auth\/refresh/);
  await page
    .getByRole("alert")
    .getByRole("button", { name: /retry|try again/i })
    .click();
  await expect(page).toHaveURL(new RegExp(at.replace(/\//g, "\\/")), {
    timeout: 20_000,
  });
  await expect(page).not.toHaveURL(/\/sign-in/);
});

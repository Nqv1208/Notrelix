import { expect, test } from "@playwright/test";
import { CONFIRMATION_LINK, devMails } from "./support/dev-mail";
import { registerPerson } from "./support/fresh-person";
import { apiWrite } from "./support/real-session";
import { anonymousApi, uniqueEmail } from "./support/real-workspace";

const tokenOf = (link: string) =>
  decodeURIComponent(new URL(link).hash.replace("#token=", ""));

test("asking for the confirmation link again waits before the next ask; only the newest link confirms; the answer does not tell a known address from an unknown one", async ({
  page,
}) => {
  // The first test after a cold stack start waits on the development log for each mail.
  test.setTimeout(90_000);
  const person = await registerPerson(page, "resend");
  const first = (await devMails(person.email, CONFIRMATION_LINK)).at(-1)!;

  // UC-IDN-B05: the onboarding notice offers "Send the link again", says it went out and waits.
  await page.goto("/onboarding");
  await page.getByLabel("Company name").fill(`Resend ${Date.now()}`);
  await page.getByRole("button", { name: "Continue", exact: true }).click();
  await expect(page.getByRole("alert")).toContainText("Confirm your email");
  // On a phone the link is a 44 px target and the header does not squeeze (UC-IDN-B05).
  await page.setViewportSize({ width: 390, height: 844 });
  const again = page.getByRole("button", { name: "Send the link again" });
  expect((await again.boundingBox())!.height).toBeGreaterThanOrEqual(44);
  expect(
    await page.evaluate(
      () => document.documentElement.scrollWidth - window.innerWidth,
    ),
  ).toBeLessThanOrEqual(0);
  const header = await page.locator("header").first().boundingBox();
  expect(header!.height, "header stays one line").toBeLessThanOrEqual(57);
  await page.setViewportSize({ width: 1280, height: 720 });
  await again.click();
  await expect(page.getByText(/A new link is on its way/)).toBeVisible();
  await expect(
    page.getByRole("button", { name: /Send again in \d:\d\d/ }),
  ).toBeDisabled();
  const second = (await devMails(person.email, CONFIRMATION_LINK, 1)).at(-1)!;
  expect(second).not.toBe(first);

  // A third ask from the visitor's resend route replaces the second.
  const visitor = await anonymousApi();
  const known = await visitor.post("/auth/email-verification/resend", {
    email: person.email,
  });
  const unknown = await visitor.post("/auth/email-verification/resend", {
    email: uniqueEmail("nobody"),
  });
  expect(known.status).toBe(unknown.status);
  expect(known.data ?? null).toEqual(unknown.data ?? null);
  const third = (await devMails(person.email, CONFIRMATION_LINK, 2)).at(-1)!;
  expect(third).not.toBe(second);

  // Only the newest link confirms; an older one fails with the same refusal as a spent link.
  const older = await visitor.post("/auth/email-verification/confirm", {
    token: tokenOf(first),
  });
  const older2 = await visitor.post("/auth/email-verification/confirm", {
    token: tokenOf(second),
  });
  expect(older.status).toBe(400);
  expect(older2.status).toBe(400);
  const newest = await apiWrite(
    page,
    "POST",
    "/auth/email-verification/confirm",
    { token: tokenOf(third) },
  );
  expect(newest.status).toBe(200);

  // Once confirmed, a resend sends nothing and still answers the same.
  const confirmed = await visitor.post("/auth/email-verification/resend", {
    email: person.email,
  });
  expect(confirmed.status).toBe(known.status);
  await visitor.dispose();
});

import { execFileSync } from "node:child_process";
import { expect, test } from "@playwright/test";
import { apiWrite } from "./support/real-session";
import { registerPerson } from "./support/fresh-person";

/**
 * UC-IDN-D05: a sign-in chain ends 90 days after it began, however often it refreshed. No clock can be moved on
 * a running stack, so the development database stands in for the passing days: it moves the session's creation back and its expiry with it, as the 90-day cap would have set it.
 */
function sql(statement: string) {
  execFileSync("docker", [
    "exec",
    "notrelix-fe-real-e2e-postgres",
    "psql",
    "-U",
    "postgres",
    "-d",
    "notrelix_real_e2e",
    "-v",
    "ON_ERROR_STOP=1",
    "-c",
    statement,
  ]);
}

const age = (email: string, days: number) =>
  sql(
    `update identity.user_sessions set created_at = now() - interval '${days} days', expires_at = now() - interval '${days} days' + interval '90 days' where user_id = (select id from identity.users where email = '${email}')`,
  );

test("a chain of 91 days is refused at the refresh and one of 89 days is kept", async ({
  page,
}) => {
  const person = await registerPerson(page, "ninety-days");

  // 89 days after sign-in the refresh still works and the answer is a session.
  age(person.email, 89);
  expect((await apiWrite(page, "POST", "/auth/refresh")).status).toBe(200);

  // 91 days after sign-in the refresh is refused with the generic 401, although the idle window is not over.
  age(person.email, 91);
  const refused = await apiWrite(page, "POST", "/auth/refresh");
  expect(refused.status).toBe(401);
});

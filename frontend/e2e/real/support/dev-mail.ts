import { execFileSync } from "node:child_process";

const backendContainer = "notrelix-fe-real-e2e-backend";

/** The development backend logs the recipient masked (`c***5@example.com`), same rule as `NoopEmailService`. */
export function maskedRecipient(email: string): string {
  const at = email.indexOf("@");
  if (at <= 1 || at === email.length - 1) return "***";
  return `${email[0]}***${email.slice(at - 1)}`;
}

function escapeRegExp(text: string): string {
  return text.replace(/[.*+?^${}()|[\]\\]/g, "\\$&");
}

/**
 * No mail provider runs on a developer machine: the development backend logs
 * the message text. Waits until more than `seen` messages to the address match
 * `body` (the capture group of each match is returned, oldest first).
 */
export async function devMails(
  email: string,
  body: string,
  seen = 0,
  /** How far back the backend log is read; a spec that mails many people in one run narrows it. */
  since = "20m",
): Promise<string[]> {
  const pattern = new RegExp(
    `Development email to ${escapeRegExp(maskedRecipient(email))}: [\\s\\S]*?${body}`,
    "g",
  );
  for (let attempt = 0; attempt < 40; attempt += 1) {
    const logs = execFileSync(
      "docker",
      ["logs", "--since", since, backendContainer],
      {
        encoding: "utf8",
        maxBuffer: 256 * 1024 * 1024,
      },
    );
    // One log entry per message: cut the log at each entry start so a match never spans two mails.
    const found = logs
      .split(/(?=^.*Development email to )/m)
      .map((entry) => new RegExp(pattern.source).exec(entry)?.[1])
      .filter((value): value is string => value !== undefined);
    if (found.length > seen) return found;
    await new Promise((resolve) => setTimeout(resolve, 1000));
  }
  throw new Error(`no matching email was logged for ${email}`);
}

/**
 * The newest message matching `body` for each address, from one read of the backend log (a spec that mails many people
 * reads the log once instead of once per person; each read is slow while the backend logs every query).
 */
export async function latestMails(
  emails: readonly string[],
  body: string,
  since = "10m",
): Promise<Map<string, string>> {
  const found = new Map<string, string>();
  for (let attempt = 0; attempt < 40; attempt += 1) {
    const logs = execFileSync(
      "docker",
      ["logs", "--since", since, backendContainer],
      { encoding: "utf8", maxBuffer: 512 * 1024 * 1024 },
    );
    const entries = logs.split(/(?=^.*Development email to )/m);
    for (const email of emails) {
      const pattern = new RegExp(
        `Development email to ${escapeRegExp(maskedRecipient(email))}: [\\s\\S]*?${body}`,
      );
      const hit = entries
        .map((entry) => pattern.exec(entry)?.[1])
        .filter((value): value is string => value !== undefined)
        .at(-1);
      if (hit) found.set(email, hit);
    }
    if (found.size === emails.length) return found;
    await new Promise((resolve) => setTimeout(resolve, 1000));
  }
  throw new Error("not every address was mailed");
}

export const CONFIRMATION_LINK =
  '(http[^\\s"<>]*/verify-email#token=[^\\s"<>]+)';

const redisContainer = "notrelix-fe-real-e2e-redis";

/**
 * The password-reset code is mailed through the legacy sender, whose development log line
 * carries no text, so the spec reads the code where the backend keeps it (the real-run
 * Redis, under the key `OtpService` writes). Only the newest code exists there.
 */
export function resetCodeOf(email: string): string {
  const connection = execFileSync(
    "docker",
    ["exec", backendContainer, "printenv", "ConnectionStrings__Redis"],
    { encoding: "utf8" },
  );
  const password = /password=([^,\s]+)/.exec(connection)?.[1] ?? "";
  const code = execFileSync(
    "docker",
    [
      "exec",
      "-e",
      `REDISCLI_AUTH=${password}`,
      redisContainer,
      "redis-cli",
      "--no-auth-warning",
      "GET",
      `Notrelix_otp:forgot-password:${email.trim().toLowerCase()}`,
    ],
    { encoding: "utf8" },
  ).trim();
  if (!/^\d{6}$/.test(code)) throw new Error(`no reset code held for ${email}`);
  return code;
}

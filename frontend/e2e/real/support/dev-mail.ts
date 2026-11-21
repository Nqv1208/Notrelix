import { execFileSync } from "node:child_process";

const backendContainer = "notrelix-fe-real-e2e-backend";

/**
 * The real-run stack sends mail over SMTP to a Mailpit container that shares the backend's network
 * namespace, and publishes its HTTP API on this address. The backend never logs a recipient, so the
 * specs read the sink instead of the log.
 */
const mailApi = process.env.REAL_E2E_MAIL_URL ?? "http://127.0.0.1:58025";

interface MailpitSummary {
  ID: string;
}
interface MailpitMessage {
  Text?: string;
  HTML?: string;
}

async function mailApiJson<T>(path: string): Promise<T> {
  const response = await fetch(`${mailApi}${path}`);
  if (!response.ok) throw new Error(`mail sink ${path}: ${response.status}`);
  return (await response.json()) as T;
}

/** The text and html of every message sent to the address, oldest first. */
async function bodiesTo(email: string): Promise<string[]> {
  const found = await mailApiJson<{ messages: MailpitSummary[] }>(
    `/api/v1/search?limit=200&query=${encodeURIComponent(`to:"${email}"`)}`,
  );
  const bodies: string[] = [];
  for (const { ID } of [...found.messages].reverse()) {
    const message = await mailApiJson<MailpitMessage>(`/api/v1/message/${ID}`);
    bodies.push(`${message.Text ?? ""}\n${message.HTML ?? ""}`);
  }
  return bodies;
}

/**
 * Waits until more than `seen` messages to the address match `body` (the capture group of each match
 * is returned, oldest first). `since` is kept for older callers; the sink holds this run only.
 */
export async function devMails(
  email: string,
  body: string,
  seen = 0,
  _since = "20m",
): Promise<string[]> {
  for (let attempt = 0; attempt < 40; attempt += 1) {
    const found = (await bodiesTo(email))
      .map((text) => new RegExp(body).exec(text)?.[1])
      .filter((value): value is string => value !== undefined);
    if (found.length > seen) return found;
    await new Promise((resolve) => setTimeout(resolve, 1000));
  }
  const held = await mailApiJson<{
    messages: { To: { Address: string }[]; Subject: string }[];
  }>("/api/v1/messages?limit=50");
  throw new Error(
    `no matching email reached the mail sink for ${email}; it holds ${held.messages
      .map((m) => `${m.To.map((t) => t.Address).join(",")} "${m.Subject}"`)
      .join(" | ")}`,
  );
}

/** The newest message matching `body` for each address (a spec that mails many people reads once per person). */
export async function latestMails(
  emails: readonly string[],
  body: string,
  _since = "10m",
): Promise<Map<string, string>> {
  const found = new Map<string, string>();
  for (const email of emails) {
    found.set(email, (await devMails(email, body)).at(-1)!);
  }
  return found;
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

import { headers } from "next/headers";

const origin = (value: string | undefined): string | undefined => {
  try {
    const url = new URL(value ?? "");
    return url.protocol === "https:" || url.protocol === "http:"
      ? url.origin
      : undefined;
  } catch {
    return undefined;
  }
};

// Pure so it is testable. Only SITE_URL, or a Host header present in
// ALLOWED_HOSTS, may become the origin; x-forwarded-* is never trusted.
export function resolveSiteOrigin(
  env: { SITE_URL?: string; ALLOWED_HOSTS?: string },
  host: string | null,
): string | undefined {
  const configured = origin(env.SITE_URL);
  const requested = host?.trim().toLowerCase();
  const allowed = (env.ALLOWED_HOSTS ?? "")
    .split(",")
    .map((h) => h.trim().toLowerCase())
    .filter(Boolean);
  if (requested && allowed.includes(requested)) {
    // Scheme comes from SITE_URL (http|https only), defaulting to https.
    const proto = configured ? new URL(configured).protocol : "https:";
    return origin(`${proto}//${requested}`) ?? configured;
  }
  return configured;
}

// Read at request time (no NEXT_PUBLIC) so the promoted image stays env-agnostic.
export async function getSiteOrigin(): Promise<string | undefined> {
  const h = await headers();
  const { SITE_URL, ALLOWED_HOSTS } = process.env;
  return resolveSiteOrigin({ SITE_URL, ALLOWED_HOSTS }, h.get("host"));
}

import { headers } from "next/headers";

// The release image is built once and promoted unchanged, so the public origin
// cannot be baked at build time; read it from the request instead.
export async function getSiteOrigin(): Promise<string> {
  const h = await headers();
  const first = (v: string | null) => v?.split(",")[0]?.trim();
  const host = first(h.get("x-forwarded-host")) ?? first(h.get("host"));
  const proto = first(h.get("x-forwarded-proto")) ?? "http";
  return `${proto}://${host ?? "localhost"}`;
}

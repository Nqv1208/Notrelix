import { createHmac } from "node:crypto";

const ALPHABET = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

/** The 6-digit authenticator code (RFC 6238, SHA-1, 30 s) of a base32 secret at a moment. */
export function totpCode(secret: string, at: number = Date.now()): string {
  let bits = "";
  for (const char of secret.replace(/=+$/, "").toUpperCase()) {
    bits += ALPHABET.indexOf(char).toString(2).padStart(5, "0");
  }
  const key = Buffer.from(
    (bits.match(/.{8}/g) ?? []).map((byte) => parseInt(byte, 2)),
  );
  const counter = Buffer.alloc(8);
  counter.writeBigUInt64BE(BigInt(Math.floor(at / 1000 / 30)));
  const hmac = createHmac("sha1", key).update(counter).digest();
  const offset = hmac[hmac.length - 1]! & 0x0f;
  const value =
    ((hmac[offset]! & 0x7f) << 24) |
    (hmac[offset + 1]! << 16) |
    (hmac[offset + 2]! << 8) |
    hmac[offset + 3]!;
  return String(value % 1_000_000).padStart(6, "0");
}
